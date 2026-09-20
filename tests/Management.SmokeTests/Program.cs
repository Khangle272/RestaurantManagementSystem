using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;

// Run from the repository root. All writes target a newly generated test database.
var root = Directory.GetCurrentDirectory();
var database = "RestaurantCrudTests_" + Guid.NewGuid().ToString("N");
var connection = new SqlConnectionStringBuilder
{
    DataSource = Environment.GetEnvironmentVariable("CRUD_TEST_SQL_SERVER") ?? @".\SQLEXPRESS",
    InitialCatalog = database, IntegratedSecurity = true, TrustServerCertificate = true
};
var options = new DbContextOptionsBuilder<RestaurantDbContext>().UseSqlServer(connection.ConnectionString).Options;
var listener = new TcpListener(IPAddress.Loopback, 0);
listener.Start();
var port = ((IPEndPoint)listener.LocalEndpoint).Port;
listener.Stop();
var start = new ProcessStartInfo("dotnet")
{
    WorkingDirectory = Path.Combine(root, "RestaurantManagement.Web"),
    UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
};
start.ArgumentList.Add(Path.Combine(start.WorkingDirectory, "bin", "Debug", "net10.0", "RestaurantManagement.Web.dll"));
start.Environment["ConnectionStrings__DefaultConnection"] = connection.ConnectionString;
start.Environment["ASPNETCORE_URLS"] = $"http://127.0.0.1:{port}";
start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
using var process = new Process { StartInfo = start };
using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() })
{
    BaseAddress = new Uri($"http://127.0.0.1:{port}"), Timeout = TimeSpan.FromSeconds(10)
};
Task<string>? output = null;
Task<string>? errors = null;
var checks = 0;
try
{
    await StartWeb();

    foreach (var controller in new[] { "NhanVien", "BanAn", "DanhMuc" })
    {
        await Get($"/{controller}");
        await Get($"/{controller}/Create");
        using var missing = await client.GetAsync($"/{controller}/Edit/2147483647");
        Check(missing.StatusCode == HttpStatusCode.NotFound, controller + " missing ID returns 404");
        using var csrf = await client.PostAsync($"/{controller}/Create", new FormUrlEncodedContent(new Dictionary<string, string>()));
        Check(csrf.StatusCode == HttpStatusCode.BadRequest, controller + " rejects missing CSRF token");
    }

    await using var db = new RestaurantDbContext(options);
    var areaId = await db.KhuVuc.Where(x => x.DangSuDung).Select(x => x.Id).FirstAsync();
    var cases = new[]
    {
        (Controller: "NhanVien", Key: "MaNhanVien", Value: "TEST-NV", Fields: new Dictionary<string, string>
        {
            ["MaNhanVien"] = "TEST-NV", ["HoTen"] = "Nhân viên kiểm thử", ["SoDienThoai"] = "0901234567",
            ["ChucVu"] = "Phục vụ", ["NgayVaoLam"] = "2026-09-20", ["DangLamViec"] = "true"
        }),
        (Controller: "BanAn", Key: "MaBan", Value: "TEST-BAN", Fields: new Dictionary<string, string>
        {
            ["MaBan"] = "TEST-BAN", ["KhuVucId"] = areaId.ToString(), ["SoChoNgoi"] = "4", ["TrangThai"] = "SanSang"
        }),
        (Controller: "DanhMuc", Key: "TenDanhMuc", Value: "TEST-DM", Fields: new Dictionary<string, string>
        {
            ["TenDanhMuc"] = "TEST-DM", ["MoTa"] = "Mô tả kiểm thử", ["DangSuDung"] = "true"
        })
    };

    foreach (var item in cases)
    {
        var path = "/" + item.Controller;
        var invalid = new Dictionary<string, string>(item.Fields) { [item.Key] = "   " };
        await Post(path + "/Create", await Get(path + "/Create"), invalid, false, "Vui lòng nhập");
        await Post(path + "/Create", await Get(path + "/Create"), item.Fields, true);
        var id = item.Controller switch
        {
            "NhanVien" => await db.NhanVien.Where(x => x.MaNhanVien == item.Value).Select(x => x.Id).SingleAsync(),
            "BanAn" => await db.BanAn.Where(x => x.MaBan == item.Value).Select(x => x.Id).SingleAsync(),
            _ => await db.DanhMuc.Where(x => x.TenDanhMuc == item.Value).Select(x => x.Id).SingleAsync()
        };
        var duplicate = new Dictionary<string, string>(item.Fields) { [item.Key] = " " + item.Value + " " };
        await Post(path + "/Create", await Get(path + "/Create"), duplicate, false, "đã tồn tại");
        Check((await Get(path + "?search=" + item.Value + "&page=2147483647")).Contains(item.Value), item.Controller + " search and page clamp");

        var editPath = $"{path}/Edit/{id}";
        var originalForm = await Get(editPath);
        var edited = new Dictionary<string, string>(item.Fields) { [item.Key] = item.Value + "-EDIT", ["Id"] = id.ToString() };
        await Post(editPath, originalForm, edited, true);
        Check((await Get(path + "?search=" + item.Value + "-EDIT")).Contains(item.Value + "-EDIT"), item.Controller + " update persisted");
        var staleEdit = new Dictionary<string, string>(edited) { [item.Key] = item.Value + "-STALE" };
        await Post(editPath, originalForm, staleEdit, false, "đã bị thay đổi");
        await Post($"{path}/Delete/{id}", originalForm, new() { ["Id"] = id.ToString() }, false, "đã bị thay đổi");
        await Post(editPath, await Get(editPath), new(edited) { ["RowVersion"] = "invalid" }, false, "Phiên bản dữ liệu không hợp lệ");
        await Post($"{path}/Delete/{id}", await Get($"{path}/Delete/{id}"), new() { ["Id"] = id.ToString() }, true);
        using var removed = await client.GetAsync(editPath);
        Check(removed.StatusCode == HttpStatusCode.NotFound, item.Controller + " deletion persisted");
    }

    var categoryId = await db.MonAn.Select(x => x.DanhMucId).FirstAsync();
    var employeeId = await db.PhieuNhap.Select(x => x.NhanVienId).FirstAsync();
    await BlockDelete("DanhMuc", categoryId);
    await BlockDelete("NhanVien", employeeId);
    var tableId = await db.BanAn.Select(x => x.Id).FirstAsync();
    db.DatBan.Add(new DatBan
    {
        MaDatBan = "TEST-BOOKING", LaKhachTrucTiep = true, HoTenLienHe = "Khách kiểm thử",
        GioDen = DateTimeOffset.Now, GioKetThucDuKien = DateTimeOffset.Now.AddHours(1), SoNguoiLon = 1,
        Ban = new List<ChiTietDatBan> { new() { BanAnId = tableId } }
    });
    await db.SaveChangesAsync();
    await BlockDelete("BanAn", tableId);

    var tableFields = new Dictionary<string, string>(cases[1].Fields);
    foreach (var invalid in new[]
    {
        new Dictionary<string, string>(tableFields) { ["SoChoNgoi"] = "0" },
        new Dictionary<string, string>(tableFields) { ["KhuVucId"] = "2147483647" },
        new Dictionary<string, string>(tableFields) { ["TrangThai"] = "999" }
    })
        await Post("/BanAn/Create", await Get("/BanAn/Create"), invalid, false, "field-validation-error");
    await Post("/NhanVien/Create", await Get("/NhanVien/Create"), new(cases[0].Fields) { ["Email"] = "invalid" }, false, "Email không hợp lệ");
    await Post("/NhanVien/Create", await Get("/NhanVien/Create"), new(cases[0].Fields) { ["NgayVaoLam"] = "" }, false, "Vui lòng chọn ngày vào làm");

    // A linked login must survive profile edits and prevent deleting the employee.
    var login = new TaiKhoan { UserName = "test-login" };
    var linkedEmployee = new NhanVien
    {
        MaNhanVien = "TEST-LINK", HoTen = "Có tài khoản", SoDienThoai = "0901234567",
        ChucVu = "Phục vụ", NgayVaoLam = new DateOnly(2026, 9, 20), TaiKhoan = login
    };
    db.NhanVien.Add(linkedEmployee);
    var inactiveArea = new KhuVuc { TenKhuVuc = "Ngừng dùng", DangSuDung = false };
    db.KhuVuc.Add(inactiveArea);
    for (var index = 0; index < 12; index++)
        db.DanhMuc.Add(new DanhMuc { TenDanhMuc = $"PAGE-{index:D2}", DangSuDung = false });
    await db.SaveChangesAsync();
    var profilePath = $"/NhanVien/Edit/{linkedEmployee.Id}";
    await Post(profilePath, await Get(profilePath), new(cases[0].Fields)
    {
        ["Id"] = linkedEmployee.Id.ToString(), ["MaNhanVien"] = "TEST-LINK", ["HoTen"] = "Đã cập nhật",
        ["TaiKhoanId"] = "999999", ["DangLamViec"] = "false"
    }, true);
    await db.Entry(linkedEmployee).ReloadAsync();
    Check(linkedEmployee.TaiKhoanId == login.Id && !linkedEmployee.DangLamViec, "Profile edit preserves login and updates inactive state");
    await BlockDelete("NhanVien", linkedEmployee.Id);
    await Post("/BanAn/Create", await Get("/BanAn/Create"), new(tableFields) { ["KhuVucId"] = inactiveArea.Id.ToString() }, false, "Khu vực không tồn tại hoặc đã ngừng sử dụng");
    var filtered = await Get("/DanhMuc?search=PAGE-&trangThai=false&page=2");
    Check(Regex.Matches(filtered, "class=\"fw-medium\">PAGE-").Count == 2, "Pagination returns only the last two filtered records");
    Check(filtered.Contains("search=PAGE-") && filtered.Contains("trangThai=false"), "Pagination preserves filters");
    Check(!(await Get("/DanhMuc?search=PAGE-&trangThai=true")).Contains("class=\"fw-medium\">PAGE-"), "Active filter excludes inactive categories");

    var seedEmployeeId = await db.NhanVien.Where(x => x.MaNhanVien == "NV001").Select(x => x.Id).SingleAsync();
    await Post($"/NhanVien/Delete/{seedEmployeeId}", await Get($"/NhanVien/Delete/{seedEmployeeId}"), new() { ["Id"] = seedEmployeeId.ToString() }, true);
    process.Kill(entireProcessTree: true);
    await process.WaitForExitAsync();
    if (output != null) await output;
    if (errors != null) await errors;
    await StartWeb();
    Check(!await db.NhanVien.AnyAsync(x => x.MaNhanVien == "NV001"), "Deleted seed data stays deleted after restart");

    Console.WriteLine($"PASS: {checks} HTTP/database checks.");
}
catch
{
    if (!process.HasExited) process.Kill(entireProcessTree: true);
    if (output != null) Console.Error.WriteLine(await output);
    if (errors != null) Console.Error.WriteLine(await errors);
    throw;
}
finally
{
    if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); }
    SqlConnection.ClearAllPools();
    connection.InitialCatalog = "master";
    await using var sql = new SqlConnection(connection.ConnectionString);
    await sql.OpenAsync();
    // Only the GUID-named database created by this run can be dropped.
    if (!Regex.IsMatch(database, "^RestaurantCrudTests_[a-f0-9]{32}$")) throw new InvalidOperationException("Unsafe test database name");
    await using var command = sql.CreateCommand();
    command.CommandText = $"IF DB_ID(N'{database}') IS NOT NULL BEGIN ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]; END";
    await command.ExecuteNonQueryAsync();
    Console.WriteLine("Temporary test database removed.");
}

