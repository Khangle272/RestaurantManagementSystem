using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Security;

namespace RestaurantManagement.Web.Controllers;

public class AccountController(
    RestaurantDbContext db,
    UserManager<TaiKhoan> users,
    SignInManager<TaiKhoan> signIn) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return User.IsInRole(AppRoles.KhachHang)
                ? RedirectToAction("Index", "Home") : RedirectToAction("Index", "Staff");
        ViewData["StaffLogin"] = false;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
        => await LoginCore(model, staffPortal: false);

    [AllowAnonymous, HttpGet("/Staff/Login")]
    public IActionResult StaffLogin(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true && !User.IsInRole(AppRoles.KhachHang))
            return RedirectToAction("Index", "Staff");
        ViewData["StaffLogin"] = true;
        return View("Login", new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous, HttpPost("/Staff/Login"), ValidateAntiForgeryToken]
    public async Task<IActionResult> StaffLogin(LoginViewModel model)
        => await LoginCore(model, staffPortal: true);

    private async Task<IActionResult> LoginCore(LoginViewModel model, bool staffPortal)
    {
        ViewData["StaffLogin"] = staffPortal;
        if (!ModelState.IsValid) return View("Login", model);
        var user = await users.FindByEmailAsync(model.Email.Trim());
        if (user is not null)
        {
            var roles = await users.GetRolesAsync(user);
            var eligible = staffPortal
                ? !roles.Contains(AppRoles.KhachHang) && ((roles.Contains(AppRoles.Admin)
                    && await db.NhanVien.Where(x => x.TaiKhoanId == user.Id).AllAsync(x => x.DangLamViec))
                    || (roles.Any(x => AppRoles.AssignableStaff.Contains(x)) && await db.NhanVien.AnyAsync(x => x.TaiKhoanId == user.Id && x.DangLamViec)))
                : roles.Contains(AppRoles.KhachHang) && roles.All(x => x == AppRoles.KhachHang)
                    && await db.KhachHang.AnyAsync(x => x.TaiKhoanId == user.Id && x.DangSuDung);
            if (eligible && !string.IsNullOrEmpty(user.SecurityStamp))
            {
                var result = await signIn.PasswordSignInAsync(user, model.Password, false, lockoutOnFailure: true);
                if (result.Succeeded)
                    return !string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)
                        ? LocalRedirect(model.ReturnUrl)
                        : roles.Contains(AppRoles.ThuNgan)
                            ? RedirectToAction("Index", "HoaDon")
                            : staffPortal ? RedirectToAction("Index", "Staff") : RedirectToAction("Index", "Home");
                if (result.IsLockedOut) ModelState.AddModelError("", "Tài khoản tạm khóa 15 phút sau nhiều lần đăng nhập sai.");
                else ModelState.AddModelError("", "Email hoặc mật khẩu không đúng, hoặc tài khoản chưa được phép sử dụng.");
                return View("Login", model);
            }
        }
        ModelState.AddModelError("", "Email hoặc mật khẩu không đúng, hoặc tài khoản chưa được phép sử dụng.");
        return View("Login", model);
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        model.Email = model.Email.Trim();
        model.SoDienThoai = model.SoDienThoai.Trim();
        if (await db.KhachHang.AnyAsync(x => x.SoDienThoai == model.SoDienThoai))
        {
            ModelState.AddModelError(nameof(model.SoDienThoai), "Số điện thoại đã có hồ sơ khách hàng. Vui lòng liên hệ nhà hàng để liên kết tài khoản.");
            return View(model);
        }
        if (await users.FindByEmailAsync(model.Email) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng.");
            return View(model);
        }
        if (!await db.Roles.AnyAsync(x => x.Name == AppRoles.KhachHang))
        {
            ModelState.AddModelError("", "Chưa khởi tạo vai trò hệ thống. Vui lòng báo người quản trị.");
            return View(model);
        }

        await using var transaction = await db.Database.BeginTransactionAsync();
        var user = new TaiKhoan { UserName = model.Email, Email = model.Email, PhoneNumber = model.SoDienThoai, EmailConfirmed = true };
        var created = await users.CreateAsync(user, model.Password);
        if (!created.Succeeded)
        {
            foreach (var error in created.Errors) ModelState.AddModelError("", error.Description);
            return View(model);
        }
        var assigned = await users.AddToRoleAsync(user, AppRoles.KhachHang);
        if (!assigned.Succeeded)
        {
            foreach (var error in assigned.Errors) ModelState.AddModelError("", error.Description);
            return View(model);
        }
        db.KhachHang.Add(new KhachHang
        {
            HoTen = model.HoTen.Trim(), SoDienThoai = model.SoDienThoai, Email = model.Email,
            NgayDangKy = DateTimeOffset.UtcNow, TaiKhoanId = user.Id,
            DongYNhanUuDai = model.DongYNhanUuDai,
            ThoiDiemDongYNhanUuDai = model.DongYNhanUuDai ? DateTimeOffset.UtcNow : null
        });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Không lưu được hồ sơ. Email hoặc số điện thoại có thể đã được dùng.");
            return View(model);
        }
        await transaction.CommitAsync();
        await signIn.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Home");
    }

    [Authorize, HttpGet]
    public IActionResult Index() => View();

    [Authorize, HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge();
        var result = await users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View(model);
        }
        await signIn.RefreshSignInAsync(user);
        TempData["Success"] = "Đã đổi mật khẩu.";
        return RedirectToAction("Index", "Account");
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var staff = !User.IsInRole(AppRoles.KhachHang);
        await signIn.SignOutAsync();
        return staff ? RedirectToAction(nameof(StaffLogin)) : RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult Denied() => View();
}
