using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Services;

// Shared by the reservation queue and floor board so both enforce the same lifecycle.
public class TableService(RestaurantDbContext db, IKhoService? khoService = null)
{
    public static DateTimeOffset VietnamTime(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), TimeSpan.FromHours(7));
    public static DateTime VietnamNow => DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime;
    public static string NewCode(string prefix) => prefix + "-" + Guid.NewGuid().ToString("N")[..20].ToUpperInvariant();

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
        if (booking.TrangThai is not (TrangThaiDatBan.ChoXacNhan or TrangThaiDatBan.DaXacNhan))
            return "Chỉ xếp bàn cho yêu cầu chờ xác nhận hoặc đã xác nhận; đơn chờ cọc phải xử lý cọc trước.";
        if (booking.GioKetThucDuKien <= DateTimeOffset.UtcNow) return "Yêu cầu đã quá giờ kết thúc dự kiến.";
        if (tables.Count != ids.Length || tables.Any(x => !x.KhuVuc.DangSuDung || x.TrangThai == TrangThaiBan.NgungSuDung))
            return "Có bàn hoặc khu vực đã ngừng sử dụng.";
        if (tables.Select(x => x.KhuVucId).Distinct().Count() != 1) return "Bàn ghép phải thuộc cùng một khu vực.";
        if (tables.Sum(x => x.SoChoNgoi) < booking.SoNguoiLon + booking.SoTreEm) return "Các bàn đã chọn không đủ chỗ cho khách.";
        if (booking.YeuCauVip && tables.Any(x => !x.KhuVuc.LaPhongVip)) return "Yêu cầu phòng VIP cần được xếp vào khu vực VIP.";
        if (await db.ChiTietDatBan.AnyAsync(x => ids.Contains(x.BanAnId) && x.DatBanId != id
            && (x.DatBan.TrangThai == TrangThaiDatBan.DaXacNhan || x.DatBan.TrangThai == TrangThaiDatBan.ChoCoc
                || (x.DatBan.TrangThai == TrangThaiDatBan.DaNhanBan && x.DatBan.ThoiDiemKetThuc == null))
            && x.DatBan.GioDen < booking.GioKetThucDuKien && x.DatBan.GioKetThucDuKien > booking.GioDen))
            return "Bàn đã có lịch trùng khoảng thời gian này. Chọn bàn khác hoặc đổi lịch với khách.";
        var removed = booking.Ban.Where(x => !ids.Contains(x.BanAnId)).ToList();
        db.ChiTietDatBan.RemoveRange(removed);
        foreach (var tableId in ids.Where(x => !booking.Ban.Any(b => b.BanAnId == x)))
            booking.Ban.Add(new ChiTietDatBan { BanAnId = tableId });
        booking.TrangThai = TrangThaiDatBan.DaXacNhan;
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
        if (booking.GioDen > DateTimeOffset.UtcNow.AddMinutes(30) || booking.GioKetThucDuKien <= DateTimeOffset.UtcNow)
            return "Chưa đến giờ nhận bàn hoặc lịch đã hết hạn. Điều chỉnh lịch trước khi nhận khách.";
        if (tables.Any(x => !x.KhuVuc.DangSuDung || x.TrangThai != TrangThaiBan.SanSang)) return "Bàn chưa sẵn sàng; hãy dọn bàn hoặc xếp bàn khác.";
        booking.TrangThai = TrangThaiDatBan.DaNhanBan;
        booking.ThoiDiemNhanBan = DateTimeOffset.UtcNow;
        booking.NhanVienTiepNhanId = staffId;
        foreach (var table in tables) table.TrangThai = TrangThaiBan.DangPhucVu;

        // Ensure bill exists
        var bill = await db.HoaDon.FirstOrDefaultAsync(x => x.DatBanId == id && x.TrangThai != TrangThaiHoaDon.DaHuy);
        if (bill == null)
        {
            var empId = staffId ?? await db.NhanVien.Where(x => x.DangLamViec).Select(x => (int?)x.Id).FirstOrDefaultAsync() ?? 1;
            bill = new HoaDon
            {
                MaHoaDon = NewCode("HD"),
                DatBanId = id,
                KhachHangId = booking.KhachHangId,
                NhanVienId = empId,
                ThoiDiemLap = DateTimeOffset.UtcNow,
                TrangThai = TrangThaiHoaDon.ChuaThanhToan
            };
            db.HoaDon.Add(bill);
            await db.SaveChangesAsync();
        }

        // Transfer pre-ordered dishes to bill and deduct inventory
        var preOrders = await db.MonDatTruoc.Where(x => x.DatBanId == id).ToListAsync();
        if (preOrders.Count > 0)
        {
            foreach (var po in preOrders)
            {
                if (await db.ChiTietHoaDon.AnyAsync(c => c.HoaDonId == bill.Id && c.MonDatTruocId == po.Id))
                    continue;

                string sizeName = "Mặc định";
                if (po.MonAnSizeId.HasValue)
                {
                    sizeName = await db.MonAnSize.Where(s => s.Id == po.MonAnSizeId.Value).Select(s => s.TenSize).FirstOrDefaultAsync() ?? "Mặc định";
                }

                db.ChiTietHoaDon.Add(new ChiTietHoaDon
                {
                    HoaDonId = bill.Id,
                    MonAnId = po.MonAnId,
                    MonAnSizeId = po.MonAnSizeId,
                    TenMonLucBan = po.TenMonLucDat,
                    TenSizeLucBan = sizeName,
                    DonGia = po.DonGiaThoaThuan,
                    SoLuong = po.SoLuong,
                    YeuCauCheBien = po.YeuCauCheBien,
                    TrangThai = TrangThaiCheBien.ChoCheBien,
                    ThoiDiemGoi = DateTimeOffset.UtcNow,
                    MonDatTruocId = po.Id
                });
            }
            await db.SaveChangesAsync();

            var activeItems = await db.ChiTietHoaDon.Where(c => c.HoaDonId == bill.Id && c.TrangThai != TrangThaiCheBien.DaHuy).ToListAsync();
            bill.TongTienHang = activeItems.Sum(c => c.SoLuong * c.DonGia);
            await db.SaveChangesAsync();

            if (khoService != null)
            {
                await khoService.DeductInventoryForBookingPreOrdersAsync(id, bill.Id, staffId);
            }
        }
        return null;
    });

    public Task<string?> Cancel(int id, string? version, string? reason, int? staffId) => Run(async () =>
    {
        var booking = await db.DatBan.SingleOrDefaultAsync(x => x.Id == id);
        if (booking is null || !SameVersion(booking, version)) return "Yêu cầu đã thay đổi. Vui lòng mở lại.";
        if (booking.TrangThai is not (TrangThaiDatBan.ChoXacNhan or TrangThaiDatBan.ChoCoc or TrangThaiDatBan.DaXacNhan))
            return "Không hủy lịch đã nhận khách hoặc đã kết thúc. Hãy xử lý đơn hàng/thanh toán trước.";
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500) return "Nhập lý do hủy từ 1 đến 500 ký tự.";
        if (booking.TienCocDaNop > 0) return "Lịch đã có tiền cọc; cần xử lý hoàn/giữ cọc trước khi hủy.";
        booking.TrangThai = TrangThaiDatBan.DaHuy;
        booking.LyDoHuy = reason.Trim(); booking.ThoiDiemHuy = DateTimeOffset.UtcNow; booking.NhanVienHuyId = staffId;
        // A future booking never owns the physical table's current occupancy.
        return null;
    });

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
        var bills = await db.HoaDon.Include(x => x.ChiTiet).Where(x => x.DatBanId == bookingId).ToListAsync();
        if (bills.Any(x => x.TrangThai is not (TrangThaiHoaDon.DaThanhToan or TrangThaiHoaDon.DaHuy)
            && (x.ChiTiet.Count > 0 || x.TongTienHang > 0 || x.TienCocDaTru > 0))) return "Còn hóa đơn chưa thanh toán. Không thể kết thúc bàn.";
        if (bills.Any(x => x.TrangThai != TrangThaiHoaDon.DaHuy && x.ChiTiet.Any(c => c.TrangThai is not (TrangThaiCheBien.DaPhucVu or TrangThaiCheBien.DaHuy))))
            return "Còn món chưa phục vụ hoặc hủy. Vui lòng xử lý đơn hàng trước.";
        foreach (var bill in bills.Where(x => x.TrangThai is not (TrangThaiHoaDon.DaThanhToan or TrangThaiHoaDon.DaHuy))) bill.TrangThai = TrangThaiHoaDon.DaHuy;
        var booking = await db.DatBan.SingleAsync(x => x.Id == bookingId);
        booking.ThoiDiemKetThuc = DateTimeOffset.UtcNow;
        foreach (var table in tables) table.TrangThai = TrangThaiBan.CanDon;
        return null;
    });
}
