using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;

// Project này là tầng dữ liệu dùng chung (entity, DbContext, migrations). REST API của hệ thống
// chạy trong RestaurantManagement.Web tại /api để dùng chung service nghiệp vụ với website.
// Chương trình chỉ còn lệnh --seed nạp dữ liệu minh họa cho database Development.
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<RestaurantDbContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Database=RestaurantManagement;Integrated Security=True;Encrypt=True;TrustServerCertificate=True"));
var app = builder.Build();

if (!args.Contains("--seed", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine("RestaurantManagement.API là tầng dữ liệu. Chạy website/API bằng RestaurantManagement.Web; " +
        "dùng --seed để nạp dữ liệu minh họa.");
    return;
}

await using var scope = app.Services.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
await db.Database.MigrateAsync();
await DbSeeder.SeedAsync(db);
Console.WriteLine($"Seed hoàn tất: NhanVien={await db.NhanVien.CountAsync()}, " +
    $"DanhMuc={await db.DanhMuc.CountAsync()}, MonAn={await db.MonAn.CountAsync()}, " +
    $"MonAnSize={await db.Set<MonAnSize>().CountAsync()}, " +
    $"NguyenLieu={await db.NguyenLieu.CountAsync()}, BanAn={await db.Set<BanAn>().CountAsync()}, " +
    $"PhieuNhap={await db.PhieuNhap.CountAsync()}.");
