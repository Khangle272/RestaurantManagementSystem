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
    public IActionResult Create() => View("Form", new NguyenLieuFormViewModel());

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
        return View("Form", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await Db.NguyenLieu.FindAsync(id);
        if (entity == null) return NotFound();
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
        await ValidateAsync(model, id);
        if (entity.DonViTinh != model.DonViTinh && await InUseAsync(id))
            ModelState.AddModelError(nameof(model.DonViTinh), "Không thể đổi đơn vị khi đã có định mức hoặc phiếu nhập/xuất. Hãy tạo nguyên liệu riêng cho đơn vị mới.");
        if (ModelState.IsValid && ApplyVersion(entity, model.RowVersion))
        {
            Map(model, entity);
            if (await SaveAsync(nameof(model.TenNguyenLieu), "Tên nguyên liệu đã tồn tại."))
            {
                TempData["Success"] = "Cập nhật nguyên liệu thành công.";
                return RedirectToAction(nameof(Index));
            }
        }
        return View("Form", model);
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

    private static void Map(NguyenLieuFormViewModel model, NguyenLieu entity)
    {
        entity.TenNguyenLieu = model.TenNguyenLieu; entity.DonViTinh = model.DonViTinh;
        entity.NguongCanhBao = model.NguongCanhBao; entity.DangSuDung = model.DangSuDung;
    }
}
