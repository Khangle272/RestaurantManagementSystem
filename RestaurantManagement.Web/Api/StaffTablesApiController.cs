using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Shared.Mobile;
using RestaurantManagement.Web.Services;

namespace RestaurantManagement.Web.Api;

// Sơ đồ bàn và tiếp nhận đặt bàn trên mobile. Mọi thay đổi đi qua TableService như website.
[Route("api/staff")]
[Authorize(Roles = MobileApi.StaffRoles)]
public class StaffTablesApiController(RestaurantDbContext db, UserManager<TaiKhoan> users,
    TableService tables, OrderService orders) : MobileApiControllerBase(db, users)
{
    [HttpGet("tables")]
    public async Task<ActionResult<List<TableDto>>> Tables()
    {
        var all = await Db.BanAn.AsNoTracking()
            .Where(x => x.KhuVuc.DangSuDung && x.TrangThai != TrangThaiBan.NgungSuDung)
            .OrderBy(x => x.KhuVuc.Tang).ThenBy(x => x.KhuVuc.TenKhuVuc).ThenBy(x => x.MaBan)
            .Select(x => new
            {
                x.Id, x.MaBan, KhuVuc = x.KhuVuc.TenKhuVuc, x.KhuVuc.Tang, x.SoChoNgoi, x.TrangThai,
                Version = EF.Property<byte[]>(x, "RowVersion")
            }).ToListAsync();
        var serving = (await orders.GetOccupiedTablesAsync()).ToDictionary(x => x.Id);

        var now = DateTimeOffset.UtcNow;
        var today = TableService.VietnamTime(TableService.VietnamNow.Date);
        var upcoming = (await Db.ChiTietDatBan.AsNoTracking()
                .Where(x => x.DatBan.GioDen >= today && x.DatBan.GioDen < today.AddDays(1) && x.DatBan.GioKetThucDuKien > now
                    && (x.DatBan.TrangThai == TrangThaiDatBan.DaXacNhan || x.DatBan.TrangThai == TrangThaiDatBan.ChoCoc))
                .Select(x => new { x.BanAnId, x.DatBan.GioDen, x.DatBan.HoTenLienHe }).ToListAsync())
            .GroupBy(x => x.BanAnId).ToDictionary(g => g.Key, g => g.OrderBy(x => x.GioDen).First());

        return all.Select(t =>
        {
            serving.TryGetValue(t.Id, out var card);
            upcoming.TryGetValue(t.Id, out var next);
            var nextText = next is null ? null : $"{next.GioDen.ToOffset(TimeSpan.FromHours(7)):HH:mm} · {next.HoTenLienHe}";
            var (code, text) = t.TrangThai switch
            {
                TrangThaiBan.DangPhucVu => ("DangPhucVu", "Đang phục vụ"),
                TrangThaiBan.CanDon => ("CanDon", "Chờ dọn"),
                _ when next is not null => ("DaDat", "Đã đặt"),
                _ => ("Trong", "Trống")
            };
            return new TableDto(t.Id, t.MaBan, t.KhuVuc, t.Tang, t.SoChoNgoi, code, text, MobileApi.Version(t.Version),
                card?.DatBanId, card?.KhachHang, card?.SoKhach ?? 0, card?.HoaDonIdDangMo, card?.MaHoaDonDangMo,
                card?.OrderedTotal ?? 0, card?.OrderedQuantity ?? 0,
                card?.OrderedItems.Count(x => x.TrangThai == TrangThaiCheBien.SanSang) ?? 0, nextText);
        }).ToList();
    }

    // Kết thúc lượt khách (đã thanh toán, đã phục vụ đủ món) → bàn sang Chờ dọn.
    [HttpPost("tables/{id:int}/finish"), Authorize(Roles = MobileApi.ServeRoles)]
    public Task<IActionResult> Finish(int id, TableActionRequest request) => ChangeTable(id, request, clean: false);

    // Bồi bàn xác nhận đã dọn xong → bàn Sẵn sàng.
    [HttpPost("tables/{id:int}/clean"), Authorize(Roles = MobileApi.ServeRoles)]
    public Task<IActionResult> Clean(int id, TableActionRequest request) => ChangeTable(id, request, clean: true);

    private async Task<IActionResult> ChangeTable(int id, TableActionRequest request, bool clean)
    {
        var error = await tables.ChangeTable(id, request.RowVersion, clean);
        if (error is not null) return Fail(error, StatusCodes.Status409Conflict);
        var dirty = await Db.BanAn.AnyAsync(x => x.Id == id && x.TrangThai == TrangThaiBan.CanDon);
        return Ok(new MessageDto(dirty ? "Đã kết thúc lượt khách, bàn chuyển sang chờ dọn." : "Đã dọn xong, bàn sẵn sàng đón khách."));
    }

    // Yêu cầu chờ xác nhận (mọi ngày) và lịch hôm nay chưa nhận bàn.
    [HttpGet("reservations"), Authorize(Roles = MobileApi.ReceptionRoles)]
    public async Task<ActionResult<List<ReservationDto>>> Reservations()
    {
        var now = DateTimeOffset.UtcNow;
        var tomorrow = TableService.VietnamTime(TableService.VietnamNow.Date).AddDays(1);
        var bookings = await Db.DatBan.AsNoTracking()
            .Where(x => x.GioKetThucDuKien > now && (x.TrangThai == TrangThaiDatBan.ChoXacNhan
                || ((x.TrangThai == TrangThaiDatBan.ChoCoc || x.TrangThai == TrangThaiDatBan.DaXacNhan) && x.GioDen < tomorrow)))
            .OrderBy(x => x.GioDen).Take(50)
            .Select(x => new
            {
                x.Id, x.MaDatBan, x.HoTenLienHe, x.SoDienThoaiLienHe, x.GioDen, x.GioKetThucDuKien,
                Guests = x.SoNguoiLon + x.SoTreEm, x.TrangThai, x.YeuCau, x.YeuCauVip, x.YeuCauHuy, x.ThoiDiemBaoChuyenKhoan,
                Tables = x.Ban.Select(b => b.BanAn.MaBan).ToList(), Version = EF.Property<byte[]>(x, "RowVersion")
            }).ToListAsync();

        var today = TableService.VietnamNow.Date;
        var result = new List<ReservationDto>();
        foreach (var b in bookings)
        {
            var suggestions = b.Tables.Count == 0 ? await SuggestAsync(b.Id, b.GioDen, b.GioKetThucDuKien, b.Guests, b.YeuCauVip) : [];
            result.Add(new ReservationDto(b.Id, b.MaDatBan, b.HoTenLienHe, b.SoDienThoaiLienHe, b.GioDen, b.Guests,
                b.TrangThai.ToString(), MobileApi.BookingStatusText(b.TrangThai),
                b.Tables.Count == 0 ? null : string.Join(", ", b.Tables.Order()), b.YeuCau, b.YeuCauVip,
                MobileApi.Version(b.Version), CoTheXepBan: true,
                CoTheNhanBan: b.Tables.Count > 0 && b.GioDen.ToOffset(TimeSpan.FromHours(7)).Date == today
                    && b.YeuCauHuy is null && b.ThoiDiemBaoChuyenKhoan is null,
                suggestions));
        }
        return result;
    }

    // Gợi ý bàn đủ chỗ, đúng loại VIP và không trùng lịch; server kiểm tra lại khi xếp.
    private async Task<List<SuggestedTableDto>> SuggestAsync(int bookingId, DateTimeOffset from, DateTimeOffset until, int guests, bool vip)
    {
        var taken = await tables.OverlappingBookings(from, until, bookingId).Select(x => x.BanAnId).Distinct().ToListAsync();
        taken.AddRange(await tables.PhysicalBusyTables(from, bookingId));
        return await Db.BanAn.AsNoTracking()
            .Where(x => x.KhuVuc.DangSuDung && x.TrangThai != TrangThaiBan.NgungSuDung && x.SoChoNgoi >= guests
                && x.KhuVuc.LaPhongVip == vip && !taken.Contains(x.Id))
            .OrderBy(x => x.SoChoNgoi).ThenBy(x => x.KhuVuc.Tang).ThenBy(x => x.MaBan).Take(5)
            .Select(x => new SuggestedTableDto(x.Id, x.MaBan, x.KhuVuc.TenKhuVuc, x.SoChoNgoi)).ToListAsync();
    }

    // Xác nhận yêu cầu bằng cách xếp một bàn (Chờ xác nhận → Đã xác nhận hoặc Chờ cọc).
    [HttpPost("reservations/{id:int}/assign"), Authorize(Roles = MobileApi.ReceptionRoles)]
    public async Task<IActionResult> Assign(int id, AssignTableRequest request)
    {
        var error = await tables.Assign(id, [request.BanAnId], request.RowVersion, await CurrentStaffIdAsync());
        if (error is not null) return Fail(error, StatusCodes.Status409Conflict);
        var status = await Db.DatBan.Where(x => x.Id == id).Select(x => x.TrangThai).SingleAsync();
        return Ok(new MessageDto(status == TrangThaiDatBan.ChoCoc
            ? "Đã xếp bàn. Lịch cần cọc nên đang chờ khách thanh toán cọc."
            : "Đã xác nhận đặt bàn và xếp bàn cho khách."));
    }

    [HttpPost("reservations/{id:int}/checkin"), Authorize(Roles = MobileApi.ReceptionRoles)]
    public async Task<IActionResult> CheckIn(int id, TableActionRequest request)
    {
        var error = await tables.CheckIn(id, request.RowVersion, await CurrentStaffIdAsync());
        return error is null ? Ok(new MessageDto("Đã nhận bàn, bàn chuyển sang đang phục vụ.")) : Fail(error, StatusCodes.Status409Conflict);
    }
}
