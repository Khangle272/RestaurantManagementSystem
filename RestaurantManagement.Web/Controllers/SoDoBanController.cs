using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Security;
using RestaurantManagement.Web.Services;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin + "," + AppRoles.TiepTan + "," + AppRoles.BoiBan)]
public class SoDoBanController(RestaurantDbContext db, TableService tables) : ManagementControllerBase(db)
{
    [HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> KhungGio(DateOnly ngay, int soNguoi = 2, int? maKhuVuc = null)
    {
        if (!ModelState.IsValid) return BadRequest(new { message = "Ngày hoặc số khách không hợp lệ." });
        var result = await tables.AvailableSlots(ngay, soNguoi, maKhuVuc);
        return result.Error is { } error ? BadRequest(new { message = error }) : Json(new { slots = result.Slots });
    }
    [HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Index(int? khuVucId, int? tang, int? datBanId, bool fragment = false,
        int? preferredTableId = null, DateOnly? ngay = null, TimeOnly? gio = null, string? ca = null, bool dialog = false)
    {
        if (datBanId.HasValue && !User.IsInRole(AppRoles.Admin) && !User.IsInRole(AppRoles.TiepTan)) return Forbid();
        var booking = datBanId is int id ? await Db.DatBan.SingleOrDefaultAsync(x => x.Id == id) : null;
        if (datBanId.HasValue && booking is null) return NotFound();
        if (dialog && !Request.Query.ContainsKey("khuVucId")) khuVucId ??= booking?.KhuVucUuTienId;
        if (!ModelState.IsValid) return BadRequest("Ngày hoặc giờ xem lịch không hợp lệ.");
        var local = (booking?.GioDen ?? DateTimeOffset.UtcNow).ToOffset(TimeSpan.FromHours(7));
        var day = booking is null ? ngay ?? DateOnly.FromDateTime(local.DateTime) : DateOnly.FromDateTime(local.DateTime);
        var time = booking is null ? gio ?? TimeOnly.FromDateTime(local.DateTime) : TimeOnly.FromDateTime(local.DateTime);
        if (day == DateOnly.MinValue || day == DateOnly.MaxValue) return BadRequest("Ngày xem lịch ngoài phạm vi hỗ trợ.");
        var slot = ReservationSchedule.Slots.FirstOrDefault(x => x.Id == ca);
        if (!string.IsNullOrEmpty(ca) && ca != "now" && slot is null) return BadRequest("Ca xem lịch không hợp lệ.");
        if (booking is null && slot is null && !gio.HasValue && day != DateOnly.FromDateTime(TableService.VietnamNow))
            slot = ReservationSchedule.Slots[0];
        var from = booking?.GioDen ?? (slot is not null ? TableService.VietnamTime(slot.Arrival(day))
            : TableService.VietnamTime(day.ToDateTime(time)));
        var until = booking?.GioKetThucDuKien ?? (slot is not null
            ? TableService.VietnamTime(day.ToDateTime(slot.End)) : gio.HasValue ? from.AddHours(TableService.MaxDiningHours) : from.AddTicks(1));
        var checkUntil = booking is not null ? TableService.BlockingUntil(booking)
            : slot is not null || gio.HasValue ? (from.AddHours(TableService.MaxDiningHours) > until ? from.AddHours(TableService.MaxDiningHours) : until)
            : until;
        var isLive = (booking is null && slot is null && !gio.HasValue)
            || (from <= DateTimeOffset.UtcNow && until > DateTimeOffset.UtcNow);
        var bookingSlot = booking is null ? null : ReservationSchedule.Match(local.DateTime);
        if (bookingSlot is not null && ReservationSchedule.EndOf(local.DateTime) != booking!.GioKetThucDuKien) bookingSlot = null;
        var areas = await Db.KhuVuc.AsNoTracking().OrderBy(x => x.Tang).ThenBy(x => x.TenKhuVuc).ToListAsync();
        var items = await Db.BanAn.Include(x => x.KhuVuc)
            .Where(x => (!khuVucId.HasValue || x.KhuVucId == khuVucId) && (!tang.HasValue || x.KhuVuc.Tang == tang))
            .OrderBy(x => x.KhuVuc.Tang).ThenBy(x => x.KhuVuc.TenKhuVuc).ThenBy(x => x.MaBan).ToListAsync();
        var tableIds = items.Select(x => x.Id).ToArray();
        var links = await tables.BlockingBookings().Include(x => x.DatBan).Where(x => tableIds.Contains(x.BanAnId))
            .ToListAsync();
        var model = new TableBoardViewModel
        {
            Areas = areas, AreaId = khuVucId, Floor = tang, Booking = booking, PreferredTableId = preferredTableId,
            BookingVersion = booking is null ? "" : VersionOf(booking), From = from, Until = until, CheckUntil = checkUntil,
            ScheduleDay = day, ScheduleTime = TimeOnly.FromDateTime(from.ToOffset(TimeSpan.FromHours(7)).DateTime),
            SlotId = booking is not null ? bookingSlot?.Id ?? "legacy" : slot?.Id ?? (gio.HasValue ? "legacy" : "now"),
            IsLive = isLive,
            Tables = items.Select(table =>
            {
                var occupant = isLive && table.TrangThai == TrangThaiBan.DangPhucVu
                    ? links.Where(x => x.BanAnId == table.Id && x.DatBan.TrangThai == TrangThaiDatBan.DaNhanBan)
                        .OrderByDescending(x => x.DatBan.ThoiDiemNhanBan).Select(x => x.DatBan).FirstOrDefault() : null;
                return new TableCard { Table = table, Version = VersionOf(table), Occupant = occupant,
                    OccupantVersion = occupant is null ? "" : VersionOf(occupant),
                    ScheduledBookings = links.Where(x => x.BanAnId == table.Id && x.DatBan.GioDen < checkUntil
                        && TableService.BlockingUntil(x.DatBan) > from).Select(x => x.DatBan).OrderBy(x => x.GioDen).ToList(),
                    Conflicts = links.Where(x => x.BanAnId == table.Id && x.DatBanId != datBanId && x.DatBan.GioDen < checkUntil
                        && TableService.BlockingUntil(x.DatBan) > from).Select(x => x.DatBan).OrderBy(x => x.GioDen).ToList() };
            }).ToList()
        };
        foreach (var card in model.Tables)
        {
            card.UnavailableReason = !card.Table.KhuVuc.DangSuDung || card.Table.TrangThai == TrangThaiBan.NgungSuDung ? "Bàn hoặc khu vực ngừng sử dụng."
                : card.Conflicts.FirstOrDefault() is { } conflict ? $"Đã có lịch {conflict.GioDen.ToOffset(TimeSpan.FromHours(7)):HH:mm}–{conflict.GioKetThucDuKien.ToOffset(TimeSpan.FromHours(7)):HH:mm}, chỉ mở lại sau khi phục vụ và dọn xong (tối đa {TableService.BlockingUntil(conflict).ToOffset(TimeSpan.FromHours(7)):HH:mm}). Chọn bàn khác."
                : booking is not null && booking.YeuCauVip != card.Table.KhuVuc.LaPhongVip ? "Không đúng loại khu vực khách yêu cầu."
                : !TableService.ReadyForSchedule(card.Table, from) ? "Bàn đang phục vụ hoặc chưa dọn trong ngày này. Chờ dọn xong rồi kiểm tra lại." : null;
            card.CanChoose = model.CanAssign && card.UnavailableReason is null;
            card.IsSelected = card.CanChoose && (links.Any(x => x.BanAnId == card.Table.Id && x.DatBanId == datBanId)
                || preferredTableId == card.Table.Id);
        }
        if (model.CanAssign)
        {
            var suggestion = TableService.PickTables(model.Tables.Where(x => x.CanChoose
                && (!booking!.KhuVucUuTienId.HasValue || x.Table.KhuVucId == booking.KhuVucUuTienId)).Select(x => x.Table),
                booking!.SoNguoiLon + booking.SoTreEm);
            foreach (var card in model.Tables) card.IsSuggested = suggestion?.Any(x => x.Id == card.Table.Id) == true;
        }
        if (!fragment && (User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.TiepTan)))
            model.Requests = await Db.DatBan.AsNoTracking().Where(x => x.TrangThai == TrangThaiDatBan.ChoXacNhan
                && x.GioKetThucDuKien > DateTimeOffset.UtcNow).OrderBy(x => x.GioDen).Take(30).ToListAsync();
        if (dialog) return booking is null ? BadRequest("Thiếu yêu cầu cần xếp bàn.") : PartialView("_Assignment", model);
        return fragment ? PartialView("_Board", model) : View(model);
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.BoiBan), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TrangThai(int id, string? rowVersion, bool clean)
    {
        var error = await tables.ChangeTable(id, rowVersion, clean);
        TempData[error is null ? "SuccessMessage" : "ErrorMessage"] = error ?? (clean ? "Đã dọn xong, bàn sẵn sàng." : "Đã kết thúc phục vụ. Các bàn của lượt khách chuyển sang cần dọn.");
        return RedirectToAction(nameof(Index));
    }
}
