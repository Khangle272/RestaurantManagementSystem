using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Security;

internal static class MenuSmoke
{
    public static async Task Run(RestaurantDbContext db, HttpClient admin, Func<HttpClient> newClient,
        Func<string, string, string> hidden, Action<bool, string> check, string connection)
    {
        async Task<HttpResponseMessage> Submit(HttpClient client, string path, string form, Dictionary<string, string> fields)
        {
            fields = new(fields) { ["__RequestVerificationToken"] = hidden(form, "__RequestVerificationToken") };
            return await client.PostAsync(path, new FormUrlEncodedContent(fields));
        }
        using var guest = newClient();
        using var kitchen = newClient();
        var publicHome = await guest.GetStringAsync("/");
        check(publicHome.Contains("href=\"/DatBan\""), "Anonymous customer still sees booking actions");
        check(!(await admin.GetStringAsync("/")).Contains("href=\"/DatBan"), "Admin has no customer booking actions");
        var login = await kitchen.GetStringAsync("/admin");
        using (var response = await Submit(kitchen, "/admin", login, new()
        { ["Email"] = "bep.demo@example.test", ["Password"] = "Demo@2026!" }))
            check(response.StatusCode == HttpStatusCode.Redirect, "Kitchen manager logs in");
        var create = await kitchen.GetStringAsync("/MonAn/Create");
        check(!create.Contains(".GiaBan") && !create.Contains("name=\"DaDuyet\"")
            && !create.Contains("name=\"TrangThai\"") && !create.Contains("value=\"Set\""), "Kitchen form exposes no price, publish, status or combo fields");
        var category = await db.DanhMuc.Where(x => x.DangSuDung).Select(x => x.Id).FirstAsync();
        var fields = new Dictionary<string, string> {
            ["TenMon"] = "TEST-KITCHEN-DRAFT", ["DanhMucId"] = category.ToString(), ["Loai"] = "MonLe",
            ["Sizes[0].Id"] = "0", ["Sizes[0].TenSize"] = "M", ["Sizes[0].DangSuDung"] = "true",
            ["Sizes[0].GiaBan"] = "999999", ["DaDuyet"] = "true", ["LaMonNoiBat"] = "true", ["TrangThai"] = "NgungKinhDoanh"
        };
        using (var response = await Submit(kitchen, "/MonAn/Create", create, fields))
            check(response.StatusCode == HttpStatusCode.Redirect, "Kitchen saves technical draft");
        var draft = await db.MonAn.Include(x => x.Sizes).SingleAsync(x => x.TenMon == "TEST-KITCHEN-DRAFT");
        check(!draft.DaDuyet && !draft.LaMonNoiBat && draft.TrangThai == TrangThaiMon.DangPhucVu
            && draft.Sizes.Single().GiaBan == 0, "Forged kitchen price/publish/marketing/status cannot grant business rights");
        using (var response = await guest.GetAsync("/Home/MonAn/" + draft.Id))
            check(response.StatusCode == HttpStatusCode.NotFound, "Draft detail is not public");
        check(!(await guest.GetStringAsync("/")).Contains("TEST-KITCHEN-DRAFT"), "Draft not listed on public homepage");
        var editPath = "/MonAn/Edit/" + draft.Id;
        fields["Id"] = draft.Id.ToString(); fields["Sizes[0].Id"] = draft.Sizes.Single().Id.ToString();
        fields["Sizes[0].GiaBan"] = "0"; fields["TrangThai"] = "DangPhucVu";
        var approvalForm = await admin.GetStringAsync(editPath);
        fields["RowVersion"] = hidden(approvalForm, "RowVersion");
        using (var response = await Submit(admin, editPath, approvalForm, fields))
            check(response.StatusCode == HttpStatusCode.OK && WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()).Contains("giá lớn hơn 0"),
                "Admin cannot publish a zero-price active size");
        fields["Sizes[0].GiaBan"] = "120000";
        using (var response = await Submit(admin, editPath, approvalForm, fields))
            check(response.StatusCode == HttpStatusCode.Redirect, "Admin sets price and approves draft");
        await db.Entry(draft).ReloadAsync(); await db.Entry(draft.Sizes.Single()).ReloadAsync();
        check(draft.DaDuyet && draft.Sizes.Single().GiaBan == 120000, "Approval and price saved together");
        var detail = await guest.GetStringAsync("/Home/MonAn/" + draft.Id);
        check(detail.Contains("TEST-KITCHEN-DRAFT") && detail.Contains("href=\"/DatBan\""), "Approved dish becomes public with customer booking action");
        check(!(await kitchen.GetStringAsync("/Home/MonAn/" + draft.Id)).Contains("href=\"/DatBan"), "Staff dish detail has no customer booking action");
        var kitchenEdit = await kitchen.GetStringAsync(editPath);
        fields["RowVersion"] = hidden(kitchenEdit, "RowVersion"); fields["Sizes[0].GiaBan"] = "777777"; fields["MoTa"] = "Cập nhật kỹ thuật";
        using (var response = await Submit(kitchen, editPath, kitchenEdit, fields))
            check(response.StatusCode == HttpStatusCode.Redirect, "Kitchen edits technical content");
        await db.Entry(draft).ReloadAsync(); await db.Entry(draft.Sizes.Single()).ReloadAsync();
        check(!draft.DaDuyet && draft.Sizes.Single().GiaBan == 120000, "Kitchen edit requires reapproval and preserves Admin price");
        var kitchenIndex = WebUtility.HtmlDecode(await kitchen.GetStringAsync("/MonAn?search=TEST-KITCHEN-DRAFT"));
        check(kitchenIndex.Contains("Chờ duyệt") && !kitchenIndex.Contains("120.000")
            && !kitchenIndex.Contains("120,000") && !kitchenIndex.Contains("/MonAn/Delete/"), "Kitchen preparation listing omits money and delete controls");
        using (var response = await kitchen.GetAsync("/MonAn/Delete/" + draft.Id))
            check(response.StatusCode == HttpStatusCode.Redirect, "Kitchen cannot open deletion endpoint");
        using (var response = await Submit(kitchen, "/MonAn/Delete/" + draft.Id, kitchenEdit, new() { ["Id"] = draft.Id.ToString() }))
            check(response.StatusCode == HttpStatusCode.Redirect, "Kitchen cannot POST deletion endpoint");
        using (var response = await Submit(kitchen, "/MonAn/Create", create, new(fields) { ["Loai"] = "Set" }))
            check(response.StatusCode == HttpStatusCode.Redirect, "Kitchen cannot create priced combo through forged POST");
        check(await db.MonAn.CountAsync(x => x.TenMon == "TEST-KITCHEN-DRAFT") == 1, "Rejected combo does not write extra dish");

