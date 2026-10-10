using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Services;

namespace Management.SmokeTests;

public static class PaymentQrSmoke
{
    public static async Task Run(RestaurantDbContext db, HttpClient admin, Func<HttpClient> newClient,
        Func<string, string, string> hidden, Action<bool, string> check)
    {
        async Task<string> Page(HttpClient client, string path)
        {
            using var response = await client.GetAsync(path);
            check(response.StatusCode == HttpStatusCode.OK, "QR GET " + path);
            return await response.Content.ReadAsStringAsync();
        }
        async Task Upload(HttpClient client, string form, byte[] bytes, string filename, string contentType, HttpStatusCode expected, bool csrf = true)
        {
            using var data = new MultipartFormDataContent();
            if (csrf) data.Add(new StringContent(hidden(form, "__RequestVerificationToken")), "__RequestVerificationToken");
            data.Add(new StringContent(hidden(form, "Version")), "Version");
            var image = new ByteArrayContent(bytes); image.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            data.Add(image, "AnhQr", filename);
            using var response = await client.PostAsync("/CauHinhQr", data);
            var errors = response.StatusCode == expected ? "" : string.Join(" ", Regex.Matches(await response.Content.ReadAsStringAsync(), "<li>([^<]+)</li>").Select(x => WebUtility.HtmlDecode(x.Groups[1].Value)));
            check(response.StatusCode == expected, "QR upload: " + response.StatusCode + "; " + expected + " " + errors);
        }
        using var customer = newClient(); using var cashier = newClient(); using var anonymous = newClient();
        foreach (var (client, email, path) in new[] { (customer, "khach.demo@example.test", "/Account/Login"), (cashier, "thungan.demo@example.test", "/admin") })
        {
            var page = await Page(client, path);
            using var login = await client.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string>
                { ["Email"] = email, ["Password"] = "Demo@2026!", ["__RequestVerificationToken"] = hidden(page, "__RequestVerificationToken") }));
            check(login.StatusCode == HttpStatusCode.Redirect, "QR login " + email);
        }
        using (var denied = await anonymous.GetAsync("/CauHinhQr"))
            check(denied.StatusCode == HttpStatusCode.Redirect && denied.Headers.Location!.OriginalString.StartsWith("/admin"), "QR settings use internal login");
        foreach (var client in new[] { customer, cashier })
            using (var denied = await client.GetAsync("/CauHinhQr")) check(denied.StatusCode == HttpStatusCode.Redirect, "Non-admin cannot open QR settings");
        check(!(await Page(cashier, "/admin/TrangChu")).Contains("/CauHinhQr"), "Cashier menu does not offer QR configuration");
        var form = await Page(admin, "/CauHinhQr");
        check(hidden(form, "Version") == "", "Isolated test storage starts without a real QR");
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+bTK0AAAAASUVORK5CYII=");
        await Upload(admin, form, png, "qr.png", "image/png", HttpStatusCode.BadRequest, csrf: false);
        await Upload(cashier, form, png, "qr.png", "image/png", HttpStatusCode.Redirect);
        await Upload(admin, form, "<svg></svg>"u8.ToArray(), "qr.png", "image/png", HttpStatusCode.BadRequest);
        await Upload(admin, form, new byte[2 * 1024 * 1024 + 1], "qr.png", "image/png", HttpStatusCode.BadRequest);
        check(hidden(await Page(admin, "/CauHinhQr"), "Version") == "", "Rejected uploads do not change the current QR");
        await Upload(admin, form, png, "qr.png", "image/png", HttpStatusCode.Redirect);
        var first = hidden(await Page(admin, "/CauHinhQr"), "Version");
        check(first.Length == 64, "Saved image has a versioned URL");
        using (var image = await anonymous.GetAsync("/CauHinhQr/Anh?v=" + first))
            check(image.StatusCode == HttpStatusCode.OK && image.Content.Headers.ContentType!.MediaType == "image/png"
                && (await image.Content.ReadAsByteArrayAsync()).SequenceEqual(png), "Customer can read the exact validated image without admin rights");
        using (var invalid = await anonymous.GetAsync("/CauHinhQr/Anh?v=..%2F..%2Fappsettings.json"))
            check(invalid.StatusCode == HttpStatusCode.NotFound, "Image URL cannot read arbitrary server files");
        await Upload(admin, form, png, "qr.png", "image/png", HttpStatusCode.BadRequest);
        check(hidden(await Page(admin, "/CauHinhQr"), "Version") == first, "Stale upload cannot overwrite an image saved from another tab");
        var newPng = png.Concat(new byte[] { 0 }).ToArray();
        await Upload(admin, await Page(admin, "/CauHinhQr"), newPng, "new.png", "image/png", HttpStatusCode.Redirect);
        var second = hidden(await Page(admin, "/CauHinhQr"), "Version");
        check(second != first, "Replacing QR changes the current image URL");
        using (var old = await anonymous.GetAsync("/CauHinhQr/Anh?v=" + first))
            check((await old.Content.ReadAsByteArrayAsync()).SequenceEqual(png), "An already-open customer page retains its old QR image");

        var area = new KhuVuc { TenKhuVuc = "QR smoke", DangSuDung = true };
        db.BanAn.Add(new BanAn { MaBan = "QR-SMOKE", KhuVuc = area, SoChoNgoi = 4, TrangThai = TrangThaiBan.SanSang });
        await db.SaveChangesAsync();
        var bookingForm = await Page(customer, "/DatBan");
        var request = Guid.NewGuid();
        using (var create = await customer.PostAsync("/DatBan", new FormUrlEncodedContent(new Dictionary<string, string>
            { ["__RequestVerificationToken"] = hidden(bookingForm, "__RequestVerificationToken"), ["RequestId"] = request.ToString(),
                ["HoTen"] = "QR smoke", ["SoDienThoai"] = "0901234567", ["SoNguoi"] = "2", ["MaKhuVuc"] = area.Id.ToString(),
                ["ThoiGianDen"] = TableService.VietnamNow.AddDays(2).ToString("yyyy-MM-ddTHH:mm") })))
            check(create.StatusCode == HttpStatusCode.Redirect, "Reservation with configured QR is created normally");
        var booking = await db.DatBan.AsNoTracking().SingleAsync(x => x.YeuCauTaoId == request);
        var checkout = await Page(customer, "/DatBan/ThanhToanCoc/" + booking.Id);
        check(checkout.Contains(second) && !checkout.Contains("payment-qr-demo.svg") && checkout.Contains("299.000"), "Customer checkout uses uploaded QR and retains the authoritative deposit amount");
        check(booking.TienCocDaNop == 0 && booking.TrangThai == TrangThaiDatBan.ChoCoc, "Uploading QR never confirms money or a booking");
    }
}
