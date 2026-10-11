using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize(Roles = RestaurantManagement.Web.Security.AppRoles.Admin)]
public class DanhMucController(RestaurantDbContext context) : ManagementControllerBase(context)
{
    public async Task<IActionResult> Index(string? search, string? trangThai, int page = 1)
    {
        search = search?.Trim();
        var query = Db.DanhMuc.AsNoTracking();
        if (!string.IsNullOrEmpty(search)) query = query.Where(x => x.TenDanhMuc.Contains(search));
        if (bool.TryParse(trangThai, out var active)) query = query.Where(x => x.DangSuDung == active);
        return View(await PageAsync(query.OrderByDescending(x => x.Id), page, search, trangThai));
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new DanhMucFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DanhMucFormViewModel model)
    {
        await ValidateAsync(model, 0);
        if (ModelState.IsValid)
        {
            var entity = new DanhMuc();
            Map(model, entity);
            Db.DanhMuc.Add(entity);
            if (await SaveAsync(nameof(model.TenDanhMuc), "Tên danh mục đã tồn tại."))
            {
                TempData["Success"] = "Thêm danh mục thành công.";
                return RedirectToAction(nameof(Index));
            }
        }
        return View("Form", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await Db.DanhMuc.FindAsync(id);
        if (entity == null) return NotFound();
        return View("Form", new DanhMucFormViewModel
        {
            Id = id, RowVersion = VersionOf(entity), TenDanhMuc = entity.TenDanhMuc,
            MoTa = entity.MoTa, DangSuDung = entity.DangSuDung
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DanhMucFormViewModel model)
    {
        if (id != model.Id) return NotFound();
        var entity = await Db.DanhMuc.FindAsync(id);
        if (entity == null) return NotFound();
        await ValidateAsync(model, id);
        if (ModelState.IsValid && ApplyVersion(entity, model.RowVersion))
        {
            Map(model, entity);
            if (await SaveAsync(nameof(model.TenDanhMuc), "Tên danh mục đã tồn tại."))
            {
                TempData["Success"] = "Cập nhật danh mục thành công.";
                return RedirectToAction(nameof(Index));
            }
        }
        return View("Form", model);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await Db.DanhMuc.FindAsync(id);
        if (entity == null) return NotFound();
        return View(DeleteModel(entity, id, entity.TenDanhMuc, "danh mục"));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, DeleteRecordViewModel model)
    {
        if (id != model.Id) return NotFound();
        var entity = await Db.DanhMuc.FindAsync(id);
        if (entity == null) return NotFound();
        return await DeleteRecordAsync(entity, model, entity.TenDanhMuc, "danh mục",
            await Db.MonAn.AnyAsync(x => x.DanhMucId == id));
    }

    private async Task ValidateAsync(DanhMucFormViewModel model, int id)
    {
        model.TenDanhMuc = model.TenDanhMuc?.Trim() ?? "";
        if (await Db.DanhMuc.AnyAsync(x => x.Id != id && x.TenDanhMuc == model.TenDanhMuc))
            ModelState.AddModelError(nameof(model.TenDanhMuc), "Tên danh mục đã tồn tại.");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickCreate([FromBody] QuickCreateDanhMucVM model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.TenDanhMuc))
            return Json(new { success = false, message = "Vui lòng nhập tên danh mục." });

        if (!TryValidateModel(model))
        {
            var err = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault();
            return Json(new { success = false, message = err ?? "Thông tin danh mục không hợp lệ." });
        }

        var name = model.TenDanhMuc.Trim();
        if (await Db.DanhMuc.AnyAsync(x => x.TenDanhMuc == name))
            return Json(new { success = false, message = "Tên danh mục đã tồn tại trong hệ thống." });

        var entity = new DanhMuc
        {
            TenDanhMuc = name,
            MoTa = model.MoTa?.Trim(),
            DangSuDung = true
        };

        Db.DanhMuc.Add(entity);
        try
        {
            await Db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Json(new { success = false, message = "Tên danh mục đã tồn tại trong hệ thống." });
        }

        return Json(new { success = true, id = entity.Id, name = entity.TenDanhMuc });
    }

    private static void Map(DanhMucFormViewModel model, DanhMuc entity)
    {
        entity.TenDanhMuc = model.TenDanhMuc;
        entity.MoTa = model.MoTa?.Trim();
        entity.DangSuDung = model.DangSuDung;
    }
}
