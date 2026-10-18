using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Services;

public static class BookingSlotSmoke
{
    public static async Task Run(RestaurantDbContext db, HttpClient admin, Func<HttpClient> newClient,
        Func<string, string, string> hidden, Action<bool, string> check)
    {
        async Task<string> Get(HttpClient client, string path)
        {
            using var response = await client.GetAsync(path);
            check(response.StatusCode == HttpStatusCode.OK, "W9 GET " + path);
            return await response.Content.ReadAsStringAsync();
        }
        async Task<HttpStatusCode> Post(HttpClient client, string path, string form, Dictionary<string, string> fields, bool ajax = false)
        {
            fields["__RequestVerificationToken"] = hidden(form, "__RequestVerificationToken");
            using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = new FormUrlEncodedContent(fields) };
            if (ajax) request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            using var response = await client.SendAsync(request);
            return response.StatusCode;
        }
        string Version(DatBan booking) => Convert.ToBase64String(db.Entry(booking).Property<byte[]>("RowVersion").CurrentValue!);
        string Card(string html, int id) => WebUtility.HtmlDecode(Regex.Match(html,
            "<article[^>]*data-table-id=\"" + id + "\"[\\s\\S]*?</article>").Value);
        var day = DateOnly.FromDateTime(TableService.VietnamNow).AddDays(12);
        var lunch = TableService.VietnamTime(day.ToDateTime(new(11, 0)));
        check(ReservationSchedule.Validate(day.ToDateTime(new(11, 0)), TableService.VietnamNow) is null, "Fixed lunch start accepted");
        check(ReservationSchedule.Validate(day.ToDateTime(new(11, 30)), TableService.VietnamNow) is not null, "Arbitrary arrival rejected");
        check(ReservationSchedule.Validate(day.ToDateTime(new(11, 0, 1)), TableService.VietnamNow) is not null, "Seconds cannot bypass fixed slot");
        check(ReservationSchedule.NextArrival(day.ToDateTime(new(23, 0))) == day.AddDays(1).ToDateTime(new(11, 0)), "Next slot crosses midnight in Vietnam without UTC date shift");
        check(ReservationSchedule.EndOf(day.ToDateTime(new(17, 0))) == TableService.VietnamTime(day.ToDateTime(new(19, 0)))
            && TableService.MaxDiningHours == 3, "Two-hour scheduled slot does not change the three-hour serving watchdog");

