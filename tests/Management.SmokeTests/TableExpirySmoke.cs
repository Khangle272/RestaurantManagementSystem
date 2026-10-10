using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Services;

namespace Management.SmokeTests;

public static class TableExpirySmoke
{
    public static async Task Run(RestaurantDbContext db, HttpClient admin, Action<bool, string> check)
    {
        var options = new DbContextOptionsBuilder<RestaurantDbContext>().UseSqlServer(db.Database.GetConnectionString()).Options;
        var staffId = await db.NhanVien.Select(x => x.Id).FirstAsync();
        var dishId = await db.MonAn.Select(x => x.Id).FirstAsync();
        var area = new KhuVuc { TenKhuVuc = "Expiry smoke", Tang = 1, DangSuDung = true };
        db.KhuVuc.Add(area); await db.SaveChangesAsync();
        var reception = new ReceptionBookingViewModel();
        check(reception.SoPhut == 180, "Reception defaults to three hours");
        check(!Validator.TryValidateProperty(181, new ValidationContext(reception) { MemberName = nameof(reception.SoPhut) }, []), "Server rejects dining duration above three hours");

        async Task<(DatBan Booking, BanAn[] Tables, HoaDon Bill)> Party(double hours, int tableCount = 1, bool paid = true, decimal amount = 100)
        {
            var now = DateTimeOffset.UtcNow;
            var booking = new DatBan { MaDatBan = "EXP-" + Guid.NewGuid().ToString("N")[..12], HoTenLienHe = "Expiry smoke",
                LaKhachTrucTiep = true, ThoiDiemTao = now.AddHours(-8), GioDen = now.AddHours(-hours),
                GioKetThucDuKien = now.AddHours(-hours + 3), ThoiDiemNhanBan = now.AddHours(-hours),
                SoNguoiLon = 2, TrangThai = TrangThaiDatBan.DaNhanBan };
            var tables = Enumerable.Range(0, tableCount).Select(_ => new BanAn { MaBan = "EX-" + Guid.NewGuid().ToString("N")[..10],
                KhuVucId = area.Id, SoChoNgoi = 4, TrangThai = TrangThaiBan.DangPhucVu }).ToArray();
            foreach (var table in tables) booking.Ban.Add(new ChiTietDatBan { BanAn = table });
            var bill = new HoaDon { MaHoaDon = "EXP-HD-" + Guid.NewGuid().ToString("N")[..12], DatBan = booking,
                NhanVienId = staffId, ThoiDiemLap = now.AddHours(-hours), TongTienHang = amount,
                TrangThai = paid ? TrangThaiHoaDon.DaThanhToan : TrangThaiHoaDon.ChuaThanhToan };
            db.HoaDon.Add(bill); await db.SaveChangesAsync();
            return (booking, tables, bill);
        }
        async Task<string?> Close(int id)
        {
            await using var context = new RestaurantDbContext(options);
            return await new TableService(context, new PreorderService(context, new OrderService(context))).CloseOverdue(id);
        }
        async Task Refresh((DatBan Booking, BanAn[] Tables, HoaDon Bill) party)
        {
            await db.Entry(party.Booking).ReloadAsync(); await db.Entry(party.Bill).ReloadAsync();
            foreach (var table in party.Tables) await db.Entry(table).ReloadAsync();
        }

        var young = await Party(2.5);
        young.Booking.GioDen = DateTimeOffset.UtcNow.AddHours(-8); await db.SaveChangesAsync();
        check(await Close(young.Booking.Id) != null, "Automatic end uses actual check-in, not booking creation or scheduled arrival");
        await Refresh(young);
        check(young.Booking.ThoiDiemKetThuc == null && young.Tables[0].TrangThai == TrangThaiBan.DangPhucVu, "A party below three hours is kept serving");

        var combined = await Party(3.1, 2);
        check(await Close(combined.Booking.Id) == null, "Overdue paid party ends successfully");
        await Refresh(combined);
        check(combined.Booking.ThoiDiemKetThuc != null && combined.Tables.All(x => x.TrangThai == TrangThaiBan.CanDon), "Every linked table becomes Needs cleaning, never automatically Ready");
        check(combined.Bill.TrangThai == TrangThaiHoaDon.DaThanhToan && combined.Bill.TongTienHang == 100, "Automatic end preserves paid invoice and money");
        var finishedAt = combined.Booking.ThoiDiemKetThuc;
        await Close(combined.Booking.Id); await Refresh(combined);
        check(combined.Booking.ThoiDiemKetThuc == finishedAt, "Repeated expiry checks do not rewrite the end time");
        await using (var context = new RestaurantDbContext(options))
        {
            var error = await new TableService(context, new PreorderService(context, new OrderService(context))).ChangeTable(
                combined.Tables[0].Id, Convert.ToBase64String(db.Entry(combined.Tables[0]).Property<byte[]>("RowVersion").CurrentValue!), true);
            check(error == null, "Waiter can mark an automatically ended table as cleaned");
        }
        await Refresh(combined);
        check(combined.Tables[0].TrangThai == TrangThaiBan.SanSang && combined.Tables[1].TrangThai == TrangThaiBan.CanDon, "Cleaning one table does not falsely clean the other linked table");

        var unpaid = await Party(4, paid: false);
        check(await Close(unpaid.Booking.Id) != null, "Three hours cannot auto-settle an unpaid invoice");
        await Refresh(unpaid);
        check(unpaid.Booking.ThoiDiemKetThuc == null && unpaid.Tables[0].TrangThai == TrangThaiBan.DangPhucVu
            && unpaid.Bill.TrangThai == TrangThaiHoaDon.ChuaThanhToan, "Unpaid party and invoice stay open");
        var board = WebUtility.HtmlDecode(await admin.GetStringAsync($"/SoDoBan?khuVucId={area.Id}"));
        check(board.Contains("Đã quá 3 tiếng") && board.Contains(unpaid.Tables[0].MaBan), "Floor board warns about overdue parties blocked by unresolved work");
        unpaid.Bill.TrangThai = TrangThaiHoaDon.DaThanhToan; await db.SaveChangesAsync();
        check(await Close(unpaid.Booking.Id) == null, "An overdue party can end after payment is actually recorded");

        var cooking = await Party(4);
        var line = new ChiTietHoaDon { HoaDonId = cooking.Bill.Id, MonAnId = dishId, TenMonLucBan = "Expiry dish",
            SoLuong = 1, DonGia = 100, TrangThai = TrangThaiCheBien.DangCheBien };
        db.ChiTietHoaDon.Add(line); await db.SaveChangesAsync();
        check(await Close(cooking.Booking.Id) != null, "Even a paid party cannot auto-end with unfinished dishes");
        line.TrangThai = TrangThaiCheBien.DaPhucVu; await db.SaveChangesAsync();
        check(await Close(cooking.Booking.Id) == null, "Delivered dishes allow a paid overdue party to end");

        var deposit = await Party(4);
        deposit.Booking.TienCocDaNop = 100; await db.SaveChangesAsync();
        check(await Close(deposit.Booking.Id) != null, "Unallocated deposit blocks automatic departure");
        deposit.Bill.TienCocDaTru = 100; await db.SaveChangesAsync();
        check(await Close(deposit.Booking.Id) == null, "Reconciled deposit allows automatic departure without altering the receipt");

        var empty = await Party(4, paid: false, amount: 0);
        check(await Close(empty.Booking.Id) == null, "Empty overdue test party can finish without creating a fake payment");
        await Refresh(empty);
        check(empty.Bill.TrangThai == TrangThaiHoaDon.DaHuy && empty.Tables[0].TrangThai == TrangThaiBan.CanDon, "Empty placeholder invoice is cancelled while history is preserved");

        var race = await Party(4);
        await Task.WhenAll(Close(race.Booking.Id), Close(race.Booking.Id));
        await Refresh(race);
        check(race.Booking.ThoiDiemKetThuc != null && race.Tables[0].TrangThai == TrangThaiBan.CanDon, "Concurrent automatic checks end a party only once and keep Needs cleaning");
    }
}
