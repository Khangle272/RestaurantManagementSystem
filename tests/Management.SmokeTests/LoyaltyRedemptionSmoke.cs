using System.Net;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Services;

namespace Management.SmokeTests;

public static class LoyaltyRedemptionSmoke
{
    public static async Task Run(RestaurantDbContext db, HttpClient admin, HttpClient customer, HttpClient stranger,
        int ownerId, int otherId, Func<string, int, HoaDon> bill,
        Func<string, string, string> hidden, Action<bool, string> check, string connection)
    {
        var options = new DbContextOptionsBuilder<RestaurantDbContext>().UseSqlServer(connection).Options;
        var now = DateTimeOffset.UtcNow;
        var balance = await Balance(ownerId);
        check(balance >= 5, "Payment earnings are available for redemption");
        // Create the offer through the actual admin form, including validation of point cost.
        var form = await Get(admin, "/KhuyenMai/Create");
        var fields = new Dictionary<string, string> {
            ["TenChuongTrinh"] = "LOYALTY-REWARDS", ["CanVoucher"] = "true", ["DangSuDung"] = "true",
            ["BatDau"] = PromotionTime.Local(now.AddDays(-1)).ToString("yyyy-MM-ddTHH:mm:ss"),
            ["KetThuc"] = PromotionTime.Local(now.AddDays(1)).ToString("yyyy-MM-ddTHH:mm:ss"),
            ["Vouchers[0].Ma"] = "LOYALTY-5", ["Vouchers[0].DiemCanDoi"] = "-1",
            ["Vouchers[0].PhamVi"] = "HoaDon", ["Vouchers[0].KieuGiam"] = "SoTien", ["Vouchers[0].GiaTri"] = "10000",
            ["Vouchers[0].BatDau"] = PromotionTime.Local(now.AddHours(-1)).ToString("yyyy-MM-ddTHH:mm:ss"),
            ["Vouchers[0].KetThuc"] = PromotionTime.Local(now.AddHours(1)).ToString("yyyy-MM-ddTHH:mm:ss"),
            ["Vouchers[0].DangSuDung"] = "true"
        };
        await Post(admin, "/KhuyenMai/Create", form, fields, HttpStatusCode.OK);
        check(!await db.KhuyenMai.AnyAsync(x => x.TenChuongTrinh == "LOYALTY-REWARDS"), "Negative redemption cost cannot create a campaign");
        fields["Vouchers[0].DiemCanDoi"] = "5";
        await Post(admin, "/KhuyenMai/Create", form, fields, HttpStatusCode.Redirect);
        var reward = await db.Voucher.Include(x => x.KhuyenMai).SingleAsync(x => x.Ma == "LOYALTY-5");
        check(reward.DiemCanDoi == 5, "Admin can configure a point cost on a voucher");
        var previewBill = bill("LOYALTY-ORDER", ownerId);
        var otherBill = bill("LOYALTY-OTHER", otherId);
        var anonymousBill = bill("LOYALTY-ANONYMOUS", ownerId); anonymousBill.KhachHangId = null;
        db.HoaDon.AddRange(previewBill, otherBill, anonymousBill); await db.SaveChangesAsync();
        var promotions = new PromotionService(db);
        check((await promotions.EvaluatePromotionsAsync(previewBill.Id, reward.Ma)).AppliedVoucherId is null
            && !(await promotions.ApplyVoucherToInvoiceAsync(previewBill.Id, reward.Ma, Version(previewBill))).Success,
            "Typing the reward code cannot bypass point redemption");
        check((await promotions.EvaluatePromotionsAsync(anonymousBill.Id, reward.Ma)).AppliedVoucherId is null,
            "A reward requires a customer wallet, including staff checkout");

        var catalog = await Get(customer, "/KhachHang/Vouchers");
        check(WebUtility.HtmlDecode(catalog).Contains("Đổi 5 điểm lấy voucher") && catalog.Contains("expectedPoints"),
            "Catalog displays the real redemption cost and action");
        await Post(customer, "/KhachHang/Collect", catalog, new() { ["id"] = reward.Id.ToString() }, HttpStatusCode.Redirect);
        check(!await Saved(ownerId, reward.Id) && await Balance(ownerId) == balance, "Free collection cannot acquire a paid reward");
        await Post(customer, "/KhachHang/Redeem", catalog, new() { ["id"] = reward.Id.ToString(), ["expectedPoints"] = "1" }, HttpStatusCode.Redirect);
        check(!await Saved(ownerId, reward.Id) && await Balance(ownerId) == balance, "Forging the displayed price cannot reduce the point cost");
        using (var noToken = await customer.PostAsync("/KhachHang/Redeem", new FormUrlEncodedContent(new Dictionary<string, string> {
            ["id"] = reward.Id.ToString(), ["expectedPoints"] = "5" })))
            check(noToken.StatusCode == HttpStatusCode.BadRequest, "Redemption requires an antiforgery token");
        var strangerForm = await Get(stranger, "/KhachHang/Vouchers");
        await Post(stranger, "/KhachHang/Redeem", strangerForm, new() {
            ["id"] = reward.Id.ToString(), ["expectedPoints"] = "5", ["customerId"] = ownerId.ToString()
        }, HttpStatusCode.Redirect);
        check(!await Saved(otherId, reward.Id) && !await Saved(ownerId, reward.Id) && await Balance(ownerId) == balance,
            "Insufficient points and a forged customer ID cannot spend another customer's balance");

        await Post(customer, "/KhachHang/Redeem", catalog, new() { ["id"] = reward.Id.ToString(), ["expectedPoints"] = "5" }, HttpStatusCode.Redirect);
        await Post(customer, "/KhachHang/Redeem", catalog, new() { ["id"] = reward.Id.ToString(), ["expectedPoints"] = "5" }, HttpStatusCode.Redirect);
        var debit = await db.LichSuTichDiem.SingleAsync(x => x.KhachHangId == ownerId && x.VoucherId == reward.Id);
        check(await Saved(ownerId, reward.Id) && await Balance(ownerId) == balance - 5 && debit.SoDiem == -5
            && debit.HoaDonId is null && debit.SoTienTinhDiem == 0,
            "Redemption atomically grants one wallet ticket and one debit; repeating the request cannot charge twice");
        var wallet = await Get(customer, "/KhachHang/Vouchers?wallet=true");
        var history = WebUtility.HtmlDecode(await Get(customer, "/KhachHang"));
        var managed = WebUtility.HtmlDecode(await Get(admin, $"/TaiKhoanKhachHang/Details/{ownerId}"));
        check(wallet.Contains(reward.Ma) && history.Contains("-5") && managed.Contains("Đổi điểm lấy voucher LOYALTY-5"),
            "Wallet, customer history and management history expose the redeemed voucher and debit");
        check((await promotions.EvaluatePromotionsAsync(otherBill.Id, reward.Ma)).AppliedVoucherId is null,
            "Another customer cannot use a reward from someone else's wallet");
        var applied = await promotions.ApplyVoucherToInvoiceAsync(previewBill.Id, reward.Ma, Version(previewBill));
        check(applied.Success && applied.Result?.TongTienGiam == 10000, "A redeemed wallet voucher discounts the invoice normally");
        var secondBill = bill("LOYALTY-SECOND", ownerId); db.HoaDon.Add(secondBill); await db.SaveChangesAsync();
        check((await promotions.EvaluatePromotionsAsync(secondBill.Id, reward.Ma)).AppliedVoucherId is null,
            "One redeemed ticket cannot be held on two invoices");
        var employeeId = previewBill.NhanVienId;
        var payment = await new OrderService(db).ProcessPaymentAsync(new PaymentFormModel {
            Id = previewBill.Id, RowVersion = Version(previewBill), TienKhachDua = 90000, SoTienPhaiThuDuKien = 90000
        }, employeeId);
        check(payment is null && await Balance(ownerId) == balance - 5 + 9,
            "Redeemed voucher settles and earns points only on the remaining 90,000 purchase: " + payment);

        // A configured cost cannot change after customers have paid it.
        var edit = await Get(admin, $"/KhuyenMai/Edit/{reward.KhuyenMaiId}");
        var changes = new Dictionary<string, string>(fields) {
            ["Id"] = reward.KhuyenMaiId.ToString(), ["RowVersion"] = hidden(edit, "RowVersion"),
            ["Vouchers[0].Id"] = reward.Id.ToString(), ["Vouchers[0].RowVersion"] = Version(reward), ["Vouchers[0].DiemCanDoi"] = "0"
        };
        await Post(admin, $"/KhuyenMai/Edit/{reward.KhuyenMaiId}", edit, changes, HttpStatusCode.OK);
        check(await db.Voucher.Where(x => x.Id == reward.Id).Select(x => x.DiemCanDoi).SingleAsync() == 5,
            "Admin cannot turn an issued reward into a free code");

        var expired = Reward("LOYALTY-EXPIRED", 1); expired.KetThuc = now.AddMinutes(-1);
        var disabled = Reward("LOYALTY-DISABLED", 1); disabled.DangSuDung = false;
        var privateReward = Reward("LOYALTY-PRIVATE", 1); privateReward.KhachHangId = otherId;
        var exhausted = Reward("LOYALTY-EXHAUSTED", 1); exhausted.GioiHanPhatHanh = 1;
        var future = Reward("LOYALTY-FUTURE", 1); future.BatDau = now.AddHours(1);
        var limitedProgram = Reward("LOYALTY-PARENT-FULL", 1); limitedProgram.KhuyenMai.GioiHanPhatHanh = 1;
        var sibling = new Voucher { Ma = "LOYALTY-PARENT-SIBLING", KhuyenMai = limitedProgram.KhuyenMai, DiemCanDoi = 1 };
        db.Voucher.AddRange(expired, disabled, privateReward, exhausted, future, limitedProgram, sibling); await db.SaveChangesAsync();
        db.VoucherKhachHang.AddRange(new VoucherKhachHang { KhachHangId = otherId, VoucherId = exhausted.Id, ThoiDiemThuThap = now },
            new VoucherKhachHang { KhachHangId = otherId, VoucherId = sibling.Id, ThoiDiemThuThap = now });
        await db.SaveChangesAsync();
        var beforeFailures = await Balance(ownerId);
        foreach (var invalid in new[] { expired, disabled, privateReward, exhausted, future, limitedProgram })
            check(await Redeem(ownerId, invalid) is not null && !await Saved(ownerId, invalid.Id), "Reject invalid/exhausted reward " + invalid.Ma);
        check(await Balance(ownerId) == beforeFailures, "Failed redemptions leave the ledger and wallet unchanged");

        var otherPurchase = bill("LOYALTY-OTHER-EARN", otherId); db.HoaDon.Add(otherPurchase); await db.SaveChangesAsync();
        check(await new OrderService(db).ProcessPaymentAsync(new PaymentFormModel {
            Id = otherPurchase.Id, RowVersion = Version(otherPurchase), TienKhachDua = 100000, SoTienPhaiThuDuKien = 100000
        }, employeeId) is null, "Second customer earns points through a paid purchase");
        var lastTicket = Reward("LOYALTY-LAST-TICKET", 1); lastTicket.GioiHanPhatHanh = 1;
        db.Voucher.Add(lastTicket); await db.SaveChangesAsync();
        var totalBefore = await Balance(ownerId) + await Balance(otherId);
        var lastRace = await Task.WhenAll(Redeem(ownerId, lastTicket), Redeem(otherId, lastTicket));
        check(lastRace.Count(x => x is null) == 1 && await db.VoucherKhachHang.CountAsync(x => x.VoucherId == lastTicket.Id) == 1
            && await Balance(ownerId) + await Balance(otherId) == totalBefore - 1,
            "Two funded customers competing for the last ticket charge only the winner");

        var beforeDuplicate = await Balance(ownerId);
        var duplicate = Reward("LOYALTY-DUPLICATE", 1); db.Voucher.Add(duplicate); await db.SaveChangesAsync();
        await Task.WhenAll(Redeem(ownerId, duplicate), Redeem(ownerId, duplicate));
        check(await Saved(ownerId, duplicate.Id) && await Balance(ownerId) == beforeDuplicate - 1
            && await db.LichSuTichDiem.CountAsync(x => x.KhachHangId == ownerId && x.VoucherId == duplicate.Id) == 1,
            "Concurrent duplicate requests create exactly one ticket and one debit");

        // Different programs have different voucher locks; only the customer lock prevents overspending.
        var available = await Balance(ownerId);
        var a = Reward("LOYALTY-RACE-A", available); var b = Reward("LOYALTY-RACE-B", available);
        db.Voucher.AddRange(a, b); await db.SaveChangesAsync();
        var race = await Task.WhenAll(Redeem(ownerId, a), Redeem(ownerId, b));
        check(race.Count(x => x is null) == 1 && await Balance(ownerId) == 0
            && await db.VoucherKhachHang.CountAsync(x => x.KhachHangId == ownerId && (x.VoucherId == a.Id || x.VoucherId == b.Id)) == 1,
            "Simultaneous redemptions across programs cannot overspend a customer's balance");
        var emptyCatalog = WebUtility.HtmlDecode(await Get(customer, "/KhachHang/Vouchers"));
        check(emptyCatalog.Contains("Cần thêm") && emptyCatalog.Contains("disabled"), "Insufficient balance is explained and disables redemption in the catalog");

        await MigrationProof(connection, check);

        Voucher Reward(string code, int cost) => new() { Ma = code, DiemCanDoi = cost,
            KhuyenMai = new KhuyenMai { TenChuongTrinh = code, BatDau = now.AddDays(-1), KetThuc = now.AddDays(1),
                CanVoucher = true, PhamVi = PhamViUuDai.HoaDon, KieuGiam = KieuGiam.SoTien, GiaTri = 10000 } };
        Task<bool> Saved(int customerId, int voucherId) => db.VoucherKhachHang.AnyAsync(x => x.KhachHangId == customerId && x.VoucherId == voucherId);
        async Task<int> Balance(int id) { await using var context = new RestaurantDbContext(options); return await new CustomerHistoryService(context).BalanceAsync(id); }
        async Task<string?> Redeem(int id, Voucher voucher) { await using var context = new RestaurantDbContext(options); return await new CustomerVoucherService(context, new PromotionService(context)).RedeemAsync(id, voucher.Id, voucher.DiemCanDoi); }
        string Version<T>(T entity) where T : class => Convert.ToBase64String(db.Entry(entity).Property<byte[]>("RowVersion").CurrentValue!);
        async Task<string> Get(HttpClient client, string path) { using var response = await client.GetAsync(path); check(response.StatusCode == HttpStatusCode.OK, "Loyalty GET " + path); return await response.Content.ReadAsStringAsync(); }
        async Task Post(HttpClient client, string path, string html, Dictionary<string, string> data, HttpStatusCode status) {
            var values = new Dictionary<string, string>(data) { ["__RequestVerificationToken"] = hidden(html, "__RequestVerificationToken") };
            using var response = await client.PostAsync(path, new FormUrlEncodedContent(values));
            check(response.StatusCode == status, "Loyalty POST " + path + ": " + response.StatusCode);
        }
    }