        var area = new KhuVuc { TenKhuVuc = "W9-CHECK-AREA", Tang = 8 };
        var vip = new KhuVuc { TenKhuVuc = "W9-CHECK-VIP", Tang = 8, LaPhongVip = true };
        var a = new BanAn { MaBan = "W9-CHECK-A", KhuVuc = area, SoChoNgoi = 4, TrangThai = TrangThaiBan.DangPhucVu };
        var b = new BanAn { MaBan = "W9-CHECK-B", KhuVuc = area, SoChoNgoi = 4, TrangThai = TrangThaiBan.SanSang };
        var v = new BanAn { MaBan = "W9-CHECK-V", KhuVuc = vip, SoChoNgoi = 8, TrangThai = TrangThaiBan.SanSang };
        db.BanAn.AddRange(a, b, v);
        await db.SaveChangesAsync();
        DatBan Booking(string code, DateTimeOffset start, TrangThaiDatBan state) => new()
        {
            MaDatBan = code, HoTenLienHe = code, SoDienThoaiLienHe = "0901234567", SoNguoiLon = 4,
            GioDen = start, GioKetThucDuKien = start.AddHours(2), TrangThai = state, LaKhachTrucTiep = true,
            KhuVucUuTienId = area.Id, ThoiDiemTao = DateTimeOffset.UtcNow
        };
        var bookedLunch = Booking("W9-LUNCH", lunch, TrangThaiDatBan.DaXacNhan);
        var bookedDinner = Booking("W9-DINNER", TableService.VietnamTime(day.ToDateTime(new(19, 30))), TrangThaiDatBan.DaXacNhan);
        var pending = Booking("W9-PENDING", lunch, TrangThaiDatBan.ChoXacNhan);
        bookedLunch.Ban.Add(new() { BanAnId = a.Id }); bookedDinner.Ban.Add(new() { BanAnId = b.Id });
        db.DatBan.AddRange(bookedLunch, bookedDinner, pending); await db.SaveChangesAsync();
        var prefix = $"/SoDoBan?ngay={day:yyyy-MM-dd}&khuVucId={area.Id}";
        var lunchHtml = await Get(admin, prefix + "&ca=trua");
        var dinnerHtml = await Get(admin, prefix + "&ca=toi");
        check(Card(lunchHtml, a.Id).Contains("Đã đặt trong ca") && Card(lunchHtml, b.Id).Contains("Trống trong ca"), "Lunch board reflects only lunch reservations");
        check(Card(dinnerHtml, a.Id).Contains("Trống trong ca") && Card(dinnerHtml, b.Id).Contains("Đã đặt trong ca"), "Current physical occupancy does not contaminate future dinner availability");
        check(!dinnerHtml.Contains("action=\"/SoDoBan/TrangThai\""), "Forecast board cannot finish or clean a current serving session");
        check(lunchHtml.Contains("id=\"table-blocked-modal\"") && Card(dinnerHtml, b.Id).Contains("data-table-blocked="), "A booked table exposes the blocked popup reason while assigning");
        var dialog = await Get(admin, $"/SoDoBan?datBanId={pending.Id}&dialog=true&khuVucId={area.Id}");
        check(dialog.Contains("assignment-filters") && dialog.Contains("data-ajax-form=\"true\"") && !dialog.Contains("<html"), "Assignment dialog is a reusable authenticated partial, not a new page");
        check(Card(dialog, a.Id).Contains("disabled=\"disabled\"") && Card(dialog, a.Id).Contains("Đã có lịch") && Card(dialog, a.Id).Contains("data-table-blocked="), "Conflicting table disabled with a visible reason and blocked popup");
        check(Card(dialog, b.Id).Contains("data-suggested=\"true\""), "Only a free fitting table is suggested");
        var originalVersion = Version(pending);
        check(await Post(admin, "/QuanLyDatBan/XacNhan", dialog, new() { ["datBanId"] = pending.Id.ToString(), ["banAnIds"] = b.Id.ToString(), ["rowVersion"] = originalVersion }, true) == HttpStatusCode.OK,
            "AJAX assignment succeeds without redirect");
        await db.Entry(pending).ReloadAsync();
        check(pending.TrangThai == TrangThaiDatBan.DaXacNhan && await db.ChiTietDatBan.AnyAsync(x => x.DatBanId == pending.Id && x.BanAnId == b.Id), "AJAX path persists the same shared assignment lifecycle");
        check(await Post(admin, "/QuanLyDatBan/XacNhan", dialog, new() { ["datBanId"] = pending.Id.ToString(), ["banAnIds"] = b.Id.ToString(), ["rowVersion"] = originalVersion }, true) == HttpStatusCode.Conflict,
            "Stale dialog cannot overwrite another update");

