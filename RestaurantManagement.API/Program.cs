using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<RestaurantDbContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("RestaurantDb")
    ?? "Server=localhost;Database=RestaurantManagement;Integrated Security=True;Encrypt=True;TrustServerCertificate=True"));
builder.Services.AddIdentityCore<TaiKhoan>()
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<RestaurantDbContext>();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (args.Contains("--seed", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
    Console.WriteLine($"Seed hoàn tất: NhanVien={await db.NhanVien.CountAsync()}, " +
        $"DanhMuc={await db.DanhMuc.CountAsync()}, MonAn={await db.MonAn.CountAsync()}, " +
        $"NguyenLieu={await db.NguyenLieu.CountAsync()}, BanAn={await db.Set<BanAn>().CountAsync()}, " +
        $"PhieuNhap={await db.PhieuNhap.CountAsync()}.");
    return;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
