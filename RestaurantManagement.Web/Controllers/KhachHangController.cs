using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Security;
using RestaurantManagement.Web.Services;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.KhachHang)]
public class KhachHangController(RestaurantDbContext db, UserManager<TaiKhoan> users,
    CustomerVoucherService vouchers, CustomerHistoryService history, IPromotionService promotions) : Controller
{
    private Task<int?> CustomerId() {
        if (!int.TryParse(users.GetUserId(User), out var accountId)) return Task.FromResult<int?>(null);
        return db.KhachHang.Where(x => x.TaiKhoanId == accountId && x.DangSuDung).Select(x => (int?)x.Id).SingleOrDefaultAsync();
    }
    public async Task<IActionResult> Index(int orderPage = 1, int pointPage = 1, TrangThaiHoaDon? status = null) {
        var id = await CustomerId(); if (id is null) return Forbid();
        return View("~/Views/Shared/CustomerHistory.cshtml", await history.LoadAsync(id.Value, false, orderPage, pointPage, status));
    }
    public async Task<IActionResult> Vouchers(bool wallet = false) {
        var id = await CustomerId(); if (id is null) return Forbid();
        ViewBag.Wallet = wallet; ViewBag.PointBalance = await history.BalanceAsync(id.Value);
        return View(await vouchers.ListAsync(id.Value, wallet));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Collect(int id) {
        var customerId = await CustomerId(); if (customerId is null) return Forbid();
        var error = await vouchers.CollectAsync(customerId.Value, id);
        TempData[error is null ? "Success" : "Error"] = error ?? "Đã lưu voucher vào ví.";
        return RedirectToAction(nameof(Vouchers), new { wallet = true });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Redeem(int id, int expectedPoints) {
        var customerId = await CustomerId(); if (customerId is null) return Forbid();
        var error = await vouchers.RedeemAsync(customerId.Value, id, expectedPoints);
        TempData[error is null ? "Success" : "Error"] = error ?? "Đã đổi điểm và nhận voucher vào ví.";
        return RedirectToAction(nameof(Vouchers), new { wallet = error is null });
    }
    public async Task<IActionResult> Order(int id) {
        var customerId = await CustomerId(); if (customerId is null) return Forbid();
        var bill = await db.HoaDon.Include(x => x.ChiTiet).Include(x => x.Voucher).SingleOrDefaultAsync(x => x.Id == id && x.KhachHangId == customerId);
        if (bill is null) return NotFound();
        ViewBag.RowVersion = Convert.ToBase64String(db.Entry(bill).Property<byte[]>("RowVersion").CurrentValue ?? []);
        ViewBag.Wallet = await vouchers.ListAsync(customerId.Value, true);
        ViewBag.Evaluation = await promotions.EvaluatePromotionsAsync(id, bill.Voucher?.Ma);
        return View(bill);
    }
    [HttpGet]
    public async Task<IActionResult> OrderStatus(int id) {
        var customerId = await CustomerId(); if (customerId is null) return Forbid();
        var bill = await db.HoaDon.AsNoTracking().Include(x => x.ChiTiet).SingleOrDefaultAsync(x => x.Id == id && x.KhachHangId == customerId);
        if (bill is null) return NotFound();
        return Json(new { status = CustomerHistoryService.PaymentStatus(bill.TrangThai),
            closed = bill.TrangThai is TrangThaiHoaDon.DaThanhToan or TrangThaiHoaDon.DaHuy,
            lines = bill.ChiTiet.Select(x => new { id = x.Id, status = CustomerHistoryService.DishStatus(x.TrangThai) }) });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyVoucher(int id, string voucherCode, string? rowVersion) {
        var customerId = await CustomerId(); if (customerId is null) return Forbid();
        if (!await db.HoaDon.AnyAsync(x => x.Id == id && x.KhachHangId == customerId)) return NotFound();
        var result = await promotions.ApplyVoucherToInvoiceAsync(id, voucherCode, rowVersion);
        TempData[result.Success ? "Success" : "Error"] = result.Error ?? "Đã chọn voucher cho hóa đơn. Thu ngân sẽ xác nhận thanh toán.";
        return RedirectToAction(nameof(Order), new { id });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveVoucher(int id, string? rowVersion) {
        var customerId = await CustomerId(); if (customerId is null) return Forbid();
        if (!await db.HoaDon.AnyAsync(x => x.Id == id && x.KhachHangId == customerId)) return NotFound();
        var result = await promotions.RemoveVoucherFromInvoiceAsync(id, rowVersion);
        TempData[result.Success ? "Success" : "Error"] = result.Error ?? "Đã gỡ voucher.";
        return RedirectToAction(nameof(Order), new { id });
    }
}