        using var customer = newClient();
        var login = await Get(customer, "/Account/Login");
        check(await Post(customer, "/Account/Login", login, new() { ["Email"] = "khach.demo@example.test", ["Password"] = "Demo@2026!" }) == HttpStatusCode.Redirect, "W9 customer login");
        var forecast = await Get(customer, $"/DatBan/KhungGio?ngay={day:yyyy-MM-dd}&soNguoi=4&maKhuVuc={area.Id}");
        using (var json = JsonDocument.Parse(forecast))
        {
            var slots = json.RootElement.GetProperty("slots").EnumerateArray().ToList();
            check(slots.Count == 3 && !slots.Single(x => x.GetProperty("id").GetString() == "trua").GetProperty("available").GetBoolean()
                && slots.Single(x => x.GetProperty("id").GetString() == "toi").GetProperty("available").GetBoolean(), "Customer slot availability agrees with the floor board and held tables");
        }
        var form = await Get(customer, "/DatBan");
        check(form.Contains("name=\"NgayDat\"") && form.Contains("<select") && !form.Contains("time-btn"), "Customer form uses day and three slots, not free arrival/pills");
        var requestId = hidden(form[form.IndexOf("id=\"formDatBan\"", StringComparison.Ordinal)..], "RequestId");
        var before = await db.DatBan.CountAsync();
        check(await Post(customer, "/DatBan", form, new() { ["HoTen"] = "W9-TAMPER", ["SoDienThoai"] = "0901234567", ["SoNguoi"] = "2",
            ["ThoiGianDen"] = day.ToDateTime(new(11, 30)).ToString("yyyy-MM-ddTHH:mm"), ["RequestId"] = requestId }, true) == HttpStatusCode.UnprocessableEntity
            && await db.DatBan.CountAsync() == before, "Forged arbitrary arrival is rejected by server before holding tables or charging deposit");
        using (var denied = await customer.GetAsync($"/SoDoBan?datBanId={pending.Id}&dialog=true"))
            check(denied.StatusCode == HttpStatusCode.Redirect, "Customer cannot read internal assignment dialog/contact data");
        using (var invalid = await admin.GetAsync("/QuanLyDatBan?filterNgay=not-a-date")) check(invalid.StatusCode == HttpStatusCode.BadRequest, "Invalid queue date is not silently ignored");
        var oldPending = Booking("W9-OLD-PENDING", lunch.AddDays(-15), TrangThaiDatBan.ChoXacNhan);
        db.DatBan.Add(oldPending); await db.SaveChangesAsync();
        var defaultQueue = await Get(admin, "/QuanLyDatBan");
        var pastQueue = await Get(admin, $"/QuanLyDatBan?filterNgay={DateOnly.FromDateTime(oldPending.GioDen.ToOffset(TimeSpan.FromHours(7)).DateTime):yyyy-MM-dd}");
        check(!defaultQueue.Contains(oldPending.MaDatBan) && pastQueue.Contains(oldPending.MaDatBan), "Old unpaid pending requests do not clutter the default queue but remain accessible by explicit date");
        check(defaultQueue.Contains("id=\"table-blocked-modal\""), "Queue page ships the blocked-table popup for assignment from the list");
        var cancelled = Booking("W9-CANCELLED", lunch.AddDays(1), TrangThaiDatBan.DaHuy);
        cancelled.ThoiDiemHuy = DateTimeOffset.UtcNow;
        db.DatBan.Add(cancelled); await db.SaveChangesAsync();
        var daHuyQueue = await Get(admin, "/QuanLyDatBan?tab=DaHuy");
        var hoanTatQueue = await Get(admin, "/QuanLyDatBan?tab=HoanTat");
        check(daHuyQueue.Contains(cancelled.MaDatBan) && !hoanTatQueue.Contains(cancelled.MaDatBan), "Cancelled reservations live in their own tab instead of mixing with completed history");
        check(!(await Get(admin, "/QuanLyDatBan?tab=HoanTatHuy")).Contains(cancelled.MaDatBan), "Legacy HoanTatHuy link still resolves without leaking cancellations");

