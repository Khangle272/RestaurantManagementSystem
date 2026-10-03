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
    public async Task<IActionResult> Index(int? khuVucId, int? tang, int? datBanId, bool fragment = false, int? preferredTableId = null)
    {
        if (datBanId.HasValue && !User.IsInRole(AppRoles.Admin) && !User.IsInRole(AppRoles.TiepTan)) return Forbid();
        var booking = datBanId is int id ? await Db.DatBan.SingleOrDefaultAsync(x => x.Id == id) : null;
        if (datBanId.HasValue && booking is null) return NotFound();
        var from = booking?.GioDen ?? DateTimeOffset.UtcNow;
        var until = booking?.GioKetThucDuKien ?? from.AddHours(2);
        var areas = await Db.KhuVuc.AsNoTracking().OrderBy(x => x.Tang).ThenBy(x => x.TenKhuVuc).ToListAsync();
        var items = await Db.BanAn.Include(x => x.KhuVuc)
            .Where(x => (!khuVucId.HasValue || x.KhuVucId == khuVucId) && (!tang.HasValue || x.KhuVuc.Tang == tang))
            .OrderBy(x => x.KhuVuc.Tang).ThenBy(x => x.KhuVuc.TenKhuVuc).ThenBy(x => x.MaBan).ToListAsync();
        var tableIds = items.Select(x => x.Id).ToArray();
        var links = await Db.ChiTietDatBan.Include(x => x.DatBan).Where(x => tableIds.Contains(x.BanAnId)
            && (x.DatBan.TrangThai == TrangThaiDatBan.DaXacNhan || x.DatBan.TrangThai == TrangThaiDatBan.ChoCoc
                || (x.DatBan.TrangThai == TrangThaiDatBan.DaNhanBan && x.DatBan.ThoiDiemKetThuc == null)))
            .ToListAsync();
        var model = new TableBoardViewModel
        {
            Areas = areas, AreaId = khuVucId, Floor = tang, Booking = booking, PreferredTableId = preferredTableId,
            BookingVersion = booking is null ? "" : VersionOf(booking), From = from, Until = until,
            Tables = items.Select(table =>
            {
                var occupant = table.TrangThai == TrangThaiBan.DangPhucVu
                    ? links.Where(x => x.BanAnId == table.Id && x.DatBan.TrangThai == TrangThaiDatBan.DaNhanBan)
                        .OrderByDescending(x => x.DatBan.ThoiDiemNhanBan).Select(x => x.DatBan).FirstOrDefault() : null;
                return new TableCard { Table = table, Version = VersionOf(table), Occupant = occupant,
                    OccupantVersion = occupant is null ? "" : VersionOf(occupant),
                    Conflicts = links.Where(x => x.BanAnId == table.Id && x.DatBanId != datBanId && x.DatBan.GioDen < until
                        && x.DatBan.GioKetThucDuKien > from).Select(x => x.DatBan).OrderBy(x => x.GioDen).ToList() };
            }).ToList()
        };
        foreach (var card in model.Tables)
        {
            card.CanChoose = model.CanAssign && card.Conflicts.Count == 0 && card.Table.KhuVuc.DangSuDung
                && card.Table.TrangThai != TrangThaiBan.NgungSuDung && (!booking!.YeuCauVip || card.Table.KhuVuc.LaPhongVip);
            card.IsSelected = card.CanChoose && (links.Any(x => x.BanAnId == card.Table.Id && x.DatBanId == datBanId)
                || preferredTableId == card.Table.Id);
        }
        if (model.CanAssign)
        {
            var suggestion = model.Tables.Where(x => x.CanChoose && x.Table.SoChoNgoi >= booking!.SoNguoiLon + booking.SoTreEm
                && (from > DateTimeOffset.UtcNow.AddMinutes(30) || x.Table.TrangThai == TrangThaiBan.SanSang))
                .OrderBy(x => booking!.KhuVucUuTienId.HasValue && x.Table.KhuVucId != booking.KhuVucUuTienId.Value)
                .ThenBy(x => x.Table.SoChoNgoi).ThenBy(x => x.Table.MaBan).FirstOrDefault();
            if (suggestion is not null) suggestion.IsSuggested = true;
        }
        if (!fragment && (User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.TiepTan)))
            model.Requests = await Db.DatBan.AsNoTracking().Where(x => x.TrangThai == TrangThaiDatBan.ChoXacNhan
                && x.GioKetThucDuKien > DateTimeOffset.UtcNow).OrderBy(x => x.GioDen).Take(30).ToListAsync();
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
