using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize(Roles = RestaurantManagement.Web.Security.AppRoles.Admin + "," + RestaurantManagement.Web.Security.AppRoles.Kho)]
public class NguyenLieuController(RestaurantDbContext context) : ManagementControllerBase(context)
{
    public async Task<IActionResult> Index(string? search, string? donViTinh, string? trangThai, int page = 1)
    {
        search = search?.Trim();
        var query = Db.NguyenLieu.AsNoTracking();
        if (!string.IsNullOrEmpty(search)) query = query.Where(x => x.TenNguyenLieu.Contains(search));
        if (!string.IsNullOrEmpty(donViTinh)) query = query.Where(x => x.DonViTinh == donViTinh);
        if (trangThai == "dang-su-dung") query = query.Where(x => x.DangSuDung);
        if (trangThai == "ngung-su-dung") query = query.Where(x => !x.DangSuDung);
        var rows = query.OrderByDescending(x => x.Id).Select(x => new NguyenLieuRowViewModel
        {
            Item = x,
            TonKho = (Db.ChiTietPhieuNhap.Where(c => c.NguyenLieuId == x.Id && c.PhieuNhap.TrangThai == TrangThaiPhieu.DaGhiSo)
                .Sum(c => (decimal?)c.SoLuong) ?? 0)
                - (Db.ChiTietPhieuXuat.Where(c => c.NguyenLieuId == x.Id && c.PhieuXuat.TrangThai == TrangThaiPhieu.DaGhiSo)
                .Sum(c => (decimal?)c.SoLuong) ?? 0)
        });
        var result = await PageAsync(rows, page, search, trangThai);
        var total = await Db.NguyenLieu.CountAsync();
        var active = await Db.NguyenLieu.CountAsync(x => x.DangSuDung);
        return View(new NguyenLieuIndexViewModel
        {
            Items = result.Items, Pagination = result.Pagination, DonViTinh = donViTinh,
            DonViTinhList = await Db.NguyenLieu.Select(x => x.DonViTinh).Distinct().OrderBy(x => x).ToListAsync(),
            TotalCount = total, ActiveCount = active, InactiveCount = total - active
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateSuppliersAsync();
        return View("Form", new NguyenLieuFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NguyenLieuFormViewModel model)
    {
        await ValidateAsync(model, 0);
        if (ModelState.IsValid)
        {
            var entity = new NguyenLieu();
            Map(model, entity);
            Db.Add(entity);
            if (await SaveAsync(nameof(model.TenNguyenLieu), "Tên nguyên liệu đã tồn tại."))
            {
                TempData["Success"] = "Thêm nguyên liệu thành công.";
                return RedirectToAction(nameof(Index));
            }
        }
        await PopulateSuppliersAsync();
        return View("Form", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await Db.NguyenLieu.FindAsync(id);
        if (entity == null) return NotFound();
        await PopulateSuppliersAsync(id);
        return View("Form", new NguyenLieuFormViewModel
        {
            Id = id, RowVersion = VersionOf(entity), TenNguyenLieu = entity.TenNguyenLieu,
            DonViTinh = entity.DonViTinh, NguongCanhBao = entity.NguongCanhBao, DangSuDung = entity.DangSuDung
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, NguyenLieuFormViewModel model)
    {
        if (id != model.Id) return NotFound();
        var entity = await Db.NguyenLieu.FindAsync(id);
        if (entity == null) return NotFound();
        
        if (ModelState.IsValid && ApplyVersion(entity, model.RowVersion))
        {
            Map(model, entity);
            if (await SaveAsync(nameof(model.TenNguyenLieu), "Tên nguyên liệu đã tồn tại."))
            {
                TempData["Success"] = "Cập nhật nguyên liệu thành công.";
                return RedirectToAction(nameof(Index));
            }
        }
        await PopulateSuppliersAsync(id);
        return View("Form", model);
    }

    private async Task PopulateSuppliersAsync(int? ingredientId = null)
    {
        ViewBag.AllSuppliers = await Db.NhaCungCap.Where(x => x.DangSuDung).OrderBy(x => x.TenNhaCungCap).ToListAsync();
        if (ingredientId.HasValue && ingredientId.Value > 0)
        {
            ViewBag.LinkedSuppliers = await Db.NhaCungCapNguyenLieus
                .Include(x => x.NhaCungCap)
                .Where(x => x.MaNguyenLieu == ingredientId.Value && x.TrangThai)
                .ToListAsync();
        }
        else
        {
            ViewBag.LinkedSuppliers = new List<NhaCungCapNguyenLieu>();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await Db.NguyenLieu.FindAsync(id);
        return entity == null ? NotFound() : View(DeleteModel(entity, id, entity.TenNguyenLieu, "nguyên liệu"));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, DeleteRecordViewModel model)
    {
        if (id != model.Id) return NotFound();
        var entity = await Db.NguyenLieu.FindAsync(id);
        if (entity == null) return NotFound();
        return await DeleteRecordAsync(entity, model, entity.TenNguyenLieu, "nguyên liệu", await InUseAsync(id));
    }

    private async Task<bool> InUseAsync(int id) => await Db.DinhMucMon.AnyAsync(x => x.NguyenLieuId == id)
        || await Db.ChiTietPhieuNhap.AnyAsync(x => x.NguyenLieuId == id)
        || await Db.ChiTietPhieuXuat.AnyAsync(x => x.NguyenLieuId == id);

    private async Task ValidateAsync(NguyenLieuFormViewModel model, int id)
    {
        model.TenNguyenLieu = model.TenNguyenLieu?.Trim() ?? "";
        model.DonViTinh = model.DonViTinh?.Trim() ?? "";
        if (await Db.NguyenLieu.AnyAsync(x => x.Id != id && x.TenNguyenLieu == model.TenNguyenLieu))
            ModelState.AddModelError(nameof(model.TenNguyenLieu), "Tên nguyên liệu đã tồn tại.");
    }

    [HttpPost]
    public async Task<IActionResult> QuickCreate([FromBody] QuickCreateNguyenLieuVM model)
    {
        if (string.IsNullOrWhiteSpace(model?.TenNguyenLieu))
            return Json(new { success = false, message = "Vui lòng nhập tên nguyên liệu." });

        if (string.IsNullOrWhiteSpace(model.DonViTinh))
            return Json(new { success = false, message = "Vui lòng nhập đơn vị tính." });

        var name = model.TenNguyenLieu.Trim();
        if (await Db.NguyenLieu.AnyAsync(x => x.TenNguyenLieu == name))
            return Json(new { success = false, message = "Tên nguyên liệu đã tồn tại trong hệ thống." });

        var entity = new NguyenLieu
        {
            TenNguyenLieu = name,
            DonViTinh = model.DonViTinh.Trim(),
            DanhMuc = model.DanhMuc?.Trim(),
            DonGia = model.DonGia,
            NguongCanhBao = model.DinhMucToiThieu,
            SoLuongTon = model.SoLuongTon,
            DangSuDung = true
        };

        Db.NguyenLieu.Add(entity);
        await Db.SaveChangesAsync();

        return Json(new { success = true, id = entity.Id, name = entity.TenNguyenLieu, unit = entity.DonViTinh, price = entity.DonGia });
    }

    [HttpGet]
    public async Task<IActionResult> GetSuppliersByIngredient(int ingredientId)
    {
        var links = await Db.NhaCungCapNguyenLieus
            .Include(x => x.NhaCungCap)
            .Where(x => x.MaNguyenLieu == ingredientId && x.TrangThai)
            .Select(x => new
            {
                id = x.MaNhaCungCap,
                name = x.NhaCungCap.TenNhaCungCap,
                phone = x.NhaCungCap.SoDienThoai,
                price = x.DonGiaCungUng,
                productCode = x.MaHangNCC
            })
            .ToListAsync();

        return Json(new { success = true, suppliers = links });
    }

    [HttpPost]
    public async Task<IActionResult> LinkSuppliersToIngredient(int ingredientId, [FromBody] List<SupplierLinkDto> suppliers)
    {
        var ingredient = await Db.NguyenLieu.FindAsync(ingredientId);
        if (ingredient == null) return NotFound();

        suppliers ??= [];
        var existing = await Db.NhaCungCapNguyenLieus.Where(x => x.MaNguyenLieu == ingredientId).ToListAsync();
        var newSupplierIds = suppliers.Select(s => s.MaNhaCungCap).ToHashSet();

        foreach (var ex in existing)
        {
            if (!newSupplierIds.Contains(ex.MaNhaCungCap))
                ex.TrangThai = false;
        }

        foreach (var s in suppliers)
        {
            var target = existing.FirstOrDefault(x => x.MaNhaCungCap == s.MaNhaCungCap);
            if (target != null)
            {
                target.DonGiaCungUng = s.DonGiaCungUng;
                target.MaHangNCC = s.MaHangNCC?.Trim();
                target.GhiChu = s.GhiChu?.Trim();
                target.TrangThai = true;
            }
            else
            {
                Db.NhaCungCapNguyenLieus.Add(new NhaCungCapNguyenLieu
                {
                    MaNguyenLieu = ingredientId,
                    MaNhaCungCap = s.MaNhaCungCap,
                    DonGiaCungUng = s.DonGiaCungUng,
                    MaHangNCC = s.MaHangNCC?.Trim(),
                    GhiChu = s.GhiChu?.Trim(),
                    NgayLienKet = DateTime.Now,
                    TrangThai = true
                });
            }
        }

        await Db.SaveChangesAsync();
        return Json(new { success = true, message = "Cập nhật nhà cung cấp liên kết thành công." });
    }

    private static void Map(NguyenLieuFormViewModel model, NguyenLieu entity)
    {
        entity.TenNguyenLieu = model.TenNguyenLieu; entity.DonViTinh = model.DonViTinh;
        entity.NguongCanhBao = model.NguongCanhBao; entity.DangSuDung = model.DangSuDung;
    }
}