        // Exercise cleanup on this disposable database; unrelated users and business records must survive.
        var legacyRole = new IdentityRole<int>("ThucDon") { NormalizedName = "THUCDON" };
        var otherLegacyRole = new IdentityRole<int>("DanhMucMon") { NormalizedName = "DANHMUCMON" };
        var legacy = new TaiKhoan { UserName = "retired@example.test", NormalizedUserName = "RETIRED@EXAMPLE.TEST",
            Email = "retired@example.test", NormalizedEmail = "RETIRED@EXAMPLE.TEST", SecurityStamp = Guid.NewGuid().ToString(),
            PasswordHash = await db.Users.Where(x => x.Email == "bep.demo@example.test").Select(x => x.PasswordHash).SingleAsync() };
        db.AddRange(legacyRole, otherLegacyRole, legacy); await db.SaveChangesAsync();
        var employee = new NhanVien { MaNhanVien = "TEST-RETIRED", HoTen = "Vai trò mẫu cũ", SoDienThoai = "0998887777",
            ChucVu = "ThucDon", TaiKhoanId = legacy.Id, NgayVaoLam = new DateOnly(2026, 1, 1) };
        db.AddRange(employee, new IdentityUserRole<int> { UserId = legacy.Id, RoleId = legacyRole.Id }); await db.SaveChangesAsync();
        using var retiredClient = newClient();
        using (var response = await Submit(retiredClient, "/admin", await retiredClient.GetStringAsync("/admin"), new()
        { ["Email"] = legacy.Email, ["Password"] = "Demo@2026!" }))
            check(response.StatusCode == HttpStatusCode.OK, "Retired role cannot authenticate before cleanup");
        var history = new HoaDon { MaHoaDon = "TEST-RETIRED-HISTORY", NhanVienId = employee.Id,
            ThoiDiemLap = DateTimeOffset.UtcNow, TrangThai = TrangThaiHoaDon.DaHuy,
            ChiTiet = [new ChiTietHoaDon { MonAnId = draft.Id, MonAnSizeId = draft.Sizes.Single().Id,
                TenMonLucBan = "TEST-CANCELED-KITCHEN-LINE", SoLuong = 1, DonGia = 120000, TrangThai = TrangThaiCheBien.ChoCheBien }] };
        db.HoaDon.Add(history); await db.SaveChangesAsync();
        var kitchenQueue = await kitchen.GetStringAsync("/Bep");
        check(!kitchenQueue.Contains("TEST-CANCELED-KITCHEN-LINE"), "Kitchen queue excludes canceled invoices");
        using (var response = await Submit(kitchen, "/Bep/CapNhat", kitchenEdit, new()
        { ["id"] = history.ChiTiet.Single().Id.ToString(), ["trangThai"] = "DangCheBien" }))
            check(response.StatusCode == HttpStatusCode.BadRequest, "Direct kitchen update cannot resume canceled invoice");
        using var services = new ServiceCollection().AddDbContext<RestaurantDbContext>(o => o.UseSqlServer(connection)).BuildServiceProvider();
        var blocked = false;
        try { await RetiredRoleCleanup.RunAsync(services); }
        catch (InvalidOperationException) { blocked = true; }
        check(blocked && await db.Users.AnyAsync(x => x.Id == legacy.Id) && await db.HoaDon.AnyAsync(x => x.Id == history.Id),
            "Cleanup aborts entire batch when a retired employee has business history");
        db.ChiTietHoaDon.RemoveRange(history.ChiTiet); db.HoaDon.Remove(history); await db.SaveChangesAsync();
        var usersBefore = await db.Users.CountAsync(); var dishesBefore = await db.MonAn.CountAsync();
        await RetiredRoleCleanup.RunAsync(services); await RetiredRoleCleanup.RunAsync(services);
        check(!await db.Roles.AnyAsync(x => x.Name == "ThucDon" || x.Name == "DanhMucMon")
            && !await db.Users.AnyAsync(x => x.Id == legacy.Id) && !await db.NhanVien.AnyAsync(x => x.Id == employee.Id),
            "Cleanup removes only unused retired roles, identities and linked profiles; rerun is idempotent");
        check(await db.Users.CountAsync() == usersBefore - 1 && await db.MonAn.CountAsync() == dishesBefore,
            "Cleanup preserves other accounts and menu data");
    }
}
