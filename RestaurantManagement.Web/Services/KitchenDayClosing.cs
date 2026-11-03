using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Services;

// Đóng sổ bếp: món gọi trước mốc chốt không được trôi sang ngày hôm sau trên màn hình bếp.
public static class KitchenDayClosing
{
    public static DateTimeOffset StartOfToday => TableService.VietnamTime(TableService.VietnamNow.Date);

    public static async Task<int> CloseBeforeAsync(RestaurantDbContext db, DateTimeOffset cutoff)
    {
        var pending = await db.ChiTietHoaDon.Include(x => x.HoaDon)
            .Where(x => x.ThoiDiemGoi < cutoff && (x.TrangThai == TrangThaiCheBien.ChoCheBien
                || x.TrangThai == TrangThaiCheBien.DangCheBien || x.TrangThai == TrangThaiCheBien.SanSang))
            .ToListAsync();
        if (pending.Count == 0) return 0;
        var now = DateTimeOffset.UtcNow;
        foreach (var item in pending)
        {
            // Món đã nấu xong, hoặc bill đã thu tiền: coi như đã giao; món chưa nấu của bill chưa thu tiền: hủy.
            if (item.TrangThai == TrangThaiCheBien.SanSang || item.HoaDon.TrangThai != TrangThaiHoaDon.ChuaThanhToan)
                item.TrangThai = TrangThaiCheBien.DaPhucVu;
            else { item.TrangThai = TrangThaiCheBien.DaHuy; item.ThoiDiemHuy = now; }
        }
        var billIds = pending.Where(x => x.HoaDon.TrangThai == TrangThaiHoaDon.ChuaThanhToan).Select(x => x.HoaDonId).Distinct().ToList();
        var bills = await db.HoaDon.Include(x => x.ChiTiet).Where(x => billIds.Contains(x.Id)).ToListAsync();
        foreach (var bill in bills)
        {
            bill.TongTienHang = bill.ChiTiet.Where(x => x.TrangThai != TrangThaiCheBien.DaHuy).Sum(x => x.SoLuong * x.DonGia);
            bill.TienGiam = Math.Min(bill.TienGiam, bill.TongTienHang);
            bill.TienCocDaTru = Math.Min(bill.TienCocDaTru, bill.TongTienHang - bill.TienGiam);
            // Đơn mang đi/giao hàng thử nghiệm của ngày cũ không còn món nào: đóng hẳn.
            if (bill.LoaiDonHang != LoaiDonHang.TaiBan && bill.TongTienHang == 0 && bill.ThoiDiemLap < cutoff)
                bill.TrangThai = TrangThaiHoaDon.DaHuy;
        }
        await db.SaveChangesAsync();
        return pending.Count;
    }
}
