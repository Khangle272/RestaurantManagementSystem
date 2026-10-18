using System.Data;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Services;

public static class Week9DemoSetup
{
    public const string AreaName = "Sảnh đặt bàn";
    private const string Marker = "Khách dùng bữa theo ca tại nhà hàng.";

    public static async Task InitializeAsync(RestaurantDbContext db, TableService tables)
    {
        var customer = await db.KhachHang.SingleAsync(x => x.TaiKhoan != null && x.TaiKhoan.Email == "khach.demo@example.test");
        var day = DateOnly.FromDateTime(TableService.VietnamNow).AddDays(1);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var area = await db.KhuVuc.SingleOrDefaultAsync(x => x.TenKhuVuc == AreaName);
        if (area is null) { area = new KhuVuc { TenKhuVuc = AreaName, Tang = 2 }; db.KhuVuc.Add(area); }
        else if (!area.DangSuDung || area.LaPhongVip) throw new InvalidOperationException("Khu vực đã thay đổi; không tự ghi đè cấu hình.");
        for (var index = 0; index < 2; index++)
        {
            var name = $"Bàn Sảnh {index + 1}";
            var table = await db.BanAn.SingleOrDefaultAsync(x => x.MaBan == name);
            if (table is null) { table = new BanAn { MaBan = name, KhuVuc = area, SoChoNgoi = 4, TrangThai = TrangThaiBan.SanSang }; db.BanAn.Add(table); }
            else if (table.KhuVucId != area.Id) throw new InvalidOperationException($"{name} thuộc khu vực khác; không tự di chuyển bàn.");
            if (table.SoChoNgoi < 4 || table.TrangThai == TrangThaiBan.NgungSuDung)
                throw new InvalidOperationException($"{name} không còn phù hợp bộ dữ liệu thử; không tự ghi đè cấu hình.");
            var code = $"W9-{day:yyyyMMdd}-{index + 1}";
            var existing = await db.DatBan.Include(x => x.Ban).SingleOrDefaultAsync(x => x.MaDatBan == code);
            if (existing is not null)
            {
                if (existing.YeuCau != Marker || existing.KhachHangId != customer.Id)
                    throw new InvalidOperationException($"Lịch {code} đã thay đổi; không tự ghi đè.");
                continue;
            }
            var slot = ReservationSchedule.Slots[index == 0 ? 0 : 2];
            var arrival = slot.Arrival(day);
            if (table.Id > 0 && await tables.OverlappingBookings(TableService.VietnamTime(arrival), ReservationSchedule.EndOf(arrival))
                .AnyAsync(x => x.BanAnId == table.Id))
                throw new InvalidOperationException($"{name} đã có lịch, không tự chèn dữ liệu chồng lịch.");
            var booking = new DatBan
            {
                MaDatBan = code, HoTenLienHe = index == 0 ? "Khách hẹn ca trưa" : "Khách hẹn ca tối", KhachHangId = customer.Id,
                SoDienThoaiLienHe = customer.SoDienThoai, SoNguoiLon = 4, KhuVucUuTien = area, YeuCau = Marker,
                ThoiDiemTao = DateTimeOffset.UtcNow, GioDen = TableService.VietnamTime(arrival), GioKetThucDuKien = ReservationSchedule.EndOf(arrival),
                LaKhachTrucTiep = true, TrangThai = TrangThaiDatBan.DaXacNhan
            };
            booking.Ban.Add(new ChiTietDatBan { BanAn = table }); db.DatBan.Add(booking);
        }
        var pendingCode = $"W9-{day:yyyyMMdd}-CHO";
        if (!await db.DatBan.AnyAsync(x => x.MaDatBan == pendingCode))
            db.DatBan.Add(new DatBan
            {
                MaDatBan = pendingCode, HoTenLienHe = "Khách gọi điện chờ xếp bàn", SoDienThoaiLienHe = customer.SoDienThoai,
                SoNguoiLon = 4, KhachHangId = customer.Id, KhuVucUuTien = area, YeuCau = Marker,
                ThoiDiemTao = DateTimeOffset.UtcNow, GioDen = TableService.VietnamTime(day.ToDateTime(new(11, 0))),
                GioKetThucDuKien = TableService.VietnamTime(day.ToDateTime(new(13, 0))), LaKhachTrucTiep = true, TrangThai = TrangThaiDatBan.ChoXacNhan
            });
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
}
