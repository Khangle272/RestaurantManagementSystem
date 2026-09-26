using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Security;

public static class AuthSetup
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration config)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
        await db.Database.MigrateAsync();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        foreach (var name in AppRoles.All)
        {
            if (!await roles.RoleExistsAsync(name))
            {
                var result = await roles.CreateAsync(new IdentityRole<int>(name));
                if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }

        var email = config["AuthBootstrap:AdminEmail"]?.Trim();
        var password = config["AuthBootstrap:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
        {
            Console.WriteLine("Đã khởi tạo vai trò. Chưa tạo Admin vì chưa cung cấp AuthBootstrap:AdminEmail/AdminPassword.");
            return;
        }
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Cần cung cấp cả AuthBootstrap:AdminEmail và AuthBootstrap:AdminPassword.");

        var users = scope.ServiceProvider.GetRequiredService<UserManager<TaiKhoan>>();
        var existing = await users.FindByEmailAsync(email);
        if (existing is null)
        {
            var user = new TaiKhoan { UserName = email, Email = email, EmailConfirmed = true };
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
            result = await users.AddToRoleAsync(user, AppRoles.Admin);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
            Console.WriteLine($"Đã tạo tài khoản Admin: {email}");
        }
        else if (await users.IsInRoleAsync(existing, AppRoles.Admin))
            Console.WriteLine("Tài khoản Admin đã tồn tại; không đổi mật khẩu.");
        else
            throw new InvalidOperationException("Email đã thuộc tài khoản khác; không tự nâng quyền Admin.");
    }
}
