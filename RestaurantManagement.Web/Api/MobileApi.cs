using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Shared.Mobile;
using RestaurantManagement.Web.Security;
using RestaurantManagement.Web.Services;

namespace RestaurantManagement.Web.Api;

public static class MobileApi
{
    public const string AuthScheme = "RMS.WebOrApi";

    // Vai trò dùng được app: phục vụ tại nhà hàng và khách hàng. Bếp/Kho làm việc trên website.
    public const string StaffRoles = AppRoles.Admin + "," + AppRoles.TiepTan + "," + AppRoles.BoiBan + "," + AppRoles.ThuNgan;
    // Thu ngân không gọi món (chỉ xem đơn, thanh toán).
    public const string OrderRoles = AppRoles.GoiMon;
    public const string ServeRoles = AppRoles.Admin + "," + AppRoles.BoiBan;
    public const string PayRoles = AppRoles.Admin + "," + AppRoles.ThuNgan + "," + AppRoles.BoiBan;
    public const string ReceptionRoles = AppRoles.Admin + "," + AppRoles.TiepTan;
    public static readonly string[] AppUserRoles = [AppRoles.Admin, AppRoles.TiepTan, AppRoles.BoiBan, AppRoles.ThuNgan, AppRoles.KhachHang];

    public static bool IsApiRequest(HttpRequest request) => request.Path.StartsWithSegments("/api");

    public static string Version(byte[]? rowVersion) => Convert.ToBase64String(rowVersion ?? []);

    public static string OrderTypeText(LoaiDonHang type) => type switch
    {
        LoaiDonHang.MangDi => "Mang đi", LoaiDonHang.GiaoHang => "Giao hàng", _ => "Tại bàn"
    };

    public static string BookingStatusText(TrangThaiDatBan status) => status switch
    {
        TrangThaiDatBan.ChoXacNhan => "Chờ xác nhận", TrangThaiDatBan.ChoCoc => "Chờ cọc",
        TrangThaiDatBan.DaXacNhan => "Đã xác nhận", TrangThaiDatBan.DaNhanBan => "Đã nhận bàn",
        TrangThaiDatBan.DaHuy => "Đã hủy", _ => "Không đến"
    };

    public static string PaymentMethodText(PhuongThucThanhToan method) => PaymentLedger.HinhThucText(method);

    public static string TableLabel(HoaDon bill) => bill.LoaiDonHang switch
    {
        LoaiDonHang.TaiBan => bill.DatBan is { Ban.Count: > 0 } booking
            ? string.Join(", ", booking.Ban.Select(x => x.BanAn.MaBan).Order()) : "Tại bàn",
        _ => OrderTypeText(bill.LoaiDonHang)
    };

    public static OrderSummaryDto Summary(HoaDon bill)
    {
        var active = bill.ChiTiet.Where(x => x.TrangThai != TrangThaiCheBien.DaHuy).ToList();
        return new OrderSummaryDto(bill.Id, bill.MaHoaDon, bill.LoaiDonHang.ToString(), OrderTypeText(bill.LoaiDonHang),
            TableLabel(bill), bill.DatBan?.HoTenLienHe ?? bill.TenNguoiNhan ?? bill.KhachHang?.HoTen,
            bill.ThoiDiemLap, bill.TrangThai.ToString(), CustomerHistoryService.PaymentStatus(bill.TrangThai),
            active.Sum(x => x.SoLuong * x.DonGia), active.Sum(x => x.SoLuong),
            active.Count(x => x.TrangThai == TrangThaiCheBien.SanSang));
    }
}

// Lớp cơ sở cho API mobile: ControllerBase (không phải Controller) nên không bị bộ lọc trang lỗi HTML của website can thiệp.
[ApiController]
[Produces("application/json")]
public abstract class MobileApiControllerBase(RestaurantDbContext db, UserManager<TaiKhoan> users) : ControllerBase
{
    protected RestaurantDbContext Db => db;
    protected UserManager<TaiKhoan> Users => users;

    protected IActionResult Fail(string message, int status = StatusCodes.Status400BadRequest) =>
        StatusCode(status, new ApiError(message));

    protected int? AccountId() => int.TryParse(users.GetUserId(User), out var id) ? id : null;

