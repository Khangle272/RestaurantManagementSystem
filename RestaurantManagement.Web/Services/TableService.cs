using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Services;

// Shared by the reservation queue and floor board so both enforce the same lifecycle.
public class TableService(RestaurantDbContext db, PreorderService preorders, IKhoService khoService)
{
    public const int MaxDiningHours = 3;
    public static DateTimeOffset ServingDeadline(DatBan booking) => (booking.ThoiDiemNhanBan ?? booking.GioDen).AddHours(MaxDiningHours);
    public static DateTimeOffset VietnamTime(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), TimeSpan.FromHours(7));
    public static DateTime VietnamNow => DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime;
    public static string NewCode(string prefix) => prefix + "-" + Guid.NewGuid().ToString("N")[..20].ToUpperInvariant();

    public static async Task<string> NewBookingCode(RestaurantDbContext db)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var code = "DB-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            if (!await db.DatBan.AnyAsync(x => x.MaDatBan == code)) return code;
        }
        throw new InvalidOperationException("Không cấp được mã đặt bàn. Vui lòng thử lại.");
    }

    // Caller owns the serializable transaction; reserve before asking the customer for money.
    public async Task<string?> ReserveOnlineWithinTransaction(DatBan booking)
    {
        var now = DateTimeOffset.UtcNow;
        var candidates = await db.BanAn.Where(x => x.KhuVuc.DangSuDung && x.TrangThai != TrangThaiBan.NgungSuDung
            && (!booking.KhuVucUuTienId.HasValue || x.KhuVucId == booking.KhuVucUuTienId)
            && (!booking.YeuCauVip || x.KhuVuc.LaPhongVip)).Select(x => x.Id).ToListAsync();
        var tables = await LockTables(candidates);
        var busy = await db.ChiTietDatBan.Where(x => candidates.Contains(x.BanAnId) && x.DatBanId != booking.Id
            && (x.DatBan.TrangThai == TrangThaiDatBan.DaXacNhan
                || (x.DatBan.TrangThai == TrangThaiDatBan.ChoCoc && (!x.DatBan.CocTuDong
                    || x.DatBan.HanThanhToanCoc > now || x.DatBan.ThoiDiemBaoChuyenKhoan != null
                    || x.DatBan.TienCocDaNop > x.DatBan.TienCocDaHoan + x.DatBan.TienCocDaGiu))
                || (x.DatBan.TrangThai == TrangThaiDatBan.DaNhanBan && x.DatBan.ThoiDiemKetThuc == null))
            && x.DatBan.GioDen < booking.GioKetThucDuKien && x.DatBan.GioKetThucDuKien > booking.GioDen)
            .Select(x => x.BanAnId).Distinct().ToListAsync();
        var available = tables.Where(x => !busy.Contains(x.Id)
            && (booking.GioDen > now.AddMinutes(30) || x.TrangThai == TrangThaiBan.SanSang));
        var guests = booking.SoNguoiLon + booking.SoTreEm;
        var single = available.Where(x => x.SoChoNgoi >= guests).OrderBy(x => x.SoChoNgoi).ThenBy(x => x.Id).FirstOrDefault();
        List<BanAn>? chosen = single is null ? null : [single];
        if (chosen is null)
            foreach (var group in available.GroupBy(x => x.KhuVucId).OrderBy(x => x.Key))
            {
                var selection = new List<BanAn>();
                foreach (var table in group.OrderByDescending(x => x.SoChoNgoi).ThenBy(x => x.Id))
                {
                    selection.Add(table);
                    if (selection.Sum(x => x.SoChoNgoi) >= guests) break;
                }
                if (selection.Count <= 20 && selection.Sum(x => x.SoChoNgoi) >= guests) { chosen = selection; break; }
            }
        if (chosen is null) return "Khung giờ/khu vực này không còn đủ bàn phù hợp. Chọn giờ hoặc khu vực khác; bạn chưa phải thanh toán.";
        var chosenIds = chosen.Select(x => x.Id).ToArray();
        foreach (var link in booking.Ban.Where(x => !chosenIds.Contains(x.BanAnId)).ToList())
        { db.ChiTietDatBan.Remove(link); booking.Ban.Remove(link); }
        foreach (var table in chosen.Where(x => !booking.Ban.Any(b => b.BanAnId == x.Id)))
            booking.Ban.Add(new ChiTietDatBan { BanAnId = table.Id });
        return null;
    }

    private async Task<List<BanAn>> LockTables(IEnumerable<int> ids)
    {
        var tables = new List<BanAn>();
        foreach (var id in ids.Distinct().Order())
        {
            var table = await db.BanAn.FromSqlInterpolated($"SELECT * FROM [BanAn] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id}")
                .Include(x => x.KhuVuc).SingleOrDefaultAsync();
            if (table is not null) tables.Add(table);
        }
        return tables;
    }

    private bool SameVersion(object entity, string? version) => !string.IsNullOrEmpty(version)
        && Convert.ToBase64String((byte[])db.Entry(entity).Property("RowVersion").CurrentValue!) == version;

    private async Task<string?> Run(Func<Task<string?>> action)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var error = await action();
            if (error is not null) return error;
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return null;
        }
        catch (DbUpdateException) { return "Dữ liệu vừa thay đổi. Vui lòng tải lại sơ đồ và thử lại."; }
        catch (SqlException ex) when (ex.Number is 1205 or 1222)
        { return "Bàn đang được người khác xử lý. Vui lòng tải lại và thử lại."; }
    }

    public Task<string?> Assign(int id, int[] ids, string? version, int? staffId) => Run(async () =>
    {
        ids = ids.Distinct().ToArray();
        if (ids.Length is 0 or > 20) return "Chọn từ 1 đến 20 bàn để xếp cho khách.";
        var tables = await LockTables(ids);
        var booking = await db.DatBan.Include(x => x.Ban).SingleOrDefaultAsync(x => x.Id == id);
        if (booking is null || !SameVersion(booking, version)) return "Yêu cầu đã thay đổi. Vui lòng mở lại.";
        if (ReservationDepositPolicy.Expired(booking)) return "Lịch đã hết hạn giữ chỗ chờ thanh toán. Khách cần tạo lịch mới.";
        if (booking.TrangThai is not (TrangThaiDatBan.ChoXacNhan or TrangThaiDatBan.ChoCoc or TrangThaiDatBan.DaXacNhan))
            return "Chỉ xếp bàn cho yêu cầu chờ xác nhận, chờ cọc hoặc đã xác nhận.";
        if (booking.GioKetThucDuKien <= DateTimeOffset.UtcNow) return "Yêu cầu đã quá giờ kết thúc dự kiến.";
        if (tables.Count != ids.Length || tables.Any(x => !x.KhuVuc.DangSuDung || x.TrangThai == TrangThaiBan.NgungSuDung))
            return "Có bàn hoặc khu vực đã ngừng sử dụng.";
        if (tables.Select(x => x.KhuVucId).Distinct().Count() != 1) return "Bàn ghép phải thuộc cùng một khu vực.";
        if (tables.Sum(x => x.SoChoNgoi) < booking.SoNguoiLon + booking.SoTreEm) return "Các bàn đã chọn không đủ chỗ cho khách.";
        if (booking.YeuCauVip && tables.Any(x => !x.KhuVuc.LaPhongVip)) return "Yêu cầu phòng VIP cần được xếp vào khu vực VIP.";
        if (await db.ChiTietDatBan.AnyAsync(x => ids.Contains(x.BanAnId) && x.DatBanId != id
            && (x.DatBan.TrangThai == TrangThaiDatBan.DaXacNhan || (x.DatBan.TrangThai == TrangThaiDatBan.ChoCoc
                && (!x.DatBan.CocTuDong || x.DatBan.HanThanhToanCoc > DateTimeOffset.UtcNow || x.DatBan.ThoiDiemBaoChuyenKhoan != null
                    || x.DatBan.TienCocDaNop > x.DatBan.TienCocDaHoan + x.DatBan.TienCocDaGiu))
                || (x.DatBan.TrangThai == TrangThaiDatBan.DaNhanBan && x.DatBan.ThoiDiemKetThuc == null))
            && x.DatBan.GioDen < booking.GioKetThucDuKien && x.DatBan.GioKetThucDuKien > booking.GioDen))
            return "Bàn đã có lịch trùng khoảng thời gian này. Chọn bàn khác hoặc đổi lịch với khách.";
        var removed = booking.Ban.Where(x => !ids.Contains(x.BanAnId)).ToList();
        db.ChiTietDatBan.RemoveRange(removed);
        foreach (var tableId in ids.Where(x => !booking.Ban.Any(b => b.BanAnId == x)))
            booking.Ban.Add(new ChiTietDatBan { BanAnId = tableId });
        booking.YeuCauCoc |= booking.SoNguoiLon + booking.SoTreEm >= 8 || booking.YeuCauVip || booking.YeuCauTrangTri;
        booking.TrangThai = booking.YeuCauCoc && (booking.TienCocYeuCau <= 0
            || booking.TienCocDaNop - booking.TienCocDaHoan - booking.TienCocDaGiu < booking.TienCocYeuCau)
            ? TrangThaiDatBan.ChoCoc : TrangThaiDatBan.DaXacNhan;
        booking.NhanVienTiepNhanId = staffId;
        return null;
    });

    public Task<string?> CheckIn(int id, string? version, int? staffId) => Run(async () =>
    {
        var ids = await db.ChiTietDatBan.Where(x => x.DatBanId == id).Select(x => x.BanAnId).ToArrayAsync();
        var tables = await LockTables(ids);
        var booking = await db.DatBan.SingleOrDefaultAsync(x => x.Id == id);
        if (booking is null || !SameVersion(booking, version)) return "Yêu cầu đã thay đổi. Vui lòng mở lại.";
        if (booking.TrangThai != TrangThaiDatBan.DaXacNhan || tables.Count == 0) return "Chỉ nhận khách đã được xác nhận và xếp bàn.";
        if (booking.YeuCauHuy is not null) return "Khách đang yêu cầu hủy. Xử lý yêu cầu trước khi nhận bàn.";
        if (booking.YeuCauCoc && (booking.TienCocYeuCau <= 0
            || booking.TienCocDaNop - booking.TienCocDaHoan - booking.TienCocDaGiu < booking.TienCocYeuCau))
            return "Chưa nhận đủ cọc đã thỏa thuận; đối chiếu cọc trước khi nhận khách.";
        if (booking.GioDen > DateTimeOffset.UtcNow.AddMinutes(30) || booking.GioKetThucDuKien <= DateTimeOffset.UtcNow)
            return "Chưa đến giờ nhận bàn hoặc lịch đã hết hạn. Điều chỉnh lịch trước khi nhận khách.";
        if (tables.Any(x => !x.KhuVuc.DangSuDung || x.TrangThai != TrangThaiBan.SanSang)) return "Bàn chưa sẵn sàng; hãy dọn bàn hoặc xếp bàn khác.";
        booking.TrangThai = TrangThaiDatBan.DaNhanBan;
        booking.ThoiDiemNhanBan = DateTimeOffset.UtcNow;
        booking.NhanVienTiepNhanId = staffId;
        foreach (var table in tables) table.TrangThai = TrangThaiBan.DangPhucVu;
        // Release customer drafts once, inside the same transaction as check-in.
        if (staffId is int employeeId)
        {
            var releaseError = await preorders.ReleaseWithinTransaction(booking, employeeId);
            if (releaseError is not null) return releaseError;
            if (!db.HoaDon.Local.Any(x => x.DatBanId == id && x.TrangThai != TrangThaiHoaDon.DaHuy)
                && !await db.HoaDon.AnyAsync(x => x.DatBanId == id && x.TrangThai != TrangThaiHoaDon.DaHuy))
                db.HoaDon.Add(new HoaDon { MaHoaDon = NewCode("HD"), DatBanId = id, KhachHangId = booking.KhachHangId,
                    NhanVienId = employeeId, ThoiDiemLap = DateTimeOffset.UtcNow, TrangThai = TrangThaiHoaDon.ChuaThanhToan });

            await db.SaveChangesAsync();

            var bill = db.HoaDon.Local.FirstOrDefault(x => x.DatBanId == id && x.TrangThai != TrangThaiHoaDon.DaHuy)
                ?? await db.HoaDon.Where(x => x.DatBanId == id && x.TrangThai != TrangThaiHoaDon.DaHuy)
                    .OrderByDescending(x => x.Id).FirstOrDefaultAsync();
            if (bill is not null)
                await khoService.DeductInventoryForBookingPreOrdersAsync(id, bill.Id, employeeId);
        }
        return null;
    });

    public Task<string?> Cancel(int id, string? version, string? reason, int? staffId) => Run(async () =>
    {
        var booking = await db.DatBan.SingleOrDefaultAsync(x => x.Id == id);
        if (booking is null || !SameVersion(booking, version)) return "Yêu cầu đã thay đổi. Vui lòng mở lại.";
        return await CancelWithinTransaction(booking, reason, staffId);
    });

    public Task<string?> ExpireUnpaidHold(int id) => Run(async () =>
    {
        var booking = await db.DatBan.FromSqlInterpolated($"SELECT * FROM [DatBan] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id}").SingleOrDefaultAsync();
        if (booking is null || booking.TrangThai != TrangThaiDatBan.ChoCoc || !ReservationDepositPolicy.Expired(booking)
            || booking.TienCocDaNop != 0) return null;
        return await CancelWithinTransaction(booking, "Tự động hủy: quá hạn giữ chỗ, chưa báo chuyển khoản và chưa ghi nhận tiền cọc.", null);
    });

    private async Task<string?> CancelWithinTransaction(DatBan booking, string? reason, int? staffId)
    {
        if (booking.TrangThai is not (TrangThaiDatBan.ChoXacNhan or TrangThaiDatBan.ChoCoc or TrangThaiDatBan.DaXacNhan))
            return "Không hủy lịch đã nhận khách hoặc đã kết thúc. Hãy xử lý đơn hàng/thanh toán trước.";
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500) return "Nhập lý do hủy từ 1 đến 500 ký tự.";
        if (booking.ThoiDiemBaoChuyenKhoan is not null) return "Khách đã báo chuyển khoản. Thu ngân cần đối chiếu và xử lý thông báo/tiền cọc trước khi hủy.";
        if (booking.TienCocDaNop - booking.TienCocDaHoan - booking.TienCocDaGiu > 0)
            return "Lịch đã có tiền cọc; cần xử lý hoàn/giữ cọc trước khi hủy.";
        var bills = await db.HoaDon.Include(x => x.ChiTiet).Where(x => x.DatBanId == booking.Id && x.TrangThai != TrangThaiHoaDon.DaHuy).ToListAsync();
        if (bills.Any(x => x.TrangThai != TrangThaiHoaDon.ChuaThanhToan
            || x.ChiTiet.Any(i => i.TrangThai is TrangThaiCheBien.DangCheBien or TrangThaiCheBien.SanSang or TrangThaiCheBien.DaPhucVu)))
            return "Đã có món chế biến hoặc hóa đơn thanh toán. Quản lý cần xử lý món và khoản tiền trước khi hủy lịch.";
        foreach (var bill in bills)
        {
            foreach (var line in bill.ChiTiet) line.TrangThai = TrangThaiCheBien.DaHuy;
            bill.TongTienHang = 0; bill.TienGiam = 0; bill.TrangThai = TrangThaiHoaDon.DaHuy;
        }
        booking.TrangThai = TrangThaiDatBan.DaHuy;
        booking.LyDoHuy = reason.Trim(); booking.ThoiDiemHuy = DateTimeOffset.UtcNow; booking.NhanVienHuyId = staffId;
        // A future booking never owns the physical table's current occupancy.
        return null;
    }

    public Task<string?> ChangeTable(int id, string? version, bool clean) => Run(async () =>
    {
        var bookingId = await db.ChiTietDatBan.Where(x => x.BanAnId == id && x.DatBan.TrangThai == TrangThaiDatBan.DaNhanBan
            && x.DatBan.ThoiDiemKetThuc == null).OrderByDescending(x => x.DatBan.ThoiDiemNhanBan).Select(x => (int?)x.DatBanId).FirstOrDefaultAsync();
        var ids = !clean && bookingId is int b ? await db.ChiTietDatBan.Where(x => x.DatBanId == b).Select(x => x.BanAnId).ToArrayAsync() : [id];
        var tables = await LockTables(ids);
        var selected = tables.SingleOrDefault(x => x.Id == id);
        if (selected is null || !SameVersion(selected, version)) return "Bàn đã thay đổi. Vui lòng tải lại sơ đồ.";
        if (clean)
        {
            if (selected.TrangThai != TrangThaiBan.CanDon) return "Chỉ xác nhận dọn xong cho bàn đang cần dọn.";
            selected.TrangThai = TrangThaiBan.SanSang;
            return null;
        }
        if (tables.Any(x => x.TrangThai != TrangThaiBan.DangPhucVu)) return "Chỉ kết thúc phục vụ bàn đang có khách.";
        if (bookingId is null) return "Chưa xác định được lượt khách của bàn. Quản lý cần kiểm tra dữ liệu trước khi giải phóng.";
        var booking = await db.DatBan.SingleAsync(x => x.Id == bookingId);
        return await FinishWithinTransaction(booking, tables);
    });

    public Task<string?> CloseOverdue(int id) => Run(async () =>
    {
        var ids = await db.ChiTietDatBan.Where(x => x.DatBanId == id).Select(x => x.BanAnId).ToArrayAsync();
        var tables = await LockTables(ids);
        var booking = await db.DatBan.SingleOrDefaultAsync(x => x.Id == id);
        if (booking is null || booking.TrangThai != TrangThaiDatBan.DaNhanBan || booking.ThoiDiemKetThuc is not null) return null;
        if (ServingDeadline(booking) > DateTimeOffset.UtcNow) return "Chưa đủ 3 tiếng từ lúc nhận bàn.";
        if (tables.Count == 0 || tables.Any(x => x.TrangThai != TrangThaiBan.DangPhucVu)) return "Trạng thái bàn không khớp lượt phục vụ; cần kiểm tra.";
        return await FinishWithinTransaction(booking, tables);
    });

    private async Task<string?> FinishWithinTransaction(DatBan booking, List<BanAn> tables)
    {
        if (booking.TrangThai != TrangThaiDatBan.DaNhanBan || booking.ThoiDiemKetThuc is not null)
            return "Lượt phục vụ đã thay đổi. Vui lòng tải lại sơ đồ.";
        var bills = await db.HoaDon.Include(x => x.ChiTiet).Where(x => x.DatBanId == booking.Id).ToListAsync();
        if (bills.Any(x => x.TrangThai is not (TrangThaiHoaDon.DaThanhToan or TrangThaiHoaDon.DaHuy)
            && (x.ChiTiet.Count > 0 || x.TongTienHang > 0 || x.TienCocDaTru > 0))) return "Còn hóa đơn chưa thanh toán. Không thể kết thúc bàn.";
        if (bills.Any(x => x.TrangThai != TrangThaiHoaDon.DaHuy && x.ChiTiet.Any(c => c.TrangThai is not (TrangThaiCheBien.DaPhucVu or TrangThaiCheBien.DaHuy))))
            return "Còn món chưa phục vụ hoặc hủy. Vui lòng xử lý đơn hàng trước.";
        var unallocatedDeposit = booking.TienCocDaNop - booking.TienCocDaHoan - booking.TienCocDaGiu
            - bills.Where(x => x.TrangThai != TrangThaiHoaDon.DaHuy).Sum(x => x.TienCocDaTru);
        if (unallocatedDeposit > 0) return "Còn tiền cọc chưa đối trừ hoặc hoàn/giữ. Thu ngân cần xử lý trước khi kết thúc bàn.";
        foreach (var bill in bills.Where(x => x.TrangThai is not (TrangThaiHoaDon.DaThanhToan or TrangThaiHoaDon.DaHuy))) bill.TrangThai = TrangThaiHoaDon.DaHuy;
        booking.ThoiDiemKetThuc = DateTimeOffset.UtcNow;
        foreach (var table in tables) table.TrangThai = TrangThaiBan.CanDon;
        return null;
    }
}
