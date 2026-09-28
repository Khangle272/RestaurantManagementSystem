using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Security;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class TaiKhoanNhanVienController(
    RestaurantDbContext db,
    UserManager<TaiKhoan> users) : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("Index", "NhanVien");
    }

    [HttpGet]
    public async Task<IActionResult> Create(int nhanVienId)
    {
        var staff = await db.NhanVien.AsNoTracking().SingleOrDefaultAsync(x => x.Id == nhanVienId);
        if (staff is null || !staff.DangLamViec || staff.TaiKhoanId != null) return NotFound();
        ViewBag.Employee = staff;
        return View(new CreateStaffAccountViewModel { NhanVienId = staff.Id, Email = staff.Email ?? "" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateStaffAccountViewModel model)
    {
        var staff = await db.NhanVien.SingleOrDefaultAsync(x => x.Id == model.NhanVienId);
        if (staff is null || !staff.DangLamViec || staff.TaiKhoanId is not null)
            ModelState.AddModelError(nameof(model.NhanVienId), "Nhân viên không tồn tại, đã nghỉ hoặc đã có tài khoản.");
        var role = AppRoles.RoleForJob(staff?.ChucVu);
        if (role is null || !AppRoles.AssignableStaff.Contains(role))
            ModelState.AddModelError("", "Chọn vai trò nhân viên hợp lệ trong hồ sơ trước khi cấp tài khoản.");
        model.Email = model.Email.Trim();
        if (await users.FindByEmailAsync(model.Email) is not null)
            ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng.");
        ViewBag.Employee = staff;
        if (!ModelState.IsValid) return View(model);

        await using var transaction = await db.Database.BeginTransactionAsync();
        var user = new TaiKhoan { UserName = model.Email, Email = model.Email, EmailConfirmed = true };
        var created = await users.CreateAsync(user, model.Password);
        if (!created.Succeeded)
        {
            foreach (var error in created.Errors) ModelState.AddModelError("", error.Description);
            return View(model);
        }
        var assigned = await users.AddToRoleAsync(user, role!);
        if (!assigned.Succeeded)
        {
            foreach (var error in assigned.Errors) ModelState.AddModelError("", error.Description);
            return View(model);
        }
        staff!.TaiKhoanId = user.Id;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Hồ sơ đã thay đổi. Vui lòng mở lại hồ sơ nhân viên để cấp tài khoản.");
            return View(model);
        }
        await transaction.CommitAsync();
        TempData["Success"] = "Đã cấp tài khoản cho nhân viên.";
        return RedirectToAction("Edit", "NhanVien", new { id = staff.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetLocked(int id, bool locked)
    {
        var staff = await db.NhanVien.SingleOrDefaultAsync(x => x.Id == id);
        if (staff?.TaiKhoanId is not int accountId) return NotFound();
        var user = await users.FindByIdAsync(accountId.ToString());
        if (user is null) return NotFound();
        if (string.IsNullOrEmpty(user.SecurityStamp)) return BadRequest();
        var roles = await users.GetRolesAsync(user);
        if (roles.Count != 1 || !AppRoles.AssignableStaff.Contains(roles[0])) return BadRequest();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var result = await users.SetLockoutEndDateAsync(user, locked ? DateTimeOffset.UtcNow.AddYears(20) : null);
        if (!result.Succeeded) return BadRequest();
        if (!locked)
        {
            result = await users.ResetAccessFailedCountAsync(user);
            if (!result.Succeeded) return BadRequest();
        }
        if (locked && !(await users.UpdateSecurityStampAsync(user)).Succeeded) return BadRequest();
        await transaction.CommitAsync();
        TempData["Success"] = locked ? "Đã khóa tài khoản." : "Đã mở khóa tài khoản.";
        return RedirectToAction("Edit", "NhanVien", new { id });
    }

}