        var legacy = Booking("W9-LEGACY", TableService.VietnamTime(day.ToDateTime(new(20, 15))), TrangThaiDatBan.DaXacNhan);
        legacy.Ban.Add(new() { BanAnId = a.Id }); db.DatBan.Add(legacy); await db.SaveChangesAsync();
        var legacyStart = legacy.GioDen; var legacyEnd = legacy.GioKetThucDuKien;
        var legacyHtml = await Get(admin, prefix + "&ca=toi");
        check(Card(legacyHtml, a.Id).Contains("Đã đặt trong ca"), "Off-slot legacy bookings still block overlapping new slots");
        await db.Entry(legacy).ReloadAsync();
        check(legacy.GioDen == legacyStart && legacy.GioKetThucDuKien == legacyEnd, "Reading new schedule never rewrites old booking intervals");
        var legacyLong = Booking("W9-LEGACY-LONG", lunch.AddDays(5).AddHours(6), TrangThaiDatBan.ChoXacNhan);
        legacyLong.GioKetThucDuKien = legacyLong.GioDen.AddHours(3); db.DatBan.Add(legacyLong); await db.SaveChangesAsync();
        var legacyLongHtml = WebUtility.HtmlDecode(await Get(admin, $"/SoDoBan?datBanId={legacyLong.Id}"));
        check(legacyLongHtml.Contains("Khoảng giờ lịch cũ") && legacyLongHtml.Contains("20:00"), "Legacy 17–20 reservation is not mislabeled as the new 17–19 slot");

        var vipRequest = Booking("W9-NO-VIP", lunch.AddDays(1), TrangThaiDatBan.ChoXacNhan);
        db.DatBan.Add(vipRequest); await db.SaveChangesAsync();
        var vipDialog = await Get(admin, $"/SoDoBan?datBanId={vipRequest.Id}&dialog=true");
        check(await Post(admin, "/QuanLyDatBan/XacNhan", vipDialog, new() { ["datBanId"] = vipRequest.Id.ToString(), ["banAnIds"] = v.Id.ToString(), ["rowVersion"] = Version(vipRequest) }, true) == HttpStatusCode.Conflict,
            "Direct POST cannot assign ordinary guest to an unrequested VIP room");

