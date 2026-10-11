using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Security;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class VoucherController(RestaurantDbContext db) : Controller
{
    private const int PageSize = 12;

    public async Task<IActionResult> Index(string? search, int? khuyenMaiId, string? statusFilter, int page = 1)
    {
        var query = db.Voucher.AsNoTracking()
            .Include(x => x.KhuyenMai)
            .Include(x => x.KhachHang)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(x => x.Ma.Contains(s) || x.KhuyenMai.TenChuongTrinh.Contains(s));
        }

        if (khuyenMaiId.HasValue && khuyenMaiId.Value > 0)
        {
            query = query.Where(x => x.KhuyenMaiId == khuyenMaiId.Value);
        }

        var now = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = statusFilter switch
            {
                "ConHieuLuc" => query.Where(x => x.DangSuDung && x.KhuyenMai.DangSuDung && x.KhuyenMai.KetThuc >= now),
                "HetHan" => query.Where(x => x.KhuyenMai.KetThuc < now),
                "NgungSuDung" => query.Where(x => !x.DangSuDung),
                _ => query
            };
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
        if (totalPages < 1) totalPages = 1;
        if (page < 1) page = 1;
        if (page > totalPages) page = totalPages;

        var voucherIds = await query
            .OrderByDescending(x => x.DangSuDung)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(x => x.Id)
            .ToListAsync();

        var vouchers = await query
            .Where(x => voucherIds.Contains(x.Id))
            .OrderByDescending(x => x.DangSuDung)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        // Đếm số lượt đã dùng trên hóa đơn
        var usageStats = await db.HoaDon.AsNoTracking()
            .Where(h => h.VoucherId.HasValue && voucherIds.Contains(h.VoucherId.Value) && h.TrangThai != TrangThaiHoaDon.DaHuy)
            .GroupBy(h => h.VoucherId!.Value)
            .Select(g => new { VoucherId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.VoucherId, x => x.Count);

        var items = vouchers.Select(x => new VoucherItemViewModel
        {
            Id = x.Id,
            Ma = x.Ma,
            KhuyenMaiId = x.KhuyenMaiId,
            TenKhuyenMai = x.KhuyenMai.TenChuongTrinh,
            UuDaiMota = x.KhuyenMai.KieuGiam == KieuGiam.PhanTram
                ? $"Giảm {x.KhuyenMai.GiaTri:0.##}%{(x.KhuyenMai.MucGiamToiDa.HasValue ? $" (tối đa {x.KhuyenMai.MucGiamToiDa.Value:N0} đ)" : "")}"
                : $"Giảm {x.KhuyenMai.GiaTri:N0} đ",
            KhachHangId = x.KhachHangId,
            TenKhachHang = x.KhachHang?.HoTen,
            GioiHanTongLuot = x.GioiHanTongLuot,
            SoLuotDaDung = usageStats.TryGetValue(x.Id, out var count) ? count : 0,
            DangSuDung = x.DangSuDung,
            BatDau = x.KhuyenMai.BatDau,
            KetThuc = x.KhuyenMai.KetThuc
        }).ToList();

        var promoOptions = await db.KhuyenMai.AsNoTracking()
            .Where(x => x.DangSuDung)
            .OrderByDescending(x => x.BatDau)
            .Select(x => new KhuyenMaiOptionItem { Id = x.Id, TenChuongTrinh = x.TenChuongTrinh })
            .ToListAsync();

        return View(new VoucherListViewModel
        {
            Search = search,
            KhuyenMaiId = khuyenMaiId,
            StatusFilter = statusFilter,
            Page = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            Items = items,
            PromotionOptions = promoOptions
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? khuyenMaiId)
    {
        var promoOptions = await db.KhuyenMai.AsNoTracking()
            .Where(x => x.DangSuDung && x.CanVoucher)
            .OrderByDescending(x => x.BatDau)
            .Select(x => new KhuyenMaiOptionItem { Id = x.Id, TenChuongTrinh = x.TenChuongTrinh })
            .ToListAsync();

        var model = new VoucherFormViewModel
        {
            KhuyenMaiId = khuyenMaiId ?? promoOptions.FirstOrDefault()?.Id ?? 0,
            GioiHanTongLuot = 100,
            Ma = GenerateVoucherCode(),
            PromotionOptions = promoOptions
        };

        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VoucherFormViewModel model)
    {
        var code = model.Ma?.Trim().ToUpper() ?? "";
        if (string.IsNullOrWhiteSpace(code))
        {
            ModelState.AddModelError(nameof(model.Ma), "Vui lòng nhập mã Voucher.");
        }
        else if (await db.Voucher.AnyAsync(x => x.Ma.ToUpper() == code))
        {
            ModelState.AddModelError(nameof(model.Ma), "Mã Voucher này đã tồn tại trong hệ thống.");
        }

        var promo = await db.KhuyenMai.FindAsync(model.KhuyenMaiId);
        if (promo == null)
        {
            ModelState.AddModelError(nameof(model.KhuyenMaiId), "Chương trình khuyến mãi không hợp lệ.");
        }

        if (!ModelState.IsValid)
        {
            model.PromotionOptions = await GetPromotionOptionsAsync();
            return View("Form", model);
        }

        var voucher = new Voucher
        {
            Ma = code,
            KhuyenMaiId = model.KhuyenMaiId,
            KhachHangId = model.KhachHangId,
            GioiHanTongLuot = model.GioiHanTongLuot,
            DangSuDung = model.DangSuDung
        };

        db.Voucher.Add(voucher);
        await db.SaveChangesAsync();

        TempData["Success"] = $"Đã tạo mã Voucher '{voucher.Ma}' thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var voucher = await db.Voucher.FindAsync(id);
        if (voucher == null) return NotFound();

        var model = new VoucherFormViewModel
        {
            Id = voucher.Id,
            Ma = voucher.Ma,
            KhuyenMaiId = voucher.KhuyenMaiId,
            KhachHangId = voucher.KhachHangId,
            GioiHanTongLuot = voucher.GioiHanTongLuot,
            DangSuDung = voucher.DangSuDung,
            PromotionOptions = await GetPromotionOptionsAsync()
        };

        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VoucherFormViewModel model)
    {
        if (id != model.Id) return NotFound();

        var voucher = await db.Voucher.FindAsync(id);
        if (voucher == null) return NotFound();

        var code = model.Ma?.Trim().ToUpper() ?? "";
        if (string.IsNullOrWhiteSpace(code))
        {
            ModelState.AddModelError(nameof(model.Ma), "Vui lòng nhập mã Voucher.");
        }
        else if (await db.Voucher.AnyAsync(x => x.Id != id && x.Ma.ToUpper() == code))
        {
            ModelState.AddModelError(nameof(model.Ma), "Mã Voucher này đã tồn tại trong hệ thống.");
        }

        if (!ModelState.IsValid)
        {
            model.PromotionOptions = await GetPromotionOptionsAsync();
            return View("Form", model);
        }

        voucher.Ma = code;
        voucher.KhuyenMaiId = model.KhuyenMaiId;
        voucher.KhachHangId = model.KhachHangId;
        voucher.GioiHanTongLuot = model.GioiHanTongLuot;
        voucher.DangSuDung = model.DangSuDung;

        await db.SaveChangesAsync();

        TempData["Success"] = $"Đã cập nhật Voucher '{voucher.Ma}'.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var voucher = await db.Voucher.FindAsync(id);
        if (voucher == null) return NotFound();

        voucher.DangSuDung = !voucher.DangSuDung;
        await db.SaveChangesAsync();

        TempData["Success"] = $"Đã {(voucher.DangSuDung ? "kích hoạt" : "vô hiệu hóa")} Voucher '{voucher.Ma}'.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var voucher = await db.Voucher.FindAsync(id);
        if (voucher == null) return NotFound();

        var usedInInvoices = await db.HoaDon.AnyAsync(h => h.VoucherId == id);
        if (usedInInvoices)
        {
            voucher.DangSuDung = false;
            await db.SaveChangesAsync();
            TempData["Success"] = $"Voucher '{voucher.Ma}' đã có lịch sử áp dụng nên được chuyển sang trạng thái Ngừng sử dụng.";
            return RedirectToAction(nameof(Index));
        }

        db.Voucher.Remove(voucher);
        await db.SaveChangesAsync();

        TempData["Success"] = $"Đã xóa Voucher '{voucher.Ma}'.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult GenerateCode()
    {
        return Json(new { code = GenerateVoucherCode() });
    }

    private static string GenerateVoucherCode()
    {
        var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        var code = new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());
        return $"SEN-{code}";
    }

    private async Task<List<KhuyenMaiOptionItem>> GetPromotionOptionsAsync()
    {
        return await db.KhuyenMai.AsNoTracking()
            .Where(x => x.DangSuDung)
            .OrderByDescending(x => x.BatDau)
            .Select(x => new KhuyenMaiOptionItem { Id = x.Id, TenChuongTrinh = x.TenChuongTrinh })
            .ToListAsync();
    }
}
