using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Security;

public sealed class ActiveAccountMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, RestaurantDbContext db,
        UserManager<TaiKhoan> users, SignInManager<TaiKhoan> signIn)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var user = await users.GetUserAsync(context.User);
            var active = user is not null && !await users.IsLockedOutAsync(user) &&
                (context.User.IsInRole(AppRoles.Admin)
                || (context.User.IsInRole(AppRoles.KhachHang)
                    && await db.KhachHang.AnyAsync(x => x.TaiKhoanId == user.Id && x.DangSuDung))
                || (AppRoles.AssignableStaff.Any(context.User.IsInRole)
                    && await db.NhanVien.AnyAsync(x => x.TaiKhoanId == user.Id && x.DangLamViec)));
            if (!active)
            {
                await signIn.SignOutAsync();
                context.User = new ClaimsPrincipal();
            }
        }
        await next(context);
    }
}