        var raceArea = new KhuVuc { TenKhuVuc = "W9-RACE-AREA" };
        var raceTable = new BanAn { MaBan = "W9-RACE-TABLE", KhuVuc = raceArea, SoChoNgoi = 4, TrangThai = TrangThaiBan.SanSang };
        db.BanAn.Add(raceTable); await db.SaveChangesAsync();
        var r1 = Booking("W9-RACE-1", lunch.AddDays(2), TrangThaiDatBan.ChoXacNhan);
        var r2 = Booking("W9-RACE-2", lunch.AddDays(2), TrangThaiDatBan.ChoXacNhan);
        r1.KhuVucUuTienId = r2.KhuVucUuTienId = raceArea.Id;
        db.DatBan.AddRange(r1, r2); await db.SaveChangesAsync();
        var raceDialog = await Get(admin, $"/SoDoBan?datBanId={r1.Id}&dialog=true");
        var results = await Task.WhenAll(new[] { r1, r2 }.Select(booking => Post(admin, "/QuanLyDatBan/XacNhan", raceDialog,
            new() { ["datBanId"] = booking.Id.ToString(), ["banAnIds"] = raceTable.Id.ToString(), ["rowVersion"] = Version(booking) }, true)));
        check(results.Count(x => x == HttpStatusCode.OK) == 1 && results.Count(x => x == HttpStatusCode.Conflict) == 1, "Two staff submissions race for one slot: exactly one reservation succeeds");
        check(await db.ChiTietDatBan.CountAsync(x => x.BanAnId == raceTable.Id) == 1, "Concurrent assignment leaves one actual table link");
        var depositPending = Booking("W9-DEPOSIT-WAIT", lunch.AddDays(3), TrangThaiDatBan.ChoCoc);
        depositPending.KhuVucUuTienId = raceArea.Id; depositPending.YeuCauCoc = true; depositPending.CocTuDong = true;
        depositPending.TienCocYeuCau = 299000; depositPending.HanThanhToanCoc = DateTimeOffset.UtcNow.AddHours(1);
        db.DatBan.Add(depositPending); await db.SaveChangesAsync();
        var depositDialog = await Get(admin, $"/SoDoBan?datBanId={depositPending.Id}&dialog=true");
        check(await Post(admin, "/QuanLyDatBan/XacNhan", depositDialog, new() { ["datBanId"] = depositPending.Id.ToString(),
            ["banAnIds"] = raceTable.Id.ToString(), ["rowVersion"] = Version(depositPending) }, true) == HttpStatusCode.OK, "Table can be assigned while awaiting verified deposit");
        await db.Entry(depositPending).ReloadAsync();
        check(depositPending.TrangThai == TrangThaiDatBan.ChoCoc && depositPending.TienCocDaNop == 0,
            "Assigning a table never fakes deposit receipt or confirms an unpaid reservation");
        var staffForm = await Get(admin, "/QuanLyDatBan/Create");
        var staffFields = new Dictionary<string, string> { ["HoTen"] = "W9-STAFF-SLOT", ["SoDienThoai"] = "0901234567", ["SoNguoi"] = "4",
            ["KhuVucId"] = area.Id.ToString(), ["GioDen"] = day.ToDateTime(new(16, 30)).ToString("yyyy-MM-ddTHH:mm"), ["SoPhut"] = "180" };
        await Post(admin, "/QuanLyDatBan/Create", staffForm, staffFields);
        check(!await db.DatBan.AnyAsync(x => x.HoTenLienHe == "W9-STAFF-SLOT"), "Staff cannot bypass slots by posting a future walk-in time");
        var shortWalkIn = new Dictionary<string, string>(staffFields) { ["HoTen"] = "W9-STAFF-SHORT", ["SoPhut"] = "30",
            ["GioDen"] = TableService.VietnamNow.AddMinutes(5).ToString("yyyy-MM-ddTHH:mm") };
        shortWalkIn.Remove("DatTheoCa");
        check(await Post(admin, "/QuanLyDatBan/Create", staffForm, shortWalkIn) != HttpStatusCode.Redirect
            && !await db.DatBan.AnyAsync(x => x.HoTenLienHe == "W9-STAFF-SHORT"), "Walk-in shorter than the 60-minute minimum meal time is rejected before saving");
        staffFields["GioDen"] = day.ToDateTime(new(17, 0)).ToString("yyyy-MM-ddTHH:mm"); staffFields["DatTheoCa"] = "true";
        check(await Post(admin, "/QuanLyDatBan/Create", staffForm, staffFields) == HttpStatusCode.Redirect, "Staff can create a fixed-slot phone reservation");
        var staffBooking = await db.DatBan.SingleAsync(x => x.HoTenLienHe == "W9-STAFF-SLOT");
        check(staffBooking.GioKetThucDuKien - staffBooking.GioDen == TimeSpan.FromHours(2), "Posted walk-in duration cannot silently extend a scheduled slot");
        var pendingList = await Get(admin, "/QuanLyDatBan?xepBanId=" + staffBooking.Id);
        check(pendingList.Contains($"data-assign-booking=\"{staffBooking.Id}\""), "New staff request returns to the queue that hosts its assignment dialog");
        var service = new TableService(db, new PreorderService(db, new OrderService(db)));
        await Week9DemoSetup.InitializeAsync(db, service);
        var demoCount = await db.DatBan.CountAsync(x => x.MaDatBan.StartsWith("W9-") && x.YeuCau == "Khách dùng bữa theo ca tại nhà hàng.");
        await Week9DemoSetup.InitializeAsync(db, service);
        check(demoCount == 3 && await db.DatBan.CountAsync(x => x.MaDatBan.StartsWith("W9-") && x.YeuCau == "Khách dùng bữa theo ca tại nhà hàng.") == 3,
            "Explicit week-nine demo creates lunch, dinner and pending requests idempotently without resetting existing bookings");

