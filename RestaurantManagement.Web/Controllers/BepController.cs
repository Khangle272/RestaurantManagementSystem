using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Security;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin + "," + AppRoles.Bep)]
public class BepController(RestaurantDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var rows = await db.ChiTietHoaDon.AsNoTracking()
            .Include(x => x.HoaDon).ThenInclude(x => x.DatBan).ThenInclude(x => x!.Ban).ThenInclude(x => x.BanAn)
            .Where(x => x.TrangThai == TrangThaiCheBien.ChoCheBien || x.TrangThai == TrangThaiCheBien.DangCheBien)
            .OrderBy(x => x.HoaDon.ThoiDiemLap).ThenBy(x => x.Id).Take(100)
            .ToListAsync();
        var items = rows.Select(x => new BepRow
            {
                Id = x.Id, Mon = x.TenMonLucBan, SoLuong = x.SoLuong,
                GhiChu = x.YeuCauCheBien, TrangThai = x.TrangThai,
                MaHoaDon = x.HoaDon.MaHoaDon,
                Ban = x.HoaDon.DatBan == null ? "Mang đi" : string.Join(", ", x.HoaDon.DatBan.Ban.Select(b => b.BanAn.MaBan))
            }).ToList();
        return View(items);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CapNhat(int id, TrangThaiCheBien trangThai)
    {
        var item = await db.ChiTietHoaDon.SingleOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();
        var valid = item.TrangThai == TrangThaiCheBien.ChoCheBien && trangThai == TrangThaiCheBien.DangCheBien
            || item.TrangThai == TrangThaiCheBien.DangCheBien && trangThai == TrangThaiCheBien.SanSang;
        if (!valid) return BadRequest();
        item.TrangThai = trangThai;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { TempData["Error"] = "Món đã được người khác cập nhật. Vui lòng tải lại."; }
        return RedirectToAction(nameof(Index));
    }
}

public class BepRow
{
    public int Id { get; set; }
    public string Mon { get; set; } = "";
    public int SoLuong { get; set; }
    public string? GhiChu { get; set; }
    public TrangThaiCheBien TrangThai { get; set; }
    public string MaHoaDon { get; set; } = "";
    public string Ban { get; set; } = "";
}
