using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Services;

namespace Management.SmokeTests;

public static class PreorderSmoke
{
    public static async Task Run(RestaurantDbContext db, HttpClient admin, Func<HttpClient> newClient,
        Func<string, string, string> hidden, Action<bool, string> check)
    {
        async Task<string> Get(HttpClient client, string path)
        {
            using var response = await client.GetAsync(path);
            check(response.StatusCode == HttpStatusCode.OK, "Preorder GET " + path);
            return await response.Content.ReadAsStringAsync();
        }
        async Task<JsonElement> Ajax(HttpClient client, string path, string tokenForm,
            Dictionary<string, string> fields, HttpStatusCode expected = HttpStatusCode.OK)
        {
            var body = new Dictionary<string, string>(fields) { ["__RequestVerificationToken"] = hidden(tokenForm, "__RequestVerificationToken") };
            using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = new FormUrlEncodedContent(body) };
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            using var response = await client.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();
            check(response.StatusCode == expected, $"Preorder POST {path}: {response.StatusCode}; {text}");
            using var json = JsonDocument.Parse(text);
            check(json.RootElement.GetProperty("ok").GetBoolean() == (expected == HttpStatusCode.OK), "Preorder JSON outcome " + path);
            return json.RootElement.Clone();
        }
        async Task<string> Version(int id)
        {
            db.ChangeTracker.Clear();
            var booking = await db.DatBan.SingleAsync(x => x.Id == id);
            return Convert.ToBase64String((byte[])db.Entry(booking).Property("RowVersion").CurrentValue!);
        }
        async Task Login(HttpClient client, string email)
        {
            var form = await Get(client, "/Account/Login");
            using var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            { ["__RequestVerificationToken"] = hidden(form, "__RequestVerificationToken"), ["Email"] = email, ["Password"] = "Demo@2026!" }));
            check(response.StatusCode == HttpStatusCode.Redirect, "Preorder customer login");
        }

        var category = new DanhMuc { TenDanhMuc = "PREORDER-TEST", DangSuDung = true };
        var dish = new MonAn { TenMon = "PREORDER-TEST-DISH", DanhMuc = category, DaDuyet = true,
            TrangThai = TrangThaiMon.DangPhucVu, Loai = LoaiMon.MonLe,
            Sizes = { new MonAnSize { TenSize = "Quoted size", GiaBan = 125000m, DangSuDung = true } } };
        var area = new KhuVuc { TenKhuVuc = "PREORDER-TEST-AREA", Tang = 4, DangSuDung = true };
        var table = new BanAn { MaBan = "PREORDER-TEST-A", KhuVuc = area, SoChoNgoi = 4, TrangThai = TrangThaiBan.SanSang };
        var lateTable = new BanAn { MaBan = "PREORDER-TEST-B", KhuVuc = area, SoChoNgoi = 4, TrangThai = TrangThaiBan.SanSang };
        db.AddRange(dish, table, lateTable);
        await db.SaveChangesAsync();
        var size = dish.Sizes.Single();
        var customerAccount = await db.Users.Where(x => x.Email == "khach.demo@example.test").Select(x => x.Id).SingleAsync();
        var customerId = await db.KhachHang.Where(x => x.TaiKhoanId == customerAccount).Select(x => x.Id).SingleAsync();
        var staffId = await db.NhanVien.Where(x => x.DangLamViec).Select(x => x.Id).FirstAsync();
        var orders = new OrderService(db);
        var preorders = new PreorderService(db, orders);
        var tables = new TableService(db, preorders);
        using var customer = newClient();
        await Login(customer, "khach.demo@example.test");
        var customerForm = await Get(customer, "/DatBan");
        var createId = Guid.NewGuid();
        var createFields = new Dictionary<string, string>
        {
            ["RequestId"] = createId.ToString(), ["HoTen"] = "PREORDER-TEST-CUSTOMER", ["SoDienThoai"] = "0901234567",
            ["ThoiGianDen"] = TableService.VietnamNow.AddHours(4).ToString("yyyy-MM-ddTHH:mm"), ["SoNguoi"] = "2", ["ChuanBiTruoc"] = "true",
            ["Items[0].MonAnId"] = dish.Id.ToString(), ["Items[0].MonAnSizeId"] = size.Id.ToString(), ["Items[0].SoLuong"] = "1",
            ["Items[0].YeuCauCheBien"] = "PREORDER-FIFO-FIRST", ["Items[0].DonGiaThoaThuan"] = "1"
        };
        await Ajax(customer, "/DatBan", customerForm, createFields);
        var early = await db.DatBan.SingleAsync(x => x.YeuCauTaoId == createId);
        var earlyId = early.Id;
        check(await db.MonDatTruoc.AnyAsync(x => x.DatBanId == earlyId && x.DonGiaThoaThuan == 125000m), "Booking creation snapshots the server price, ignores forged client price");
        await Ajax(customer, "/DatBan", customerForm, createFields);
        check(await db.DatBan.CountAsync(x => x.YeuCauTaoId == createId) == 1, "Create RequestId replay creates one booking");
        await Ajax(customer, "/DatBan", customerForm, new(createFields) { ["Items[0].SoLuong"] = "2" }, HttpStatusCode.UnprocessableEntity);

        var context = JsonDocument.Parse(await Get(customer, $"/DatBan/CartContext/{earlyId}"));
        using (context)
            check(context.RootElement.GetProperty("bookingId").GetInt32() == earlyId && context.RootElement.GetProperty("canEdit").GetBoolean(), "Owned cart context provides booking and edit version");
        var detail = await Get(customer, $"/DatBan/ChiTiet/{earlyId}");
        check(WebUtility.HtmlDecode(detail).Contains(dish.TenMon), "Customer detail renders saved preorder snapshots");
        using (var anonymous = newClient())
        using (var denied = await anonymous.GetAsync($"/DatBan/CartContext/{earlyId}"))
            check(denied.StatusCode == HttpStatusCode.Redirect, "Anonymous cannot read cart context");
        using (var denied = await admin.GetAsync($"/DatBan/CartContext/{earlyId}"))
            check(denied.StatusCode == HttpStatusCode.Redirect, "Staff account cannot use the customer cart API");
        using var other = newClient();
        var register = await Get(other, "/Account/Register");
        using (var registered = await other.PostAsync("/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = hidden(register, "__RequestVerificationToken"), ["HoTen"] = "Other preorder owner",
            ["SoDienThoai"] = "0901234568", ["Email"] = $"preorder-{Guid.NewGuid():N}@example.test", ["Password"] = "TestOnly123!", ["ConfirmPassword"] = "TestOnly123!"
        }))) check(registered.StatusCode == HttpStatusCode.Redirect, "Register independent customer for ownership regression");
        foreach (var path in new[] { "CartContext", "ChiTiet" })
        {
            using var denied = await other.GetAsync($"/DatBan/{path}/{earlyId}");
            check(denied.StatusCode == HttpStatusCode.NotFound, "Other customer cannot read " + path);
        }
        var otherForm = await Get(other, "/DatBan");
        using (var denied = await other.PostAsync($"/DatBan/SavePreorder/{earlyId}", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["__RequestVerificationToken"] = hidden(otherForm, "__RequestVerificationToken"), ["RequestId"] = Guid.NewGuid().ToString(), ["RowVersion"] = await Version(earlyId) })))
            check(denied.StatusCode == HttpStatusCode.NotFound, "Other customer cannot replace owned preorders");
        using (var missingCsrf = await customer.PostAsync($"/DatBan/SavePreorder/{earlyId}", new FormUrlEncodedContent(new Dictionary<string, string>())))
            check(missingCsrf.StatusCode == HttpStatusCode.BadRequest, "Preorder save requires CSRF");

        var originalVersion = await Version(earlyId);
        var saveFields = new Dictionary<string, string>
        {
            ["RowVersion"] = originalVersion, ["RequestId"] = Guid.NewGuid().ToString(), ["ChuanBiTruoc"] = "true",
            ["Items[0].MonAnId"] = dish.Id.ToString(), ["Items[0].MonAnSizeId"] = size.Id.ToString(), ["Items[0].SoLuong"] = "1", ["Items[0].YeuCauCheBien"] = "PREORDER-FIFO-FIRST",
            ["Items[1].MonAnId"] = dish.Id.ToString(), ["Items[1].MonAnSizeId"] = size.Id.ToString(), ["Items[1].SoLuong"] = "2", ["Items[1].YeuCauCheBien"] = "Ít cay \"không hành\""
        };
        await Ajax(customer, $"/DatBan/SavePreorder/{earlyId}", customerForm, saveFields);
        check(await db.MonDatTruoc.CountAsync(x => x.DatBanId == earlyId) == 2, "Same size with different preparation notes persists as distinct lines");
        await Ajax(customer, $"/DatBan/SavePreorder/{earlyId}", customerForm, saveFields);
        check(await db.MonDatTruoc.CountAsync(x => x.DatBanId == earlyId) == 2, "Save RequestId replay does not duplicate lines");
        await Ajax(customer, $"/DatBan/SavePreorder/{earlyId}", customerForm, new(saveFields) { ["Items[0].SoLuong"] = "3" }, HttpStatusCode.Conflict);
        await Ajax(customer, $"/DatBan/SavePreorder/{earlyId}", customerForm, new(saveFields) { ["RequestId"] = Guid.NewGuid().ToString() }, HttpStatusCode.Conflict);
        check(await db.MonDatTruoc.Where(x => x.DatBanId == earlyId).SumAsync(x => x.SoLuong) == 3, "Modified replay and stale writers leave the saved draft intact");

        await Version(earlyId);
        var changedPrice = await db.MonAnSize.SingleAsync(x => x.Id == size.Id);
        changedPrice.GiaBan = 225000m;
        early = await db.DatBan.SingleAsync(x => x.Id == earlyId);
        early.TrangThai = TrangThaiDatBan.DaXacNhan;
        // Continue testing migrated manual agreements separately from the new automatic payment suite.
        early.CocTuDong = false;
        await db.ChiTietDatBan.Where(x => x.DatBanId == earlyId).ExecuteDeleteAsync();
        early.ThoiDiemTao = DateTimeOffset.UtcNow.AddHours(-1);
        db.ChiTietDatBan.Add(new() { DatBanId = earlyId, BanAnId = table.Id });
        await db.SaveChangesAsync();
        var internalForm = await Get(admin, $"/DatTruoc/ChiTiet/{earlyId}");
        async Task ReleaseEarly(HttpStatusCode expected) => await Ajax(admin, $"/DatTruoc/GuiBep/{earlyId}", internalForm, new()
        { ["RowVersion"] = await Version(earlyId), ["nhanVienId"] = staffId.ToString(), ["xacNhanLamTruoc"] = "true" }, expected);
        await ReleaseEarly(HttpStatusCode.BadRequest);
        check(!await db.HoaDon.AnyAsync(x => x.DatBanId == earlyId), "Early prep is blocked before a deposit agreement/payment");
        await Ajax(admin, $"/DatTruoc/ThoaThuan/{earlyId}", internalForm, new()
        { ["RowVersion"] = await Version(earlyId), ["SoTien"] = "50000", ["DieuKien"] = "Hoàn trước khi nấu; xử lý chi phí khi đã nấu." });
        await ReleaseEarly(HttpStatusCode.BadRequest);
        var depositFields = new Dictionary<string, string> { ["RowVersion"] = await Version(earlyId), ["RequestId"] = Guid.NewGuid().ToString(),
            ["Loai"] = "Thu", ["SoTien"] = "50000", ["LyDo"] = "Đã nhận tiền thực tế", ["MaThamChieu"] = "PREORDER-CASH-1" };
        await Ajax(admin, $"/DatTruoc/GiaoDichCoc/{earlyId}", internalForm, depositFields);
        await Ajax(admin, $"/DatTruoc/GiaoDichCoc/{earlyId}", internalForm, depositFields);
        await Ajax(admin, $"/DatTruoc/GiaoDichCoc/{earlyId}", internalForm, new(depositFields) { ["SoTien"] = "1" }, HttpStatusCode.BadRequest);
        check(await db.GiaoDichCoc.CountAsync(x => x.DatBanId == earlyId) == 1, "Deposit GUID replay records one transaction and rejects modified amount");
        await ReleaseEarly(HttpStatusCode.OK);
        await ReleaseEarly(HttpStatusCode.OK);
        check(await db.ChiTietHoaDon.CountAsync(x => x.MonDatTruoc != null && x.MonDatTruoc.DatBanId == earlyId) == 2, "Repeated kitchen release sends each preorder once");
        var earlyLines = await db.ChiTietHoaDon.AsNoTracking().Where(x => x.MonDatTruoc != null && x.MonDatTruoc.DatBanId == earlyId).OrderBy(x => x.Id).ToListAsync();
        check(earlyLines.All(x => x.DonGia == 125000m && x.ThoiDiemGoi > DateTimeOffset.UtcNow.AddMinutes(-2)), "Kitchen uses agreed prices and actual release timestamps");

        var late = new DatBan { MaDatBan = "PREORDER-LATE", KhachHangId = customerId, HoTenLienHe = "PREORDER-LATE",
            SoDienThoaiLienHe = "0901234567", SoNguoiLon = 2, TrangThai = TrangThaiDatBan.DaXacNhan,
            ThoiDiemTao = DateTimeOffset.UtcNow.AddDays(-3), GioDen = DateTimeOffset.UtcNow.AddMinutes(1), GioKetThucDuKien = DateTimeOffset.UtcNow.AddHours(2) };
        db.DatBan.Add(late);
        await db.SaveChangesAsync();
        var lateId = late.Id;
        db.ChiTietDatBan.Add(new() { DatBanId = lateId, BanAnId = lateTable.Id });
        await db.SaveChangesAsync();
        check(await preorders.Save(lateId, customerAccount, new SavePreorderModel { RowVersion = await Version(lateId), RequestId = Guid.NewGuid(),
            Items = [new() { MonAnId = dish.Id, MonAnSizeId = size.Id, SoLuong = 1, YeuCauCheBien = "PREORDER-FIFO-SECOND" }] }) is null,
            "Owner can add dishes one minute before arrival without an artificial cutoff");
        check(await preorders.Quote(lateId, new() { RowVersion = await Version(lateId), SoTien = 10000m, DieuKien = "Đã thỏa thuận" }) is null, "Late booking quote");
        var actorId = await db.Users.Where(x => x.Email == "auth-smoke-admin@example.test").Select(x => x.Id).SingleAsync();
        check(await preorders.RecordDeposit(lateId, actorId, new() { RowVersion = await Version(lateId), RequestId = Guid.NewGuid(), Loai = LoaiGiaoDichCoc.Thu,
            SoTien = 10000m, LyDo = "Đã nhận tiền" }) is null, "Late booking deposit receipt");
        check(await tables.CheckIn(lateId, await Version(lateId), staffId) is null, "Check-in releases unsent preorders in its transaction");
        check(await preorders.Release(lateId, await Version(lateId), staffId, false) is null, "Release after check-in is idempotent");
        check(await db.ChiTietHoaDon.CountAsync(x => x.MonDatTruoc != null && x.MonDatTruoc.DatBanId == lateId) == 1, "Check-in plus manual release produces one kitchen line");
        var lateLine = await db.ChiTietHoaDon.AsNoTracking().SingleAsync(x => x.MonDatTruoc != null && x.MonDatTruoc.DatBanId == lateId);
        check(lateLine.DonGia == 225000m, "New additions use the current server price while already agreed lines retain their snapshot");
        check(lateLine.ThoiDiemGoi >= earlyLines.Max(x => x.ThoiDiemGoi), "Older booking released later joins the queue later");
        using var cook = newClient();
        var cookLogin = await Get(cook, "/admin");
        using (var signedIn = await cook.PostAsync("/admin", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["__RequestVerificationToken"] = hidden(cookLogin, "__RequestVerificationToken"), ["Email"] = "bep.demo@example.test", ["Password"] = "Demo@2026!" })))
            check(signedIn.StatusCode == HttpStatusCode.Redirect, "Preorder kitchen login");
        var kitchen = WebUtility.HtmlDecode(await Get(cook, "/Bep"));
        var firstPosition = kitchen.IndexOf("PREORDER-FIFO-FIRST", StringComparison.Ordinal);
        var secondPosition = kitchen.IndexOf("PREORDER-FIFO-SECOND", StringComparison.Ordinal);
        check(firstPosition >= 0 && secondPosition > firstPosition, "Actual KDS renders FIFO by kitchen release, not booking/draft creation");

        check(await orders.UpdateDishStatusAsync(earlyLines[0].Id, TrangThaiCheBien.DangCheBien, false) is null, "Start cooking one early-prep line");
        await Ajax(customer, $"/DatBan/YeuCauHuy/{earlyId}", customerForm, new()
        { ["RowVersion"] = await Version(earlyId), ["LyDo"] = "Khách xin hủy sau khi bếp bắt đầu" });
        var requested = await db.DatBan.AsNoTracking().SingleAsync(x => x.Id == earlyId);
        check(requested.YeuCauHuy != null && requested.TrangThai == TrangThaiDatBan.DaXacNhan && requested.TienCocDaNop == 50000m,
            "Customer cancellation remains an administrative request; no automatic cancellation/refund");
        check(await preorders.Save(earlyId, customerAccount, new() { RowVersion = await Version(earlyId), RequestId = Guid.NewGuid() }) is not null,
            "Pending cancellation blocks additional customer edits");
        using (var deniedRequest = new HttpRequestMessage(HttpMethod.Post, $"/DatTruoc/XuLyHuy/{earlyId}")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = hidden(customerForm, "__RequestVerificationToken"),
                ["RowVersion"] = await Version(earlyId), ["lyDo"] = "Customer cannot resolve own cancellation", ["daDoiChieu"] = "true" })
        })
        {
            deniedRequest.Headers.Add("X-Requested-With", "XMLHttpRequest");
            using var denied = await customer.SendAsync(deniedRequest);
            check(denied.StatusCode == HttpStatusCode.Forbidden || (denied.StatusCode == HttpStatusCode.Redirect
                && denied.Headers.Location?.ToString().Contains("Denied") == true), "Customer cannot invoke administrative cancellation resolution");
        }
        await Ajax(admin, $"/DatTruoc/XuLyHuy/{earlyId}", internalForm, new()
        { ["RowVersion"] = await Version(earlyId), ["lyDo"] = "Đã đối chiếu", ["daDoiChieu"] = "false" }, HttpStatusCode.BadRequest);
        await Ajax(admin, $"/DatTruoc/XuLyHuy/{earlyId}", internalForm, new()
        { ["RowVersion"] = await Version(earlyId), ["lyDo"] = "Đã đối chiếu", ["daDoiChieu"] = "true" }, HttpStatusCode.BadRequest);
        await Ajax(admin, $"/DatTruoc/GiaoDichCoc/{earlyId}", internalForm, new()
        { ["RowVersion"] = await Version(earlyId), ["RequestId"] = Guid.NewGuid().ToString(), ["Loai"] = "Hoan", ["SoTien"] = "50000", ["LyDo"] = "Quản lý đồng ý hoàn toàn bộ cọc" });
        await Ajax(admin, $"/DatTruoc/XuLyHuy/{earlyId}", internalForm, new()
        { ["RowVersion"] = await Version(earlyId), ["lyDo"] = "Đã hoàn cọc và ghi nhận chi phí món đã nấu", ["daDoiChieu"] = "true" });
        var resolved = await db.DatBan.AsNoTracking().SingleAsync(x => x.Id == earlyId);
        var preserved = await db.ChiTietHoaDon.AsNoTracking().Where(x => x.MonDatTruoc != null && x.MonDatTruoc.DatBanId == earlyId).ToListAsync();
        check(resolved.TrangThai == TrangThaiDatBan.DaHuy && resolved.TienCocDaHoan == 50000m, "Admin closes cancellation after actual deposit handling");
        check(preserved.Count == 2 && preserved.Any(x => x.TrangThai == TrangThaiCheBien.DangCheBien)
            && preserved.Any(x => x.TrangThai == TrangThaiCheBien.DaHuy), "Cancellation preserves cooked history and cancels only waiting lines");
        var closedBills = await db.HoaDon.AsNoTracking().Include(x => x.ChiTiet).Where(x => x.DatBanId == earlyId).ToListAsync();
        check(closedBills.All(x => x.TrangThai == TrangThaiHoaDon.DaHuy
            && x.TongTienHang == x.ChiTiet.Where(i => i.TrangThai != TrangThaiCheBien.DaHuy).Sum(i => i.SoLuong * i.DonGia)),
            "Closed unpaid invoice excludes canceled waiting dishes from its cost total");
    }
}
