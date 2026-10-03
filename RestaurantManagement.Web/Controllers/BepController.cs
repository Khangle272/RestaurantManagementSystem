using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Security;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin + "," + AppRoles.Bep + "," + AppRoles.BoiBan)]
public class BepController(RestaurantDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? filter = "active")
    {
        ViewData["ActiveMenu"] = "Bep";
        ViewBag.Filter = filter;

        var query = db.ChiTietHoaDon.AsNoTracking()
            .Include(x => x.MonAnSize)
            .Include(x => x.HoaDon).ThenInclude(x => x.DatBan).ThenInclude(x => x!.Ban).ThenInclude(x => x.BanAn)
            .Where(x => x.HoaDon.TrangThai != TrangThaiHoaDon.DaHuy);

        if (filter == "ready")
        {
            query = query.Where(x => x.TrangThai == TrangThaiCheBien.SanSang);
        }
        else if (filter == "all")
        {
            query = query.Where(x => x.TrangThai == TrangThaiCheBien.ChoCheBien
                || x.TrangThai == TrangThaiCheBien.DangCheBien
                || x.TrangThai == TrangThaiCheBien.SanSang);
        }
        else
        {
            // Default active in kitchen: ChoCheBien & DangCheBien
            query = query.Where(x => x.TrangThai == TrangThaiCheBien.ChoCheBien || x.TrangThai == TrangThaiCheBien.DangCheBien);
        }

        var rows = await query
            .OrderBy(x => x.ThoiDiemGoi).ThenBy(x => x.Id).Take(150)
            .ToListAsync();

        var items = rows.Select(x =>
        {
            var banName = x.HoaDon.LoaiDonHang switch
            {
                LoaiDonHang.TaiBan => x.HoaDon.DatBan == null
                    ? "Tại bàn"
                    : string.Join(", ", x.HoaDon.DatBan.Ban.Select(b => b.BanAn.MaBan)),
                LoaiDonHang.MangDi => "Mang đi",
                LoaiDonHang.GiaoHang => string.IsNullOrWhiteSpace(x.HoaDon.TenNguoiNhan) ? "Giao hàng" : $"Giao hàng · {x.HoaDon.TenNguoiNhan}",
                _ => "—"
            };

            return new BepRow
            {
                Id = x.Id,
                Mon = x.TenMonLucBan,
                Size = !string.IsNullOrEmpty(x.TenSizeLucBan) ? x.TenSizeLucBan : (x.MonAnSize?.TenSize ?? "Mặc định"),
                SoLuong = x.SoLuong,
                GhiChu = x.YeuCauCheBien,
                TrangThai = x.TrangThai,
                MaHoaDon = x.HoaDon.MaHoaDon,
                Ban = banName,
                ThoiDiemGoi = x.ThoiDiemGoi
            };
        }).ToList();

        return View(items);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CapNhat(int id, TrangThaiCheBien trangThai, string? filter = null)
    {
        var item = await db.ChiTietHoaDon.Include(x => x.HoaDon).SingleOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();
        if (item.HoaDon.TrangThai == TrangThaiHoaDon.DaHuy) return BadRequest();

        var valid = (item.TrangThai == TrangThaiCheBien.ChoCheBien && trangThai == TrangThaiCheBien.DangCheBien)
            || (item.TrangThai == TrangThaiCheBien.DangCheBien && trangThai == TrangThaiCheBien.SanSang)
            || (item.TrangThai == TrangThaiCheBien.SanSang && trangThai == TrangThaiCheBien.DaPhucVu);

        if (!valid) return BadRequest();
        item.TrangThai = trangThai;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { TempData["Error"] = "Món đã được người khác cập nhật. Vui lòng tải lại."; }
        return RedirectToAction(nameof(Index), new { filter });
    }
}

public class BepRow
{
    public int Id { get; set; }
    public string Mon { get; set; } = "";
    public string Size { get; set; } = "Mặc định";
    public int SoLuong { get; set; }
    public string? GhiChu { get; set; }
    public TrangThaiCheBien TrangThai { get; set; }
    public string MaHoaDon { get; set; } = "";
    public string Ban { get; set; } = "";
    public DateTimeOffset ThoiDiemGoi { get; set; }
}
