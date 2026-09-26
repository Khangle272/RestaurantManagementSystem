using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize(Roles = RestaurantManagement.Web.Security.AppRoles.Admin + "," + RestaurantManagement.Web.Security.AppRoles.TiepTan + "," + RestaurantManagement.Web.Security.AppRoles.BoiBan)]
public class BanAnController(RestaurantDbContext context) : ManagementControllerBase(context)
{
    public async Task<IActionResult> Index(string? search, int? khuVucId, string? trangThai, int page = 1)
    {
        search = search?.Trim();
        var query = Db.BanAn.Include(x => x.KhuVuc).AsNoTracking();
        if (!string.IsNullOrEmpty(search)) query = query.Where(x => x.MaBan.Contains(search));
        if (khuVucId.HasValue) query = query.Where(x => x.KhuVucId == khuVucId);
        if (Enum.TryParse<TrangThaiBan>(trangThai, out var status) && Enum.IsDefined(status))
            query = query.Where(x => x.TrangThai == status);
        ViewBag.KhuVucOptions = await AreaOptionsAsync();
        return View(await PageAsync(query.OrderByDescending(x => x.Id), page, search, trangThai, khuVucId));
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = RestaurantManagement.Web.Security.AppRoles.Admin), HttpGet]
    public async Task<IActionResult> Create() => View("Form", new BanAnFormViewModel
    {
        KhuVucOptions = await AreaOptionsAsync(activeOnly: true)
    });

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = RestaurantManagement.Web.Security.AppRoles.Admin), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BanAnFormViewModel model)
    {
        await ValidateAsync(model, 0, null);
        if (ModelState.IsValid)
        {
            var entity = new BanAn();
            Map(model, entity);
            Db.BanAn.Add(entity);
            if (await SaveAsync(nameof(model.MaBan), "Mã bàn đã tồn tại."))
            {
                TempData["Success"] = "Thêm bàn ăn thành công.";
                return RedirectToAction(nameof(Index));
            }
        }
        model.KhuVucOptions = await AreaOptionsAsync(activeOnly: true);
        return View("Form", model);
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = RestaurantManagement.Web.Security.AppRoles.Admin), HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await Db.BanAn.FindAsync(id);
        if (entity == null) return NotFound();
        return View("Form", new BanAnFormViewModel
        {
            Id = id, RowVersion = VersionOf(entity), MaBan = entity.MaBan, KhuVucId = entity.KhuVucId,
            SoChoNgoi = entity.SoChoNgoi, TrangThai = entity.TrangThai,
            KhuVucOptions = await AreaOptionsAsync(true, entity.KhuVucId)
        });
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = RestaurantManagement.Web.Security.AppRoles.Admin), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BanAnFormViewModel model)
    {
        if (id != model.Id) return NotFound();
        var entity = await Db.BanAn.FindAsync(id);
        if (entity == null) return NotFound();
        var originalArea = entity.KhuVucId;
        await ValidateAsync(model, id, originalArea);
        if (ModelState.IsValid && ApplyVersion(entity, model.RowVersion))
        {
            Map(model, entity);
            if (await SaveAsync(nameof(model.MaBan), "Mã bàn đã tồn tại."))
            {
                TempData["Success"] = "Cập nhật bàn ăn thành công.";
                return RedirectToAction(nameof(Index));
            }
        }
        model.KhuVucOptions = await AreaOptionsAsync(true, originalArea);
        return View("Form", model);
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = RestaurantManagement.Web.Security.AppRoles.Admin), HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await Db.BanAn.FindAsync(id);
        if (entity == null) return NotFound();
        return View(DeleteModel(entity, id, entity.MaBan, "bàn ăn"));
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = RestaurantManagement.Web.Security.AppRoles.Admin), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, DeleteRecordViewModel model)
    {
        if (id != model.Id) return NotFound();
        var entity = await Db.BanAn.FindAsync(id);
        if (entity == null) return NotFound();
        return await DeleteRecordAsync(entity, model, entity.MaBan, "bàn ăn",
            entity.TrangThai == TrangThaiBan.DangPhucVu || await Db.ChiTietDatBan.AnyAsync(x => x.BanAnId == id));
    }

    private async Task ValidateAsync(BanAnFormViewModel model, int id, int? currentArea)
    {
        model.MaBan = model.MaBan?.Trim() ?? "";
        if (await Db.BanAn.AnyAsync(x => x.Id != id && x.MaBan == model.MaBan))
            ModelState.AddModelError(nameof(model.MaBan), "Mã bàn đã tồn tại.");
        if (!await Db.KhuVuc.AnyAsync(x => x.Id == model.KhuVucId && (x.DangSuDung || x.Id == currentArea)))
            ModelState.AddModelError(nameof(model.KhuVucId), "Khu vực không tồn tại hoặc đã ngừng sử dụng.");
    }

    private async Task<List<SelectListItem>> AreaOptionsAsync(bool activeOnly = false, int? currentArea = null) =>
        await Db.KhuVuc.AsNoTracking()
            .Where(x => !activeOnly || x.DangSuDung || x.Id == currentArea)
            .OrderBy(x => x.TenKhuVuc)
            .Select(x => new SelectListItem(x.TenKhuVuc + (x.DangSuDung ? "" : " (ngừng sử dụng)"), x.Id.ToString()))
            .ToListAsync();

    private static void Map(BanAnFormViewModel model, BanAn entity)
    {
        entity.MaBan = model.MaBan;
        entity.KhuVucId = model.KhuVucId;
        entity.SoChoNgoi = model.SoChoNgoi;
        entity.TrangThai = model.TrangThai;
    }
}
