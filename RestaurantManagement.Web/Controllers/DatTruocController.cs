using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Security;
using RestaurantManagement.Web.Services;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin + "," + AppRoles.TiepTan + "," + AppRoles.BoiBan + "," + AppRoles.ThuNgan)]
public class DatTruocController(RestaurantDbContext db, PreorderService preorders, OrderService orders, TableService tables) : Controller
{
    private int AccountId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    private Task<int?> StaffId() => db.NhanVien.Where(x => x.TaiKhoanId == AccountId && x.DangLamViec)
        .Select(x => (int?)x.Id).SingleOrDefaultAsync();

    [HttpGet, Authorize(Roles = AppRoles.Admin + "," + AppRoles.ThuNgan)]
    public async Task<IActionResult> Index(string? keyword)
    {
        var query = db.DatBan.AsNoTracking().Where(x => x.ThoiDiemBaoChuyenKhoan != null);
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.MaDatBan.Contains(keyword.Trim())
            || (x.SoDienThoaiLienHe != null && x.SoDienThoaiLienHe.Contains(keyword.Trim())));
        ViewData["Keyword"] = keyword;
        return View(await query.OrderBy(x => x.ThoiDiemBaoChuyenKhoan).Take(100).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> ChiTiet(int id)
    {
        var booking = await db.DatBan.Include(x => x.Ban).ThenInclude(x => x.BanAn).ThenInclude(x => x.KhuVuc)
            .Include(x => x.HoaDon).AsSplitQuery().SingleOrDefaultAsync(x => x.Id == id);
        if (booking is null) return NotFound();
        return View("~/Views/DatBan/ChiTiet.cshtml", new BookingDetailViewModel {
            Booking = booking, Version = preorders.Version(booking), Items = await preorders.Lines(id),
            Transactions = await db.GiaoDichCoc.Where(x => x.DatBanId == id).OrderByDescending(x => x.ThoiDiem).ToListAsync(),
            AvailableDeposit = await orders.AvailableDepositAsync(id), CanEdit = PreorderService.CanEdit(booking),
            Internal = true, ActorStaffId = await StaffId(),
            Staff = await db.NhanVien.Where(x => x.DangLamViec).OrderBy(x => x.HoTen).ToListAsync()
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = AppRoles.Admin + "," + AppRoles.TiepTan)]
    public async Task<IActionResult> ThoaThuan(int id, DepositQuoteModel model)
        => Result(id, ModelState.IsValid ? await preorders.Quote(id, model) : ValidationError());

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = AppRoles.Admin + "," + AppRoles.ThuNgan)]
    public async Task<IActionResult> GiaoDichCoc(int id, DepositTransactionModel model)
    {
        if (model.Loai != LoaiGiaoDichCoc.Thu && !User.IsInRole(AppRoles.Admin)) return Forbid();
        return Result(id, ModelState.IsValid ? await preorders.RecordDeposit(id, AccountId, model) : ValidationError());
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = AppRoles.Admin + "," + AppRoles.ThuNgan)]
    public async Task<IActionResult> TuChoiChuyenKhoan(int id, string rowVersion, string lyDo)
        => Result(id, await preorders.RejectPaymentNotice(id, rowVersion, lyDo));

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = AppRoles.Admin + "," + AppRoles.TiepTan + "," + AppRoles.BoiBan)]
    public async Task<IActionResult> GuiBep(int id, string rowVersion, int? nhanVienId, bool xacNhanLamTruoc)
    {
        var employeeId = await StaffId();
        if (employeeId is null && User.IsInRole(AppRoles.Admin)) employeeId = nhanVienId;
        return Result(id, employeeId is int staffId
            ? await preorders.Release(id, rowVersion, staffId, xacNhanLamTruoc)
            : "Chọn nhân viên đang làm việc phụ trách phiếu gọi món.");
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = AppRoles.Admin + "," + AppRoles.TiepTan)]
    public async Task<IActionResult> Huy(int id, string rowVersion, string? lyDo)
        => Result(id, await tables.Cancel(id, rowVersion, lyDo, await StaffId()));

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> XuLyHuy(int id, string rowVersion, string? lyDo, bool daDoiChieu)
        => Result(id, await preorders.ResolveCancel(id, rowVersion, lyDo, await StaffId(), daDoiChieu));

    private string ValidationError() => string.Join(" ", ModelState.Values.SelectMany(x => x.Errors)
        .Select(x => string.IsNullOrEmpty(x.ErrorMessage) ? "Thông tin nhập không hợp lệ." : x.ErrorMessage));

    private IActionResult Result(int id, string? error)
    {
        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            return error is null ? Json(new { ok = true, redirectUrl = Url.Action(nameof(ChiTiet), new { id }) })
                : BadRequest(new { ok = false, errors = new[] { error } });
        TempData[error is null ? "SuccessMessage" : "ErrorMessage"] = error ?? "Đã lưu xử lý lịch đặt bàn.";
        return RedirectToAction(nameof(ChiTiet), new { id });
    }
}
