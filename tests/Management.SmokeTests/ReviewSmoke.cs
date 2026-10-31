using System.Net;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;

namespace Management.SmokeTests;

// Khách chỉ đánh giá được lượt đặt bàn của mình, sau khi đã nhận bàn và hóa đơn đã thanh toán.
public static class ReviewSmoke
{
    public static async Task Run(RestaurantDbContext db, HttpClient admin, Func<HttpClient> newClient,
        Func<string, string, string> hidden, Action<bool, string> check)
    {
        async Task<string> Get(HttpClient client, string path)
        {
            var response = await client.GetAsync(path);
            check(response.StatusCode == HttpStatusCode.OK, "Review GET " + path + ": " + response.StatusCode);
            return WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        }
        async Task<HttpResponseMessage> PostReview(HttpClient client, string form, Dictionary<string, string> fields, byte[]? image = null)
        {
            var body = new MultipartFormDataContent();
            body.Add(new StringContent(hidden(form, "__RequestVerificationToken")), "__RequestVerificationToken");
            foreach (var (key, value) in fields) body.Add(new StringContent(value), key);
            if (image != null)
            {
                var file = new ByteArrayContent(image);
                file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                body.Add(file, "HinhAnhFile", "review.png");
            }
            return await client.PostAsync("/DatBan/DanhGia", body);
        }
        bool RedirectsToHistory(HttpResponseMessage response) => response.StatusCode == HttpStatusCode.Redirect
            && response.Headers.Location?.OriginalString.Contains("/DatBan/LichSu") == true;

        using var customer = newClient();
        var login = await Get(customer, "/Account/Login");
        using (var response = await customer.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            { ["Email"] = "khach.demo@example.test", ["Password"] = "Demo@2026!", ["__RequestVerificationToken"] = hidden(login, "__RequestVerificationToken") })))
            check(response.StatusCode == HttpStatusCode.Redirect, "Review customer logs in");

        var customerId = await db.KhachHang.Where(x => x.Email == "khach.demo@example.test").Select(x => x.Id).SingleAsync();
        var staffId = await db.NhanVien.Where(x => x.DangLamViec).Select(x => x.Id).FirstAsync();
        var size = await db.MonAnSize.Include(x => x.MonAn).FirstAsync(x => x.DangSuDung && x.GiaBan > 0);
        var now = DateTimeOffset.UtcNow;
        DatBan Booking(string code, int? owner, TrangThaiDatBan state, TrangThaiHoaDon billState) => new()
        {
            MaDatBan = code, KhachHangId = owner, HoTenLienHe = "REVIEW " + code, SoDienThoaiLienHe = "0901234567", LaKhachTrucTiep = owner == null,
            ThoiDiemTao = now.AddHours(-4), GioDen = now.AddHours(-3), GioKetThucDuKien = now.AddHours(-1), SoNguoiLon = 2,
            TrangThai = state, ThoiDiemNhanBan = state == TrangThaiDatBan.DaNhanBan ? now.AddHours(-3) : null,
            ThoiDiemKetThuc = billState == TrangThaiHoaDon.DaThanhToan ? now.AddMinutes(-30) : null,
            HoaDon =
            {
                new HoaDon
                {
                    MaHoaDon = "HD-" + code, KhachHangId = owner, NhanVienId = staffId, ThoiDiemLap = now.AddHours(-2),
                    TrangThai = billState, ThoiDiemThanhToan = billState == TrangThaiHoaDon.DaThanhToan ? now.AddMinutes(-40) : null,
                    TongTienHang = size.GiaBan,
                    ChiTiet = { new ChiTietHoaDon { MonAnId = size.MonAnId, MonAnSizeId = size.Id, TenMonLucBan = size.MonAn.TenMon,
                        TenSizeLucBan = size.TenSize, SoLuong = 1, DonGia = size.GiaBan, TrangThai = TrangThaiCheBien.DaPhucVu } }
                }
            }
        };
        var done = Booking("RV-DONE", customerId, TrangThaiDatBan.DaNhanBan, TrangThaiHoaDon.DaThanhToan);
        var serving = Booking("RV-SERVING", customerId, TrangThaiDatBan.DaNhanBan, TrangThaiHoaDon.ChuaThanhToan);
        var confirmed = Booking("RV-CONFIRMED", customerId, TrangThaiDatBan.DaXacNhan, TrangThaiHoaDon.ChuaThanhToan);
        var other = Booking("RV-OTHER", null, TrangThaiDatBan.DaNhanBan, TrangThaiHoaDon.DaThanhToan);
        db.DatBan.AddRange(done, serving, confirmed, other);
        await db.SaveChangesAsync();

        var history = await Get(customer, "/DatBan/LichSu");
        check(history.Contains("/DatBan/DanhGia/RV-DONE"), "Seated and paid booking shows the review button");
        check(!history.Contains("/DatBan/DanhGia/RV-SERVING") && !history.Contains("/DatBan/DanhGia/RV-CONFIRMED"),
            "Unpaid or not-yet-seated bookings never show a review button that would only bounce back");
        check(!history.Contains("RV-OTHER"), "History does not list another customer's booking");

        foreach (var code in new[] { "RV-SERVING", "RV-CONFIRMED", "RV-OTHER", other.Id.ToString() })
            using (var response = await customer.GetAsync("/DatBan/DanhGia/" + code))
                check(RedirectsToHistory(response), "Review page refuses " + code);
        var form = await Get(customer, "/DatBan/DanhGia/" + done.Id);
        check(form.Contains("RV-DONE") && form.Contains("type=\"radio\"") && form.Contains("accept=\"image/png,image/jpeg\""),
            "Review page opens by numeric id with keyboard-friendly star radios and PNG/JPEG filter");

        var valid = new Dictionary<string, string> { ["MaDatBan"] = "RV-DONE", ["SoSaoMonAn"] = "5", ["SoSaoDichVu"] = "3",
            ["NoiDung"] = "Món ngon, phục vụ hơi chậm.", ["TenKhachHang"] = "Tên giả mạo" };
        using (var response = await PostReview(customer, form, new(valid) { ["MaDatBan"] = "RV-OTHER", ["NoiDung"] = "" }))
            check(RedirectsToHistory(response), "Invalid review for someone else's booking redirects instead of re-rendering their form");
        using (var response = await PostReview(customer, form, new(valid) { ["MaDatBan"] = "RV-SERVING" }))
            check(RedirectsToHistory(response), "Unpaid booking cannot be reviewed by posting directly");

        var uploads = Path.Combine(Directory.GetCurrentDirectory(), "RestaurantManagement.Web", "wwwroot", "uploads", "reviews");
        int UploadCount() => Directory.Exists(uploads) ? Directory.GetFiles(uploads).Length : 0;
        var filesBefore = UploadCount();
        using (var response = await PostReview(customer, form, valid, "not an image"u8.ToArray()))
        {
            var page = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
            check(response.StatusCode == HttpStatusCode.OK && page.Contains("Chỉ nhận ảnh PNG/JPEG tối đa 2 MB."),
                "Fake image is rejected with a clear message: " + response.StatusCode);
            check(!page.Contains("Tên giả mạo") && page.Contains("REVIEW RV-DONE"), "Re-rendered form shows the real customer name, not the posted one");
        }
        check(UploadCount() == filesBefore && !await db.DanhGia.AnyAsync(x => x.HoaDonId == done.HoaDon.Single().Id),
            "Rejected image leaves no file and no review");

        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");
        using (var response = await PostReview(customer, form, valid, png))
            check(RedirectsToHistory(response), "Valid review with a PNG photo is accepted");
        var review = await db.DanhGia.AsNoTracking().SingleAsync(x => x.HoaDonId == done.HoaDon.Single().Id);
        check(review.KhachHangId == customerId && review.Diem == 4 && review.NoiDung.Contains("[Ảnh: /uploads/reviews/"),
            "Review stores the owner, the average score and the photo link");
        var savedPhoto = Path.Combine(uploads, review.NoiDung.Split("/uploads/reviews/")[1].TrimEnd(']'));
        check(File.Exists(savedPhoto), "Uploaded review photo is written to disk");
        File.Delete(savedPhoto);

        using (var response = await PostReview(customer, form, valid))
            check(RedirectsToHistory(response), "A second review for the same bill is refused");
        check(await db.DanhGia.CountAsync(x => x.HoaDonId == done.HoaDon.Single().Id) == 1, "Only one review is stored per bill");
        check((await Get(customer, "/DatBan/LichSu")).Contains("Đã gửi đánh giá"), "History marks the booking as reviewed");
        check((await Get(admin, "/QuanLyDatBan/DanhGia")).Contains("Món ngon, phục vụ hơi chậm."), "Staff can read the new review");
    }
}