    // Giống website: Admin chưa có hồ sơ nhân viên được ghi nhận bằng một nhân viên đang làm việc.
    protected async Task<int?> CurrentStaffIdAsync()
    {
        if (AccountId() is not int accountId) return null;
        var staffId = await db.NhanVien.AsNoTracking().Where(x => x.TaiKhoanId == accountId && x.DangLamViec)
            .Select(x => (int?)x.Id).SingleOrDefaultAsync();
        if (staffId is null && User.IsInRole(AppRoles.Admin))
            staffId = await db.NhanVien.AsNoTracking().Where(x => x.DangLamViec).Select(x => (int?)x.Id).FirstOrDefaultAsync();
        return staffId;
    }

    protected async Task<int?> CurrentCustomerIdAsync()
    {
        if (AccountId() is not int accountId) return null;
        return await db.KhachHang.AsNoTracking().Where(x => x.TaiKhoanId == accountId && x.DangSuDung)
            .Select(x => (int?)x.Id).SingleOrDefaultAsync();
    }

    // Đọc hóa đơn kèm số phải thu theo đúng công thức trang thu ngân: ưu đãi → cấn cọc → phải thu.
    protected async Task<OrderDetailDto?> LoadOrderAsync(int id, int? customerId, bool staff,
        OrderService orders, IPromotionService promotions)
    {
        var bill = await db.HoaDon.AsSplitQuery()
            .Include(x => x.ChiTiet)
            .Include(x => x.DatBan).ThenInclude(x => x!.Ban).ThenInclude(x => x.BanAn)
            .Include(x => x.Voucher).Include(x => x.KhachHang)
            .SingleOrDefaultAsync(x => x.Id == id && (customerId == null || x.KhachHangId == customerId));
        if (bill is null) return null;

        var open = bill.TrangThai == TrangThaiHoaDon.ChuaThanhToan;
        var lines = bill.ChiTiet.OrderBy(x => x.ThoiDiemGoi).ThenBy(x => x.Id).ToList();
        var total = lines.Where(x => x.TrangThai != TrangThaiCheBien.DaHuy).Sum(x => x.SoLuong * x.DonGia);
        decimal discount = bill.TienGiam, deposit = bill.TienCocDaTru;
        if (open)
        {
            var quote = await promotions.EvaluatePromotionsAsync(bill.Id, bill.Voucher?.Ma);
            if (quote.IsSuccess) discount = quote.TongTienGiam;
            var available = bill.DatBanId is int bookingId ? await orders.AvailableDepositAsync(bookingId, bill.Id) : 0;
            deposit = Math.Min(available, Math.Max(0, total - discount));
        }
        var due = open ? Math.Max(0, total - discount - deposit) : 0;

        return new OrderDetailDto(bill.Id, bill.MaHoaDon, bill.LoaiDonHang.ToString(), MobileApi.OrderTypeText(bill.LoaiDonHang),
            MobileApi.TableLabel(bill), bill.DatBan?.HoTenLienHe ?? bill.TenNguoiNhan ?? bill.KhachHang?.HoTen,
            bill.SoDienThoaiNhan ?? bill.DatBan?.SoDienThoaiLienHe, bill.DiaChiGiaoHang, bill.GhiChuDonHang,
            bill.TrangThai.ToString(), CustomerHistoryService.PaymentStatus(bill.TrangThai),
            total, discount, deposit, due,
            MobileApi.Version(db.Entry(bill).Property<byte[]>("RowVersion").CurrentValue),
            bill.DatBan is null ? null : MobileApi.Version(db.Entry(bill.DatBan).Property<byte[]>("RowVersion").CurrentValue),
            staff && open, staff && open && total > 0,
            bill.TrangThai == TrangThaiHoaDon.DaThanhToan ? MobileApi.PaymentMethodText(bill.PhuongThucThanhToan) : null,
            lines.Select(x => new OrderLineDto(x.Id, x.TenMonLucBan, x.TenSizeLucBan, x.SoLuong, x.DonGia, x.YeuCauCheBien,
                x.TrangThai.ToString(), CustomerHistoryService.DishStatus(x.TrangThai), x.ThoiDiemGoi,
                staff && x.TrangThai == TrangThaiCheBien.SanSang && bill.TrangThai != TrangThaiHoaDon.DaHuy,
                staff && open && x.TrangThai is TrangThaiCheBien.ChoCheBien or TrangThaiCheBien.DangCheBien)).ToList());
    }
}
