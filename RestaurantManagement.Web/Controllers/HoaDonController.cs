using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Security;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin + "," + AppRoles.ThuNgan)]
public class HoaDonController(RestaurantDbContext db, UserManager<TaiKhoan> users) : Controller
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
        return View(invoice);
    }

    [Authorize(Roles = AppRoles.ThuNgan)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmCash(int id, string rowVersion)
    {
        if (!int.TryParse(users.GetUserId(User), out var accountId)) return Challenge();
        var cashierId = await db.NhanVien.AsNoTracking()
            .Where(x => x.TaiKhoanId == accountId && x.DangLamViec)
            .Select(x => x.Id).SingleOrDefaultAsync();
        if (cashierId == 0) return Forbid();

        byte[] expectedVersion;
        try { expectedVersion = Convert.FromBase64String(rowVersion); }
        catch (FormatException) { return BadRequest(); }

        var invoice = await db.HoaDon.Include(x => x.ChiTiet).SingleOrDefaultAsync(x => x.Id == id);
        if (invoice is null) return NotFound();
        var currentVersion = db.Entry(invoice).Property<byte[]>("RowVersion").CurrentValue;
        if (currentVersion is null || !currentVersion.SequenceEqual(expectedVersion))
        {
            TempData["Error"] = "Hóa đơn đã thay đổi. Vui lòng kiểm tra lại trước khi thu tiền.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (invoice.TrangThai != TrangThaiHoaDon.ChuaThanhToan || invoice.TongThanhToan <= 0
            || invoice.ChiTiet.Where(x => x.TrangThai != TrangThaiCheBien.DaHuy)
                .Sum(x => x.SoLuong * x.DonGia) != invoice.TongTienHang)
        {
            TempData["Error"] = "Hóa đơn chưa đủ điều kiện xác nhận tiền mặt; hãy đối chiếu trạng thái và chi tiết món.";
            return RedirectToAction(nameof(Details), new { id });
        }

        db.Entry(invoice).Property<byte[]>("RowVersion").OriginalValue = expectedVersion;
        invoice.NhanVienId = cashierId;
        invoice.PhuongThucThanhToan = PhuongThucThanhToan.TienMat;
        invoice.ThoiDiemThanhToan = DateTimeOffset.UtcNow;
        invoice.TrangThai = TrangThaiHoaDon.DaThanhToan;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = "Hóa đơn đã thay đổi. Vui lòng kiểm tra lại trước khi thu tiền.";
            return RedirectToAction(nameof(Details), new { id });
        }
        TempData["Success"] = "Đã xác nhận thu tiền mặt.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