    private static async Task MigrationProof(string original, Action<bool, string> check)
    {
        var connection = new SqlConnectionStringBuilder(original) { InitialCatalog = "RestaurantLoyaltyMigrationTests_" + Guid.NewGuid().ToString("N") };
        await using var db = new RestaurantDbContext(new DbContextOptionsBuilder<RestaurantDbContext>().UseSqlServer(connection.ConnectionString).Options);
        try {
            await db.GetService<IMigrator>().MigrateAsync("20261025134614_CampaignVoucherWalletAndCustomerPoints");
            var employee = new NhanVien { MaNhanVien = "LOYALTY-MIGRATION", HoTen = "Migration", NgayVaoLam = new DateOnly(2026, 1, 1) };
            var customer = new KhachHang { HoTen = "Migration", SoDienThoai = "0909990000", NgayDangKy = DateTimeOffset.UtcNow };
            var invoice = new HoaDon { MaHoaDon = "LOYALTY-MIGRATION", NhanVien = employee, KhachHang = customer,
                ThoiDiemLap = DateTimeOffset.UtcNow, TrangThai = TrangThaiHoaDon.DaThanhToan, TongTienHang = 100000 };
            db.HoaDon.Add(invoice); await db.SaveChangesAsync();
            // Insert using the old schema, before VoucherId and DiemCanDoi exist.
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [LichSuTichDiem] ([KhachHangId],[HoaDonId],[SoDiem],[SoTienTinhDiem],[ThoiDiem],[NoiDung]) VALUES ({customer.Id},{invoice.Id},10,100000,SYSDATETIMEOFFSET(),N'Legacy points')");
            await db.Database.ExecuteSqlRawAsync("INSERT INTO [KhuyenMai] ([TenChuongTrinh],[BatDau],[KetThuc],[PhamVi],[KieuGiam],[GiaTri],[GiaTriToiThieu],[ChoPhepKetHop],[ThuTuApDung],[CanVoucher],[DangSuDung]) VALUES ('LOYALTY-MIGRATION',SYSDATETIMEOFFSET(),DATEADD(day,1,SYSDATETIMEOFFSET()),'HoaDon','SoTien',10000,0,0,0,1,1)");
            await db.Database.ExecuteSqlRawAsync("INSERT INTO [Voucher] ([Ma],[KhuyenMaiId],[DangSuDung],[GiaTriToiThieu]) SELECT 'LOYALTY-LEGACY',Id,1,0 FROM [KhuyenMai]");
            var script = await File.ReadAllTextAsync(Path.Combine(Directory.GetCurrentDirectory(), "RestaurantManagement.API", "Scripts", "UpgradeLoyaltyVoucherRedemption.sql"));
            for (var i = 0; i < 2; i++)
                foreach (var batch in System.Text.RegularExpressions.Regex.Split(script, @"^\s*GO\s*$", System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    if (!string.IsNullOrWhiteSpace(batch)) await db.Database.ExecuteSqlRawAsync(batch);
            await db.Database.MigrateAsync();
            var ledger = await db.LichSuTichDiem.SingleAsync();
            check(ledger.SoDiem == 10 && ledger.HoaDonId == invoice.Id && ledger.VoucherId is null
                && await new CustomerHistoryService(db).BalanceAsync(customer.Id) == 10,
                "Loyalty upgrade and script retry preserve existing earned points and invoice links");
            check((await db.Voucher.SingleAsync()).DiemCanDoi == 0, "Existing voucher codes remain free after the upgrade");
        }
        finally { SqlConnection.ClearAllPools(); await db.Database.EnsureDeletedAsync(); }
    }
}
