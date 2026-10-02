using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using Microsoft.AspNetCore.Identity;
using RestaurantManagement.Web.Security;

namespace RestaurantManagement.Web.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize(Roles = RestaurantManagement.Web.Security.AppRoles.Admin)]
public class NhanVienController(RestaurantDbContext context, UserManager<TaiKhoan> users) : ManagementControllerBase(context)
{
    public async Task<IActionResult> Index(string? search, string? trangThai, int page = 1)
    {
        search = search?.Trim();
        var query = Db.NhanVien.AsNoTracking().Include(x => x.TaiKhoan).AsQueryable();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(x => x.MaNhanVien.Contains(search) || x.HoTen.Contains(search) || x.SoDienThoai.Contains(search));
        if (bool.TryParse(trangThai, out var active))
            query = query.Where(x => x.DangLamViec == active);
        var result = await PageAsync(query.OrderByDescending(x => x.Id), page, search, trangThai);
        var accountIds = result.Items.Where(x => x.TaiKhoanId != null).Select(x => x.TaiKhoanId!.Value).ToArray();
        var links = await (from link in Db.UserRoles join role in Db.Roles on link.RoleId equals role.Id
                          where accountIds.Contains(link.UserId) select new { link.UserId, role.Name }).ToListAsync();
        ViewBag.AccountRoles = links.GroupBy(x => x.UserId).ToDictionary(x => x.Key, x => string.Join(", ", x.Select(r => AppRoles.Label(r.Name))));
        return View(result);
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
                return RedirectToAction(nameof(Edit), new { id = entity.Id });
            }
        }
        return View("Form", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await Db.NhanVien.FindAsync(id);
        if (entity == null) return NotFound();
        await LoadAccountAsync(entity);
        return View("Form", new NhanVienFormViewModel
        {
            Id = entity.Id, RowVersion = VersionOf(entity), MaNhanVien = entity.MaNhanVien,
            HoTen = entity.HoTen, SoDienThoai = entity.SoDienThoai, Email = entity.Email,
            DiaChi = entity.DiaChi, ChucVu = ViewBag.AccountRole ?? AppRoles.RoleForJob(entity.ChucVu) ?? entity.ChucVu, NgayVaoLam = entity.NgayVaoLam,
            DangLamViec = entity.DangLamViec
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, NhanVienFormViewModel model)
    {
        if (id != model.Id) return NotFound();
        var entity = await Db.NhanVien.FindAsync(id);
        if (entity == null) return NotFound();
        await LoadAccountAsync(entity);
        await ValidateAsync(model, id);
        if (ModelState.IsValid && ApplyVersion(entity, model.RowVersion))
        {
            await using var transaction = await Db.Database.BeginTransactionAsync();
            if (entity.TaiKhoanId is int accountId)
            {
                var user = await users.FindByIdAsync(accountId.ToString());
                if (user is null) { ModelState.AddModelError("", "Tài khoản liên kết không tồn tại."); return View("Form", model); }
                var roles = await users.GetRolesAsync(user);
                if (roles.Contains(AppRoles.KhachHang) || (roles.Contains(AppRoles.Admin) && model.ChucVu != AppRoles.Admin))
                {
                    ModelState.AddModelError("", "Không thay đổi tài khoản quản lý/khách qua hồ sơ nhân viên.");
                    return View("Form", model);
                }
                // Hồ sơ cũ liên kết tài khoản chưa có quyền vẫn được sửa; không tự cấp quyền cho tài khoản đó.
                if (roles.Count > 0 && (roles.Count != 1 || roles[0] != model.ChucVu))
                {
                    if (string.IsNullOrEmpty(user.SecurityStamp))
                    {
                        ModelState.AddModelError("", "Tài khoản liên kết chưa được khởi tạo hợp lệ. Chưa thể đổi vai trò.");
                        return View("Form", model);
                    }
                    var removed = await users.RemoveFromRolesAsync(user, roles);
                    var added = removed.Succeeded ? await users.AddToRoleAsync(user, model.ChucVu) : removed;
                    if (!added.Succeeded) { ModelState.AddModelError("", "Không cập nhật được vai trò. Không có thay đổi nào được lưu."); return View("Form", model); }
                    var stamp = await users.UpdateSecurityStampAsync(user);
                    if (!stamp.Succeeded) { ModelState.AddModelError("", "Không cập nhật được phiên đăng nhập."); return View("Form", model); }
                }
            }
            Map(model, entity);
            if (await SaveAsync(nameof(model.MaNhanVien), "Mã nhân viên đã tồn tại."))
            {
                await transaction.CommitAsync();
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
        var role = AppRoles.RoleForJob(model.ChucVu);
        var employee = ViewBag.Employee as NhanVien;
        var preserveAdmin = id != 0 && role == AppRoles.Admin &&
            ((ViewBag.AccountRole as string) == AppRoles.Admin
                || (employee?.TaiKhoanId is null && AppRoles.RoleForJob(employee?.ChucVu) == AppRoles.Admin));
        if (role is null || (!AppRoles.AssignableStaff.Contains(role) && !preserveAdmin))
            ModelState.AddModelError(nameof(model.ChucVu), "Chọn vai trò nhân viên hợp lệ. Tài khoản Admin được quản lý riêng, không cấp qua hồ sơ nhân viên.");
        else model.ChucVu = role;
        model.MaNhanVien = model.MaNhanVien?.Trim() ?? "";
        if (await Db.NhanVien.AnyAsync(x => x.Id != id && x.MaNhanVien == model.MaNhanVien))
            ModelState.AddModelError(nameof(model.MaNhanVien), "Mã nhân viên đã tồn tại.");
    }

    private async Task LoadAccountAsync(NhanVien employee)
    {
        ViewBag.Employee = employee;
        if (employee.TaiKhoanId is not int id) return;
        var account = await users.FindByIdAsync(id.ToString());
        ViewBag.Account = account;
        ViewBag.AccountRole = account is null ? null : (await users.GetRolesAsync(account)).FirstOrDefault();
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
