using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Controllers;

public class NhanVienController(RestaurantDbContext context) : ManagementControllerBase(context)
{
    public async Task<IActionResult> Index(string? search, string? trangThai, int page = 1)
    {
        search = search?.Trim();
        var query = Db.NhanVien.AsNoTracking();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(x => x.MaNhanVien.Contains(search) || x.HoTen.Contains(search) || x.SoDienThoai.Contains(search));
        if (bool.TryParse(trangThai, out var active))
            query = query.Where(x => x.DangLamViec == active);
        return View(await PageAsync(query.OrderByDescending(x => x.Id), page, search, trangThai));
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new NhanVienFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NhanVienFormViewModel model)
    {
        await ValidateAsync(model, 0);
        if (ModelState.IsValid)
        {
            var entity = new NhanVien();
            Map(model, entity);
            Db.NhanVien.Add(entity);
            if (await SaveAsync(nameof(model.MaNhanVien), "Mã nhân viên đã tồn tại."))
            {
                TempData["Success"] = "Thêm nhân viên thành công.";
                return RedirectToAction(nameof(Index));
            }
        }
        return View("Form", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await Db.NhanVien.FindAsync(id);
        if (entity == null) return NotFound();
        return View("Form", new NhanVienFormViewModel
        {
            Id = entity.Id, RowVersion = VersionOf(entity), MaNhanVien = entity.MaNhanVien,
            HoTen = entity.HoTen, SoDienThoai = entity.SoDienThoai, Email = entity.Email,
            DiaChi = entity.DiaChi, ChucVu = entity.ChucVu, NgayVaoLam = entity.NgayVaoLam,
            DangLamViec = entity.DangLamViec
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, NhanVienFormViewModel model)
    {
        if (id != model.Id) return NotFound();
        var entity = await Db.NhanVien.FindAsync(id);
        if (entity == null) return NotFound();
        await ValidateAsync(model, id);
        if (ModelState.IsValid && ApplyVersion(entity, model.RowVersion))
        {
            // Update only editable profile fields; preserve the linked login account.
            Map(model, entity);
            if (await SaveAsync(nameof(model.MaNhanVien), "Mã nhân viên đã tồn tại."))
            {
                TempData["Success"] = "Cập nhật nhân viên thành công.";
                return RedirectToAction(nameof(Index));
            }
        }
        return View("Form", model);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await Db.NhanVien.FindAsync(id);
        if (entity == null) return NotFound();
        return View(DeleteModel(entity, id, $"{entity.MaNhanVien} — {entity.HoTen}", "nhân viên"));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, DeleteRecordViewModel model)
    {
        if (id != model.Id) return NotFound();
        var entity = await Db.NhanVien.FindAsync(id);
        if (entity == null) return NotFound();
        var inUse = entity.TaiKhoanId.HasValue
            || await Db.DatBan.AnyAsync(x => x.NhanVienTiepNhanId == id || x.NhanVienHuyId == id)
            || await Db.HoaDon.AnyAsync(x => x.NhanVienId == id)
            || await Db.PhieuNhap.AnyAsync(x => x.NhanVienId == id)
            || await Db.PhieuXuat.AnyAsync(x => x.NhanVienId == id);
        return await DeleteRecordAsync(entity, model, $"{entity.MaNhanVien} — {entity.HoTen}", "nhân viên", inUse);
    }

    private async Task ValidateAsync(NhanVienFormViewModel model, int id)
    {
        model.MaNhanVien = model.MaNhanVien?.Trim() ?? "";
        if (await Db.NhanVien.AnyAsync(x => x.Id != id && x.MaNhanVien == model.MaNhanVien))
            ModelState.AddModelError(nameof(model.MaNhanVien), "Mã nhân viên đã tồn tại.");
    }

    private static void Map(NhanVienFormViewModel model, NhanVien entity)
    {
        entity.MaNhanVien = model.MaNhanVien;
        entity.HoTen = model.HoTen.Trim();
        entity.SoDienThoai = model.SoDienThoai.Trim();
        entity.Email = model.Email?.Trim();
        entity.DiaChi = model.DiaChi?.Trim();
        entity.ChucVu = model.ChucVu.Trim();
        entity.NgayVaoLam = model.NgayVaoLam!.Value;
        entity.DangLamViec = model.DangLamViec;
    }
}
