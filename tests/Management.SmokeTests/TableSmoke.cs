using System.Net;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Services;

public static class TableSmoke
{
    public static async Task Run(RestaurantDbContext db, HttpClient admin, Func<HttpClient> newClient,
        Func<string, string, string> hidden, Action<bool, string> check)
    {
        async Task<string> Get(HttpClient client, string path)
        {
            using var response = await client.GetAsync(path);
            check(response.StatusCode == HttpStatusCode.OK, "Table GET " + path);
            return await response.Content.ReadAsStringAsync();
        }
        async Task Post(HttpClient client, string path, string form, Dictionary<string, string> fields)
        {
            fields["__RequestVerificationToken"] = hidden(form, "__RequestVerificationToken");
            using var response = await client.PostAsync(path, new FormUrlEncodedContent(fields));
            check(response.StatusCode == HttpStatusCode.Redirect, "Table POST " + path);
        }
        async Task<string> Version<T>(T entity) where T : class
        {
            await db.Entry(entity).ReloadAsync();
            return Convert.ToBase64String(db.Entry(entity).Property<byte[]>("RowVersion").CurrentValue!);
        }
        using var customer = newClient();
        await Post(customer, "/Account/Login", await Get(customer, "/Account/Login"), new()
        { ["Email"] = "khach.demo@example.test", ["Password"] = "Demo@2026!" });
        var area = new KhuVuc { TenKhuVuc = "TABLE-TEST-FLOOR", Tang = 2 };
        db.KhuVuc.Add(area); await db.SaveChangesAsync();
        var table = new BanAn { MaBan = "TABLE-TEST-A", KhuVucId = area.Id, SoChoNgoi = 4, TrangThai = TrangThaiBan.SanSang };
        var secondTable = new BanAn { MaBan = "TABLE-TEST-B", KhuVucId = area.Id, SoChoNgoi = 4, TrangThai = TrangThaiBan.SanSang };
        db.BanAn.AddRange(table, secondTable); await db.SaveChangesAsync();
        var tableCreate = await Get(admin, "/QuanLyDatBan/Create?banAnId=" + table.Id);
        check(tableCreate.Contains(table.MaBan) && hidden(tableCreate, "BanAnId") == table.Id.ToString(), "Clicking a table pre-fills the reception request without reserving it");
        await Post(admin, "/QuanLyDatBan/Create", tableCreate, new()
        { ["HoTen"] = "Khách chọn ô bàn", ["SoDienThoai"] = "0907777888", ["GioDen"] = TableService.VietnamNow.AddHours(6).ToString("yyyy-MM-ddTHH:mm"),
          ["SoPhut"] = "120", ["SoNguoi"] = "2", ["KhuVucId"] = area.Id.ToString(), ["BanAnId"] = table.Id.ToString() });
        var selectedRequest = await db.DatBan.SingleAsync(x => x.HoTenLienHe == "Khách chọn ô bàn");
        check(selectedRequest.TrangThai == TrangThaiDatBan.ChoXacNhan && !await db.ChiTietDatBan.AnyAsync(x => x.DatBanId == selectedRequest.Id),
            "Reception table shortcut creates a pending request, never auto-approves or locks a table");
        var suggestedBoard = await Get(admin, $"/SoDoBan?datBanId={selectedRequest.Id}&khuVucId={area.Id}&preferredTableId={table.Id}");
        check(suggestedBoard.Contains("data-suggested=\"true\"") && suggestedBoard.Contains("Chọn bàn gợi ý")
            && suggestedBoard.Contains("checked=\"checked\""), "Suitable table is suggested and requested table is preselected for explicit confirmation");
        check(WebUtility.HtmlDecode(suggestedBoard).Contains("Khách chọn ô bàn") && suggestedBoard.Contains("booking-context"), "Booking context stays beside the floor board");
        using (var denied = await customer.GetAsync("/admin/TrangChu"))
            check(denied.StatusCode == HttpStatusCode.Redirect, "Customer cannot enter the internal workspace");
        using (var missing = await admin.GetAsync("/QuanLyDatBan/Create?banAnId=2147483647"))
            check(missing.StatusCode == HttpStatusCode.NotFound, "Reception shortcut rejects a nonexistent physical table");
        using var receptionist = newClient();
        await Post(receptionist, "/admin", await Get(receptionist, "/admin"), new()
        { ["Email"] = "tieptan.demo@example.test", ["Password"] = "Demo@2026!" });
        var receptionBoard = await Get(receptionist, "/SoDoBan");
        using (var denied = await receptionist.PostAsync("/SoDoBan/TrangThai", new FormUrlEncodedContent(new Dictionary<string, string> {
            ["id"] = table.Id.ToString(), ["clean"] = "true", ["rowVersion"] = await Version(table),
            ["__RequestVerificationToken"] = hidden(receptionBoard, "__RequestVerificationToken") })))
            check(denied.StatusCode == HttpStatusCode.Redirect, "Reception cannot perform waiter-only finish or cleaning operations");
        var arrival = TableService.VietnamNow.AddHours(2).ToString("yyyy-MM-ddTHH:mm");
        await Post(customer, "/DatBan", await Get(customer, "/DatBan"), new()
        { ["HoTen"] = "Khách sở hữu", ["SoDienThoai"] = "0903333444", ["ThoiGianDen"] = arrival,
          ["SoNguoi"] = "2", ["MaKhuVuc"] = area.Id.ToString() });
        var customerId = await db.KhachHang.Where(x => x.Email == "khach.demo@example.test").Select(x => x.Id).SingleAsync();
        var own = await db.DatBan.SingleAsync(x => x.HoTenLienHe == "Khách sở hữu");
        check(own.KhachHangId == customerId && own.KhuVucUuTienId == area.Id && own.TrangThai == TrangThaiDatBan.ChoXacNhan,
            "Customer booking belongs to authenticated account, stores preference and pending state");
        check((await Get(customer, "/DatBan/LichSu")).Contains(own.MaDatBan), "Customer sees own reservation without phone lookup");
        using (var anonymous = newClient())
        {
            using var blocked = await anonymous.GetAsync("/DatBan/Success/" + own.Id);
            check(blocked.StatusCode == HttpStatusCode.Redirect, "Anonymous cannot inspect reservation by ID");
        }
        var other = new DatBan { MaDatBan = "TABLE-OTHER-OWNER", HoTenLienHe = "Khách khác", LaKhachTrucTiep = true, GioDen = DateTimeOffset.UtcNow,
            GioKetThucDuKien = DateTimeOffset.UtcNow.AddHours(2), SoNguoiLon = 2, TrangThai = TrangThaiDatBan.ChoXacNhan };
        db.DatBan.Add(other); await db.SaveChangesAsync();
        using (var denied = await customer.GetAsync("/DatBan/Success/" + other.Id))
            check(denied.StatusCode == HttpStatusCode.NotFound, "Customer cannot inspect another reservation");
        check(!(await Get(customer, "/DatBan/LichSu?bookingCode=" + other.MaDatBan)).Contains(other.MaDatBan + "</"), "Search cannot expose another customer's booking");
        using (var denied = await customer.PostAsync("/QuanLyDatBan/XacNhan", new FormUrlEncodedContent(new Dictionary<string, string>())))
            check(denied.StatusCode == HttpStatusCode.Redirect, "Customer cannot approve reservations");

        await Post(admin, "/QuanLyDatBan/Create", await Get(admin, "/QuanLyDatBan/Create"), new()
        { ["HoTen"] = "Khách điện thoại", ["SoDienThoai"] = "0902222333", ["GioDen"] = TableService.VietnamNow.AddMinutes(5).ToString("yyyy-MM-ddTHH:mm"),
          ["SoPhut"] = "120", ["SoNguoi"] = "6", ["KhuVucId"] = area.Id.ToString() });
        var reception = await db.DatBan.SingleAsync(x => x.HoTenLienHe == "Khách điện thoại");
        check(reception.LaKhachTrucTiep && reception.TrangThai == TrangThaiDatBan.ChoXacNhan, "Reception creates requested booking, not a physical table");
        var board = await Get(admin, "/SoDoBan?datBanId=" + reception.Id);
        await Post(admin, "/QuanLyDatBan/XacNhan", board, new()
        { ["datBanId"] = reception.Id.ToString(), ["banAnIds"] = table.Id.ToString(), ["rowVersion"] = await Version(reception) });
        await db.Entry(reception).ReloadAsync();
        check(reception.TrangThai == TrangThaiDatBan.ChoXacNhan, "Capacity guard rejects undersized table");
        await Post(admin, "/QuanLyDatBan/XacNhan", board, new()
        { ["datBanId"] = reception.Id.ToString(), ["banAnIds[0]"] = table.Id.ToString(), ["banAnIds[1]"] = secondTable.Id.ToString(), ["rowVersion"] = await Version(reception) });
        await db.Entry(reception).ReloadAsync();
        check(reception.TrangThai == TrangThaiDatBan.DaXacNhan && await db.ChiTietDatBan.CountAsync(x => x.DatBanId == reception.Id) == 2, "Combined tables confirmed atomically");
        await Post(admin, "/QuanLyDatBan/XacNhan", board, new()
        { ["datBanId"] = other.Id.ToString(), ["banAnIds"] = table.Id.ToString(), ["rowVersion"] = await Version(other) });
        await db.Entry(other).ReloadAsync();
        check(other.TrangThai == TrangThaiDatBan.ChoXacNhan, "Actual overlapping intervals cannot claim occupied reservation slot");
        // Adjacent non-overlapping intervals can use the same physical table.
        other.GioDen = reception.GioKetThucDuKien; other.GioKetThucDuKien = other.GioDen.AddHours(1);
        await db.SaveChangesAsync();
        await Post(admin, "/QuanLyDatBan/XacNhan", board, new()
        { ["datBanId"] = other.Id.ToString(), ["banAnIds"] = table.Id.ToString(), ["rowVersion"] = await Version(other) });
        await db.Entry(other).ReloadAsync();
        check(other.TrangThai == TrangThaiDatBan.DaXacNhan, "Back-to-back reservations do not falsely conflict");
        table.TrangThai = TrangThaiBan.CanDon; await db.SaveChangesAsync();
        await Post(admin, "/QuanLyDatBan/NhanBan", board, new() { ["datBanId"] = reception.Id.ToString(), ["rowVersion"] = await Version(reception) });
        await db.Entry(reception).ReloadAsync();
        check(reception.TrangThai == TrangThaiDatBan.DaXacNhan, "Cannot receive guests at a table still needing cleaning");
        var floorBoard = await Get(admin, "/SoDoBan?tang=2");
        check(floorBoard.Contains(table.MaBan) && !floorBoard.Contains("BanAn/Create"), "Floor board displays filtered physical tables");
        await Post(admin, "/SoDoBan/TrangThai", floorBoard, new() { ["id"] = table.Id.ToString(), ["clean"] = "true", ["rowVersion"] = await Version(table) });
        await Post(admin, "/QuanLyDatBan/NhanBan", board, new() { ["datBanId"] = reception.Id.ToString(), ["rowVersion"] = await Version(reception) });
        await db.Entry(reception).ReloadAsync(); await db.Entry(table).ReloadAsync();
        check(reception.TrangThai == TrangThaiDatBan.DaNhanBan && table.TrangThai == TrangThaiBan.DangPhucVu, "Receive party starts serving tables even for Admin without employee profile");
        await Post(admin, "/QuanLyDatBan/HuyBan", board, new() { ["datBanId"] = other.Id.ToString(), ["lyDoHuy"] = "Khách đổi lịch", ["rowVersion"] = await Version(other) });
        await db.Entry(table).ReloadAsync();
        check(table.TrangThai == TrangThaiBan.DangPhucVu, "Cancelling a future booking cannot release another party's table");
        var staffId = await db.NhanVien.Select(x => x.Id).FirstAsync();
        var bill = new HoaDon { MaHoaDon = "TABLE-OPEN-BILL", NhanVienId = staffId, DatBanId = reception.Id, TongTienHang = 100,
            ThoiDiemLap = DateTimeOffset.UtcNow, TrangThai = TrangThaiHoaDon.ChuaThanhToan };
        db.HoaDon.Add(bill); await db.SaveChangesAsync();
        await Post(admin, "/SoDoBan/TrangThai", floorBoard, new() { ["id"] = table.Id.ToString(), ["rowVersion"] = await Version(table) });
        await db.Entry(table).ReloadAsync();
        check(table.TrangThai == TrangThaiBan.DangPhucVu, "Unpaid bill prevents table departure");
        bill.TrangThai = TrangThaiHoaDon.DaThanhToan; await db.SaveChangesAsync();
        check((await Get(admin, "/QuanLyDatBan?tab=DangPhucVu")).Contains(reception.MaDatBan), "Payment alone does not end table occupancy in reception queue");
        var staleVersion = await Version(table);
        await Post(admin, "/SoDoBan/TrangThai", floorBoard, new() { ["id"] = table.Id.ToString(), ["rowVersion"] = staleVersion });
        await db.Entry(table).ReloadAsync(); await db.Entry(secondTable).ReloadAsync(); await db.Entry(reception).ReloadAsync();
        check(table.TrangThai == TrangThaiBan.CanDon && secondTable.TrangThai == TrangThaiBan.CanDon && reception.ThoiDiemKetThuc.HasValue, "Party departure closes occupancy and marks all combined tables for cleaning");
        check(!(await Get(admin, "/QuanLyDatBan?tab=DangPhucVu")).Contains(reception.MaDatBan)
            && (await Get(admin, "/QuanLyDatBan?tab=HoanTatHuy")).Contains(reception.MaDatBan), "Completed occupancy moves to reception history rather than remaining active");
        await Post(admin, "/SoDoBan/TrangThai", floorBoard, new() { ["id"] = table.Id.ToString(), ["clean"] = "true", ["rowVersion"] = staleVersion });
        await db.Entry(table).ReloadAsync(); check(table.TrangThai == TrangThaiBan.CanDon, "Stale board cannot overwrite newer state");
        await Post(admin, "/SoDoBan/TrangThai", floorBoard, new() { ["id"] = table.Id.ToString(), ["clean"] = "true", ["rowVersion"] = await Version(table) });
        await db.Entry(table).ReloadAsync(); check(table.TrangThai == TrangThaiBan.SanSang, "Cleaning completes the table lifecycle");
        var requests = Enumerable.Range(0, 2).Select(i => new DatBan { MaDatBan = "TABLE-RACE-" + i, HoTenLienHe = "Race " + i, LaKhachTrucTiep = true,
            GioDen = DateTimeOffset.UtcNow.AddDays(1), GioKetThucDuKien = DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
            SoNguoiLon = 2, TrangThai = TrangThaiDatBan.ChoXacNhan }).ToArray();
        db.DatBan.AddRange(requests); await db.SaveChangesAsync();
        var versions = new[] { await Version(requests[0]), await Version(requests[1]) };
        await Task.WhenAll(requests.Select((request, i) => Post(admin, "/QuanLyDatBan/XacNhan", floorBoard, new()
        { ["datBanId"] = request.Id.ToString(), ["banAnIds"] = table.Id.ToString(), ["rowVersion"] = versions[i] })));
        foreach (var request in requests) await db.Entry(request).ReloadAsync();
        check(requests.Count(x => x.TrangThai == TrangThaiDatBan.DaXacNhan) == 1, "Concurrent approvals cannot double-book a table");
    }
}
