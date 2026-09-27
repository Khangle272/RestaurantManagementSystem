using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
    public async Task<IActionResult> Index()
    {
        var staff = await db.NhanVien.AsNoTracking().OrderBy(x => x.HoTen).ToListAsync();
        var accounts = await db.Users.AsNoTracking().ToDictionaryAsync(x => x.Id);
        var roleLinks = await db.UserRoles.AsNoTracking().ToListAsync();
        var roles = await db.Roles.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name ?? "");
        var items = staff.Select(x => new StaffAccountRow
        {
            NhanVienId = x.Id, HoTen = x.HoTen, MaNhanVien = x.MaNhanVien,
            DangLamViec = x.DangLamViec, TaiKhoanId = x.TaiKhoanId,
            Email = x.TaiKhoanId is int id && accounts.TryGetValue(id, out var account) ? account.Email : null,
            IsLocked = x.TaiKhoanId is int userId && accounts.TryGetValue(userId, out var account2)
                && account2.LockoutEnd > DateTimeOffset.UtcNow,
            Role = x.TaiKhoanId is int roleUserId
                ? roleLinks.Where(link => link.UserId == roleUserId).Select(link => roles.GetValueOrDefault(link.RoleId)).FirstOrDefault()
                : null
        }).ToList();
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await FillEmployeesAsync();
        return View(new CreateStaffAccountViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateStaffAccountViewModel model)
    {
        if (!AppRoles.AssignableStaff.Contains(model.Role))
            ModelState.AddModelError(nameof(model.Role), "Vai trò không hợp lệ.");
        var staff = await db.NhanVien.SingleOrDefaultAsync(x => x.Id == model.NhanVienId);
        if (staff is null || !staff.DangLamViec || staff.TaiKhoanId is not null)
            ModelState.AddModelError(nameof(model.NhanVienId), "Nhân viên không tồn tại, đã nghỉ hoặc đã có tài khoản.");
        model.Email = model.Email.Trim();
        if (await users.FindByEmailAsync(model.Email) is not null)
            ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng.");
        if (!ModelState.IsValid) { await FillEmployeesAsync(); return View(model); }

        await using var transaction = await db.Database.BeginTransactionAsync();
        var user = new TaiKhoan { UserName = model.Email, Email = model.Email, EmailConfirmed = true };
        var created = await users.CreateAsync(user, model.Password);
        if (!created.Succeeded)
        {
            foreach (var error in created.Errors) ModelState.AddModelError("", error.Description);
            await FillEmployeesAsync(); return View(model);
        }
        var assigned = await users.AddToRoleAsync(user, model.Role);
        if (!assigned.Succeeded)
        {
            foreach (var error in assigned.Errors) ModelState.AddModelError("", error.Description);
            await FillEmployeesAsync(); return View(model);
        }
        staff!.TaiKhoanId = user.Id;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["Success"] = "Đã cấp tài khoản cho nhân viên.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetLocked(int id, bool locked)
    {
        var staff = await db.NhanVien.SingleOrDefaultAsync(x => x.Id == id);
        if (staff?.TaiKhoanId is not int accountId) return NotFound();
        var user = await users.FindByIdAsync(accountId.ToString());
        if (user is null) return NotFound();
        var result = await users.SetLockoutEndDateAsync(user, locked ? DateTimeOffset.UtcNow.AddYears(20) : null);
        if (!result.Succeeded) return BadRequest();
        if (!locked)
        {
            result = await users.ResetAccessFailedCountAsync(user);
            if (!result.Succeeded) return BadRequest();
        }
        if (locked) await users.UpdateSecurityStampAsync(user);
        TempData["Success"] = locked ? "Đã khóa tài khoản." : "Đã mở khóa tài khoản.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(int id, string role)
    {
        if (!AppRoles.AssignableStaff.Contains(role)) return BadRequest();
        var staff = await db.NhanVien.SingleOrDefaultAsync(x => x.Id == id && x.TaiKhoanId != null);
        if (staff?.TaiKhoanId is not int accountId) return NotFound();
        var user = await users.FindByIdAsync(accountId.ToString());
        if (user is null) return NotFound();
        var current = await users.GetRolesAsync(user);
        if (current.Contains(AppRoles.Admin) || current.Contains(AppRoles.KhachHang)) return BadRequest();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var removed = await users.RemoveFromRolesAsync(user, current);
        if (!removed.Succeeded) return BadRequest();
        var added = await users.AddToRoleAsync(user, role);
        if (!added.Succeeded) return BadRequest();
        await users.UpdateSecurityStampAsync(user);
        await transaction.CommitAsync();
        TempData["Success"] = "Đã đổi vai trò. Nhân viên cần đăng nhập lại.";
        return RedirectToAction(nameof(Index));
    }

    private async Task FillEmployeesAsync() => ViewBag.Employees = await db.NhanVien.AsNoTracking()
        .Where(x => x.DangLamViec && x.TaiKhoanId == null)
        .OrderBy(x => x.HoTen)
        .Select(x => new SelectListItem(x.MaNhanVien + " – " + x.HoTen, x.Id.ToString()))
        .ToListAsync();
}
