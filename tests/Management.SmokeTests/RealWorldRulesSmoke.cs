using System.Net;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Services;

namespace Management.SmokeTests;

// Quy tắc vận hành sát thực tế: giữ bàn theo giờ, khách đến sớm/không đến, cọc tối thiểu, trả kho khi hủy món.
public static class RealWorldRulesSmoke
{
    public static async Task Run(RestaurantDbContext db, HttpClient admin, Func<HttpClient> newClient,
        Func<string, string, string> hidden, Action<bool, string> check)
    {
        var inventory = new InventoryService(db);
        var orders = new OrderService(db, inventory);
        var preorders = new PreorderService(db, orders, inventory);
        var service = new TableService(db, preorders);
        var staffId = await db.NhanVien.Where(x => x.DangLamViec).Select(x => x.Id).FirstAsync();
        var size = await db.MonAnSize.Include(x => x.MonAn).FirstAsync(x => x.DangSuDung && x.GiaBan > 0);
        var now = DateTimeOffset.UtcNow;
        string Version(object entity) => Convert.ToBase64String((byte[])db.Entry(entity).Property("RowVersion").CurrentValue!);

        var area = new KhuVuc { TenKhuVuc = "RULES-AREA", DangSuDung = true, Tang = 8, TienCocGiuCho = 200000m };
        BanAn Table(string code) => new() { MaBan = code, KhuVuc = area, SoChoNgoi = 4, TrangThai = TrangThaiBan.SanSang };
        var tables = Enumerable.Range(1, 8).Select(i => Table("RULES-" + i)).ToList();
        db.BanAn.AddRange(tables);
        await db.SaveChangesAsync();

        DatBan Booking(string code, BanAn? table, TrangThaiDatBan state, DateTimeOffset arrival, DateTimeOffset? end = null)
        {
            var booking = new DatBan
            {
                MaDatBan = code, HoTenLienHe = code, SoDienThoaiLienHe = "0901234567", LaKhachTrucTiep = true, SoNguoiLon = 2,
                ThoiDiemTao = now.AddHours(-2), GioDen = arrival, GioKetThucDuKien = end ?? arrival.AddHours(2), TrangThai = state,
                KhuVucUuTienId = area.Id
            };
            if (table is not null) booking.Ban.Add(new ChiTietDatBan { BanAnId = table.Id });
            db.DatBan.Add(booking);
            return booking;
        }
        HoaDon Bill(DatBan? booking, string code, TrangThaiCheBien state, int? preorderId = null) => new()
        {
            MaHoaDon = "HD-" + code, DatBan = booking, NhanVienId = staffId, ThoiDiemLap = now, TrangThai = TrangThaiHoaDon.ChuaThanhToan,
            TongTienHang = size.GiaBan,
            ChiTiet = { new ChiTietHoaDon { MonAnId = size.MonAnId, MonAnSizeId = size.Id, TenMonLucBan = size.MonAn.TenMon, TenSizeLucBan = size.TenSize,
                SoLuong = 1, DonGia = size.GiaBan, TrangThai = state, MonDatTruocId = preorderId } }
        };

        // Tiền cọc không thấp hơn mức giữ chỗ.
        check(ReservationDepositPolicy.Calculate(20000m, 500000m) == 500000m, "A small drink cannot lower the VIP holding deposit");
        check(ReservationDepositPolicy.Calculate(2000000m, 500000m) == 1000000m, "A large preorder still pays half of the food total");
        check(ReservationDepositPolicy.Calculate(0m, 200000m) == 200000m, "A table-only booking pays the area deposit");
        var nearArrival = Booking("RULES-HOLD", null, TrangThaiDatBan.ChoCoc, now.AddMinutes(40));
        ReservationDepositPolicy.Apply(nearArrival, 0m, 200000m);
        check(nearArrival.HanThanhToanCoc == nearArrival.GioDen, "The payment deadline never runs past the arrival time");
        db.DatBan.Remove(nearArrival);

        // Bàn đang có khách chỉ chặn lượt đến sát giờ hiện tại.
        tables[0].TrangThai = TrangThaiBan.DangPhucVu;
        await db.SaveChangesAsync();
        check((await service.PhysicalBusyTables(now.AddMinutes(10))).Contains(tables[0].Id), "An occupied table cannot take a party arriving now");
        check(!(await service.PhysicalBusyTables(now.AddHours(2))).Contains(tables[0].Id), "An occupied table can still be booked for later today");
        tables[0].TrangThai = TrangThaiBan.SanSang;

        // Khách đến sớm và lịch còn thiếu cọc vẫn được nhận bàn.
        var today = TableService.VietnamTime(TableService.VietnamNow.Date);
        var lastToday = today.AddDays(1).AddMinutes(-1);
        var soon = now.AddHours(1) < lastToday ? now.AddHours(1) : lastToday;
        var early = Booking("RULES-EARLY", tables[1], TrangThaiDatBan.DaXacNhan, soon);
        var unpaid = Booking("RULES-UNPAID", tables[2], TrangThaiDatBan.ChoCoc, now);
        unpaid.YeuCauCoc = true; unpaid.TienCocYeuCau = 200000m; unpaid.CocTuDong = true; unpaid.HanThanhToanCoc = now.AddMinutes(30);
        var partial = Booking("RULES-PARTIAL", tables[3], TrangThaiDatBan.ChoCoc, now);
        partial.YeuCauCoc = true; partial.TienCocYeuCau = 200000m; partial.TienCocDaNop = 50000m; partial.CocTuDong = true; partial.HanThanhToanCoc = now.AddMinutes(30);
        var reported = Booking("RULES-REPORTED", tables[4], TrangThaiDatBan.ChoCoc, now.AddMinutes(-30));
        reported.YeuCauCoc = true; reported.TienCocYeuCau = 200000m; reported.CocTuDong = true; reported.HanThanhToanCoc = now.AddMinutes(30); reported.ThoiDiemBaoChuyenKhoan = now;
        var preorder = Booking("RULES-PREORDER", tables[5], TrangThaiDatBan.DaXacNhan, now);
        preorder.MonDatTruoc.Add(new MonDatTruoc { MonAnId = size.MonAnId, MonAnSizeId = size.Id, TenMonLucDat = size.MonAn.TenMon, TenSizeLucDat = size.TenSize, SoLuong = 1, DonGiaThoaThuan = size.GiaBan + 1000m });
        await db.SaveChangesAsync();

        check(await service.CheckIn(early.Id, Version(early), staffId) is null, "A party that arrives early is seated when the table is free");
        check(await service.CheckIn(unpaid.Id, Version(unpaid), staffId) is null, "A party that arrives before paying the deposit can be seated");
        await db.Entry(unpaid).ReloadAsync();
        check(unpaid.TrangThai == TrangThaiDatBan.DaNhanBan && !unpaid.YeuCauCoc && unpaid.TienCocYeuCau == 0m, "Seating waives the unpaid deposit");
        check(await service.CheckIn(partial.Id, Version(partial), staffId) is null, "A party with a partial deposit can be seated");
        await db.Entry(partial).ReloadAsync();
        check(partial.TienCocYeuCau == 50000m && partial.TienCocDaNop == 50000m, "Seating keeps the deposit already received");
        check(await service.CheckIn(reported.Id, Version(reported), staffId) is not null, "A reported transfer must be reconciled before seating");
        check(await service.MarkNoShow(reported.Id, Version(reported), staffId) is not null, "A reported transfer cannot be closed as a no-show");
        check(await service.CheckIn(preorder.Id, Version(preorder), staffId) is null, "A party with preordered food is seated");
        check(await db.ChiTietHoaDon.AnyAsync(x => x.HoaDon.DatBanId == preorder.Id && x.TrangThai == TrangThaiCheBien.ChoCheBien),
            "Preorders released at seating wait for the kitchen to accept them");
        check(await db.ChiTietHoaDon.AnyAsync(x => x.HoaDon.DatBanId == preorder.Id && x.DonGia == size.GiaBan + 1000m && x.ThoiDiemGoi > now.AddMinutes(-2)),
            "The kitchen line keeps the agreed price and the actual seating time");
        await db.Entry(preorder).ReloadAsync();
        preorder.GioDen = preorder.GioDen.AddHours(-3); preorder.GioKetThucDuKien = preorder.GioDen.AddHours(2);
        check(PreorderService.CanEdit(preorder), "A seated party can still add food after the two-hour hold");
        db.Entry(preorder).State = EntityState.Unchanged;
        await db.Entry(preorder).ReloadAsync();

        // Không đến.
        var tooSoon = Booking("RULES-TOO-SOON", tables[6], TrangThaiDatBan.DaXacNhan, now.AddMinutes(-5));
        var noShow = Booking("RULES-NOSHOW", tables[7], TrangThaiDatBan.ChoCoc, now.AddMinutes(-20));
        noShow.YeuCauCoc = true; noShow.TienCocYeuCau = 200000m; noShow.TienCocDaNop = 200000m;
        db.HoaDon.Add(Bill(noShow, "RULES-NOSHOW", TrangThaiCheBien.ChoCheBien));
        var missed = Booking("RULES-MISSED", null, TrangThaiDatBan.DaXacNhan, now.AddHours(-3), now.AddHours(-1));
        await db.SaveChangesAsync();
        check(await service.MarkNoShow(tooSoon.Id, Version(tooSoon), staffId) is not null, "A no-show waits for the grace period after arrival");
        check(await service.MarkNoShow(noShow.Id, Version(noShow), staffId) is null, "Reception can mark a late party as a no-show");
        await db.Entry(noShow).ReloadAsync();
        check(noShow.TrangThai == TrangThaiDatBan.KhongDen && noShow.TienCocDaNop == 200000m, "A no-show keeps the deposit for later handling");
        check(!await service.OverlappingBookings(now, now.AddHours(1)).AnyAsync(x => x.DatBanId == noShow.Id), "A no-show releases the table");
        check(await db.HoaDon.AnyAsync(x => x.DatBanId == noShow.Id && x.TrangThai == TrangThaiHoaDon.DaHuy), "A no-show closes the unpaid bill");
        check(await service.ExpireMissedBooking(missed.Id) is null, "A booking past its window can be closed automatically");
        await db.Entry(missed).ReloadAsync();
        check(missed.TrangThai == TrangThaiDatBan.KhongDen, "A booking past its window becomes a no-show");
        check(await service.MarkNoShow(missed.Id, Version(missed), staffId) is not null, "A closed booking cannot be marked twice");

        // Khách tự hủy lịch chưa có tiền.
        var customer = await db.KhachHang.FirstAsync(x => x.TaiKhoanId != null);
        var cancel = Booking("RULES-CANCEL", null, TrangThaiDatBan.ChoXacNhan, now.AddDays(3));
        cancel.KhachHangId = customer.Id; cancel.LaKhachTrucTiep = false;
        await db.SaveChangesAsync();
        check(await preorders.RequestCancel(cancel.Id, customer.TaiKhoanId!.Value, Version(cancel), "Đổi kế hoạch") is null, "A customer can cancel a booking without money");
        await db.Entry(cancel).ReloadAsync();
        check(cancel.TrangThai == TrangThaiDatBan.DaHuy && cancel.YeuCauHuy is null, "A booking without money cancels immediately");

        // Hủy món chưa nấu thì trả nguyên liệu về kho.
        var ingredient = new NguyenLieu { TenNguyenLieu = "RULES-NL", DonViTinh = "kg", SoLuongTon = 10m, DonGia = 1000m, DangSuDung = true };
        db.NguyenLieu.Add(ingredient);
        db.DinhMucMon.Add(new DinhMucMon { MonAnId = size.MonAnId, MaKichCo = size.Id, NguyenLieu = ingredient, SoLuong = 0.5m });
        var seatedBill = Bill(null, "RULES-RETURN", TrangThaiCheBien.ChoCheBien);
        var cookingBill = Bill(null, "RULES-COOKING", TrangThaiCheBien.DangCheBien);
        var unseated = Booking("RULES-UNSEATED", null, TrangThaiDatBan.DaXacNhan, now.AddDays(1));
        unseated.MonDatTruoc.Add(new MonDatTruoc { MonAnId = size.MonAnId, MonAnSizeId = size.Id, TenMonLucDat = size.MonAn.TenMon, TenSizeLucDat = size.TenSize, SoLuong = 1, DonGiaThoaThuan = size.GiaBan });
        db.HoaDon.AddRange(seatedBill, cookingBill);
        await db.SaveChangesAsync();
        var unseatedBill = Bill(unseated, "RULES-UNSEATED", TrangThaiCheBien.ChoCheBien, unseated.MonDatTruoc.Single().Id);
        db.HoaDon.Add(unseatedBill);
        await db.SaveChangesAsync();
        check(await orders.UpdateDishStatusAsync(seatedBill.ChiTiet.Single().Id, TrangThaiCheBien.DaHuy, true) is null, "A waiting dish can be cancelled");
        await db.Entry(ingredient).ReloadAsync();
        check(ingredient.SoLuongTon == 10.5m, "Cancelling a dish the kitchen has not started returns its ingredients");
        var slip = await db.PhieuNhap.AsNoTracking().Include(x => x.ChiTiet).SingleAsync(x => x.GhiChu != null && x.GhiChu.Contains("HD-RULES-RETURN"));
        check(slip.LyDo == LyDoNhap.DieuChinhTang && slip.ChiTiet.Single(x => x.NguyenLieuId == ingredient.Id).SoLuong == 0.5m, "The returned stock is recorded on an adjustment slip");
        await orders.UpdateDishStatusAsync(unseatedBill.ChiTiet.Single().Id, TrangThaiCheBien.DaHuy, true);
        await db.Entry(ingredient).ReloadAsync();
        check(ingredient.SoLuongTon == 10.5m, "Cancelling a preorder that was never deducted does not add stock");
        await orders.UpdateDishStatusAsync(cookingBill.ChiTiet.Single().Id, TrangThaiCheBien.DaHuy, true);
        await db.Entry(ingredient).ReloadAsync();
        check(ingredient.SoLuongTon == 10.5m, "Cancelling a dish already in the kitchen does not return stock");
        db.DinhMucMon.RemoveRange(db.DinhMucMon.Where(x => x.NguyenLieuId == ingredient.Id));
        await db.SaveChangesAsync();

        // Khách chưa đến thì không có nút gửi bếp, chỉ báo món tự gửi lúc nhận bàn.
        DatBan WithDish(string code, TrangThaiDatBan state)
        {
            var booking = Booking(code, null, state, now.AddDays(2));
            if (state == TrangThaiDatBan.DaNhanBan) booking.ThoiDiemNhanBan = now;
            booking.MonDatTruoc.Add(new MonDatTruoc { MonAnId = size.MonAnId, MonAnSizeId = size.Id, TenMonLucDat = size.MonAn.TenMon, TenSizeLucDat = size.TenSize, SoLuong = 1, DonGiaThoaThuan = size.GiaBan });
            return booking;
        }
        var waiting = WithDish("RULES-WAITING-DISH", TrangThaiDatBan.DaXacNhan);
        var seatedDish = WithDish("RULES-SEATED-DISH", TrangThaiDatBan.DaNhanBan);
        await db.SaveChangesAsync();
        async Task<string> Detail(DatBan booking) => WebUtility.HtmlDecode(await (await admin.GetAsync($"/DatTruoc/ChiTiet/{booking.Id}")).Content.ReadAsStringAsync());
        const string sendButton = "Gửi các món chưa gửi xuống bếp", autoNote = "Món sẽ tự gửi bếp khi khách nhận bàn";
        var waitingHtml = await Detail(waiting);
        check(!waitingHtml.Contains(sendButton) && waitingHtml.Contains(autoNote), "A party not yet seated has no send button and is told seating sends the food");
        var seatedHtml = await Detail(seatedDish);
        check(seatedHtml.Contains(sendButton) && !seatedHtml.Contains(autoNote), "A seated party can send unsent food to the kitchen");

        // Khách đến ăn ngay: không bắt tên và số điện thoại, nhận bàn ngay.
        var walkInTable = Table("RULES-WALKIN");
        db.BanAn.Add(walkInTable);
        await db.SaveChangesAsync();
        var openNow = ReservationSchedule.Validate(TableService.VietnamNow, TableService.VietnamNow, -15) is null;
        var form = WebUtility.HtmlDecode(await (await admin.GetAsync("/QuanLyDatBan/Create")).Content.ReadAsStringAsync());
        using (var response = await admin.PostAsync("/QuanLyDatBan/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = hidden(form, "__RequestVerificationToken"), ["GioDen"] = "2030-01-01T12:00", ["SoNguoi"] = "2",
            ["BanAnId"] = walkInTable.Id.ToString(), ["KhachDenNgay"] = "true"
        })))
        {
            var walkIn = await db.DatBan.AsNoTracking().Include(x => x.Ban).SingleOrDefaultAsync(x => x.Ban.Any(b => b.BanAnId == walkInTable.Id));
            if (openNow)
            {
                check(response.StatusCode == HttpStatusCode.Redirect && walkIn is { TrangThai: TrangThaiDatBan.DaNhanBan, HoTenLienHe: "Khách lẻ", SoDienThoaiLienHe: null },
                    "A walk-in without a name or phone is seated immediately");
                check(walkIn is not null && DateOnly.FromDateTime(walkIn.GioDen.ToOffset(TimeSpan.FromHours(7)).DateTime) == DateOnly.FromDateTime(TableService.VietnamNow),
                    "A walk-in arrives now, whatever time was left on the form");
            }
            else check(walkIn is null, "Walk-ins are refused outside serving hours");
        }
        using (var response = await admin.PostAsync("/QuanLyDatBan/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = hidden(form, "__RequestVerificationToken"), ["GioDen"] = TableService.VietnamNow.AddDays(2).Date.AddHours(12).ToString("yyyy-MM-ddTHH:mm"),
            ["SoNguoi"] = "2", ["BanAnId"] = walkInTable.Id.ToString(), ["KhachDenNgay"] = "false"
        })))
            check(response.StatusCode != HttpStatusCode.Redirect, "An advance booking still needs a contact name and phone");
    }
}