        var turnoverDay = day.AddDays(20);
        var turnoverArea = new KhuVuc { TenKhuVuc = "W9-TURNOVER-AREA" };
        var afternoonTable = new BanAn { MaBan = "W9-TURNOVER-A", KhuVuc = turnoverArea, SoChoNgoi = 4, TrangThai = TrangThaiBan.SanSang };
        var otherTable = new BanAn { MaBan = "W9-TURNOVER-B", KhuVuc = turnoverArea, SoChoNgoi = 4, TrangThai = TrangThaiBan.SanSang };
        db.BanAn.AddRange(afternoonTable, otherTable); await db.SaveChangesAsync();
        var afternoonStart = TableService.VietnamTime(turnoverDay.ToDateTime(new(17, 0)));
        var eveningStart = TableService.VietnamTime(turnoverDay.ToDateTime(new(19, 30)));
        var afternoonBooking = Booking("W9-TURNOVER-AFTERNOON", afternoonStart, TrangThaiDatBan.DaXacNhan);
        afternoonBooking.KhuVucUuTienId = turnoverArea.Id;
        afternoonBooking.Ban.Add(new() { BanAnId = afternoonTable.Id });
        db.DatBan.Add(afternoonBooking); await db.SaveChangesAsync();
        check(await service.OverlappingBookings(eveningStart, eveningStart.AddHours(3)).AnyAsync(x => x.BanAnId == afternoonTable.Id),
            "A 17:00 reservation keeps its table unavailable for the 19:30 slot until the serving allowance ends");
        check(await service.HasCapacity(eveningStart, eveningStart.AddHours(2), 4, turnoverArea.Id, false),
            "The evening slot remains open when another suitable table is available");
        var eveningBooking = Booking("W9-TURNOVER-EVENING", eveningStart, TrangThaiDatBan.DaXacNhan);
        eveningBooking.KhuVucUuTienId = turnoverArea.Id;
        eveningBooking.Ban.Add(new() { BanAnId = otherTable.Id });
        db.DatBan.Add(eveningBooking); await db.SaveChangesAsync();
        check(!await service.HasCapacity(eveningStart, eveningStart.AddHours(2), 4, turnoverArea.Id, false),
            "The evening slot closes when the only other table is also reserved");
        var turnoverRequest = Booking("W9-TURNOVER-REQUEST", eveningStart, TrangThaiDatBan.ChoXacNhan);
        turnoverRequest.KhuVucUuTienId = turnoverArea.Id;
        db.DatBan.Add(turnoverRequest); await db.SaveChangesAsync();
        var turnoverDialog = await Get(admin, $"/SoDoBan?datBanId={turnoverRequest.Id}&dialog=true&khuVucId={turnoverArea.Id}");
        check(Card(turnoverDialog, afternoonTable.Id).Contains("disabled=\"disabled\"")
            && Card(turnoverDialog, afternoonTable.Id).Contains("20:00"),
            "The assignment dialog explains why the afternoon table cannot take the 19:30 reservation");
        check(await Post(admin, "/QuanLyDatBan/XacNhan", turnoverDialog, new() { ["datBanId"] = turnoverRequest.Id.ToString(),
            ["banAnIds"] = afternoonTable.Id.ToString(), ["rowVersion"] = Version(turnoverRequest) }, true) == HttpStatusCode.Conflict,
            "A direct staff POST cannot bypass the afternoon serving allowance");
        var todayEvening = TableService.VietnamTime(DateOnly.FromDateTime(TableService.VietnamNow).ToDateTime(new(19, 30)));
        check(!TableService.ReadyForSchedule(a, todayEvening), "An occupied table is unavailable for another slot today");
        a.TrangThai = TrangThaiBan.CanDon;
        check(!TableService.ReadyForSchedule(a, todayEvening), "A finished but uncleaned table remains unavailable today");
        a.TrangThai = TrangThaiBan.SanSang;
        check(TableService.ReadyForSchedule(a, todayEvening), "Cleaning reopens a table for a later slot today");
        a.TrangThai = TrangThaiBan.DangPhucVu;
    }
}
