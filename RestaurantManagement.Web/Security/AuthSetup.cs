using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Security;

public static class AuthSetup
{
    public static async Task InitializeDemoAccountsAsync(IServiceProvider services, IConfiguration config)
    {
        var password = config["AuthBootstrap:DemoPassword"];
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Cần cung cấp AuthBootstrap:DemoPassword cho tài khoản thử.");

        await InitializeAsync(services, config);
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<TaiKhoan>>();
        var accounts = new (string Email, string Role, string Code, string Name, string Phone)[]
        {
            ("admin.demo@example.test", AppRoles.Admin, "", "", ""),
            ("khach.demo@example.test", AppRoles.KhachHang, "", "Khách thử", "0999000000"),
            ("tieptan.demo@example.test", AppRoles.TiepTan, "DEMO-TT", "Tiếp tân thử", "0999000001"),
            ("boiban.demo@example.test", AppRoles.BoiBan, "DEMO-BB", "Bồi bàn thử", "0999000002"),
            ("thungan.demo@example.test", AppRoles.ThuNgan, "DEMO-TN", "Thu ngân thử", "0999000003"),
            ("bep.demo@example.test", AppRoles.Bep, "DEMO-BEP", "Bếp thử", "0999000004"),
            ("kho.demo@example.test", AppRoles.Kho, "DEMO-KHO", "Kho thử", "0999000005")
        };

        await using var transaction = await db.Database.BeginTransactionAsync();
        foreach (var (email, role, code, name, phone) in accounts)
        {
            var user = await users.FindByEmailAsync(email);
            if (user is not null)
            {
                var assignedRoles = await users.GetRolesAsync(user);
                var hasProfile = role == AppRoles.Admin
                    || (role == AppRoles.KhachHang
                        ? await db.KhachHang.AnyAsync(x => x.TaiKhoanId == user.Id && x.DangSuDung)
                        : await db.NhanVien.AnyAsync(x => x.TaiKhoanId == user.Id && x.MaNhanVien == code && x.DangLamViec));
                if (assignedRoles.Count != 1 || !assignedRoles.Contains(role) || !hasProfile
                    || await users.IsLockedOutAsync(user) || !await users.CheckPasswordAsync(user, password))
                    throw new InvalidOperationException($"{email} đã tồn tại nhưng không khớp quyền, hồ sơ hoặc mật khẩu thử; không tự ghi đè.");
                Console.WriteLine($"Đã có tài khoản thử: {email}");
                continue;
            }

            if (role == AppRoles.KhachHang && await db.KhachHang.AnyAsync(x => x.SoDienThoai == phone)
                || code.Length > 0 && await db.NhanVien.AnyAsync(x => x.MaNhanVien == code))
                throw new InvalidOperationException($"Hồ sơ thử cho {email} đã bị trùng; không tự liên kết với dữ liệu khác.");

            user = new TaiKhoan { UserName = email, Email = email, EmailConfirmed = true };
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
            result = await users.AddToRoleAsync(user, role);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));

            if (role == AppRoles.KhachHang)
                db.KhachHang.Add(new KhachHang
                {
                    HoTen = name, SoDienThoai = phone, Email = email,
                    NgayDangKy = DateTimeOffset.UtcNow, TaiKhoanId = user.Id
                });
            else if (code.Length > 0)
                db.NhanVien.Add(new NhanVien
                {
                    MaNhanVien = code, HoTen = name, SoDienThoai = phone, Email = email,
                    ChucVu = role, NgayVaoLam = DateOnly.FromDateTime(DateTime.Today), TaiKhoanId = user.Id
                });
            await db.SaveChangesAsync();
            Console.WriteLine($"Đã tạo tài khoản thử: {email}");
        }
        await transaction.CommitAsync();
        Console.WriteLine("Tài khoản thử sẵn sàng. Chỉ dùng trên database Development; không đưa mật khẩu này vào dữ liệu thật.");
    }

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
