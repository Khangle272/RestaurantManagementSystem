using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;
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

        await ValidateVoucherAsync(model, null);

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
        if (!await SaveVoucherAsync(model))
        {
            model.PromotionOptions = await GetPromotionOptionsAsync(model.KhuyenMaiId);
            return View("Form", model);
        }

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
            RowVersion = Convert.ToBase64String(db.Entry(voucher).Property<byte[]>("RowVersion").CurrentValue ?? []),
            DaDuocSuDung = await db.HoaDon.AnyAsync(x => x.VoucherId == voucher.Id),
            Ma = voucher.Ma,
            KhuyenMaiId = voucher.KhuyenMaiId,
            KhachHangId = voucher.KhachHangId,
            GioiHanTongLuot = voucher.GioiHanTongLuot,
            DangSuDung = voucher.DangSuDung,
            PromotionOptions = await GetPromotionOptionsAsync(voucher.KhuyenMaiId)
        };

        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VoucherFormViewModel model)
    {
        if (id != model.Id) return NotFound();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
        var voucher = await db.Voucher.FromSqlInterpolated($"SELECT * FROM [Voucher] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}").SingleOrDefaultAsync();
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

        await ValidateVoucherAsync(model, voucher);
        var expected = new byte[8];
        if (model.RowVersion is null || !Convert.TryFromBase64String(model.RowVersion, expected, out var length) || length != 8
            || !expected.SequenceEqual(db.Entry(voucher).Property<byte[]>("RowVersion").CurrentValue ?? []))
            ModelState.AddModelError("", "Voucher đã thay đổi. Vui lòng tải lại trang trước khi cập nhật.");
        if (!ModelState.IsValid)
        {
            model.PromotionOptions = await GetPromotionOptionsAsync(model.KhuyenMaiId);
            return View("Form", model);
        }

        voucher.Ma = code;
        voucher.KhuyenMaiId = model.KhuyenMaiId;
        voucher.KhachHangId = model.KhachHangId;
        voucher.GioiHanTongLuot = model.GioiHanTongLuot;
        voucher.DangSuDung = model.DangSuDung;

        if (!await SaveVoucherAsync(model))
        {
            model.PromotionOptions = await GetPromotionOptionsAsync(model.KhuyenMaiId);
            return View("Form", model);
        }
        await transaction.CommitAsync();
        TempData["Success"] = $"Đã cập nhật Voucher '{voucher.Ma}'.";
        return RedirectToAction(nameof(Index));
        }
        catch (SqlException e) when (e.Number is 1205 or 1222)
        {
            TempData["Error"] = "Voucher đang được xử lý bởi thao tác khác. Vui lòng tải lại và thử lại.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException e) when (e.GetBaseException() is SqlException { Number: 1205 or 1222 })
        {
            TempData["Error"] = "Voucher đang được xử lý bởi thao tác khác. Vui lòng tải lại và thử lại.";
            return RedirectToAction(nameof(Index));
        }
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
        var code = new string(Enumerable.Range(0, 8).Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)]).ToArray());
        return $"SEN-{code}";
    }

    private async Task ValidateVoucherAsync(VoucherFormViewModel model, Voucher? existing)
    {
        model.DaDuocSuDung = existing is not null && await db.HoaDon.AnyAsync(x => x.VoucherId == existing.Id);
        if (model.DaDuocSuDung && existing is not null
            && (model.KhuyenMaiId != existing.KhuyenMaiId || model.KhachHangId != existing.KhachHangId
                || !string.Equals(model.Ma?.Trim(), existing.Ma, StringComparison.OrdinalIgnoreCase)))
            ModelState.AddModelError("", "Voucher đã gắn với hóa đơn: giữ nguyên mã, chương trình và khách nhận để bảo toàn lịch sử. Hãy tạo mã mới nếu cần đổi.");
        var promotion = await db.KhuyenMai.FindAsync(model.KhuyenMaiId);
        if (promotion is null || ((!promotion.CanVoucher || !promotion.DangSuDung)
            && !(model.DaDuocSuDung && !model.DangSuDung && existing?.KhuyenMaiId == model.KhuyenMaiId)))
            ModelState.AddModelError(nameof(model.KhuyenMaiId), "Chọn chương trình đang sử dụng và có yêu cầu voucher.");
        if (model.KhachHangId is int customerId && !await db.KhachHang.AnyAsync(x => x.Id == customerId))
            ModelState.AddModelError(nameof(model.KhachHangId), "Khách hàng không tồn tại.");
        if (existing is not null && model.GioiHanTongLuot < await db.HoaDon.CountAsync(x => x.VoucherId == existing.Id && x.TrangThai != TrangThaiHoaDon.DaHuy))
            ModelState.AddModelError(nameof(model.GioiHanTongLuot), "Giới hạn không được thấp hơn số lượt đã dùng/đang giữ trên hóa đơn.");
    }

    private async Task<bool> SaveVoucherAsync(VoucherFormViewModel model)
    {
        try { await db.SaveChangesAsync(); return true; }
        catch (DbUpdateConcurrencyException) { ModelState.AddModelError("", "Voucher đã thay đổi. Vui lòng tải lại trang."); }
        catch (DbUpdateException e) when (e.GetBaseException() is SqlException { Number: 2601 or 2627 })
        { ModelState.AddModelError(nameof(model.Ma), "Mã voucher đã tồn tại. Vui lòng chọn mã khác."); }
        return false;
    }

    private async Task<List<KhuyenMaiOptionItem>> GetPromotionOptionsAsync(int? currentId = null)
    {
        return await db.KhuyenMai.AsNoTracking()
            .Where(x => (x.DangSuDung && x.CanVoucher) || x.Id == currentId)
            .OrderByDescending(x => x.BatDau)
            .Select(x => new KhuyenMaiOptionItem { Id = x.Id, TenChuongTrinh = x.TenChuongTrinh })
            .ToListAsync();
    }
}
