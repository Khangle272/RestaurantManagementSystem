using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Services;

public class CustomerHistoryService(RestaurantDbContext db)
{
    // Earn on the net purchase value, including the portion already paid as a deposit.
    public const decimal AmountPerPoint = 10000m;
    public static void EarnPoints(RestaurantDbContext db, HoaDon bill)
    {
        if (bill.KhachHangId is not int customerId || bill.TongTienHang <= 0) return;
        var net = bill.TongTienHang - bill.TienGiam;
        db.LichSuTichDiem.Add(new LichSuTichDiem {
            KhachHangId = customerId, HoaDon = bill, SoTienTinhDiem = net,
            SoDiem = (int)Math.Floor(net / AmountPerPoint), ThoiDiem = bill.ThoiDiemThanhToan!.Value,
            NoiDung = "Tích điểm thanh toán · 10.000đ = 1 điểm"
        });
    }

    public async Task<CustomerHistoryViewModel?> LoadAsync(int customerId, bool admin, int orderPage = 1, int pointPage = 1, TrangThaiHoaDon? status = null)
    {
        var customer = await db.KhachHang.AsNoTracking().SingleOrDefaultAsync(x => x.Id == customerId);
        if (customer is null) return null;
        var orders = db.HoaDon.AsNoTracking().Where(x => x.KhachHangId == customerId);
        var points = db.LichSuTichDiem.AsNoTracking().Where(x => x.KhachHangId == customerId);
        var spent = await orders.Where(x => x.TrangThai == TrangThaiHoaDon.DaThanhToan).SumAsync(x => (decimal?)(x.TongTienHang - x.TienGiam)) ?? 0;
        if (status.HasValue) orders = orders.Where(x => x.TrangThai == status);
        var orderPages = Math.Max(1, (int)Math.Ceiling(await orders.CountAsync() / 15d));
        var pointPages = Math.Max(1, (int)Math.Ceiling(await points.CountAsync() / 15d));
        orderPage = Math.Clamp(orderPage, 1, orderPages); pointPage = Math.Clamp(pointPage, 1, pointPages);
        return new CustomerHistoryViewModel {
            Customer = customer, IsAdmin = admin, TotalSpent = spent,
            Balance = await points.SumAsync(x => (int?)x.SoDiem) ?? 0,
            Orders = await orders.Include(x => x.ChiTiet).OrderByDescending(x => x.ThoiDiemLap).ThenByDescending(x => x.Id).Skip((orderPage - 1) * 15).Take(15).ToListAsync(),
            Points = await points.Include(x => x.HoaDon).OrderByDescending(x => x.ThoiDiem).ThenByDescending(x => x.Id).Skip((pointPage - 1) * 15).Take(15).ToListAsync(),
            OrderPage = orderPage, OrderPages = orderPages, PointPage = pointPage, PointPages = pointPages, Status = status
        };
    }

    public static string PaymentStatus(TrangThaiHoaDon status) => status switch {
        TrangThaiHoaDon.DaThanhToan => "Đã thanh toán", TrangThaiHoaDon.DaHuy => "Đã hủy",
        TrangThaiHoaDon.ThanhToanMotPhan => "Thanh toán một phần", _ => "Chưa thanh toán" };
    public static string DishStatus(TrangThaiCheBien status) => status switch {
        TrangThaiCheBien.ChoCheBien => "Chờ chế biến", TrangThaiCheBien.DangCheBien => "Đang chế biến",
        TrangThaiCheBien.SanSang => "Sẵn sàng phục vụ", TrangThaiCheBien.DaPhucVu => "Đã phục vụ", _ => "Đã hủy" };
}
