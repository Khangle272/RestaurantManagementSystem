using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Security;

namespace RestaurantManagement.Web.Controllers;

[Route("admin")]
public class AdminController(RestaurantDbContext db, UserManager<TaiKhoan> users,
    SignInManager<TaiKhoan> signIn) : Controller
{
    [AllowAnonymous, HttpGet("")]
    public IActionResult Index(string? returnUrl = null)
    {
        if (User.IsInRole(AppRoles.Admin)) return RedirectToAction("Index", "MonAn");
        var staffDestination = AppRoles.AssignableStaff.Where(User.IsInRole)
            .Select(AppRoles.StaffDestination).FirstOrDefault(x => x != null);
        if (staffDestination != null) return RedirectToAction("Index", staffDestination);
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous, HttpPost(""), ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await users.FindByEmailAsync(model.Email.Trim());
        if (user != null)
        {
            var roles = await users.GetRolesAsync(user);
            var staffRole = roles.Count == 1 ? roles.Single() : null;
            var isAdmin = staffRole == AppRoles.Admin;
            var isStaff = staffRole != null && AppRoles.AssignableStaff.Contains(staffRole)
                && await db.NhanVien.AnyAsync(x => x.TaiKhoanId == user.Id && x.DangLamViec);
            if (isAdmin || isStaff)
            {
                var result = await signIn.PasswordSignInAsync(user, model.Password, false, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    var destination = isAdmin ? "MonAn" : AppRoles.StaffDestination(staffRole!);
                    if (destination != null)
                    {
                        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)
                            && model.ReturnUrl.StartsWith("/", StringComparison.Ordinal)
                            && !model.ReturnUrl.StartsWith("//", StringComparison.Ordinal))
                            return LocalRedirect(model.ReturnUrl);
                        return RedirectToAction("Index", destination);
                    }
                    await signIn.SignOutAsync();
                }
                if (result.IsLockedOut)
                    ModelState.AddModelError("", "Tài khoản tạm khóa 15 phút sau nhiều lần đăng nhập sai.");
            }
        }
        ModelState.AddModelError("", "Thông tin đăng nhập không đúng hoặc tài khoản chưa được cấp quyền quản trị.");
        return View(model);
    }
}
