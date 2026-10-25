using System.Data;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
namespace RestaurantManagement.Web.Services;

public static class Week10DemoSetup
{
    public const string AreaName = "Sảnh đặt bàn W10";
    public static async Task InitializeAsync(RestaurantDbContext db)
    {
        await DbSeeder.SeedAsync(db);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var customer = await db.KhachHang.SingleAsync(x => x.TaiKhoan != null && x.TaiKhoan.Email == "khach.demo@example.test");
        var dishes = await db.MonAn.Include(x => x.Sizes).ThenInclude(x => x.DinhMuc)
            .Where(x => x.TenMon == "Gỏi ngó sen tôm thịt" || x.TenMon == "Gỏi bò bóp thấu").OrderBy(x => x.Id).ToListAsync();
        if (dishes.Count != 2 || dishes.Any(x => x.Sizes.Count(s => s.DangSuDung && s.DinhMuc.Count > 0) < 3))
            throw new InvalidOperationException("Bộ món có sẵn chưa đủ ba size/định mức. Kiểm tra công thức trước khi tạo lịch demo.");
        var area = await db.KhuVuc.SingleOrDefaultAsync(x => x.TenKhuVuc == AreaName);
        if (area is null) { area = new KhuVuc { TenKhuVuc = AreaName, Tang = 1, DangSuDung = true }; db.KhuVuc.Add(area); }
        if (!area.DangSuDung || area.LaPhongVip) throw new InvalidOperationException("Khu vực bộ dữ liệu W10 đã đổi cấu hình; không tự ghi đè.");
        await db.SaveChangesAsync();
        for (var index = 0; index < 2; index++)
        {
            var slot = ReservationSchedule.Slots[index];
            var now = TableService.VietnamNow; var day = DateOnly.FromDateTime(now);
            var arrival = day.ToDateTime(index == 0 ? new(11, 30) : new(18, 30));
            if (arrival < now.AddMinutes(30))
            {
                var next = ReservationSchedule.NextArrival(now);
                arrival = ReservationSchedule.Match(next)?.Id == slot.Id ? next : day.AddDays(1).ToDateTime(index == 0 ? new(11, 30) : new(18, 30));
            }
            var tableName = $"W10-{index + 1:00}";
            var table = await db.BanAn.SingleOrDefaultAsync(x => x.MaBan == tableName);
            if (table is null) { table = new BanAn { MaBan = tableName, KhuVuc = area, KhuVucId = area.Id, SoChoNgoi = 4, TrangThai = TrangThaiBan.SanSang }; db.BanAn.Add(table); }
            if (table.KhuVucId != area.Id || table.TrangThai == TrangThaiBan.NgungSuDung) throw new InvalidOperationException("Bàn W10 đã đổi cấu hình; không tự ghi đè.");
            var code = $"W10-{arrival:yyyyMMdd}-{slot.Id}";
            if (await db.DatBan.AnyAsync(x => x.MaDatBan == code)) continue;
            var window = ReservationSchedule.Window(TableService.VietnamTime(arrival));
            if (table.Id > 0 && await db.ChiTietDatBan.AnyAsync(x => x.BanAnId == table.Id && x.DatBan.GioDen >= window.From && x.DatBan.GioDen < window.Until && x.DatBan.TrangThai != TrangThaiDatBan.DaHuy))
                throw new InvalidOperationException("Bàn W10 đã có lịch ca này; không tự chèn đè.");
            var booking = new DatBan { MaDatBan = code, HoTenLienHe = index == 0 ? "Khách hẹn ăn trưa" : "Khách hẹn ăn tối", KhachHangId = customer.Id,
                SoDienThoaiLienHe = customer.SoDienThoai, SoNguoiLon = 4, KhuVucUuTien = area, LaKhachTrucTiep = true,
                ThoiDiemTao = DateTimeOffset.UtcNow, GioDen = TableService.VietnamTime(arrival), GioKetThucDuKien = ReservationSchedule.EndOf(arrival), TrangThai = TrangThaiDatBan.DaXacNhan };
            booking.Ban.Add(new ChiTietDatBan { BanAn = table });
            var dish = dishes[index]; var size = dish.Sizes.OrderBy(x => x.HeSoTyLe).First();
            booking.MonDatTruoc.Add(new MonDatTruoc { MonAn = dish, MonAnSize = size, TenMonLucDat = dish.TenMon, TenSizeLucDat = size.TenSize, SoLuong = 2, DonGiaThoaThuan = size.GiaBan });
            db.DatBan.Add(booking);
        }
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
}
