using System.Net;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Services;

namespace Management.SmokeTests;

// Một hóa đơn – nhiều đợt tiền: đợt cọc nhận trước và đợt thanh toán cuối cùng đều nằm trong ChiTietThanhToan.
public static class PaymentLedgerSmoke
{
    public static async Task Run(RestaurantDbContext db, HttpClient admin, Func<HttpClient> newClient,
        Func<string, string, string> hidden, Action<bool, string> check)
    {
        async Task<string> Get(HttpClient client, string path)
        {
            var response = await client.GetAsync(path);
            check(response.StatusCode == HttpStatusCode.OK, "Ledger GET " + path + ": " + response.StatusCode);
            return WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        }
        string Version<T>(T entity) where T : class => Convert.ToBase64String(db.Entry(entity).Property<byte[]>("RowVersion").CurrentValue!);

        var staffId = await db.NhanVien.Where(x => x.DangLamViec).Select(x => x.Id).FirstAsync();
        var customerId = await db.KhachHang.Where(x => x.Email == "khach.demo@example.test").Select(x => x.Id).SingleAsync();
        var dish = new MonAn { TenMon = "LEDGER-DISH", DanhMuc = new DanhMuc { TenDanhMuc = "LEDGER-MENU", DangSuDung = true },
            Loai = LoaiMon.MonLe, DaDuyet = true, TrangThai = TrangThaiMon.DangPhucVu,
            Sizes = { new MonAnSize { TenSize = "Vừa", GiaBan = 150000m, DangSuDung = true } } };
        db.MonAn.Add(dish); await db.SaveChangesAsync();
        var size = dish.Sizes.Single();
        var now = DateTimeOffset.UtcNow;
        var booking = new DatBan
        {
            MaDatBan = "LEDGER-BOOKING", KhachHangId = customerId, HoTenLienHe = "LEDGER CUSTOMER", SoDienThoaiLienHe = "0901234567", ThoiDiemTao = now.AddHours(-3),
            GioDen = now.AddHours(-1), GioKetThucDuKien = now.AddHours(1), SoNguoiLon = 2, TrangThai = TrangThaiDatBan.DaNhanBan,
            ThoiDiemNhanBan = now.AddHours(-1), YeuCauCoc = true, TienCocYeuCau = 100000m, TienCocDaNop = 100000m,
            TrangThaiCoc = TrangThaiCoc.DaCoc, ThoiDiemCoc = now.AddHours(-2)
        };
        db.DatBan.Add(booking); await db.SaveChangesAsync();
        db.ChiTietThanhToan.Add(PaymentLedger.Create(LoaiGiaoDichThanhToan.DatCoc, PhuongThucThanhToan.ChuyenKhoan, 100000m,
            datBanId: booking.Id, nhanVienId: staffId, maThamChieu: "LEDGER-BANK-01"));
        var bill = new HoaDon
        {
            MaHoaDon = "HD-LEDGER-01", DatBanId = booking.Id, KhachHangId = customerId, NhanVienId = staffId, ThoiDiemLap = now.AddMinutes(-50),
            TrangThai = TrangThaiHoaDon.ChuaThanhToan, TongTienHang = 300000m,
            ChiTiet = { new ChiTietHoaDon { MonAnId = dish.Id, MonAnSizeId = size.Id, TenMonLucBan = dish.TenMon, TenSizeLucBan = size.TenSize,
                SoLuong = 2, DonGia = size.GiaBan, TrangThai = TrangThaiCheBien.DaPhucVu } }
        };
        db.HoaDon.Add(bill); await db.SaveChangesAsync();

        PaymentFormModel Form(PhuongThucThanhToan method, decimal given) => new()
        {
            Id = bill.Id, RowVersion = Version(bill), BookingRowVersion = Version(booking), PhuongThuc = method, TienKhachDua = given
        };
        var orders = new OrderService(db);
        check(await orders.ProcessPaymentAsync(Form(PhuongThucThanhToan.The, 1000000m), staffId) is { } cardError && cardError.Contains("tiền mặt"),
            "Card payment is refused: only cash and bank transfer are accepted");
        check(await orders.ProcessPaymentAsync(Form(PhuongThucThanhToan.TienMat, 1000m), staffId) is { } shortError && shortError.Contains("không đủ"),
            "Cash below the amount due is refused");
        db.ChangeTracker.Clear();
        bill = await db.HoaDon.SingleAsync(x => x.Id == bill.Id);
        booking = await db.DatBan.SingleAsync(x => x.Id == booking.Id);
        check(bill.TrangThai == TrangThaiHoaDon.ChuaThanhToan && !await db.ChiTietThanhToan.AnyAsync(x => x.HoaDonId == bill.Id),
            "Refused payments leave the bill open and write no ledger line");

        var error = await orders.ProcessPaymentAsync(Form(PhuongThucThanhToan.TienMat, 1000000m), staffId);
        check(error is null, "Cash payment with a confirmed deposit succeeds: " + error);
        db.ChangeTracker.Clear();
        bill = await db.HoaDon.SingleAsync(x => x.Id == bill.Id);
        booking = await db.DatBan.SingleAsync(x => x.Id == booking.Id);
        var due = bill.TongTienHang - bill.TienGiam - bill.TienCocDaTru;
        check(bill.TrangThai == TrangThaiHoaDon.DaThanhToan && bill.TienCocDaTru == 100000m && bill.TienThoiLai == 1000000m - due
            && booking.TrangThaiCoc == TrangThaiCoc.DaDoiTru, "Deposit is deducted from the bill and change is calculated on the remainder");
        var lines = await db.ChiTietThanhToan.AsNoTracking().Where(x => x.HoaDonId == bill.Id).ToListAsync();
        check(lines.Count == 2
            && lines.Any(x => x.LoaiGiaoDich == LoaiGiaoDichThanhToan.DatCoc && x.HinhThuc == PhuongThucThanhToan.ChuyenKhoan && x.SoTien == 100000m)
            && lines.Any(x => x.LoaiGiaoDich == LoaiGiaoDichThanhToan.ThanhToan && x.HinhThuc == PhuongThucThanhToan.TienMat && x.SoTien == due),
            "One bill holds two payment lines: the earlier deposit and today's final cash payment");
        check(lines.Sum(PaymentLedger.SignedAmount) == bill.TongTienHang - bill.TienGiam, "Payment lines add up to the bill total after discount");
        check(await orders.ProcessPaymentAsync(Form(PhuongThucThanhToan.TienMat, 1000000m), staffId) is not null
            && await db.ChiTietThanhToan.CountAsync(x => x.HoaDonId == bill.Id) == 2, "A paid bill cannot be paid twice");

        var today = DateOnly.FromDateTime(TableService.VietnamNow);
        var report = await new BaoCaoService(db).GetShiftReportAsync(today, null);
        var todayLines = await db.ChiTietThanhToan.AsNoTracking().Where(x => x.NgayThanhToan == today).ToListAsync();
        check(report.GiaoDich.Any(x => x.HoaDonId == bill.Id && x.LoaiGiaoDich == LoaiGiaoDichThanhToan.ThanhToan)
            && report.TheoCa.Sum(x => x.ThanhToanTienMat) == todayLines.Where(x => x.LoaiGiaoDich == LoaiGiaoDichThanhToan.ThanhToan
                && x.HinhThuc == PhuongThucThanhToan.TienMat).Sum(x => x.SoTien)
            && report.TheoCa.Sum(x => x.CocChuyenKhoan) == todayLines.Where(x => x.LoaiGiaoDich == LoaiGiaoDichThanhToan.DatCoc
                && x.HinhThuc == PhuongThucThanhToan.ChuyenKhoan).Sum(x => x.SoTien),
            "Shift report totals match the payment ledger by type and method");
        var shiftPage = await Get(admin, $"/BaoCao/NopCa?ngay={today:yyyy-MM-dd}");
        check(shiftPage.Contains("HD-LEDGER-01") && shiftPage.Contains(lines.Single(x => x.LoaiGiaoDich == LoaiGiaoDichThanhToan.ThanhToan).MaThanhToan),
            "Shift report page lists the bill's payment lines");

        // Danh sách hóa đơn đã thanh toán được phân trang 20 dòng.
        for (var i = 0; i < 21; i++)
            db.HoaDon.Add(new HoaDon { MaHoaDon = $"HD-PAGE-{i:00}", NhanVienId = staffId, ThoiDiemLap = now.AddYears(-5).AddMinutes(i),
                TrangThai = TrangThaiHoaDon.DaThanhToan, ThoiDiemThanhToan = now.AddYears(-5).AddMinutes(i), TongTienHang = 10000m });
        await db.SaveChangesAsync();
        var paidCount = await db.HoaDon.CountAsync(x => x.TrangThai == TrangThaiHoaDon.DaThanhToan);
        var pages = (int)Math.Ceiling(paidCount / 20.0);
        var firstPage = await Get(admin, "/HoaDon?daThanhToan=true");
        check(firstPage.Contains($"{paidCount} hóa đơn · Trang 1 / {pages}") && firstPage.Contains("page=2"),
            "Paid invoice list shows the total count and links to the next page");
        var lastPage = await Get(admin, $"/HoaDon?daThanhToan=true&page={pages + 5}");
        check(lastPage.Contains($"Trang {pages} / {pages}") && lastPage.Contains("HD-PAGE-00"),
            "Out-of-range page is clamped to the last page, which holds the oldest invoices");
    }
}