void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException("FAIL: " + name);
    checks++;
}

async Task StartWeb()
{
    process.Start();
    output = process.StandardOutput.ReadToEndAsync();
    errors = process.StandardError.ReadToEndAsync();
    var ready = false;
    for (var attempt = 0; attempt < 90 && !process.HasExited; attempt++)
    {
        try
        {
            using var response = await client.GetAsync("/DanhMuc");
            if (response.StatusCode == HttpStatusCode.OK) { ready = true; break; }
        }
        catch (HttpRequestException) { }
        await Task.Delay(500);
    }
    Check(ready, "Application starts with migrations and seed data");
}

async Task<string> Get(string path)
{
    using var response = await client.GetAsync(path);
    Check(response.StatusCode == HttpStatusCode.OK, "GET " + path);
    return await response.Content.ReadAsStringAsync();
}

string Hidden(string html, string name)
{
    var tag = Regex.Matches(html, "<input\\b[^>]*>").Select(x => x.Value)
        .FirstOrDefault(x => x.Contains($"name=\"{name}\""));
    return tag == null ? "" : WebUtility.HtmlDecode(Regex.Match(tag, "value=\"([^\"]*)\"").Groups[1].Value);
}

async Task Post(string path, string form, Dictionary<string, string> fields, bool success, string? expected = null)
{
    var body = new Dictionary<string, string>(fields) { ["__RequestVerificationToken"] = Hidden(form, "__RequestVerificationToken") };
    if (!body.ContainsKey("RowVersion") && Hidden(form, "RowVersion") is { Length: > 0 } version) body["RowVersion"] = version;
    using var response = await client.PostAsync(path, new FormUrlEncodedContent(body));
    Check(response.StatusCode == (success ? HttpStatusCode.Redirect : HttpStatusCode.OK), "POST " + path + " status " + response.StatusCode);
    if (expected != null)
        Check(WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()).Contains(expected), path + " displays " + expected);
}

async Task BlockDelete(string controller, int id)
{
    var path = $"/{controller}/Delete/{id}";
    await Post(path, await Get(path), new() { ["Id"] = id.ToString() }, false, "Không thể xóa bản ghi đang được sử dụng");
    await Get($"/{controller}/Edit/{id}");
}
