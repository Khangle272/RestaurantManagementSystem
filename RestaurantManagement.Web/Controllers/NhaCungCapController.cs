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

            try
            {
                await Db.SaveChangesAsync();
                TempData["Success"] = "Cập nhật thông tin nhà cung cấp thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError("", "Dữ liệu nhà cung cấp đã được cập nhật bởi phiên làm việc khác. Vui lòng tải lại trang.");
            }
        }

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var entity = await Db.NhaCungCap.FindAsync(id);
        if (entity == null) return NotFound();

        entity.DangSuDung = !entity.DangSuDung;
        try
        {
            await Db.SaveChangesAsync();
            TempData["Success"] = $"Đã chuyển trạng thái nhà cung cấp '{entity.TenNhaCungCap}' sang {(entity.DangSuDung ? "Đang hợp tác" : "Ngừng hợp tác")}.";
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = "Dữ liệu nhà cung cấp đã được cập nhật bởi phiên làm việc khác. Vui lòng tải lại.";
        }
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

        var linkedIngredients = await Db.NhaCungCapNguyenLieus.AnyAsync(x => x.MaNhaCungCap == id);
        if (linkedIngredients)
        {
            TempData["Error"] = $"Không thể xóa nhà cung cấp '{entity.TenNhaCungCap}' vì đang được liên kết cung cấp nguyên liệu. Vui lòng gỡ liên kết nguyên liệu hoặc chuyển trạng thái sang Ngừng hợp tác.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            Db.NhaCungCap.Remove(entity);
            await Db.SaveChangesAsync();
            TempData["Success"] = $"Đã xóa nhà cung cấp '{entity.TenNhaCungCap}'.";
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = "Nhà cung cấp đã bị thay đổi hoặc xóa bởi một phiên làm việc khác.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = $"Không thể xóa nhà cung cấp '{entity.TenNhaCungCap}' do có dữ liệu liên quan trong hệ thống.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickCreate([FromBody] QuickCreateNhaCungCapVM model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.TenNhaCungCap))
            return Json(new { success = false, message = "Vui lòng nhập tên nhà cung cấp." });

        if (string.IsNullOrWhiteSpace(model.SoDienThoai))
            return Json(new { success = false, message = "Vui lòng nhập số điện thoại." });

        if (!TryValidateModel(model))
        {
            var err = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault();
            return Json(new { success = false, message = err ?? "Thông tin nhà cung cấp không hợp lệ." });
        }

        var ten = model.TenNhaCungCap.Trim();
        var sdt = model.SoDienThoai.Trim();

        if (await Db.NhaCungCap.AnyAsync(x => x.TenNhaCungCap == ten))
            return Json(new { success = false, message = "Tên nhà cung cấp này đã tồn tại trong hệ thống." });

        if (await Db.NhaCungCap.AnyAsync(x => x.SoDienThoai == sdt))
            return Json(new { success = false, message = "Số điện thoại này đã được đăng ký bởi nhà cung cấp khác." });

        var entity = new NhaCungCap
        {
            TenNhaCungCap = ten,
            SoDienThoai = sdt,
            Email = model.Email?.Trim(),
            DiaChi = model.DiaChi?.Trim(),
            MaSoThue = model.MaSoThue?.Trim(),
            NguoiLienHe = model.NguoiLienHe?.Trim(),
            GhiChu = model.GhiChu?.Trim(),
            DangSuDung = true
        };

        Db.NhaCungCap.Add(entity);
        try
        {
            await Db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Json(new { success = false, message = "Tên hoặc số điện thoại nhà cung cấp đã tồn tại." });
        }

        return Json(new { success = true, id = entity.Id, name = entity.TenNhaCungCap, phone = entity.SoDienThoai });
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
