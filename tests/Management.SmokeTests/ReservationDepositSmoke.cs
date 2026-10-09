using System.Net;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Services;

namespace Management.SmokeTests;

public static class ReservationDepositSmoke
{
    public static async Task Run(RestaurantDbContext db, HttpClient admin, Func<HttpClient> newClient,
        Func<string, string, string> hidden, Action<bool, string> check)
    {
        async Task<string> Get(HttpClient client, string path)
        {
            var response = await client.GetAsync(path);
            check(response.StatusCode == HttpStatusCode.OK, "Payment GET " + path);
            return await response.Content.ReadAsStringAsync();
        }
        async Task Post(HttpClient client, string path, string form, Dictionary<string, string> fields,
            HttpStatusCode expected = HttpStatusCode.OK)
        {
            var body = new Dictionary<string, string>(fields) { ["__RequestVerificationToken"] = hidden(form, "__RequestVerificationToken") };
            using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = new FormUrlEncodedContent(body) };
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            using var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            check(response.StatusCode == expected, $"Payment POST {path}: {response.StatusCode}; {content[..Math.Min(content.Length, 700)]}");
        }
        async Task Login(HttpClient client, string email, string path)
        {
            var form = await Get(client, path);
            using var response = await client.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string>
            { ["Email"] = email, ["Password"] = "Demo@2026!", ["__RequestVerificationToken"] = hidden(form, "__RequestVerificationToken") }));
            check(response.StatusCode == HttpStatusCode.Redirect, "Payment login " + email);
        }
        async Task<DatBan> Reload(int id)
        {
            db.ChangeTracker.Clear();
            return await db.DatBan.Include(x => x.Ban).SingleAsync(x => x.Id == id);
        }
        string Version(DatBan booking) => Convert.ToBase64String(db.Entry(booking).Property<byte[]>("RowVersion").CurrentValue!);
        async Task Expire(int id)
        {
            var options = new DbContextOptionsBuilder<RestaurantDbContext>().UseSqlServer(db.Database.GetConnectionString()).Options;
            await using var context = new RestaurantDbContext(options);
            await new TableService(context, new PreorderService(context, new OrderService(context))).ExpireUnpaidHold(id);
        }

        var area = new KhuVuc { TenKhuVuc = "PAYMENT-AREA", DangSuDung = true, Tang = 7 };
        var table = new BanAn { MaBan = "PAYMENT-TABLE", KhuVuc = area, SoChoNgoi = 4, TrangThai = TrangThaiBan.SanSang };
        var dish = new MonAn { TenMon = "PAYMENT-DISH", DanhMuc = new DanhMuc { TenDanhMuc = "PAYMENT-MENU", DangSuDung = true },
            Loai = LoaiMon.MonLe, DaDuyet = true, TrangThai = TrangThaiMon.DangPhucVu,
            Sizes = { new MonAnSize { TenSize = "L", GiaBan = 125000m, DangSuDung = true } } };
        db.AddRange(table, dish); await db.SaveChangesAsync();
        var areaId = area.Id; var sizeId = dish.Sizes.Single().Id;
        using var customer = newClient(); using var cashier = newClient();
        await Login(customer, "khach.demo@example.test", "/Account/Login");
        await Login(cashier, "thungan.demo@example.test", "/admin");
        var form = await Get(customer, "/DatBan");
        var arrival = TableService.VietnamNow.AddDays(2).ToString("yyyy-MM-ddTHH:mm");
        var requestId = Guid.NewGuid();
        var create = new Dictionary<string, string> { ["RequestId"] = requestId.ToString(), ["HoTen"] = "PAYMENT-CUSTOMER",
            ["SoDienThoai"] = "0901234567", ["SoNguoi"] = "2", ["MaKhuVuc"] = areaId.ToString(), ["ThoiGianDen"] = arrival,
            ["Items[0].MonAnSizeId"] = sizeId.ToString(), ["Items[0].SoLuong"] = "2", ["TienCocYeuCau"] = "1" };
        await Post(customer, "/DatBan", form, create);
        var booking = await db.DatBan.AsNoTracking().SingleAsync(x => x.YeuCauTaoId == requestId);
        var id = booking.Id; booking = await Reload(id);
        check(booking.CocTuDong && booking.TienCocYeuCau == 125000m && booking.TienCocDaNop == 0
            && booking.TrangThai == TrangThaiDatBan.ChoCoc && booking.Ban.Count == 1,
            "Server calculates half the real size price and reserves an available table before payment; forged deposit is ignored");
        check(System.Text.RegularExpressions.Regex.IsMatch(booking.MaDatBan, "^DB-[A-F0-9]{6}$"), "Booking reference is short and memorable");
        check(booking.HanThanhToanCoc is { } deadline && Math.Abs((deadline - booking.ThoiDiemTao).TotalMinutes - 60) < 1,
            "New reservation has a one-hour payment-notification deadline");
        await Expire(id); booking = await Reload(id);
        check(booking.TrangThai == TrangThaiDatBan.ChoCoc, "Automatic expiry does not cancel a valid one-hour hold");
        var paymentPage = await Get(customer, $"/DatBan/ThanhToanCoc/{id}");
        check(paymentPage.Contains("payment-qr-demo.svg") && WebUtility.HtmlDecode(paymentPage).Contains("125.000đ"), "Customer sees correct payable amount and clearly marked demo QR");
        using (var anonymous = newClient())
            check((await anonymous.GetAsync($"/DatBan/ThanhToanCoc/{id}")).StatusCode == HttpStatusCode.Redirect, "Payment details require the owning account");
        await Post(customer, "/DatBan", form, new(create) { ["RequestId"] = Guid.NewGuid().ToString() }, HttpStatusCode.UnprocessableEntity);
        check(await db.DatBan.CountAsync(x => x.KhuVucUuTienId == areaId) == 1, "No available seats means no payable booking is created");
        var notice = new Dictionary<string, string> { ["RowVersion"] = Version(booking), ["SoTien"] = "125000", ["MaGiaoDich"] = "CUSTOMER-REF" };
        await Post(customer, $"/DatBan/BaoChuyenKhoan/{id}", paymentPage, new(notice) { ["SoTien"] = "1" }, HttpStatusCode.Conflict);
        await Post(customer, $"/DatBan/BaoChuyenKhoan/{id}", paymentPage, notice);
        await Post(customer, $"/DatBan/BaoChuyenKhoan/{id}", paymentPage, notice);
        booking = await Reload(id);
        check(booking.ThoiDiemBaoChuyenKhoan != null && booking.TienCocDaNop == 0 && booking.TrangThai == TrangThaiDatBan.ChoCoc
            && !await db.GiaoDichCoc.AnyAsync(x => x.DatBanId == id), "Customer notification and retry never credit money or confirm the booking");
        var queue = WebUtility.HtmlDecode(await Get(cashier, "/DatTruoc"));
        check(queue.Contains(booking.MaDatBan), "Cashier has a searchable queue of reported transfers");
        var internalPage = await Get(cashier, $"/DatTruoc/ChiTiet/{id}");
        await Post(cashier, $"/DatTruoc/TuChoiChuyenKhoan/{id}", internalPage,
            new() { ["rowVersion"] = Version(booking), ["lyDo"] = "Chưa tìm thấy khoản tiền trên ngân hàng." });
        booking = await Reload(id);
        check(booking.ThoiDiemBaoChuyenKhoan == null && booking.TienCocDaNop == 0 && booking.LyDoTuChoiChuyenKhoan != null,
            "Cashier rejection informs the customer without recording money or confirming the booking");
        notice["RowVersion"] = Version(booking);
        await Post(customer, $"/DatBan/BaoChuyenKhoan/{id}", paymentPage, notice);
        booking = await Reload(id);
        booking.HanThanhToanCoc = DateTimeOffset.UtcNow.AddMinutes(-1); await db.SaveChangesAsync();
        await Expire(id); booking = await Reload(id);
        check(booking.TrangThai == TrangThaiDatBan.ChoCoc && booking.ThoiDiemBaoChuyenKhoan != null && !ReservationDepositPolicy.Expired(booking),
            "A timely payment notification keeps its hold pending reconciliation even after the deadline");
        check(await new TableService(db, new PreorderService(db, new OrderService(db))).Cancel(id, Version(booking), "Thử hủy khi chưa đối chiếu chuyển khoản", null) != null,
            "Manual cancellation also protects unresolved customer bank notifications");
        await Post(customer, "/DatBan", form, new(create) { ["RequestId"] = Guid.NewGuid().ToString() }, HttpStatusCode.UnprocessableEntity);
        check(WebUtility.HtmlDecode(await Get(customer, $"/DatBan/ThanhToanCoc/{id}")).Contains("Chỗ được giữ trong lúc thu ngân đối chiếu"),
            "Pending payment page explains the hold remains protected during reconciliation");
        var receipt = new Dictionary<string, string> { ["RowVersion"] = Version(booking), ["RequestId"] = Guid.NewGuid().ToString(),
            ["Loai"] = "Thu", ["SoTien"] = "125000", ["LyDo"] = "Đã đối chiếu ngân hàng" };
        await Post(cashier, $"/DatTruoc/GiaoDichCoc/{id}", internalPage, receipt, HttpStatusCode.BadRequest);
        receipt["DaDoiChieuNganHang"] = "true";
        await Post(cashier, $"/DatTruoc/GiaoDichCoc/{id}", internalPage, receipt);
        await Post(cashier, $"/DatTruoc/GiaoDichCoc/{id}", internalPage, receipt);
        booking = await Reload(id);
        check(booking.TienCocDaNop == 125000m && booking.TrangThai == TrangThaiDatBan.DaXacNhan
            && booking.ThoiDiemBaoChuyenKhoan == null && await db.GiaoDichCoc.CountAsync(x => x.DatBanId == id) == 1,
            "Cashier confirmation needs no bank transaction code; repeated confirmation records one receipt");
        check(await db.GiaoDichCoc.AnyAsync(x => x.DatBanId == id && x.MaThamChieu == null), "No transaction code is invented when the cashier simply verifies receipt");
        check(booking.GioKetThucDuKien == booking.GioDen.AddHours(TableService.MaxDiningHours), "New online reservation holds a three-hour dining interval");
        check(!internalPage.Contains("confirmed-bank-ref"), "Cashier form does not ask for a bank transaction code");
        await Post(cashier, $"/DatTruoc/GiaoDichCoc/{id}", internalPage, new(receipt) { ["RequestId"] = Guid.NewGuid().ToString() }, HttpStatusCode.BadRequest);
        var add = new Dictionary<string, string> { ["RowVersion"] = Version(booking), ["RequestId"] = Guid.NewGuid().ToString(),
            ["Items[0].MonAnSizeId"] = sizeId.ToString(), ["Items[0].SoLuong"] = "3" };
        await Post(customer, $"/DatBan/SavePreorder/{id}", form, add);
        booking = await Reload(id);
        check(booking.TienCocYeuCau == 187500m && booking.TienCocDaNop == 125000m && ReservationDepositPolicy.Remaining(booking) == 62500m,
            "Adding food before seating calculates only the missing deposit without charging the original receipt again");
        await Expire(id); booking = await Reload(id);
        check(booking.TrangThai == TrangThaiDatBan.ChoCoc && booking.TienCocDaNop == 125000m, "Existing actual deposit prevents automatic unpaid cancellation");
        notice["RowVersion"] = Version(booking); notice["SoTien"] = "62500";
        await Post(customer, $"/DatBan/BaoChuyenKhoan/{id}", paymentPage, notice);
        booking = await Reload(id);
        receipt["RowVersion"] = Version(booking); receipt["SoTien"] = "62500"; receipt["RequestId"] = Guid.NewGuid().ToString();
        receipt["DaDoiChieuNganHang"] = "false";
        await Post(cashier, $"/DatTruoc/GiaoDichCoc/{id}", internalPage, receipt, HttpStatusCode.BadRequest);
        receipt["DaDoiChieuNganHang"] = "true";
        receipt["MaThamChieu"] = "BANK-PAYMENT-02";
        await Post(cashier, $"/DatTruoc/GiaoDichCoc/{id}", internalPage, receipt);
        booking = await Reload(id);
        check(booking.TienCocDaNop == 187500m && booking.TrangThai == TrangThaiDatBan.DaXacNhan
            && await db.GiaoDichCoc.CountAsync(x => x.DatBanId == id) == 2, "A verified top-up restores confirmation without duplicate credit; bank reference remains optional");

        var emptyId = Guid.NewGuid();
        var empty = new Dictionary<string, string> { ["RequestId"] = emptyId.ToString(), ["HoTen"] = "PAYMENT-NO-FOOD", ["SoDienThoai"] = "0901234567",
            ["SoNguoi"] = "2", ["MaKhuVuc"] = areaId.ToString(), ["ThoiGianDen"] = TableService.VietnamNow.AddDays(3).ToString("yyyy-MM-ddTHH:mm") };
        await Post(customer, "/DatBan", form, empty);
        var noFood = await db.DatBan.SingleAsync(x => x.YeuCauTaoId == emptyId);
        check(noFood.TienCocYeuCau == 299000m, "No-food reservation has the published per-booking deposit, not a fabricated per-person fee");
        noFood.HanThanhToanCoc = DateTimeOffset.UtcNow.AddMinutes(-1); await db.SaveChangesAsync();
        var noFoodPage = await Get(customer, $"/DatBan/ThanhToanCoc/{noFood.Id}");
        await Post(customer, $"/DatBan/BaoChuyenKhoan/{noFood.Id}", noFoodPage, new()
            { ["RowVersion"] = Version(noFood), ["SoTien"] = "299000" }, HttpStatusCode.Conflict);
        await Expire(noFood.Id); noFood = await Reload(noFood.Id);
        check(noFood.TrangThai == TrangThaiDatBan.DaHuy && noFood.ThoiDiemHuy != null && noFood.LyDoHuy!.Contains("Tự động hủy")
            && noFood.Ban.Count == 1, "Expired unpaid hold is cancelled with history/reason and table links retained");
        await db.Entry(table).ReloadAsync();
        check(table.TrangThai == TrangThaiBan.SanSang, "Cancelling a future hold does not alter physical table readiness");
        var cancelledAt = noFood.ThoiDiemHuy;
        await Expire(noFood.Id); noFood = await Reload(noFood.Id);
        check(noFood.ThoiDiemHuy == cancelledAt, "Repeated expiry cannot rewrite cancellation history");
        var cancelledPage = WebUtility.HtmlDecode(await Get(customer, $"/DatBan/ThanhToanCoc/{noFood.Id}"));
        check(cancelledPage.Contains("Yêu cầu đặt bàn đã hủy") && !cancelledPage.Contains("payment-qr-demo.svg"), "Cancelled request clearly disables deposit payment instructions");
        check(!(await Get(admin, "/QuanLyDatBan")).Contains(noFood.MaDatBan), "Expired unpaid request is removed from the active confirmation queue");
        await Post(customer, "/DatBan", form, new(empty) { ["RequestId"] = Guid.NewGuid().ToString() });
        check(await db.DatBan.CountAsync(x => x.KhuVucUuTienId == areaId) == 3, "Expired unpaid holds release table availability for a new booking");
        var legacy = new DatBan { MaDatBan = "PAYMENT-LEGACY-NO-TABLE", HoTenLienHe = "PAYMENT-LEGACY", KhachHangId = booking.KhachHangId,
            LaKhachTrucTiep = true, YeuCauCoc = true, TienCocYeuCau = 50000m, TrangThai = TrangThaiDatBan.ChoCoc,
            ThoiDiemTao = DateTimeOffset.UtcNow, GioDen = DateTimeOffset.UtcNow.AddDays(4), GioKetThucDuKien = DateTimeOffset.UtcNow.AddDays(4).AddHours(2), SoNguoiLon = 2 };
        db.DatBan.Add(legacy); await db.SaveChangesAsync();
        legacy.HanThanhToanCoc = DateTimeOffset.UtcNow.AddMinutes(-1); await db.SaveChangesAsync();
        await Expire(legacy.Id); legacy = await Reload(legacy.Id);
        check(legacy.TrangThai == TrangThaiDatBan.ChoCoc, "Legacy manually agreed deposit bookings are not automatically cancelled");
        var legacyPage = await Get(customer, $"/DatBan/ThanhToanCoc/{legacy.Id}");
        check(!legacyPage.Contains("payment-qr-demo.svg"), "Legacy quoted bookings without assigned tables do not show payable QR instructions");
        await Post(customer, $"/DatBan/BaoChuyenKhoan/{legacy.Id}", form, new()
            { ["RowVersion"] = Version(legacy), ["SoTien"] = "50000" }, HttpStatusCode.Conflict);
        legacy.DieuKienCocDaThoaThuan = "Thỏa thuận cọc cũ; hủy được đối chiếu tiền trước khi giải quyết.";
        legacy.Ban.Add(new ChiTietDatBan { BanAnId = table.Id }); await db.SaveChangesAsync();
        await Post(customer, $"/DatBan/BaoChuyenKhoan/{legacy.Id}", form, new()
            { ["RowVersion"] = Version(legacy), ["SoTien"] = "50000" });
        legacy = await Reload(legacy.Id);
        await Post(customer, $"/DatBan/YeuCauHuy/{legacy.Id}", form, new()
            { ["RowVersion"] = Version(legacy), ["LyDo"] = "Khách đổi kế hoạch sau khi báo chuyển khoản." });
        legacy = await Reload(legacy.Id);
        await Post(cashier, $"/DatTruoc/GiaoDichCoc/{legacy.Id}", internalPage, new()
            { ["RowVersion"] = Version(legacy), ["RequestId"] = Guid.NewGuid().ToString(), ["Loai"] = "Thu", ["SoTien"] = "50000",
                ["LyDo"] = "Thử dùng lại mã tham chiếu đã có.", ["MaThamChieu"] = "BANK-PAYMENT-02" }, HttpStatusCode.BadRequest);
        await Post(cashier, $"/DatTruoc/GiaoDichCoc/{legacy.Id}", internalPage, new()
            { ["RowVersion"] = Version(legacy), ["RequestId"] = Guid.NewGuid().ToString(), ["Loai"] = "Thu", ["SoTien"] = "50000",
                ["LyDo"] = "Đã đối chiếu tiền thực nhận trong lúc khách chờ hủy.", ["MaThamChieu"] = "BANK-CANCELLATION-PENDING" });
        legacy = await Reload(legacy.Id);
        check(legacy.TienCocDaNop == 50000m && legacy.YeuCauHuy != null && legacy.TrangThai != TrangThaiDatBan.DaXacNhan,
            "Actual money received during a pending cancellation is recorded without confirming or reopening the reservation");
    }
}
