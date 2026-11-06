using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Shared.Mobile;

namespace Management.SmokeTests;

// REST API /api cho mobile: bearer token, phân quyền theo vai trò, sơ đồ bàn, gọi món, phục vụ, thanh toán,
// tiếp nhận đặt bàn và dữ liệu riêng của khách. Dùng tài khoản demo (mật khẩu Demo@2026!).
public static class MobileApiSmoke
{
    private const string Password = "Demo@2026!";

    public static async Task Run(RestaurantDbContext db, HttpClient webAdmin, Func<HttpClient> newClient, Action<bool, string> check)
    {
        // 1. Xác thực và từ chối truy cập.
        using (var anonymous = newClient())
        {
            using var response = await anonymous.GetAsync("/api/staff/tables");
            check(response.StatusCode == HttpStatusCode.Unauthorized && response.Headers.Location is null,
                "API anonymous request returns 401 instead of a login redirect");
            using var wrong = await anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest("boiban.demo@example.test", "Sai@2026!"));
            var error = await wrong.Content.ReadFromJsonAsync<ApiError>();
            check(wrong.StatusCode == HttpStatusCode.Unauthorized && error?.Message.Contains("không đúng") == true,
                "API login rejects a wrong password with a Vietnamese JSON message");
            using var kitchen = await anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest("bep.demo@example.test", Password));
            check(kitchen.StatusCode == HttpStatusCode.Forbidden, "API login refuses kitchen accounts (they use the website)");
            using var missing = await anonymous.GetAsync("/api/khong-ton-tai");
            check(missing.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized
                && missing.Content.Headers.ContentType?.MediaType != "text/html", "Unknown API path is not rendered as an HTML error page");
        }
        using (var cookieOnly = await webAdmin.GetAsync("/api/staff/tables"))
            check(cookieOnly.StatusCode == HttpStatusCode.Unauthorized, "Website cookie does not authenticate API requests (no CSRF surface)");

        using var waiter = await Login(newClient, "boiban.demo@example.test");
        using var cashier = await Login(newClient, "thungan.demo@example.test");
        using var reception = await Login(newClient, "tieptan.demo@example.test");
        using var customer = await Login(newClient, "khach.demo@example.test");
        var me = await waiter.GetFromJsonAsync<UserProfileDto>("/api/auth/me");
        check(me is { Role: "BoiBan", IsCustomer: false, CanServe: true, CanReceive: false }, "API /me returns the waiter role and permissions");
        var customerMe = await customer.GetFromJsonAsync<UserProfileDto>("/api/auth/me");
        check(customerMe is { IsCustomer: true, CanOrder: false }, "API /me marks customer accounts");
        await Expect(customer, "/api/staff/tables", HttpStatusCode.Forbidden, "Customer token cannot read the table map", check);
        await Expect(waiter, "/api/customer/summary", HttpStatusCode.Forbidden, "Staff token cannot read customer endpoints", check);
        await Expect(waiter, "/api/staff/reservations", HttpStatusCode.Forbidden, "Waiter cannot open reception bookings", check);

        // 2. Dữ liệu riêng của bài kiểm tra.
        var staffId = await db.NhanVien.Where(x => x.Email == "boiban.demo@example.test").Select(x => x.Id).SingleAsync();
        var customerId = await db.KhachHang.Where(x => x.Email == "khach.demo@example.test").Select(x => x.Id).SingleAsync();
        var area = new KhuVuc { TenKhuVuc = "MAPI-SẢNH", Tang = 9, DangSuDung = true };
        var seated = new BanAn { MaBan = "MAPI-01", KhuVuc = area, SoChoNgoi = 4, TrangThai = TrangThaiBan.DangPhucVu };
        var free = new BanAn { MaBan = "MAPI-02", KhuVuc = area, SoChoNgoi = 4, TrangThai = TrangThaiBan.SanSang };
        var small = new BanAn { MaBan = "MAPI-03", KhuVuc = area, SoChoNgoi = 2, TrangThai = TrangThaiBan.SanSang };
        var dish = new MonAn
        {
            TenMon = "MAPI-PHỞ BÒ", DanhMuc = new DanhMuc { TenDanhMuc = "MAPI-MÓN NƯỚC", DangSuDung = true },
            Loai = LoaiMon.MonLe, DaDuyet = true, TrangThai = TrangThaiMon.DangPhucVu,
            Sizes = { new MonAnSize { TenSize = "Lớn", GiaBan = 85000m, DangSuDung = true } }
        };
        var now = DateTimeOffset.UtcNow;
        var party = new DatBan
        {
            MaDatBan = "DB-MAPI-SEAT", KhachHangId = customerId, HoTenLienHe = "MAPI Khách ngồi", SoDienThoaiLienHe = "0909000111",
            ThoiDiemTao = now.AddMinutes(-5), GioDen = now.AddMinutes(-1), GioKetThucDuKien = now.AddHours(2), SoNguoiLon = 2,
            TrangThai = TrangThaiDatBan.DaNhanBan, ThoiDiemNhanBan = now.AddMinutes(-1), Ban = { new ChiTietDatBan { BanAn = seated } }
        };
        var request = new DatBan
        {
            MaDatBan = "DB-MAPI-NEW", HoTenLienHe = "MAPI Khách đặt", SoDienThoaiLienHe = "0909000222",
            ThoiDiemTao = now, GioDen = now.AddHours(3), GioKetThucDuKien = now.AddHours(5), SoNguoiLon = 3,
            TrangThai = TrangThaiDatBan.ChoXacNhan
        };
        db.AddRange(free, small, dish, party, request);
        await db.SaveChangesAsync();
        var size = dish.Sizes.Single();

        // 3. Sơ đồ bàn và thực đơn.
        var tables = await waiter.GetFromJsonAsync<List<TableDto>>("/api/staff/tables");
        var seatedCard = tables?.SingleOrDefault(x => x.Id == seated.Id);
        check(seatedCard is { TrangThai: "DangPhucVu", KhachHang: "MAPI Khách ngồi", HoaDonId: null } && !string.IsNullOrEmpty(seatedCard.RowVersion),
            "API table map shows the seated party without an order yet");
        check(tables!.Single(x => x.Id == free.Id).TrangThai == "Trong", "API table map shows a free table");
        var menu = await waiter.GetFromJsonAsync<MenuDto>("/api/staff/menu");
        check(menu?.Mon.Any(x => x.Id == dish.Id && x.Sizes.Any(s => s.Id == size.Id && s.GiaBan == 85000m)) == true,
            "API menu lists approved dishes with sizes and prices");

        // 4. Gọi món tại bàn, gọi thêm, kiểm tra lỗi.
        using (var noItems = await waiter.PostAsJsonAsync("/api/staff/orders", new CreateOrderRequest("TaiBan", seated.Id, null, null, null, null, [])))
            check(noItems.StatusCode == HttpStatusCode.BadRequest
                && (await noItems.Content.ReadFromJsonAsync<ApiError>())?.Message.Contains("ít nhất một món") == true,
                "API rejects an order without dishes with the service message");
        using (var badTakeaway = await waiter.PostAsJsonAsync("/api/staff/orders",
            new CreateOrderRequest("MangDi", null, "MAPI mang đi", null, null, null, [new(dish.Id, size.Id, 1, null)])))
            check(badTakeaway.StatusCode == HttpStatusCode.BadRequest, "API takeaway order requires the recipient phone");
        using var created = await waiter.PostAsJsonAsync("/api/staff/orders",
            new CreateOrderRequest("TaiBan", seated.Id, null, null, null, null, [new(dish.Id, size.Id, 2, "Ít hành")]));
        var createdBody = await created.Content.ReadFromJsonAsync<OrderCreatedDto>();
        check(created.StatusCode == HttpStatusCode.OK && createdBody is { HoaDonId: > 0 }, "API creates a dine-in order for the seated table");
        var billId = createdBody!.HoaDonId;
        using (var more = await waiter.PostAsJsonAsync($"/api/staff/orders/{billId}/items", new AddItemsRequest([new(dish.Id, size.Id, 1, null)])))
            check(more.StatusCode == HttpStatusCode.OK, "API adds more dishes to the open order");
        db.ChangeTracker.Clear();
        var lines = await db.ChiTietHoaDon.Where(x => x.HoaDonId == billId).OrderBy(x => x.Id).ToListAsync();
        check(lines.Count == 2 && lines.All(x => x.TrangThai == TrangThaiCheBien.ChoCheBien) && lines[0].YeuCauCheBien == "Ít hành"
            && await db.HoaDon.Where(x => x.Id == billId).Select(x => x.TongTienHang).SingleAsync() == 255000m,
            "API order lines reach the kitchen queue with notes and the invoice total");
        tables = await waiter.GetFromJsonAsync<List<TableDto>>("/api/staff/tables");
        check(tables!.Single(x => x.Id == seated.Id) is { HoaDonId: var openId, SoMon: 3, TongTien: 255000m } && openId == billId,
            "API table map links the table to its open order");

        // 5. Phục vụ món.
        using (var early = await waiter.PostAsJsonAsync($"/api/staff/orders/{billId}/lines/{lines[0].Id}", new UpdateLineRequest("DaPhucVu")))
            check(early.StatusCode == HttpStatusCode.Conflict, "API refuses to serve a dish the kitchen has not finished");
        using (var cashierServe = await cashier.PostAsJsonAsync($"/api/staff/orders/{billId}/lines/{lines[0].Id}", new UpdateLineRequest("DaPhucVu")))
            check(cashierServe.StatusCode == HttpStatusCode.Forbidden, "API lets only waiters/admins update dish service");
        await db.ChiTietHoaDon.Where(x => x.HoaDonId == billId).ExecuteUpdateAsync(x => x.SetProperty(c => c.TrangThai, TrangThaiCheBien.SanSang));
        var detail = await waiter.GetFromJsonAsync<OrderDetailDto>($"/api/staff/orders/{billId}");
        check(detail is { CoTheThanhToan: true, PhaiThu: 255000m } && detail.Lines.All(x => x.CoTheMangRa && x.TrangThaiText == "Sẵn sàng phục vụ"),
            "API order detail shows ready dishes and the amount due");
        foreach (var line in detail!.Lines)
        {
            using var served = await waiter.PostAsJsonAsync($"/api/staff/orders/{billId}/lines/{line.Id}", new UpdateLineRequest("DaPhucVu"));
            check(served.StatusCode == HttpStatusCode.OK, "API waiter serves dish " + line.Id);
        }
        using (var badStatus = await waiter.PostAsJsonAsync($"/api/staff/orders/{billId}/lines/{lines[0].Id}", new UpdateLineRequest("SanSang")))
            check(badStatus.StatusCode == HttpStatusCode.BadRequest, "API does not let staff skip kitchen states");

        // 6. Khách xem đơn của mình; không xem được đơn khác.
        var customerOrders = await customer.GetFromJsonAsync<List<OrderSummaryDto>>("/api/customer/orders");
        check(customerOrders?.Any(x => x.Id == billId && x.TrangThai == "ChuaThanhToan") == true, "API customer sees the open order linked to their booking");
        var customerDetail = await customer.GetFromJsonAsync<OrderDetailDto>($"/api/customer/orders/{billId}");
        check(customerDetail is { CoTheThanhToan: false, CoTheGoiThem: false } && customerDetail.Lines.All(x => !x.CoTheMangRa && x.TrangThaiText == "Đã phục vụ"),
            "API customer order detail is read-only and shows dish progress");
        var stranger = new HoaDon
        {
            MaHoaDon = "HD-MAPI-OTHER", LoaiDonHang = LoaiDonHang.MangDi, TenNguoiNhan = "Người khác", SoDienThoaiNhan = "0909000333",
            NhanVienId = staffId, ThoiDiemLap = now, TrangThai = TrangThaiHoaDon.ChuaThanhToan
        };
        db.HoaDon.Add(stranger); await db.SaveChangesAsync();
        await Expect(customer, $"/api/customer/orders/{stranger.Id}", HttpStatusCode.NotFound, "API customer cannot open another person's order", check);

        // 7. Thanh toán.
        detail = await cashier.GetFromJsonAsync<OrderDetailDto>($"/api/staff/orders/{billId}");
        using (var transferNoRef = await cashier.PostAsJsonAsync($"/api/staff/orders/{billId}/pay",
            new PaymentRequest("ChuyenKhoan", 0, detail!.PhaiThu, null, detail.RowVersion, detail.BookingRowVersion)))
            check(transferNoRef.StatusCode == HttpStatusCode.BadRequest, "API bank transfer requires a transaction reference");
        using (var card = await cashier.PostAsJsonAsync($"/api/staff/orders/{billId}/pay",
            new PaymentRequest("The", 0, detail.PhaiThu, "X", detail.RowVersion, detail.BookingRowVersion)))
            check(card.StatusCode == HttpStatusCode.BadRequest, "API accepts only cash or bank transfer");
        using (var stale = await cashier.PostAsJsonAsync($"/api/staff/orders/{billId}/pay",
            new PaymentRequest("TienMat", 300000m, detail.PhaiThu - 1000m, null, detail.RowVersion, detail.BookingRowVersion)))
            check(stale.StatusCode == HttpStatusCode.Conflict, "API refuses payment when the shown amount no longer matches");
        using (var tooLittle = await cashier.PostAsJsonAsync($"/api/staff/orders/{billId}/pay",
            new PaymentRequest("TienMat", 100000m, detail.PhaiThu, null, detail.RowVersion, detail.BookingRowVersion)))
            check(tooLittle.StatusCode == HttpStatusCode.Conflict, "API refuses cash below the amount due");
        using var paid = await cashier.PostAsJsonAsync($"/api/staff/orders/{billId}/pay",
            new PaymentRequest("TienMat", 300000m, detail.PhaiThu, null, detail.RowVersion, detail.BookingRowVersion));
        var paidBody = await paid.Content.ReadFromJsonAsync<PaymentResultDto>();
        check(paid.StatusCode == HttpStatusCode.OK && paidBody?.TienThoiLai == 45000m, "API cash payment succeeds and returns the change");
        using (var twice = await cashier.PostAsJsonAsync($"/api/staff/orders/{billId}/pay",
            new PaymentRequest("TienMat", 300000m, detail.PhaiThu, null, detail.RowVersion, detail.BookingRowVersion)))
            check(twice.StatusCode == HttpStatusCode.Conflict, "API cannot pay the same invoice twice");
        db.ChangeTracker.Clear();
        var paidBill = await db.HoaDon.SingleAsync(x => x.Id == billId);
        check(paidBill.TrangThai == TrangThaiHoaDon.DaThanhToan && paidBill.PhuongThucThanhToan == PhuongThucThanhToan.TienMat
            && await db.ChiTietThanhToan.AnyAsync(x => x.HoaDonId == billId && x.SoTien == 255000m),
            "API payment writes the invoice and the payment ledger");

        // 8. Bàn sang chờ dọn sau thanh toán; bồi bàn xác nhận dọn xong.
        tables = await waiter.GetFromJsonAsync<List<TableDto>>("/api/staff/tables");
        var dirty = tables!.Single(x => x.Id == seated.Id);
        check(dirty.TrangThai == "CanDon", "API table map shows the table waiting for cleaning after payment");
        using (var cashierClean = await cashier.PostAsJsonAsync($"/api/staff/tables/{seated.Id}/clean", new TableActionRequest(dirty.RowVersion)))
            check(cashierClean.StatusCode == HttpStatusCode.Forbidden, "API lets only waiters/admins clean tables");
        using (var staleClean = await waiter.PostAsJsonAsync($"/api/staff/tables/{seated.Id}/clean", new TableActionRequest(seatedCard!.RowVersion)))
            check(staleClean.StatusCode == HttpStatusCode.Conflict, "API rejects a stale table version");
        using (var cleaned = await waiter.PostAsJsonAsync($"/api/staff/tables/{seated.Id}/clean", new TableActionRequest(dirty.RowVersion)))
            check(cleaned.StatusCode == HttpStatusCode.OK, "API waiter marks the table clean");
        db.ChangeTracker.Clear();
        check(await db.BanAn.Where(x => x.Id == seated.Id).Select(x => x.TrangThai).SingleAsync() == TrangThaiBan.SanSang,
            "Cleaned table is ready for the next party");

        // 9. Khách xem điểm sau thanh toán.
        var summary = await customer.GetFromJsonAsync<CustomerSummaryDto>("/api/customer/summary");
        var points = await customer.GetFromJsonAsync<List<PointEntryDto>>("/api/customer/points");
        check(summary is { DiemHienCo: >= 25 } && points?.Any(x => x.MaHoaDon == paidBill.MaHoaDon && x.SoDiem == 25) == true,
            "API customer sees points earned from the paid order (10.000 đ = 1 điểm)");

        // 10. Tiếp tân xác nhận đặt bàn bằng bàn gợi ý.
        var bookings = await reception.GetFromJsonAsync<List<ReservationDto>>("/api/staff/reservations");
        var pending = bookings?.SingleOrDefault(x => x.Id == request.Id);
        check(pending is { TrangThai: "ChoXacNhan", Ban: null, CoTheNhanBan: false } && pending.GoiYBan.Count > 0
            && pending.GoiYBan.All(x => x.SoCho >= 3), "API lists the pending booking with table suggestions that fit the party");
        using (var tooSmall = await reception.PostAsJsonAsync($"/api/staff/reservations/{request.Id}/assign",
            new AssignTableRequest(small.Id, pending!.RowVersion)))
            check(tooSmall.StatusCode == HttpStatusCode.Conflict, "API refuses a table that is too small");
        using var assigned = await reception.PostAsJsonAsync($"/api/staff/reservations/{request.Id}/assign",
            new AssignTableRequest(pending.GoiYBan[0].Id, pending.RowVersion));
        check(assigned.StatusCode == HttpStatusCode.OK, "API reception confirms the booking with a suggested table");
        db.ChangeTracker.Clear();
        check(await db.DatBan.Where(x => x.Id == request.Id).Select(x => x.TrangThai).SingleAsync() == TrangThaiDatBan.DaXacNhan
            && await db.ChiTietDatBan.AnyAsync(x => x.DatBanId == request.Id && x.BanAnId == pending.GoiYBan[0].Id),
            "Confirmed booking is stored with its table");
    }

    private static async Task<HttpClient> Login(Func<HttpClient> newClient, string email)
    {
        var client = newClient();
        using var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, Password));
        var token = response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<TokenResponse>() : null;
        if (token is not { TokenType: "Bearer" } || string.IsNullOrEmpty(token.AccessToken))
            throw new InvalidOperationException($"FAIL: API login {email}: {response.StatusCode}");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return client;
    }

    private static async Task Expect(HttpClient client, string path, HttpStatusCode expected, string name, Action<bool, string> check)
    {
        using var response = await client.GetAsync(path);
        check(response.StatusCode == expected, $"{name}: {response.StatusCode}");
    }
}
