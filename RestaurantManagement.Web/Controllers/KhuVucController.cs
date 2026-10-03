using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Security;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class KhuVucController(RestaurantDbContext db) : ManagementControllerBase(db)
{
    public async Task<IActionResult> Index() => View(await Db.KhuVuc.AsNoTracking().OrderBy(x => x.Tang).ThenBy(x => x.TenKhuVuc).ToListAsync());
    [HttpGet]
    public async Task<IActionResult> Edit(int id = 0)
    {
        if (id == 0) return View(new AreaFormViewModel());
        var area = await Db.KhuVuc.FindAsync(id);
        return area is null ? NotFound() : View(new AreaFormViewModel { Id = area.Id, TenKhuVuc = area.TenKhuVuc, Tang = area.Tang,
            LaPhongVip = area.LaPhongVip, DangSuDung = area.DangSuDung, RowVersion = VersionOf(area) });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AreaFormViewModel model)
    {
        var area = model.Id == 0 ? new KhuVuc() : await Db.KhuVuc.FindAsync(model.Id);
        if (area is null) return NotFound();
        model.TenKhuVuc = model.TenKhuVuc.Trim();
        if (string.IsNullOrWhiteSpace(model.TenKhuVuc)) ModelState.AddModelError(nameof(model.TenKhuVuc), "Nhập tên khu vực.");
        if (await Db.KhuVuc.AnyAsync(x => x.Id != model.Id && x.Tang == model.Tang && x.TenKhuVuc == model.TenKhuVuc))
            ModelState.AddModelError(nameof(model.TenKhuVuc), "Tên khu vực đã có trên tầng này.");
        if (!model.DangSuDung && await Db.BanAn.AnyAsync(x => x.KhuVucId == model.Id && x.TrangThai == TrangThaiBan.DangPhucVu))
            ModelState.AddModelError("", "Khu vực đang có khách; chưa thể ngừng sử dụng.");
        if (!model.DangSuDung && await Db.ChiTietDatBan.AnyAsync(x => x.BanAn.KhuVucId == model.Id
            && (x.DatBan.TrangThai == TrangThaiDatBan.DaXacNhan || x.DatBan.TrangThai == TrangThaiDatBan.ChoCoc)
            && x.DatBan.GioKetThucDuKien > DateTimeOffset.UtcNow))
            ModelState.AddModelError("", "Khu vực còn lịch đã xác nhận. Chuyển lịch sang khu vực khác trước.");
        if (ModelState.IsValid && (model.Id == 0 || ApplyVersion(area, model.RowVersion)))
        {
            area.TenKhuVuc = model.TenKhuVuc; area.Tang = model.Tang; area.LaPhongVip = model.LaPhongVip; area.DangSuDung = model.DangSuDung;
            if (model.Id == 0) Db.KhuVuc.Add(area);
            if (await SaveAsync(nameof(model.TenKhuVuc), "Khu vực đã tồn tại.")) return RedirectToAction(nameof(Index));
        }
        return View(model);
    }
}
