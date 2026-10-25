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
    public async Task<IActionResult> KhungGio(DateOnly ngay, int soNguoi = 2, int? maKhuVuc = null, DateTime? gioDen = null)
    {
        if (!ModelState.IsValid) return BadRequest(new { message = "Ngày hoặc số khách không hợp lệ." });
        var result = await tables.AvailableSlots(ngay, soNguoi, maKhuVuc, -15, gioDen);
        if (result.Error is { } error) return BadRequest(new { message = error });
        var selected = gioDen.HasValue ? await tables.CheckAvailability(gioDen.Value, soNguoi, maKhuVuc, -15)
            : (Available: false, Message: "Chọn ca gợi ý hoặc nhập giờ nhận khách thực tế.");
        return Json(new { slots = result.Slots, available = selected.Available, message = selected.Message });
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
        var checkUntil = ReservationSchedule.Window(from).Until;
        var isLive = booking is null && slot is null && !gio.HasValue && day == DateOnly.FromDateTime(TableService.VietnamNow);
        var bookingSlot = booking is null ? null : ReservationSchedule.Match(local.DateTime);
        if (bookingSlot is not null && ReservationSchedule.EndOf(local.DateTime) != booking!.GioKetThucDuKien) bookingSlot = null;
        var areas = await Db.KhuVuc.AsNoTracking().OrderBy(x => x.Tang).ThenBy(x => x.TenKhuVuc).ToListAsync();
        var items = await Db.BanAn.Include(x => x.KhuVuc)
            .Where(x => (!khuVucId.HasValue || x.KhuVucId == khuVucId) && (!tang.HasValue || x.KhuVuc.Tang == tang))
            .OrderBy(x => x.KhuVuc.Tang).ThenBy(x => x.KhuVuc.TenKhuVuc).ThenBy(x => x.MaBan).ToListAsync();
        var tableIds = items.Select(x => x.Id).ToArray();
        var dayStart = TableService.VietnamTime(day.ToDateTime(TimeOnly.MinValue)); var dayEnd = dayStart.AddDays(1);
        var links = await tables.BlockingBookings().Include(x => x.DatBan).Where(x => tableIds.Contains(x.BanAnId)
            && x.DatBan.GioDen >= dayStart && x.DatBan.GioDen < dayEnd)
            .ToListAsync();
        var conflicts = await tables.OverlappingBookings(from, until, datBanId).Where(x => tableIds.Contains(x.BanAnId)).Select(x => new { x.BanAnId, x.DatBanId }).ToListAsync();
        var physicalBusy = await tables.PhysicalBusyTables(from, datBanId);
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
                    ? links.Where(x => x.BanAnId == table.Id && x.DatBan.TrangThai == TrangThaiDatBan.DaNhanBan
                        && x.DatBan.ThoiDiemKetThuc == null && x.DatBan.GioDen >= dayStart && x.DatBan.GioDen < dayEnd)
                        .OrderByDescending(x => x.DatBan.ThoiDiemNhanBan).Select(x => x.DatBan).FirstOrDefault() : null;
                return new TableCard { Table = table, DisplayStatus = table.TrangThai == TrangThaiBan.NgungSuDung || physicalBusy.Contains(table.Id) ? table.TrangThai : TrangThaiBan.SanSang, Version = VersionOf(table), Occupant = occupant,
                    NeedsInspection = isLive && occupant is null && table.TrangThai is TrangThaiBan.CanDon or TrangThaiBan.DangPhucVu,
                    Attention = occupant is not null && TableService.ServingDeadline(occupant) <= DateTimeOffset.UtcNow
                        ? "Đã phục vụ hơn 3 tiếng. Kiểm tra khách còn dùng bữa hay nhân viên quên xác nhận hoàn tất; hệ thống không tự đóng bàn." : null,
                    OccupantVersion = occupant is null ? "" : VersionOf(occupant),
                    ScheduledBookings = links.Where(x => x.BanAnId == table.Id).Select(x => x.DatBan).OrderBy(x => x.GioDen).ToList(),
                    Conflicts = links.Where(x => x.BanAnId == table.Id && conflicts.Any(c => c.BanAnId == table.Id && c.DatBanId == x.DatBanId)).Select(x => x.DatBan).OrderBy(x => x.GioDen).ToList() };
            }).ToList()
        };
        foreach (var card in model.Tables)
        {
            card.UnavailableReason = !card.Table.KhuVuc.DangSuDung || card.Table.TrangThai == TrangThaiBan.NgungSuDung ? "Bàn hoặc khu vực ngừng sử dụng."
                : card.Conflicts.FirstOrDefault() is { } conflict ? $"Bàn đã có lượt lúc {conflict.GioDen.ToOffset(TimeSpan.FromHours(7)):HH:mm} trong ca này. Mỗi bàn nhận một lượt mỗi ca; chọn bàn khác."
                : booking is not null && booking.YeuCauVip != card.Table.KhuVuc.LaPhongVip ? "Không đúng loại khu vực khách yêu cầu."
                : booking is not null && card.Table.SoChoNgoi < booking.SoNguoiLon + booking.SoTreEm ? "Bàn không đủ chỗ cho số khách; chọn một bàn lớn hơn."
                : physicalBusy.Contains(card.Table.Id) ? "Bàn còn khách hoặc chưa được nhân viên xác nhận hoàn tất. Kiểm tra bàn sẵn sàng trước khi nhận lượt mới." : null;
            card.CanChoose = model.CanAssign && card.UnavailableReason is null;
            card.IsSelected = card.CanChoose && (links.Any(x => x.BanAnId == card.Table.Id && x.DatBanId == datBanId)
                || preferredTableId == card.Table.Id);
        }
        if (isLive)
        {
            var pending = await Db.DatBan.AsSplitQuery().Include(x => x.Ban).ThenInclude(x => x.BanAn).ThenInclude(x => x.KhuVuc)
                .Include(x => x.HoaDon).Where(x => x.TrangThai == TrangThaiDatBan.DaNhanBan && x.ThoiDiemKetThuc == null
                    && x.GioDen < dayStart && x.Ban.Any(b => tableIds.Contains(b.BanAnId)))
                .OrderBy(x => x.GioDen).Take(50).ToListAsync();
            model.PendingServices = pending.Select(x => new ServiceReview(x, VersionOf(x))).ToList();
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
        TempData[error is null ? "SuccessMessage" : "ErrorMessage"] = error ?? "Đã xác nhận hoàn tất, bàn sẵn sàng. Không cần bước dọn riêng.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.BoiBan), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> HoanTatLuot(int id, string? rowVersion)
    {
        var error = await tables.CompleteService(id, rowVersion);
        TempData[error is null ? "SuccessMessage" : "ErrorMessage"] = error ?? "Đã hoàn tất lượt cũ; giữ nguyên các bàn đã có lượt khách mới.";
        return RedirectToAction(nameof(Index));
    }
}
