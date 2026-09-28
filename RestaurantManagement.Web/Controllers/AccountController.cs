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

    // Customers can edit only themselves; staff profiles are controlled by Admin.
    private async Task<TaiKhoan?> EditableAccount(int? id)
    {
        var actor = await users.GetUserAsync(User);
        if (actor is null) return null;
        if (!User.IsInRole(AppRoles.Admin))
            return User.IsInRole(AppRoles.KhachHang) && (id is null || id == actor.Id) ? actor : null;
        var target = id is null || id == actor.Id ? actor : await users.FindByIdAsync(id.Value.ToString());
        if (target is null || string.IsNullOrEmpty(target.SecurityStamp)) return null;
        var roles = await users.GetRolesAsync(target);
        if (target.Id == actor.Id) return target;
        if (roles.Count != 1) return null;
        return roles[0] == AppRoles.KhachHang
            ? await db.KhachHang.AnyAsync(x => x.TaiKhoanId == target.Id) ? target : null
            : AppRoles.AssignableStaff.Contains(roles[0]) && await db.NhanVien.AnyAsync(x => x.TaiKhoanId == target.Id) ? target : null;
    }

    private async Task<bool> ConfirmPassword(TaiKhoan actor, string password)
    {
        if (await users.IsLockedOutAsync(actor)) return false;
        if (!await users.CheckPasswordAsync(actor, password))
        { await users.AccessFailedAsync(actor); return false; }
        return actor.AccessFailedCount == 0 || (await users.ResetAccessFailedCountAsync(actor)).Succeeded;
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.KhachHang), HttpGet]
    public async Task<IActionResult> EditProfile(int? id)
    {
        var target = await EditableAccount(id);
        if (target is null) return NotFound();
        var customer = await db.KhachHang.AsNoTracking().SingleOrDefaultAsync(x => x.TaiKhoanId == target.Id);
        var employee = await db.NhanVien.AsNoTracking().SingleOrDefaultAsync(x => x.TaiKhoanId == target.Id);
        ViewData["CanEditName"] = customer is not null || employee is not null;
        return View(new EditAccountViewModel { Id = target.Id, Email = target.Email ?? "",
            SoDienThoai = customer?.SoDienThoai ?? employee?.SoDienThoai ?? target.PhoneNumber ?? "",
            HoTen = customer?.HoTen ?? employee?.HoTen, ConcurrencyStamp = target.ConcurrencyStamp ?? "" });
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.KhachHang), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(EditAccountViewModel model)
    {
        var target = await EditableAccount(model.Id);
        if (target is null) return NotFound();
        var customer = await db.KhachHang.SingleOrDefaultAsync(x => x.TaiKhoanId == target.Id);
        var employee = await db.NhanVien.SingleOrDefaultAsync(x => x.TaiKhoanId == target.Id);
        ViewData["CanEditName"] = customer is not null || employee is not null;
        if (!ModelState.IsValid) return View(model);
        if (target.ConcurrencyStamp != model.ConcurrencyStamp)
        { ModelState.AddModelError("", "Thông tin đã thay đổi. Vui lòng tải lại trang."); return View(model); }
        var actor = await users.GetUserAsync(User);
        if (actor is null || !await ConfirmPassword(actor, model.ConfirmPassword))
        {
            model.ConcurrencyStamp = target.ConcurrencyStamp ?? "";
            ModelState.Remove(nameof(model.ConcurrencyStamp));
            ModelState.AddModelError(nameof(model.ConfirmPassword), "Mật khẩu xác nhận không đúng."); return View(model);
        }
        model.ConcurrencyStamp = target.ConcurrencyStamp ?? "";
        ModelState.Remove(nameof(model.ConcurrencyStamp));
        var email = model.Email.Trim();
        var phone = model.SoDienThoai.Trim();
        var duplicate = await users.FindByEmailAsync(email);
        if (duplicate is not null && duplicate.Id != target.Id)
            ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng.");
        if ((customer is not null || employee is not null) && string.IsNullOrWhiteSpace(model.HoTen))
            ModelState.AddModelError(nameof(model.HoTen), "Vui lòng nhập họ tên.");
        if (customer is not null && await db.KhachHang.AnyAsync(x => x.Id != customer.Id && x.SoDienThoai == phone)
            || employee is not null && await db.NhanVien.AnyAsync(x => x.Id != employee.Id && x.SoDienThoai == phone))
            ModelState.AddModelError(nameof(model.SoDienThoai), "Số điện thoại đã được sử dụng.");
        if (customer is null && employee is null && !string.IsNullOrWhiteSpace(model.HoTen))
            ModelState.AddModelError(nameof(model.HoTen), "Tài khoản quản trị này chưa có hồ sơ nhân viên để lưu họ tên.");
        if (!ModelState.IsValid) return View(model);
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            target.Email = email; target.UserName = email; target.PhoneNumber = phone;
            var result = await users.UpdateAsync(target);
            if (result.Succeeded) result = await users.UpdateSecurityStampAsync(target);
            if (!result.Succeeded)
            { foreach (var error in result.Errors) ModelState.AddModelError("", error.Description); return View(model); }
            if (customer is not null) { customer.HoTen = model.HoTen!.Trim(); customer.Email = email; customer.SoDienThoai = phone; }
            if (employee is not null) { employee.HoTen = model.HoTen!.Trim(); employee.Email = email; employee.SoDienThoai = phone; }
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        { ModelState.AddModelError("", "Không lưu được thông tin. Dữ liệu có thể đã trùng hoặc thay đổi, vui lòng tải lại."); return View(model); }
        await transaction.CommitAsync();
        if (target.Id == actor.Id) await signIn.RefreshSignInAsync(target);
        TempData["Success"] = "Đã cập nhật thông tin tài khoản.";
        return target.Id == actor.Id ? RedirectToAction(nameof(Index))
            : customer is not null ? RedirectToAction("Index", "TaiKhoanKhachHang")
            : RedirectToAction("Edit", "NhanVien", new { id = employee!.Id });
    }

    [Authorize(Roles = AppRoles.Admin), HttpGet]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var target = await EditableAccount(id);
        if (target is null || target.Id.ToString() == users.GetUserId(User)) return NotFound();
        ViewData["TargetEmail"] = target.Email;
        return View(new ResetAccountPasswordViewModel { Id = id });
    }

    [Authorize(Roles = AppRoles.Admin), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetAccountPasswordViewModel model)
    {
        var target = await EditableAccount(model.Id);
        var actor = await users.GetUserAsync(User);
        if (target is null || actor is null || target.Id == actor.Id) return NotFound();
        ViewData["TargetEmail"] = target.Email;
        if (!ModelState.IsValid) return View(model);
        if (!await ConfirmPassword(actor, model.AdminPassword))
        { ModelState.AddModelError(nameof(model.AdminPassword), "Mật khẩu quản trị không đúng."); return View(model); }
        var token = await users.GeneratePasswordResetTokenAsync(target);
        var result = await users.ResetPasswordAsync(target, token, model.NewPassword);
        if (!result.Succeeded)
        { foreach (var error in result.Errors) ModelState.AddModelError("", error.Description); return View(model); }
        TempData["Success"] = "Đã đặt lại mật khẩu. Phiên đăng nhập cũ của tài khoản đã bị thu hồi; trạng thái khóa không thay đổi.";
        var employee = await db.NhanVien.AsNoTracking().SingleOrDefaultAsync(x => x.TaiKhoanId == target.Id);
        return employee is not null ? RedirectToAction("Edit", "NhanVien", new { id = employee.Id })
            : RedirectToAction("Index", "TaiKhoanKhachHang");
    }

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
