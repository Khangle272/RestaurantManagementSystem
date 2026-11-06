using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Shared;

namespace RestaurantManagement.Web.Services;

public sealed record TableAvailabilityItem(int Id, string MaBan, int KhuVucId, string KhuVuc, bool Vip, int SoCho, string TrangThai, string GhiChu, decimal TienCoc);

// Shared by the reservation queue and floor board so both enforce the same lifecycle.
public class TableService(RestaurantDbContext db, PreorderService preorders, IKhoService? khoService = null)
{
    public const int MaxDiningHours = 3;
    public static DateTimeOffset ServingDeadline(DatBan booking) => (booking.ThoiDiemNhanBan ?? booking.GioDen).AddHours(MaxDiningHours);
    // Bàn đang có khách hoặc chưa dọn chỉ chặn lượt đến sát hiện tại; lịch muộn hơn trong ngày vẫn đặt được.
    public const int PhysicalBusyMinutes = 30;
    // Quá giờ hẹn chừng này mà khách chưa đến thì lễ tân được ghi nhận "Không đến" để trả bàn.
    public const int NoShowGraceMinutes = 15;
    public static DateTimeOffset VietnamTime(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), TimeSpan.FromHours(7));
    public static DateTime VietnamNow => DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime;
    public static IQueryable<ChiTietDatBan> CurrentServices(RestaurantDbContext db)
    {
        var today = VietnamTime(VietnamNow.Date); var tomorrow = today.AddDays(1);
        return db.ChiTietDatBan.Where(x => x.DatBan.GioDen >= today && x.DatBan.GioDen < tomorrow
            && x.DatBan.TrangThai == TrangThaiDatBan.DaNhanBan && x.DatBan.ThoiDiemKetThuc == null);
    }
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

    public IQueryable<ChiTietDatBan> BlockingBookings()
    {
        var now = DateTimeOffset.UtcNow;
        return db.ChiTietDatBan.Where(x => x.DatBan.TrangThai == TrangThaiDatBan.DaXacNhan
            || (x.DatBan.TrangThai == TrangThaiDatBan.ChoCoc && (!x.DatBan.CocTuDong
                || x.DatBan.HanThanhToanCoc > now || x.DatBan.ThoiDiemBaoChuyenKhoan != null
                || x.DatBan.TienCocDaNop > x.DatBan.TienCocDaHoan + x.DatBan.TienCocDaGiu))
            // Lượt đã kết thúc không còn giữ bàn: bàn nhận được khách mới ngay trong cùng ca.
            || (x.DatBan.TrangThai == TrangThaiDatBan.DaNhanBan && x.DatBan.ThoiDiemKetThuc == null));
    }

    public IQueryable<ChiTietDatBan> OverlappingBookings(DateTimeOffset from, DateTimeOffset until, int? exclude = null)
    {
        return BlockingBookings().Where(x => (!exclude.HasValue || x.DatBanId != exclude.Value)
            && x.DatBan.GioDen < until && x.DatBan.GioKetThucDuKien > from);
    }

    public async Task<List<int>> PhysicalBusyTables(DateTimeOffset arrival, int? exclude = null)
    {
        var day = arrival.ToOffset(TimeSpan.FromHours(7)).Date;
        if (day != VietnamNow.Date || arrival > DateTimeOffset.UtcNow.AddMinutes(PhysicalBusyMinutes)) return [];
        var start = VietnamTime(day); var end = start.AddDays(1);
        var busy = await db.ChiTietDatBan.Where(x => (!exclude.HasValue || x.DatBanId != exclude.Value) && x.DatBan.GioDen >= start && x.DatBan.GioDen < end
            && x.DatBan.TrangThai == TrangThaiDatBan.DaNhanBan
            && (x.DatBan.ThoiDiemKetThuc == null || x.BanAn.TrangThai == TrangThaiBan.CanDon))
            .Select(x => x.BanAnId).Distinct().ToListAsync();
        // Physical flags require staff confirmation before seating someone now, but never block a later arrival.
        busy.AddRange(await db.BanAn.Where(t => (t.TrangThai == TrangThaiBan.DangPhucVu || t.TrangThai == TrangThaiBan.CanDon)
            && (!exclude.HasValue || !db.ChiTietDatBan.Any(x => x.BanAnId == t.Id && x.DatBanId == exclude.Value
                && x.DatBan.TrangThai == TrangThaiDatBan.DaNhanBan && x.DatBan.ThoiDiemKetThuc == null)))
            .Select(t => t.Id).ToListAsync());
        return busy;
    }

    public static List<BanAn>? PickTables(IEnumerable<BanAn> available, int guests)
    {
        var single = available.Where(x => x.SoChoNgoi >= guests).OrderBy(x => x.SoChoNgoi).ThenBy(x => x.Id).FirstOrDefault();
        return single is null ? null : [single];
    }

    public async Task<bool> HasCapacity(DateTimeOffset from, DateTimeOffset until, int guests, int? areaId, bool vip)
    {
        var candidates = await db.BanAn.AsNoTracking().Where(x => x.KhuVuc.DangSuDung
            && x.TrangThai != TrangThaiBan.NgungSuDung && (!areaId.HasValue || x.KhuVucId == areaId)
            && x.KhuVuc.LaPhongVip == vip).ToListAsync();
        var busy = await OverlappingBookings(from, until)
            .Select(x => x.BanAnId).Distinct().ToListAsync();
        busy.AddRange(await PhysicalBusyTables(from));
        return PickTables(candidates.Where(x => !busy.Contains(x.Id)), guests) is not null;
    }

    public async Task<(string? Error, List<DiningSlotAvailability> Slots)> AvailableSlots(DateOnly day, int guests, int? areaId, int leadMinutes = 30, DateTime? preferredArrival = null)
    {
        var today = DateOnly.FromDateTime(VietnamNow);
        if (day < today || day > today.AddDays(180) || guests is < 1 or > 50)
            return ("Chọn ngày trong 180 ngày tới và từ 1 đến 50 khách.", []);
        var area = areaId.HasValue ? await db.KhuVuc.AsNoTracking().SingleOrDefaultAsync(x => x.Id == areaId && x.DangSuDung) : null;
        if (areaId.HasValue && area is null) return ("Khu vực không còn sử dụng.", []);
        var result = new List<DiningSlotAvailability>();
        var now = VietnamNow;
        if (preferredArrival.HasValue && DateOnly.FromDateTime(preferredArrival.Value) != day) return ("Ngày đặt và giờ đến chưa khớp.", []);
        foreach (var arrival in ReservationSchedule.QuickArrivals(day, now, leadMinutes))
        {
            var time = TimeOnly.FromDateTime(arrival);
            var slot = ReservationSchedule.Match(arrival)!;
            if (ReservationSchedule.Validate(arrival, now, leadMinutes) is not null) continue;
            var available = await HasCapacity(VietnamTime(arrival), ReservationSchedule.EndOf(arrival), guests, areaId, area?.LaPhongVip == true);
            result.Add(new(slot.Id, slot.Name + " · " + time.ToString("HH:mm"), arrival.ToString("yyyy-MM-ddTHH:mm"), available,
                available ? "Giờ sớm nhất có thể nhận trong ca này; có thể tự chọn giờ khác." : "Giờ này không còn đủ bàn phù hợp với số khách/khu vực; thử giờ khác."));
        }
        return (null, result.OrderBy(x => x.Value).ToList());
    }

    public async Task<(bool Available, string Message)> CheckAvailability(DateTime arrival, int guests, int? areaId, int leadMinutes = 30)
    {
        if (guests is < 1 or > 50) return (false, "Chọn từ 1 đến 50 khách.");
        if (ReservationSchedule.Validate(arrival, VietnamNow, leadMinutes) is { } error) return (false, error);
        var area = areaId.HasValue ? await db.KhuVuc.SingleOrDefaultAsync(x => x.Id == areaId && x.DangSuDung) : null;
        if (areaId.HasValue && area is null) return (false, "Khu vực không còn sử dụng.");
        var available = await HasCapacity(VietnamTime(arrival), ReservationSchedule.EndOf(arrival), guests, areaId, area?.LaPhongVip == true);
        if (available) return (true, "Còn bàn phù hợp trong giờ này. Hệ thống kiểm tra lại khi lưu.");
        var largest = await db.BanAn.Where(x => x.KhuVuc.DangSuDung && x.TrangThai != TrangThaiBan.NgungSuDung).MaxAsync(x => (int?)x.SoChoNgoi) ?? 0;
        return (false, guests > largest
            ? $"Nhóm {guests} khách lớn hơn bàn lớn nhất ({largest} chỗ). Vui lòng gọi nhà hàng để được sắp xếp."
            : "Không còn bàn phù hợp trong giờ này. Chọn giờ, ngày hoặc khu vực khác.");
    }

    // Sơ đồ bàn/phòng theo ngày + ca để khách hoặc lễ tân tự chọn đúng bàn mình muốn.
    public async Task<(string? Error, List<TableAvailabilityItem> Tables)> TableAvailability(DateTime arrival, int guests, int? areaId, int leadMinutes = 30)
    {
        if (guests is < 1 or > 50) return ("Chọn từ 1 đến 50 khách.", []);
        if (ReservationSchedule.Validate(arrival, VietnamNow, leadMinutes) is { } error) return (error, []);
        var from = VietnamTime(arrival);
        var tables = await db.BanAn.AsNoTracking().Include(x => x.KhuVuc)
            .Where(x => x.KhuVuc.DangSuDung && x.TrangThai != TrangThaiBan.NgungSuDung && (!areaId.HasValue || x.KhuVucId == areaId))
            .OrderBy(x => x.KhuVuc.LaPhongVip).ThenBy(x => x.KhuVuc.Tang).ThenBy(x => x.KhuVuc.TenKhuVuc).ThenBy(x => x.MaBan).ToListAsync();
        var busy = await OverlappingBookings(from, ReservationSchedule.EndOf(arrival)).Select(x => x.BanAnId).Distinct().ToListAsync();
        var physical = await PhysicalBusyTables(from);
        return (null, tables.Select(t =>
        {
            var status = busy.Contains(t.Id) || physical.Contains(t.Id) ? "da-dat" : t.SoChoNgoi < guests ? "khong-du-cho" : "trong";
            var note = status switch { "da-dat" => busy.Contains(t.Id) ? "Đã có khách đặt trong giờ này" : "Đang có khách hoặc chưa dọn xong", "khong-du-cho" => $"Chỉ {t.SoChoNgoi} chỗ, không đủ {guests} khách", _ => "Còn trống" };
            return new TableAvailabilityItem(t.Id, t.MaBan, t.KhuVucId, t.KhuVuc.TenKhuVuc, t.KhuVuc.LaPhongVip, t.SoChoNgoi, status, note, t.KhuVuc.TienCocGiuCho);
        }).ToList());
    }

    // Caller owns the serializable transaction; reserve before asking the customer for money.
    public async Task<string?> ReserveOnlineWithinTransaction(DatBan booking, int? chosenTableId = null)
    {
        var guestCount = booking.SoNguoiLon + booking.SoTreEm;
        // Khách đã chọn bàn trên sơ đồ: giữ đúng bàn đó, không tự gán bàn khác.
        if (chosenTableId is int tableId)
        {
            var chosenTable = (await LockTables([tableId])).SingleOrDefault();
            if (chosenTable is null || !chosenTable.KhuVuc.DangSuDung || chosenTable.TrangThai == TrangThaiBan.NgungSuDung)
                return "Bàn đã chọn không còn sử dụng. Vui lòng chọn bàn khác trên sơ đồ.";
            if (chosenTable.SoChoNgoi < guestCount) return $"Bàn {chosenTable.MaBan} chỉ có {chosenTable.SoChoNgoi} chỗ. Vui lòng chọn bàn lớn hơn.";
            var taken = await OverlappingBookings(booking.GioDen, booking.GioKetThucDuKien, booking.Id).AnyAsync(x => x.BanAnId == tableId)
                || (await PhysicalBusyTables(booking.GioDen, booking.Id)).Contains(tableId);
            if (taken) return $"Bàn {chosenTable.MaBan} vừa có người đặt trong giờ này. Vui lòng chọn bàn khác trên sơ đồ.";
            booking.KhuVucUuTienId = chosenTable.KhuVucId;
            booking.YeuCauVip = chosenTable.KhuVuc.LaPhongVip;
            foreach (var link in booking.Ban.Where(x => x.BanAnId != tableId).ToList())
            { db.ChiTietDatBan.Remove(link); booking.Ban.Remove(link); }
            if (!booking.Ban.Any(x => x.BanAnId == tableId)) booking.Ban.Add(new ChiTietDatBan { BanAnId = tableId });
            return null;
        }
        if (booking.Ban.Count == 1 && await ReserveOnlineWithinTransaction(booking, booking.Ban.Single().BanAnId) is null) return null;
        var candidates = await db.BanAn.Where(x => x.KhuVuc.DangSuDung && x.TrangThai != TrangThaiBan.NgungSuDung
            && (!booking.KhuVucUuTienId.HasValue || x.KhuVucId == booking.KhuVucUuTienId)
            && x.KhuVuc.LaPhongVip == booking.YeuCauVip).Select(x => x.Id).ToListAsync();
        var tables = await LockTables(candidates);
        var busy = await OverlappingBookings(booking.GioDen, booking.GioKetThucDuKien, booking.Id)
            .Where(x => candidates.Contains(x.BanAnId))
            .Select(x => x.BanAnId).Distinct().ToListAsync();
        busy.AddRange(await PhysicalBusyTables(booking.GioDen, booking.Id));
        var available = tables.Where(x => !busy.Contains(x.Id));
        var chosen = PickTables(available, guestCount);
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
        if (ids.Length != 1) return "Chọn đúng một bàn đủ chỗ cho khách. Chọn bàn mới sẽ thay bàn cũ.";
        var tables = await LockTables(ids);
        var booking = await db.DatBan.Include(x => x.Ban).SingleOrDefaultAsync(x => x.Id == id);
        if (booking is null || !SameVersion(booking, version)) return "Yêu cầu đã thay đổi. Vui lòng mở lại.";
        if (ReservationDepositPolicy.Expired(booking)) return "Lịch đã hết hạn giữ chỗ chờ thanh toán. Khách cần tạo lịch mới.";
        if (booking.TrangThai is not (TrangThaiDatBan.ChoXacNhan or TrangThaiDatBan.ChoCoc or TrangThaiDatBan.DaXacNhan))
            return "Chỉ chọn hoặc đổi bàn cho lịch chờ xác nhận, chờ cọc hoặc đã xác nhận.";
        if (booking.GioKetThucDuKien <= DateTimeOffset.UtcNow) return "Yêu cầu đã quá giờ kết thúc dự kiến.";
        if (tables.Count != ids.Length || tables.Any(x => !x.KhuVuc.DangSuDung || x.TrangThai == TrangThaiBan.NgungSuDung))
            return "Có bàn hoặc khu vực đã ngừng sử dụng.";
        if (tables[0].SoChoNgoi < booking.SoNguoiLon + booking.SoTreEm) return "Bàn đã chọn không đủ chỗ cho khách.";
        if (booking.YeuCauVip && tables.Any(x => !x.KhuVuc.LaPhongVip)) return "Yêu cầu phòng VIP cần được xếp vào khu vực VIP.";
        if (!booking.YeuCauVip && tables.Any(x => x.KhuVuc.LaPhongVip)) return "Khách chưa yêu cầu VIP. Thống nhất yêu cầu với khách trước khi chọn khu vực VIP.";
        var physicallyBusy = await PhysicalBusyTables(booking.GioDen, id);
        if (tables.Any(x => physicallyBusy.Contains(x.Id)))
            return "Bàn đang phục vụ hoặc chưa dọn. Chọn bàn sẵn sàng hoặc chờ dọn xong.";
        var conflict = await OverlappingBookings(booking.GioDen, booking.GioKetThucDuKien, id)
            .Where(x => ids.Contains(x.BanAnId)).OrderBy(x => x.DatBan.GioDen).Select(x => x.DatBan).FirstOrDefaultAsync();
        if (conflict is not null)
            return $"Bàn đã có lượt {conflict.GioDen.ToOffset(TimeSpan.FromHours(7)):HH:mm}–{conflict.GioKetThucDuKien.ToOffset(TimeSpan.FromHours(7)):HH:mm}, trùng giờ của lịch này. Chọn bàn khác hoặc đổi giờ.";
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

    public Task<string?> Reschedule(int id, string? version, DateTime arrival, string reason, int? staffId) => Run(async () =>
    {
        if (ReservationSchedule.Validate(arrival, VietnamNow, -15) is { } invalid) return invalid;
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 200) return "Nhập lý do đổi giờ, tối đa 200 ký tự.";
        var ids = await db.ChiTietDatBan.Where(x => x.DatBanId == id).Select(x => x.BanAnId).ToArrayAsync();
        var selected = await LockTables(ids);
        var booking = await db.DatBan.SingleOrDefaultAsync(x => x.Id == id);
        if (booking is null || !SameVersion(booking, version)) return "Yêu cầu đã thay đổi; mở lại để đổi giờ.";
        if (booking.TrangThai is not (TrangThaiDatBan.ChoXacNhan or TrangThaiDatBan.ChoCoc or TrangThaiDatBan.DaXacNhan)
            || ReservationDepositPolicy.Expired(booking)) return "Chỉ đổi giờ lịch chưa nhận bàn và còn hiệu lực giữ chỗ.";
        if (await db.ChiTietHoaDon.AnyAsync(x => x.HoaDon.DatBanId == id && x.TrangThai != TrangThaiCheBien.ChoCheBien && x.TrangThai != TrangThaiCheBien.DaHuy))
            return "Món đã bắt đầu chế biến; phối hợp với bếp trước khi đổi lịch.";
        var from = VietnamTime(arrival); var until = ReservationSchedule.EndOf(arrival);
        if (await OverlappingBookings(from, until, id).AnyAsync(x => ids.Contains(x.BanAnId)))
            return "Bàn đang xếp đã có lượt trùng giờ mới. Xếp lại bàn hoặc chọn giờ khác.";
        var busy = await PhysicalBusyTables(from, id);
        if (selected.Any(x => x.TrangThai == TrangThaiBan.NgungSuDung || !x.KhuVuc.DangSuDung || busy.Contains(x.Id)))
            return "Bàn đang xếp chưa sẵn sàng cho giờ mới; chọn bàn khác trước khi đổi giờ.";
        var audit = $"\nĐổi giờ {VnTime.NgayGio(booking.GioDen)} → {VnTime.NgayGio(arrival)}; {staffId?.ToString() ?? "Admin"}: {reason.Trim()}";
        if ((booking.YeuCau?.Length ?? 0) + audit.Length > 1000) return "Ghi chú lịch đã đầy; rút gọn trước khi thêm lý do đổi giờ.";
        booking.YeuCau = (booking.YeuCau ?? "") + audit;
        booking.GioDen = from; booking.GioKetThucDuKien = until;
        return null;
    });

    public Task<string?> CheckIn(int id, string? version, int? staffId) => Run(async () =>
    {
        var ids = await db.ChiTietDatBan.Where(x => x.DatBanId == id).Select(x => x.BanAnId).ToArrayAsync();
        var tables = await LockTables(ids);
        var booking = await db.DatBan.SingleOrDefaultAsync(x => x.Id == id);
        if (booking is null || !SameVersion(booking, version)) return "Yêu cầu đã thay đổi. Vui lòng mở lại.";
        if (booking.TrangThai is not (TrangThaiDatBan.ChoXacNhan or TrangThaiDatBan.ChoCoc or TrangThaiDatBan.DaXacNhan) || tables.Count == 0)
            return "Chỉ nhận khách của lịch còn hiệu lực và đã có bàn.";
        if (booking.YeuCauHuy is not null) return "Khách đang yêu cầu hủy. Xử lý yêu cầu trước khi nhận bàn.";
        if (booking.ThoiDiemBaoChuyenKhoan is not null) return "Khách đã báo chuyển khoản cọc. Thu ngân đối chiếu khoản tiền trước khi nhận bàn.";
        var now = DateTimeOffset.UtcNow;
        if (booking.GioDen.ToOffset(TimeSpan.FromHours(7)).Date != VietnamNow.Date || booking.GioKetThucDuKien <= now)
            return "Lịch không thuộc hôm nay hoặc đã quá giờ. Đổi giờ lịch trước khi nhận khách.";
        // Khách đến sớm: bàn phải trống từ bây giờ tới hết lượt của lịch.
        var seatFrom = now < booking.GioDen ? now : booking.GioDen;
        if (await OverlappingBookings(seatFrom, booking.GioKetThucDuKien, id).AnyAsync(x => ids.Contains(x.BanAnId)))
            return "Bàn đang có lịch khác trùng giờ. Kiểm tra và xếp lại bàn trước khi nhận khách.";
        var physicallyBusy = await PhysicalBusyTables(now, id);
        if (tables.Any(x => !x.KhuVuc.DangSuDung || x.TrangThai == TrangThaiBan.NgungSuDung || physicallyBusy.Contains(x.Id)))
            return "Bàn chưa sẵn sàng; hãy dọn bàn hoặc đổi sang bàn khác.";
        // Cọc chỉ để giữ chỗ: khách đã có mặt thì phần cọc chưa nộp không còn cần thu trước.
        booking.TienCocYeuCau = Math.Min(booking.TienCocYeuCau, Math.Max(0, ReservationDepositPolicy.Received(booking)));
        booking.YeuCauCoc = booking.TienCocYeuCau > 0;
        booking.TrangThai = TrangThaiDatBan.DaNhanBan;
        booking.ThoiDiemNhanBan = DateTimeOffset.UtcNow;
        booking.NhanVienTiepNhanId = staffId;
        foreach (var table in tables) table.TrangThai = TrangThaiBan.DangPhucVu;
        // Release customer drafts once, inside the same transaction as check-in.
        if (staffId is int employeeId)
        {
            var releaseError = await preorders.ReleaseWithinTransaction(booking, employeeId);
            if (releaseError is not null) return releaseError;
            var localOpenBills = db.HoaDon.Local
                .Where(x => x.DatBanId == id && x.TrangThai == TrangThaiHoaDon.ChuaThanhToan).ToList();
            var storedOpenBillIds = await db.HoaDon.AsNoTracking()
                .Where(x => x.DatBanId == id && x.TrangThai == TrangThaiHoaDon.ChuaThanhToan)
                .Select(x => x.Id).ToListAsync();
            var openBillCount = localOpenBills.Count + storedOpenBillIds.Count(idInDb => localOpenBills.All(x => x.Id != idInDb));
            if (openBillCount > 1)
                return "Lượt bàn đang có nhiều hóa đơn chưa thanh toán. Hãy đối soát hóa đơn trước khi nhận khách.";
            if (openBillCount == 0)
                db.HoaDon.Add(new HoaDon { MaHoaDon = NewCode("HD"), DatBanId = id, KhachHangId = booking.KhachHangId,
                    NhanVienId = employeeId, ThoiDiemLap = DateTimeOffset.UtcNow, TrangThai = TrangThaiHoaDon.ChuaThanhToan });

            await db.SaveChangesAsync();

            var bill = db.HoaDon.Local.FirstOrDefault(x => x.DatBanId == id && x.TrangThai == TrangThaiHoaDon.ChuaThanhToan)
                ?? await db.HoaDon.Where(x => x.DatBanId == id && x.TrangThai == TrangThaiHoaDon.ChuaThanhToan)
                    .OrderBy(x => x.ThoiDiemLap).FirstOrDefaultAsync();
            if (bill is not null && khoService is not null)
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

    // Khách không đến: trả bàn cho lượt khác, giữ nguyên khoản cọc để quản lý hoàn hoặc giữ theo điều kiện.
    public Task<string?> MarkNoShow(int id, string? version, int? staffId) => Run(async () =>
    {
        var booking = await db.DatBan.SingleOrDefaultAsync(x => x.Id == id);
        if (booking is null || !SameVersion(booking, version)) return "Yêu cầu đã thay đổi. Vui lòng mở lại.";
        if (booking.GioDen.AddMinutes(NoShowGraceMinutes) > DateTimeOffset.UtcNow)
            return $"Chỉ ghi nhận không đến sau giờ hẹn {NoShowGraceMinutes} phút.";
        return await NoShowWithinTransaction(booking, staffId);
    });

    public Task<string?> SaveReceptionBooking(DatBan booking, int tableId) => Run(async () =>
    {
        if (await ReserveOnlineWithinTransaction(booking, tableId) is { } error) return error;
        db.DatBan.Add(booking);
        return null;
    });

    public Task<string?> ExpireMissedBooking(int id) => Run(async () =>
    {
        var booking = await db.DatBan.FromSqlInterpolated($"SELECT * FROM [DatBan] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id}").SingleOrDefaultAsync();
        if (booking is null || booking.GioKetThucDuKien > DateTimeOffset.UtcNow) return null;
        return await NoShowWithinTransaction(booking, null);
    });

    private async Task<string?> NoShowWithinTransaction(DatBan booking, int? staffId)
    {
        if (booking.TrangThai is not (TrangThaiDatBan.ChoXacNhan or TrangThaiDatBan.ChoCoc or TrangThaiDatBan.DaXacNhan))
            return "Chỉ ghi nhận không đến cho lịch chưa nhận bàn.";
        if (booking.ThoiDiemBaoChuyenKhoan is not null) return "Khách đã báo chuyển khoản. Thu ngân cần đối chiếu khoản tiền trước.";
        var bills = await db.HoaDon.Include(x => x.ChiTiet).Where(x => x.DatBanId == booking.Id && x.TrangThai == TrangThaiHoaDon.ChuaThanhToan).ToListAsync();
        foreach (var bill in bills)
        {
            foreach (var line in bill.ChiTiet.Where(x => x.TrangThai == TrangThaiCheBien.ChoCheBien))
            { line.TrangThai = TrangThaiCheBien.DaHuy; line.ThoiDiemHuy = DateTimeOffset.UtcNow; }
            bill.TongTienHang = bill.ChiTiet.Where(x => x.TrangThai != TrangThaiCheBien.DaHuy).Sum(x => x.SoLuong * x.DonGia);
            bill.TienGiam = Math.Min(bill.TienGiam, bill.TongTienHang);
            bill.TrangThai = TrangThaiHoaDon.DaHuy;
        }
        booking.TrangThai = TrangThaiDatBan.KhongDen;
        booking.NhanVienHuyId = staffId;
        return null;
    }

    public Task<string?> ExpireUnpaidHold(int id) => Run(async () =>
    {
        var booking = await db.DatBan.FromSqlInterpolated($"SELECT * FROM [DatBan] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id}").SingleOrDefaultAsync();
        if (booking is null || booking.TrangThai != TrangThaiDatBan.ChoCoc || !ReservationDepositPolicy.Expired(booking)
            || booking.TienCocDaNop != 0) return null;
        return await CancelWithinTransaction(booking, "Tự động hủy: quá hạn giữ chỗ, chưa báo chuyển khoản và chưa ghi nhận tiền cọc.", null);
    });

    public Task<string?> CancelByCustomerWithinCurrentTransaction(DatBan booking, string reason) => CancelWithinTransaction(booking, reason, null);

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
        var bookingId = await db.ChiTietDatBan.Where(x => x.BanAnId == id && x.DatBan.TrangThai == TrangThaiDatBan.DaNhanBan)
            .OrderByDescending(x => x.DatBan.ThoiDiemNhanBan ?? x.DatBan.GioDen).ThenByDescending(x => x.DatBanId)
            .Select(x => (int?)x.DatBanId).FirstOrDefaultAsync();
        var ids = bookingId is int b ? await db.ChiTietDatBan.Where(x => x.DatBanId == b).Select(x => x.BanAnId).ToArrayAsync() : [id];
        var tables = await LockTables(ids);
        var selected = tables.SingleOrDefault(x => x.Id == id);
        if (selected is null || !SameVersion(selected, version)) return "Bàn đã thay đổi. Vui lòng tải lại sơ đồ.";
        if (selected.TrangThai is not (TrangThaiBan.CanDon or TrangThaiBan.DangPhucVu)) return "Bàn đã sẵn sàng hoặc ngừng sử dụng. Vui lòng tải lại sơ đồ.";
        if (bookingId is int activeId)
        {
            var booking = await db.DatBan.SingleAsync(x => x.Id == activeId);
            if (booking.ThoiDiemKetThuc is null) return await FinishWithinTransaction(db, booking, tables);
            if (await CompletionError(db, booking) is { } error) return error;
        }
        // Legacy dirty/orphan flags are cleared only by an explicit staff confirmation, never by elapsed time.
        if (!clean) return "Hãy kiểm tra bàn đã trống, dọn xong rồi xác nhận.";
        selected.TrangThai = TrangThaiBan.SanSang;
        return null;
    });

    public Task<string?> CompleteService(int id, string? version) => Run(async () =>
    {
        var ids = await db.ChiTietDatBan.Where(x => x.DatBanId == id).Select(x => x.BanAnId).ToArrayAsync();
        var tables = await LockTables(ids);
        var booking = await db.DatBan.SingleOrDefaultAsync(x => x.Id == id);
        if (booking is null || !SameVersion(booking, version)) return "Lượt khách đã thay đổi. Vui lòng tải lại sơ đồ.";
        return await FinishWithinTransaction(db, booking, tables);
    });

    public Task<string?> CloseOverdue(int id) => Task.FromResult<string?>("Quá 3 tiếng chỉ nhắc kiểm tra; nhân viên xác nhận hoàn tất bàn sau khi xử lý đủ tiền, món và cọc.");

    public Task<string?> CompletePaidService(int id) => Run(async () => { await ReleasePaidWithinTransaction(db, id); return null; });

    public static async Task ReleasePaidWithinTransaction(RestaurantDbContext db, int id)
    {
        var ids = await db.ChiTietDatBan.Where(x => x.DatBanId == id).Select(x => x.BanAnId).ToArrayAsync();
        var tables = new List<BanAn>();
        foreach (var tableId in ids.Order())
        {
            var table = await db.BanAn.FromSqlInterpolated($"SELECT * FROM [BanAn] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={tableId}").SingleAsync();
            tables.Add(table);
        }
        var booking = await db.DatBan.SingleOrDefaultAsync(x => x.Id == id);
        if (booking is null || booking.TrangThai != TrangThaiDatBan.DaNhanBan || booking.ThoiDiemKetThuc is not null) return;
        if (!await db.HoaDon.AnyAsync(x => x.DatBanId == id && x.TrangThai == TrangThaiHoaDon.DaThanhToan)) return;
        if (tables.Count == 0) return;
        await FinishWithinTransaction(db, booking, tables);
    }

    private static async Task<string?> FinishWithinTransaction(RestaurantDbContext db, DatBan booking, List<BanAn> tables)
    {
        if (booking.TrangThai != TrangThaiDatBan.DaNhanBan || booking.ThoiDiemKetThuc is not null)
            return "Lượt phục vụ đã thay đổi. Vui lòng tải lại sơ đồ.";
        if (await CompletionError(db, booking) is { } error) return error;
        var bills = await db.HoaDon.Where(x => x.DatBanId == booking.Id).ToListAsync();
        foreach (var bill in bills.Where(x => x.TrangThai is not (TrangThaiHoaDon.DaThanhToan or TrangThaiHoaDon.DaHuy))) bill.TrangThai = TrangThaiHoaDon.DaHuy;
        booking.ThoiDiemKetThuc = DateTimeOffset.UtcNow;
        foreach (var table in tables)
        {
            // Resolving old paperwork must never release a newer party on the same physical table.
            var newer = await db.ChiTietDatBan.AnyAsync(x => x.BanAnId == table.Id && x.DatBanId != booking.Id
                && x.DatBan.TrangThai == TrangThaiDatBan.DaNhanBan
                && ((x.DatBan.ThoiDiemNhanBan ?? x.DatBan.GioDen) > (booking.ThoiDiemNhanBan ?? booking.GioDen)
                    || ((x.DatBan.ThoiDiemNhanBan ?? x.DatBan.GioDen) == (booking.ThoiDiemNhanBan ?? booking.GioDen) && x.DatBanId > booking.Id)));
            if (newer || table.TrangThai == TrangThaiBan.NgungSuDung) continue;
            table.TrangThai = booking.GioDen >= VietnamTime(VietnamNow.Date) ? TrangThaiBan.CanDon : TrangThaiBan.SanSang;
        }
        return null;
    }

    private static async Task<string?> CompletionError(RestaurantDbContext db, DatBan booking)
    {
        var bills = await db.HoaDon.Include(x => x.ChiTiet).Where(x => x.DatBanId == booking.Id).ToListAsync();
        if (bills.Any(x => x.TrangThai is not (TrangThaiHoaDon.DaThanhToan or TrangThaiHoaDon.DaHuy)
            && (x.ChiTiet.Count > 0 || x.TongTienHang > 0 || x.TienCocDaTru > 0))) return "Còn hóa đơn chưa thanh toán. Không thể kết thúc bàn.";
        if (bills.Any(x => x.TrangThai != TrangThaiHoaDon.DaHuy && x.ChiTiet.Any(c => c.TrangThai is not (TrangThaiCheBien.DaPhucVu or TrangThaiCheBien.DaHuy))))
            return "Còn món chưa phục vụ hoặc hủy. Vui lòng xử lý đơn hàng trước.";
        var unallocatedDeposit = booking.TienCocDaNop - booking.TienCocDaHoan - booking.TienCocDaGiu
            - bills.Where(x => x.TrangThai != TrangThaiHoaDon.DaHuy).Sum(x => x.TienCocDaTru);
        if (unallocatedDeposit > 0) return "Còn tiền cọc chưa đối trừ hoặc hoàn/giữ. Thu ngân cần xử lý trước khi kết thúc bàn.";
        return null;
    }
}
