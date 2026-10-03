using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Security;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<RestaurantDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentity<TaiKhoan, IdentityRole<int>>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
    .AddEntityFrameworkStores<RestaurantDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Denied";
    options.Events.OnRedirectToLogin = context =>
    {
        var path = context.Request.Path.Value ?? "";
        var management = new[] { "/admin", "/MonAn", "/DanhMuc", "/NhanVien", "/TaiKhoanNhanVien",
            "/NguyenLieu", "/BanAn", "/QuanLyDatBan", "/HoaDon", "/DonHang", "/Bep", "/TaiKhoanKhachHang", "/SoDoBan", "/KhuVuc", "/Staff", "/Account/ResetPassword",
            "/Kho", "/NhapKho", "/XuatKho", "/ThanhLy", "/NhaCungCap" };
        var login = management.Any(x => path.Equals(x, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(x + "/", StringComparison.OrdinalIgnoreCase)) ? "/admin" : "/Account/Login";
        context.Response.Redirect(login + "?returnUrl=" + Uri.EscapeDataString(context.Request.Path + context.Request.QueryString));
        return Task.CompletedTask;
    };
});
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.Zero);
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<RestaurantManagement.Web.Services.TableService>();
builder.Services.AddScoped<RestaurantManagement.Web.Services.OrderService>();

var app = builder.Build();

if (args.Contains("--init-auth", StringComparer.OrdinalIgnoreCase))
{
    await AuthSetup.InitializeAsync(app.Services, builder.Configuration);
    return;
}

if (args.Contains("--init-demo-accounts", StringComparer.OrdinalIgnoreCase))
{
    if (!app.Environment.IsDevelopment())
        throw new InvalidOperationException("Chỉ được tạo tài khoản thử trong môi trường Development.");
    await AuthSetup.InitializeDemoAccountsAsync(app.Services, builder.Configuration);
    return;
}

if (args.Contains("--import-sql-data", StringComparer.OrdinalIgnoreCase))
{
    if (!app.Environment.IsDevelopment())
        throw new InvalidOperationException("Chỉ được nạp dữ liệu mẫu trong môi trường Development.");
    await AuthSetup.InitializeAsync(app.Services, builder.Configuration);
    await SampleDataImport.RunAsync(app.Services);
    return;
}

if (args.Contains("--remove-retired-menu-roles", StringComparer.OrdinalIgnoreCase))
{
    if (!app.Environment.IsDevelopment())
        throw new InvalidOperationException("Chỉ được dọn vai trò mẫu cũ trong môi trường Development.");
    await AuthSetup.InitializeAsync(app.Services, builder.Configuration);
    await RetiredRoleCleanup.RunAsync(app.Services);
    return;
}

// Đồng bộ schema và các vai trò; tài khoản nhân viên được cấp qua giao diện quản trị.
await AuthSetup.InitializeAsync(app.Services, builder.Configuration);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseMiddleware<ActiveAccountMiddleware>();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
