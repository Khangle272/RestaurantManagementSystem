using System.Net;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Services;

namespace Management.SmokeTests;

public static class Week8DemoSmoke
{
    public static async Task Run(RestaurantDbContext db, HttpClient admin, Action<bool, string> check)
    {
        await Week8DemoSetup.InitializeAsync(db);
        var bookings = await db.DatBan.Include(x => x.Ban).ThenInclude(x => x.BanAn)
            .Include(x => x.KhachHang).Include(x => x.NhanVienTiepNhan)
            .Where(x => x.MaDatBan.StartsWith("W8-DEMO-")).OrderBy(x => x.Id).ToListAsync();
        check(bookings.Count == 3 && bookings.All(x => x.TrangThai == TrangThaiDatBan.DaNhanBan
            && x.KhachHang?.TaiKhoanId is not null && x.NhanVienTiepNhan?.MaNhanVien == "DEMO-TT"
            && x.Ban.Count == 1 && x.Ban.Single().BanAn.TrangThai == TrangThaiBan.DangPhucVu),
            "Week 8 demo has three occupied tables linked to existing customer and reception profiles");
        var versions = bookings.ToDictionary(x => x.Id, x => Convert.ToBase64String(db.Entry(x).Property<byte[]>("RowVersion").CurrentValue!));
        var billCount = await db.HoaDon.CountAsync();
        var tableCount = await db.BanAn.CountAsync();
        await Week8DemoSetup.InitializeAsync(db);
        foreach (var booking in bookings) await db.Entry(booking).ReloadAsync();
        check(await db.HoaDon.CountAsync() == billCount && await db.BanAn.CountAsync() == tableCount
            && bookings.All(x => versions[x.Id] == Convert.ToBase64String(db.Entry(x).Property<byte[]>("RowVersion").CurrentValue!)),
            "Repeating Week 8 initialization creates no duplicate tables/bills and leaves booking versions unchanged");

        var sample = bookings[0];
        var day = DateOnly.FromDateTime(sample.GioDen.ToOffset(TimeSpan.FromHours(7)).DateTime);
        var scanTime = TimeOnly.FromDateTime(sample.GioDen.ToOffset(TimeSpan.FromHours(7)).DateTime).AddHours(12);
        var areaId = sample.Ban.Single().BanAn.KhuVucId;
        var query = $"/SoDoBan?ngay={day:yyyy-MM-dd}&gio={Uri.EscapeDataString(scanTime.ToString("HH:mm"))}&khuVucId={areaId}";
        var page = WebUtility.HtmlDecode(await admin.GetStringAsync(query));
        check(page.Contains("type=\"date\"") && page.Contains("type=\"time\"")
            && bookings.All(x => page.Contains(x.MaDatBan)
                && page.Contains(x.GioDen.ToOffset(TimeSpan.FromHours(7)).ToString("HH:mm dd/MM"))
                && page.Contains(x.GioKetThucDuKien.ToOffset(TimeSpan.FromHours(7)).ToString("HH:mm dd/MM"))),
            "Floor board shows full-day booking start/end ranges outside the selected two-hour window");
        var fragment = WebUtility.HtmlDecode(await admin.GetStringAsync(query + "&fragment=true"));
        check(bookings.All(x => fragment.Contains(x.MaDatBan)), "AJAX board refresh preserves the selected schedule day");
        var otherDay = WebUtility.HtmlDecode(await admin.GetStringAsync($"/SoDoBan?ngay={day.AddDays(2):yyyy-MM-dd}&gio=12%3A00&khuVucId={areaId}&fragment=true"));
        check(!bookings.Any(x => otherDay.Contains(x.MaDatBan)), "Floor board schedule excludes bookings outside the selected day");
        var bookingPage = await admin.GetStringAsync($"/SoDoBan?datBanId={sample.Id}");
        check(bookingPage.Contains($"name=\"datBanId\" value=\"{sample.Id}\"")
            && bookingPage.Contains("data-poll=\"false\""), "Selecting a booking keeps its assignment context and existing polling behavior");
    }
}
