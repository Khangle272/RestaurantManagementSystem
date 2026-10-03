using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Security;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin + "," + AppRoles.Kho)]
public class NhaCungCapController(RestaurantDbContext context) : ManagementControllerBase(context)
{
    public async Task<IActionResult> Index(string? search, bool? active, int page = 1)
    {
        search = search?.Trim();
        var query = Db.NhaCungCap.AsNoTracking();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(x =>
                x.TenNhaCungCap.Contains(search) ||
                (x.SoDienThoai != null && x.SoDienThoai.Contains(search)) ||
                (x.MaSoThue != null && x.MaSoThue.Contains(search)) ||
                (x.NguoiLienHe != null && x.NguoiLienHe.Contains(search)));
        }

        if (active.HasValue)
        {
            query = query.Where(x => x.DangSuDung == active.Value);
        }

        const int pageSize = 10;
        var total = await query.CountAsync();
        var pages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, pages);

        var items = await query.OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalAll = await Db.NhaCungCap.CountAsync();
        var activeCount = await Db.NhaCungCap.CountAsync(x => x.DangSuDung);

        return View(new NhaCungCapIndexVM
        {
            Items = items,
            Pagination = new PaginationViewModel
            {
                CurrentPage = page,
                TotalPages = pages,
                TotalItems = total,
                Search = search,
                TrangThai = active.HasValue ? (active.Value ? "active" : "inactive") : null
            },
            TotalCount = totalAll,
            ActiveCount = activeCount,
            InactiveCount = totalAll - activeCount,
            Search = search,
            Active = active
        });
    }

    [HttpGet]
    public IActionResult Create() => View(new NhaCungCapVM());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NhaCungCapVM model)
    {
        await ValidateAsync(model, 0);
        if (ModelState.IsValid)
        {
            var entity = new NhaCungCap
            {
                TenNhaCungCap = model.TenNhaCungCap.Trim(),
                SoDienThoai = model.SoDienThoai.Trim(),
                Email = model.Email?.Trim(),
                DiaChi = model.DiaChi?.Trim(),
                MaSoThue = model.MaSoThue?.Trim(),
                NguoiLienHe = model.NguoiLienHe?.Trim(),
                GhiChu = model.GhiChu?.Trim(),
                DangSuDung = model.DangSuDung
            };

            Db.NhaCungCap.Add(entity);
            await Db.SaveChangesAsync();
            TempData["Success"] = "Thêm nhà cung cấp mới thành công.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await Db.NhaCungCap.FindAsync(id);
        if (entity == null) return NotFound();

        return View(new NhaCungCapVM
        {
            Id = entity.Id,
            TenNhaCungCap = entity.TenNhaCungCap,
            SoDienThoai = entity.SoDienThoai ?? "",
            Email = entity.Email,
            DiaChi = entity.DiaChi,
            MaSoThue = entity.MaSoThue,
            NguoiLienHe = entity.NguoiLienHe,
            GhiChu = entity.GhiChu,
            DangSuDung = entity.DangSuDung
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, NhaCungCapVM model)
    {
        if (id != model.Id) return NotFound();
        var entity = await Db.NhaCungCap.FindAsync(id);
        if (entity == null) return NotFound();

        await ValidateAsync(model, id);
        if (ModelState.IsValid)
        {
            entity.TenNhaCungCap = model.TenNhaCungCap.Trim();
            entity.SoDienThoai = model.SoDienThoai.Trim();
            entity.Email = model.Email?.Trim();
            entity.DiaChi = model.DiaChi?.Trim();
            entity.MaSoThue = model.MaSoThue?.Trim();
            entity.NguoiLienHe = model.NguoiLienHe?.Trim();
            entity.GhiChu = model.GhiChu?.Trim();
            entity.DangSuDung = model.DangSuDung;

            await Db.SaveChangesAsync();
            TempData["Success"] = "Cập nhật thông tin nhà cung cấp thành công.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var entity = await Db.NhaCungCap.FindAsync(id);
        if (entity == null) return NotFound();

        entity.DangSuDung = !entity.DangSuDung;
        await Db.SaveChangesAsync();
        TempData["Success"] = $"Đã chuyển trạng thái nhà cung cấp '{entity.TenNhaCungCap}' sang {(entity.DangSuDung ? "Đang hợp tác" : "Ngừng hợp tác")}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await Db.NhaCungCap.FindAsync(id);
        if (entity == null) return NotFound();

        var inUse = await Db.PhieuNhap.AnyAsync(x => x.NhaCungCapId == id);
        if (inUse)
        {
            TempData["Error"] = $"Không thể xóa nhà cung cấp '{entity.TenNhaCungCap}' vì đã có chứng từ nhập hàng. Vui lòng chuyển trạng thái sang Ngừng hợp tác.";
            return RedirectToAction(nameof(Index));
        }

        Db.NhaCungCap.Remove(entity);
        await Db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa nhà cung cấp '{entity.TenNhaCungCap}'.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateAsync(NhaCungCapVM model, int id)
    {
        var ten = model.TenNhaCungCap?.Trim() ?? "";
        var sdt = model.SoDienThoai?.Trim() ?? "";

        if (await Db.NhaCungCap.AnyAsync(x => x.Id != id && x.TenNhaCungCap == ten))
        {
            ModelState.AddModelError(nameof(model.TenNhaCungCap), "Tên nhà cung cấp này đã tồn tại trong hệ thống.");
        }

        if (!string.IsNullOrEmpty(sdt) && await Db.NhaCungCap.AnyAsync(x => x.Id != id && x.SoDienThoai == sdt))
        {
            ModelState.AddModelError(nameof(model.SoDienThoai), "Số điện thoại này đã được đăng ký bởi nhà cung cấp khác.");
        }
    }
}
