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
    [AllowAnonymous, HttpGet(""), HttpGet("/Staff/Login")]
    public IActionResult Index(string? returnUrl = null)
    {
        if (User.IsInRole(AppRoles.Admin)) return RedirectToAction(nameof(TrangChu));
        var staffDestination = AppRoles.AssignableStaff.Where(User.IsInRole)
            .Select(AppRoles.StaffDestination).FirstOrDefault(x => x != null);
        if (staffDestination != null) return RedirectToAction("Index", staffDestination);
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous, HttpPost(""), HttpPost("/Staff/Login"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await users.FindByEmailAsync(model.Email.Trim());
        if (user != null)
        {
            var roles = await users.GetRolesAsync(user);
            var staffRole = roles.Count == 1 ? roles.Single() : null;
            var isAdmin = staffRole == AppRoles.Admin
                && await db.NhanVien.Where(x => x.TaiKhoanId == user.Id).AllAsync(x => x.DangLamViec);
            var isStaff = staffRole != null && AppRoles.AssignableStaff.Contains(staffRole)
                && await db.NhanVien.AnyAsync(x => x.TaiKhoanId == user.Id && x.DangLamViec);
            if ((isAdmin || isStaff) && !string.IsNullOrEmpty(user.SecurityStamp))
            {
                var result = await signIn.PasswordSignInAsync(user, model.Password, false, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    var destination = isAdmin ? "Admin" : AppRoles.StaffDestination(staffRole!);
                    if (destination != null)
                    {
                        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)
                            && model.ReturnUrl.StartsWith("/", StringComparison.Ordinal)
                            && !model.ReturnUrl.StartsWith("//", StringComparison.Ordinal))
                            return LocalRedirect(model.ReturnUrl);
                        return RedirectToAction(isAdmin ? nameof(TrangChu) : "Index", destination);
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

    [Authorize(Roles = AppRoles.NhanVien), HttpGet("TrangChu")]
    public async Task<IActionResult> TrangChu()
    {
        // Chi so tong quan chi hien cho Admin; nhan vien khac chi thay loi tat chuc nang.
        if (!User.IsInRole(AppRoles.Admin)) return View(new DashboardViewModel());
        var today = new DateTimeOffset(DateTime.Today);
        var tomorrow = today.AddDays(1);
        var paidToday = db.HoaDon.AsNoTracking()
            .Where(x => x.TrangThai == TrangThaiHoaDon.DaThanhToan && x.ThoiDiemLap >= today && x.ThoiDiemLap < tomorrow);
        var model = new DashboardViewModel
        {
            ShowStats = true,
            InvoicesToday = await paidToday.CountAsync(),
            RevenueToday = await paidToday.SumAsync(x => (decimal?)(x.TongTienHang - x.TienGiam)) ?? 0m,
            UnpaidInvoices = await db.HoaDon.AsNoTracking().CountAsync(x =>
                x.TrangThai == TrangThaiHoaDon.ChuaThanhToan || x.TrangThai == TrangThaiHoaDon.ThanhToanMotPhan),
            PendingReservations = await db.DatBan.AsNoTracking().CountAsync(x =>
                x.TrangThai == TrangThaiDatBan.ChoXacNhan || x.TrangThai == TrangThaiDatBan.ChoCoc),
            ReservationsToday = await db.DatBan.AsNoTracking().CountAsync(x =>
                x.GioDen >= today && x.GioDen < tomorrow
                && (x.TrangThai == TrangThaiDatBan.DaXacNhan || x.TrangThai == TrangThaiDatBan.DaNhanBan)),
            OccupiedTables = await db.BanAn.AsNoTracking().CountAsync(x => x.TrangThai == TrangThaiBan.DangPhucVu),
            TotalTables = await db.BanAn.AsNoTracking().CountAsync(x => x.TrangThai != TrangThaiBan.NgungSuDung),
            LowStockItems = await db.NguyenLieu.AsNoTracking().CountAsync(x => x.DangSuDung && x.SoLuongTon <= x.NguongCanhBao)
        };
        return View(model);
    }
}

public class DashboardViewModel
{
    public bool ShowStats { get; set; }
    public int InvoicesToday { get; set; }
    public decimal RevenueToday { get; set; }
    public int UnpaidInvoices { get; set; }
    public int PendingReservations { get; set; }
    public int ReservationsToday { get; set; }
    public int OccupiedTables { get; set; }
    public int TotalTables { get; set; }
    public int LowStockItems { get; set; }
}
