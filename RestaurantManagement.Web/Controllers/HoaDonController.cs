using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Security;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin + "," + AppRoles.ThuNgan + "," + AppRoles.BoiBan)]
public class HoaDonController(RestaurantDbContext db, UserManager<TaiKhoan> users, RestaurantManagement.Web.Services.OrderService orderService, RestaurantManagement.Web.Services.IPromotionService promotionService) : Controller
{
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.ThuNgan)]
    public async Task<IActionResult> Index(bool daThanhToan = false, int page = 1)
    {
        ViewData["AutoCollapseSidebar"] = true;
        var query = db.HoaDon.AsNoTracking();
        query = daThanhToan
            ? query.Where(x => x.TrangThai == TrangThaiHoaDon.DaThanhToan)
            : query.Where(x => x.TrangThai == TrangThaiHoaDon.ChuaThanhToan
                || x.TrangThai == TrangThaiHoaDon.ThanhToanMotPhan);

        const int pageSize = 20;
        var count = await query.CountAsync();
        var pages = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize));
        page = Math.Clamp(page, 1, pages);
        ViewBag.DaThanhToan = daThanhToan;
        ViewBag.Pagination = new RestaurantManagement.Web.Models.PaginationViewModel { CurrentPage = page, TotalPages = pages, TotalItems = count };
        return View(await query.OrderByDescending(x => x.ThoiDiemLap).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        ViewData["AutoCollapseSidebar"] = true;
        var invoice = await db.HoaDon.AsSplitQuery()
            .Include(x => x.ChiTiet).ThenInclude(x => x.MonAnSize)
            .Include(x => x.DatBan).ThenInclude(x => x!.Ban).ThenInclude(x => x.BanAn)
            .Include(x => x.Voucher).ThenInclude(v => v!.KhuyenMai)
            .SingleOrDefaultAsync(x => x.Id == id);
        if (invoice is null) return NotFound();

        // Đánh giá các chương trình ưu đãi và voucher áp dụng
        var promoEval = await promotionService.EvaluatePromotionsAsync(id, invoice.Voucher?.Ma);
        ViewBag.PromotionEvaluation = promoEval;
        var effectiveDiscount = invoice.TrangThai == TrangThaiHoaDon.ChuaThanhToan ? promoEval.TongTienGiam : invoice.TienGiam;
        ViewBag.EffectiveDiscount = effectiveDiscount;
        ViewBag.ManualDiscount = promoEval.TienGiamThuCong;

        ViewBag.RowVersion = Convert.ToBase64String(db.Entry(invoice).Property<byte[]>("RowVersion").CurrentValue ?? []);
        ViewBag.BookingRowVersion = invoice.DatBan is null ? ""
            : Convert.ToBase64String(db.Entry(invoice.DatBan).Property<byte[]>("RowVersion").CurrentValue ?? []);
        var total = invoice.ChiTiet.Where(x => x.TrangThai != TrangThaiCheBien.DaHuy).Sum(x => x.SoLuong * x.DonGia);
        var available = invoice.DatBanId is int bookingId && invoice.TrangThai == TrangThaiHoaDon.ChuaThanhToan
            ? await orderService.AvailableDepositAsync(bookingId, invoice.Id) : 0;
        var deposit = invoice.TrangThai == TrangThaiHoaDon.ChuaThanhToan
            ? Math.Min(available, Math.Max(0, total - effectiveDiscount))
            : invoice.TienCocDaTru;
        ViewBag.AvailableDeposit = available;
        ViewBag.ActiveTotal = total;
        ViewBag.DepositDeduction = deposit;
        ViewBag.PaymentAmount = Math.Max(0, total - effectiveDiscount - deposit);
        ViewBag.CustomerWallet = invoice.KhachHangId.HasValue
            ? await new RestaurantManagement.Web.Services.CustomerVoucherService(db, promotionService).ListAsync(invoice.KhachHangId.Value, true)
            : new List<RestaurantManagement.Web.Services.CustomerVoucherItem>();
        ViewBag.Customer = invoice.KhachHangId.HasValue ? await db.KhachHang.AsNoTracking().SingleOrDefaultAsync(x => x.Id == invoice.KhachHangId) : null;
        // Các đợt thanh toán của hóa đơn: cọc nhận trước (theo lịch đặt bàn) + thanh toán dứt điểm.
        ViewBag.Payments = await db.ChiTietThanhToan.AsNoTracking()
            .Where(x => x.HoaDonId == id || (invoice.DatBanId != null && x.DatBanId == invoice.DatBanId && x.HoaDonId == null))
            .OrderBy(x => x.NgayThanhToan).ThenBy(x => x.GioThanhToan).ToListAsync();
        return View(invoice);
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.ThuNgan)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCustomer(int id, string phone, string? rowVersion)
    {
        var bill = await db.HoaDon.SingleOrDefaultAsync(x => x.Id == id);
        if (bill is null) return NotFound();
        if (bill.TrangThai != TrangThaiHoaDon.ChuaThanhToan || bill.VoucherId.HasValue) return BadRequest();
        var bytes = new byte[8];
        if (rowVersion is null || !Convert.TryFromBase64String(rowVersion, bytes, out var length) || length != 8
            || !bytes.SequenceEqual(db.Entry(bill).Property<byte[]>("RowVersion").CurrentValue ?? [])) return Conflict("Hóa đơn đã thay đổi. Vui lòng tải lại.");
        // Số điện thoại là duy nhất: tra ra mã khách hàng và mở ví voucher, không cần khách đăng nhập tại quầy.
        var customer = await db.KhachHang.SingleOrDefaultAsync(x => x.SoDienThoai == (phone ?? "").Trim() && x.DangSuDung);
        if (customer is null) TempData["Error"] = $"Số {phone} chưa là khách hàng thành viên. Thanh toán bình thường, không áp mã.";
        else {
            bill.KhachHangId = customer.Id;
            try { await db.SaveChangesAsync(); TempData["Success"] = $"Khách hàng: {customer.HoTen} (mã KH {customer.Id}). Chọn voucher trong ví bên dưới nếu có."; }
            catch (DbUpdateConcurrencyException) { TempData["Error"] = "Hóa đơn đã thay đổi. Vui lòng tải lại."; }
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.ThuNgan)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyVoucher(int id, string voucherCode, string? rowVersion)
    {
        var (success, error, result) = await promotionService.ApplyVoucherToInvoiceAsync(id, voucherCode, rowVersion);
        if (!success) TempData["Error"] = error;
        else TempData["Success"] = $"Đã áp dụng mã Voucher '{voucherCode.Trim()}' thành công (Giảm {result?.TongTienGiam:N0} đ).";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.ThuNgan)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveVoucher(int id, string? rowVersion)
    {
        var (success, error) = await promotionService.RemoveVoucherFromInvoiceAsync(id, rowVersion);
        if (!success) TempData["Error"] = error;
        else TempData["Success"] = "Đã gỡ bỏ mã Voucher khỏi hóa đơn.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.ThuNgan + "," + AppRoles.BoiBan)]
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
        var invoice = await db.HoaDon.AsNoTracking().Include(x => x.ChiTiet).Include(x => x.Voucher).SingleOrDefaultAsync(x => x.Id == id);
        if (invoice is null) return NotFound();
        var total = invoice.ChiTiet.Where(x => x.TrangThai != TrangThaiCheBien.DaHuy).Sum(x => x.SoLuong * x.DonGia);
        var deposit = invoice.DatBanId is int bookingId ? await orderService.AvailableDepositAsync(bookingId, id) : 0;
        var quote = await promotionService.EvaluatePromotionsAsync(id, invoice.Voucher?.Ma);
        if (!quote.IsSuccess)
        {
            TempData["Error"] = quote.ErrorMessage;
            return RedirectToAction(nameof(Details), new { id });
        }
        var quotedDue = Math.Max(0, total - quote.TongTienGiam - Math.Min(deposit, Math.Max(0, total - quote.TongTienGiam)));
        var error = await orderService.ProcessPaymentAsync(new RestaurantManagement.Web.Models.PaymentFormModel
        {
            Id = id, RowVersion = rowVersion, BookingRowVersion = bookingRowVersion,
            PhuongThuc = PhuongThucThanhToan.TienMat,
            TienKhachDua = quotedDue, SoTienPhaiThuDuKien = quotedDue
        }, cashierId);
        if (error is not null) TempData["Error"] = error;
        else TempData["Success"] = "Đã xác nhận thu tiền mặt.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.ThuNgan + "," + AppRoles.BoiBan)]
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
