using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Security;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class KhuyenMaiController(RestaurantDbContext db) : Controller
{
    private const int PageSize = 10;

    public async Task<IActionResult> Index(string? search, string? statusFilter, PhamViUuDai? phamViFilter, int page = 1)
    {
        var query = db.KhuyenMai.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(x => x.TenChuongTrinh.Contains(s));
        }

        if (phamViFilter.HasValue)
        {
            query = query.Where(x => x.PhamVi == phamViFilter.Value);
        }

        var now = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = statusFilter switch
            {
                "DangDienRa" => query.Where(x => x.DangSuDung && x.BatDau <= now && x.KetThuc >= now),
                "SapDienRa" => query.Where(x => x.DangSuDung && x.BatDau > now),
                "DaKetThuc" => query.Where(x => x.DangSuDung && x.KetThuc < now),
                "NgungSuDung" => query.Where(x => !x.DangSuDung),
                _ => query
            };
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
        if (totalPages < 1) totalPages = 1;
        if (page < 1) page = 1;
        if (page > totalPages) page = totalPages;

        var items = await query
            .OrderByDescending(x => x.DangSuDung)
            .ThenByDescending(x => x.BatDau)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(x => new KhuyenMaiItemViewModel
            {
                Id = x.Id,
                TenChuongTrinh = x.TenChuongTrinh,
                BatDau = x.BatDau,
                KetThuc = x.KetThuc,
                PhamVi = x.PhamVi,
                KieuGiam = x.KieuGiam,
                GiaTri = x.GiaTri,
                MucGiamToiDa = x.MucGiamToiDa,
                GiaTriToiThieu = x.GiaTriToiThieu,
                ChoPhepKetHop = x.ChoPhepKetHop,
                ThuTuApDung = x.ThuTuApDung,
                CanVoucher = x.CanVoucher,
                DangSuDung = x.DangSuDung,
                SoMonApDung = db.KhuyenMaiMon.Count(km => km.KhuyenMaiId == x.Id),
                SoVoucherDaPhatHanh = db.Voucher.Count(v => v.KhuyenMaiId == x.Id),
                SoLuotDaSuDung = db.HoaDon.Count(h => h.Voucher != null && h.Voucher.KhuyenMaiId == x.Id && h.TrangThai != TrangThaiHoaDon.DaHuy)
            })
            .ToListAsync();

        var model = new KhuyenMaiListViewModel
        {
            Search = search,
            StatusFilter = statusFilter,
            PhamViFilter = phamViFilter,
            Page = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            Items = items
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new KhuyenMaiFormViewModel
        {
            BatDau = DateTime.Today,
            KetThuc = DateTime.Today.AddMonths(1),
            AvailableDishes = await GetAvailableDishesAsync()
        };
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(KhuyenMaiFormViewModel model)
    {
        await ValidateFormAsync(model);

        if (!ModelState.IsValid)
        {
            model.AvailableDishes = await GetAvailableDishesAsync(model.SelectedMonAnIds);
            return View("Form", model);
        }

        var entity = new KhuyenMai
        {
            TenChuongTrinh = model.TenChuongTrinh.Trim(),
            BatDau = new DateTimeOffset(model.BatDau.ToUniversalTime(), TimeSpan.Zero),
            KetThuc = new DateTimeOffset(model.KetThuc.Date.AddDays(1).AddTicks(-1).ToUniversalTime(), TimeSpan.Zero),
            PhamVi = model.PhamVi,
            KieuGiam = model.KieuGiam,
            GiaTri = model.GiaTri,
            MucGiamToiDa = model.KieuGiam == KieuGiam.PhanTram ? model.MucGiamToiDa : null,
            GiaTriToiThieu = model.GiaTriToiThieu,
            ChoPhepKetHop = model.ChoPhepKetHop,
            ThuTuApDung = model.ThuTuApDung,
            CanVoucher = model.CanVoucher,
            DangSuDung = model.DangSuDung
        };

        db.KhuyenMai.Add(entity);
        await db.SaveChangesAsync();

        if (model.PhamVi == PhamViUuDai.MonAn && model.SelectedMonAnIds.Count > 0)
        {
            foreach (var dishId in model.SelectedMonAnIds.Distinct())
            {
                db.KhuyenMaiMon.Add(new KhuyenMaiMon
                {
                    KhuyenMaiId = entity.Id,
                    MonAnId = dishId
                });
            }
            await db.SaveChangesAsync();
        }

        TempData["Success"] = $"Đã tạo chương trình ưu đãi '{entity.TenChuongTrinh}' thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await db.KhuyenMai.SingleOrDefaultAsync(x => x.Id == id);
        if (entity == null) return NotFound();

        var selectedDishes = await db.KhuyenMaiMon.Where(x => x.KhuyenMaiId == id).Select(x => x.MonAnId).ToListAsync();

        var model = new KhuyenMaiFormViewModel
        {
            Id = entity.Id,
            TenChuongTrinh = entity.TenChuongTrinh,
            BatDau = entity.BatDau.LocalDateTime,
            KetThuc = entity.KetThuc.LocalDateTime,
            PhamVi = entity.PhamVi,
            KieuGiam = entity.KieuGiam,
            GiaTri = entity.GiaTri,
            MucGiamToiDa = entity.MucGiamToiDa,
            GiaTriToiThieu = entity.GiaTriToiThieu,
            ChoPhepKetHop = entity.ChoPhepKetHop,
            ThuTuApDung = entity.ThuTuApDung,
            CanVoucher = entity.CanVoucher,
            DangSuDung = entity.DangSuDung,
            SelectedMonAnIds = selectedDishes,
            AvailableDishes = await GetAvailableDishesAsync(selectedDishes)
        };

        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, KhuyenMaiFormViewModel model)
    {
        if (id != model.Id) return NotFound();

        var entity = await db.KhuyenMai.SingleOrDefaultAsync(x => x.Id == id);
        if (entity == null) return NotFound();

        await ValidateFormAsync(model, id);

        if (!ModelState.IsValid)
        {
            model.AvailableDishes = await GetAvailableDishesAsync(model.SelectedMonAnIds);
            return View("Form", model);
        }

        entity.TenChuongTrinh = model.TenChuongTrinh.Trim();
        entity.BatDau = new DateTimeOffset(model.BatDau.ToUniversalTime(), TimeSpan.Zero);
        entity.KetThuc = new DateTimeOffset(model.KetThuc.Date.AddDays(1).AddTicks(-1).ToUniversalTime(), TimeSpan.Zero);
        entity.PhamVi = model.PhamVi;
        entity.KieuGiam = model.KieuGiam;
        entity.GiaTri = model.GiaTri;
        entity.MucGiamToiDa = model.KieuGiam == KieuGiam.PhanTram ? model.MucGiamToiDa : null;
        entity.GiaTriToiThieu = model.GiaTriToiThieu;
        entity.ChoPhepKetHop = model.ChoPhepKetHop;
        entity.ThuTuApDung = model.ThuTuApDung;
        entity.CanVoucher = model.CanVoucher;
        entity.DangSuDung = model.DangSuDung;

        // Cập nhật lại liên kết món
        var existingLinks = await db.KhuyenMaiMon.Where(x => x.KhuyenMaiId == id).ToListAsync();
        db.KhuyenMaiMon.RemoveRange(existingLinks);

        if (model.PhamVi == PhamViUuDai.MonAn && model.SelectedMonAnIds.Count > 0)
        {
            foreach (var dishId in model.SelectedMonAnIds.Distinct())
            {
                db.KhuyenMaiMon.Add(new KhuyenMaiMon
                {
                    KhuyenMaiId = entity.Id,
                    MonAnId = dishId
                });
            }
        }

        await db.SaveChangesAsync();

        TempData["Success"] = $"Đã cập nhật chương trình '{entity.TenChuongTrinh}'.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var entity = await db.KhuyenMai.FindAsync(id);
        if (entity == null) return NotFound();

        entity.DangSuDung = !entity.DangSuDung;
        await db.SaveChangesAsync();

        TempData["Success"] = $"Đã {(entity.DangSuDung ? "kích hoạt" : "tắt")} chương trình '{entity.TenChuongTrinh}'.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await db.KhuyenMai.FindAsync(id);
        if (entity == null) return NotFound();

        // Kiểm tra xem đã có hóa đơn nào dùng voucher thuộc khuyến mãi này chưa
        var hasUsedInvoices = await db.HoaDon.AnyAsync(h => h.Voucher != null && h.Voucher.KhuyenMaiId == id);
        if (hasUsedInvoices)
        {
            // Bảo toàn lịch sử kinh doanh: chuyển sang Ngưng sử dụng thay vì xóa cứng
            entity.DangSuDung = false;
            await db.SaveChangesAsync();
            TempData["Success"] = $"Chương trình '{entity.TenChuongTrinh}' đã có hóa đơn lịch sử nên được chuyển sang trạng thái Ngừng sử dụng.";
            return RedirectToAction(nameof(Index));
        }

        var linkedVouchers = await db.Voucher.Where(v => v.KhuyenMaiId == id).ToListAsync();
        db.Voucher.RemoveRange(linkedVouchers);

        var linkedDishes = await db.KhuyenMaiMon.Where(km => km.KhuyenMaiId == id).ToListAsync();
        db.KhuyenMaiMon.RemoveRange(linkedDishes);

        db.KhuyenMai.Remove(entity);
        await db.SaveChangesAsync();

        TempData["Success"] = $"Đã xóa chương trình '{entity.TenChuongTrinh}'.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateFormAsync(KhuyenMaiFormViewModel model, int excludeId = 0)
    {
        if (model.BatDau > model.KetThuc)
        {
            ModelState.AddModelError(nameof(model.KetThuc), "Thời gian kết thúc phải sau thời gian bắt đầu.");
        }

        if (model.KieuGiam == KieuGiam.PhanTram)
        {
            if (model.GiaTri <= 0 || model.GiaTri > 100)
            {
                ModelState.AddModelError(nameof(model.GiaTri), "Giảm giá theo phần trăm phải nằm trong khoảng từ 0.01% đến 100%.");
            }
        }
        else
        {
            if (model.GiaTri <= 0)
            {
                ModelState.AddModelError(nameof(model.GiaTri), "Số tiền giảm phải lớn hơn 0 đ.");
            }
        }

        if (model.PhamVi == PhamViUuDai.MonAn && model.SelectedMonAnIds.Count == 0)
        {
            ModelState.AddModelError(nameof(model.SelectedMonAnIds), "Vui lòng chọn ít nhất một món ăn khi áp dụng ưu đãi theo Món ăn.");
        }

        var duplicateName = await db.KhuyenMai.AnyAsync(x => x.Id != excludeId && x.TenChuongTrinh.ToLower() == model.TenChuongTrinh.Trim().ToLower());
        if (duplicateName)
        {
            ModelState.AddModelError(nameof(model.TenChuongTrinh), "Tên chương trình khuyến mãi đã tồn tại. Vui lòng chọn tên khác.");
        }
    }

    private async Task<List<MonAnSelectionItem>> GetAvailableDishesAsync(List<int>? selectedIds = null)
    {
        selectedIds ??= [];
        var dishes = await db.MonAn.AsNoTracking()
            .Include(x => x.DanhMuc)
            .Include(x => x.Sizes)
            .Where(x => x.DaDuyet && x.TrangThai != TrangThaiMon.NgungKinhDoanh)
            .OrderBy(x => x.DanhMuc.TenDanhMuc)
            .ThenBy(x => x.TenMon)
            .ToListAsync();

        return dishes.Select(x => new MonAnSelectionItem
        {
            Id = x.Id,
            TenMon = x.TenMon,
            TenDanhMuc = x.DanhMuc?.TenDanhMuc ?? "Chung",
            GiaThamKhao = x.Sizes.FirstOrDefault(s => s.DangSuDung)?.GiaBan ?? 0,
            IsSelected = selectedIds.Contains(x.Id)
        }).ToList();
    }
}
