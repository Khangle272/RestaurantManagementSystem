using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Security;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin + "," + AppRoles.ThuNgan)]
public class HoaDonController(RestaurantDbContext db, UserManager<TaiKhoan> users, RestaurantManagement.Web.Services.OrderService orderService) : Controller
{
    public async Task<IActionResult> Index(bool daThanhToan = false)
    {
        var query = db.HoaDon.AsNoTracking();
        query = daThanhToan
            ? query.Where(x => x.TrangThai == TrangThaiHoaDon.DaThanhToan)
            : query.Where(x => x.TrangThai == TrangThaiHoaDon.ChuaThanhToan
                || x.TrangThai == TrangThaiHoaDon.ThanhToanMotPhan);

        ViewBag.DaThanhToan = daThanhToan;
        return View(await query.OrderByDescending(x => x.ThoiDiemLap).Take(100).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var invoice = await db.HoaDon.AsSplitQuery()
            .Include(x => x.ChiTiet).ThenInclude(x => x.MonAnSize)
            .Include(x => x.DatBan).ThenInclude(x => x!.Ban).ThenInclude(x => x.BanAn)
            .SingleOrDefaultAsync(x => x.Id == id);
        if (invoice is null) return NotFound();
        ViewBag.RowVersion = Convert.ToBase64String(db.Entry(invoice).Property<byte[]>("RowVersion").CurrentValue ?? []);
        ViewBag.BookingRowVersion = invoice.DatBan is null ? ""
            : Convert.ToBase64String(db.Entry(invoice.DatBan).Property<byte[]>("RowVersion").CurrentValue ?? []);
        var total = invoice.ChiTiet.Where(x => x.TrangThai != TrangThaiCheBien.DaHuy).Sum(x => x.SoLuong * x.DonGia);
        var available = invoice.DatBanId is int bookingId && invoice.TrangThai == TrangThaiHoaDon.ChuaThanhToan
            ? await orderService.AvailableDepositAsync(bookingId, invoice.Id) : 0;
        var deposit = invoice.TrangThai == TrangThaiHoaDon.ChuaThanhToan
            ? Math.Min(available, Math.Max(0, total - invoice.TienGiam))
            : invoice.TienCocDaTru;
        ViewBag.AvailableDeposit = available;
        ViewBag.ActiveTotal = total;
        ViewBag.DepositDeduction = deposit;
        ViewBag.PaymentAmount = Math.Max(0, total - invoice.TienGiam - deposit);
        return View(invoice);
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.ThuNgan)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmCash(int id, string? rowVersion, string? bookingRowVersion)
    {
        if (!int.TryParse(users.GetUserId(User), out var accountId)) return Challenge();
        var cashierId = await db.NhanVien.AsNoTracking()
            .Where(x => x.TaiKhoanId == accountId && x.DangLamViec)
            .Select(x => x.Id).SingleOrDefaultAsync();

        if (cashierId == 0 && User.IsInRole(AppRoles.Admin))
        {
            cashierId = await db.NhanVien.AsNoTracking().Where(x => x.DangLamViec).Select(x => x.Id).FirstOrDefaultAsync();
        }

        if (cashierId == 0) return Forbid();

        var expectedVersion = new byte[8];
        if (rowVersion is null || !Convert.TryFromBase64String(rowVersion, expectedVersion, out var length) || length != 8)
            return BadRequest();
        var invoice = await db.HoaDon.AsNoTracking().Include(x => x.ChiTiet).SingleOrDefaultAsync(x => x.Id == id);
        if (invoice is null) return NotFound();
        var total = invoice.ChiTiet.Where(x => x.TrangThai != TrangThaiCheBien.DaHuy).Sum(x => x.SoLuong * x.DonGia);
        var deposit = invoice.DatBanId is int bookingId ? await orderService.AvailableDepositAsync(bookingId, id) : 0;
        var error = await orderService.ProcessPaymentAsync(new RestaurantManagement.Web.Models.PaymentFormModel
        {
            Id = id, RowVersion = rowVersion, BookingRowVersion = bookingRowVersion,
            PhuongThuc = PhuongThucThanhToan.TienMat,
            TienKhachDua = Math.Max(0, total - invoice.TienGiam - deposit)
        }, cashierId);
        if (error is not null) TempData["Error"] = error;
        else TempData["Success"] = "Đã xác nhận thu tiền mặt.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.ThuNgan)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ThanhToan(RestaurantManagement.Web.Models.PaymentFormModel form)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Thông tin thanh toán không hợp lệ. Vui lòng kiểm tra lại các số tiền.";
            return RedirectToAction(nameof(Details), new { id = form.Id });
        }
        if (!int.TryParse(users.GetUserId(User), out var accountId)) return Challenge();
        var cashierId = await db.NhanVien.AsNoTracking()
            .Where(x => x.TaiKhoanId == accountId && x.DangLamViec)
            .Select(x => x.Id).SingleOrDefaultAsync();

        if (cashierId == 0 && User.IsInRole(AppRoles.Admin))
        {
            cashierId = await db.NhanVien.AsNoTracking().Where(x => x.DangLamViec).Select(x => x.Id).FirstOrDefaultAsync();
        }

        if (cashierId == 0) return Forbid();

        var error = await orderService.ProcessPaymentAsync(form, cashierId);
        if (error != null)
        {
            TempData["Error"] = error;
        }
        else
        {
            TempData["Success"] = $"Đã thanh toán hóa đơn thành công bằng hình thức {form.PhuongThuc}.";
        }

        return RedirectToAction(nameof(Details), new { id = form.Id });
    }
}
