using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Shared.Mobile;
using RestaurantManagement.Web.Security;

namespace RestaurantManagement.Web.Api;

[Route("api/auth")]
public class AuthApiController(RestaurantDbContext db, UserManager<TaiKhoan> userManager, SignInManager<TaiKhoan> signIn)
    : MobileApiControllerBase(db, userManager)
{
    private const string Invalid = "Email hoặc mật khẩu không đúng, hoặc tài khoản chưa được phép sử dụng.";

    // Trả về bearer token (định dạng Identity: tokenType, accessToken, expiresIn, refreshToken), hạn 8 giờ.
    [AllowAnonymous, HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = string.IsNullOrWhiteSpace(request.Email) ? null : await Users.FindByEmailAsync(request.Email.Trim());
        if (user is null) return Fail(Invalid, StatusCodes.Status401Unauthorized);
        // Kiểm tra mật khẩu trước để không lộ vai trò của email người khác; sai nhiều lần thì khóa như website.
        var check = await signIn.CheckPasswordSignInAsync(user, request.Password ?? "", lockoutOnFailure: true);
        if (check.IsLockedOut) return Fail("Tài khoản tạm khóa 15 phút sau nhiều lần đăng nhập sai.", StatusCodes.Status401Unauthorized);
        if (!check.Succeeded) return Fail(Invalid, StatusCodes.Status401Unauthorized);

        var roles = await Users.GetRolesAsync(user);
        if (roles.Count != 1 || !await IsActiveAsync(user.Id, roles[0])) return Fail(Invalid, StatusCodes.Status401Unauthorized);
        if (!MobileApi.AppUserRoles.Contains(roles[0]))
            return Fail("Ứng dụng mobile dành cho quản lý, tiếp tân, bồi bàn, thu ngân và khách hàng. Bếp và kho làm việc trên website.",
                StatusCodes.Status403Forbidden);

        var principal = await signIn.CreateUserPrincipalAsync(user);
        return SignIn(principal, IdentityConstants.BearerScheme);
    }

    [Authorize, HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> Me()
    {
        var user = await Users.GetUserAsync(User);
        if (user is null) return Unauthorized(new ApiError("Phiên đăng nhập đã hết hạn."));
        var role = (await Users.GetRolesAsync(user)).Single();
        var name = role == AppRoles.KhachHang
            ? await Db.KhachHang.Where(x => x.TaiKhoanId == user.Id).Select(x => x.HoTen).FirstOrDefaultAsync()
            : await Db.NhanVien.Where(x => x.TaiKhoanId == user.Id).Select(x => x.HoTen).FirstOrDefaultAsync();
        return new UserProfileDto(user.Email ?? "", string.IsNullOrWhiteSpace(name) ? user.Email ?? "" : name,
            role, AppRoles.Label(role), role == AppRoles.KhachHang,
            CanOrder: MobileApi.OrderRoles.Split(',').Contains(role),
            CanServe: MobileApi.ServeRoles.Split(',').Contains(role),
            CanPay: MobileApi.PayRoles.Split(',').Contains(role),
            CanReceive: MobileApi.ReceptionRoles.Split(',').Contains(role));
    }

    // Cùng điều kiện với ActiveAccountMiddleware: hồ sơ khách/nhân viên phải còn hoạt động.
    private async Task<bool> IsActiveAsync(int accountId, string role) => role switch
    {
        AppRoles.Admin => await Db.NhanVien.Where(x => x.TaiKhoanId == accountId).AllAsync(x => x.DangLamViec),
        AppRoles.KhachHang => await Db.KhachHang.AnyAsync(x => x.TaiKhoanId == accountId && x.DangSuDung),
        _ => AppRoles.AssignableStaff.Contains(role)
            && await Db.NhanVien.AnyAsync(x => x.TaiKhoanId == accountId && x.DangLamViec)
    };
}
