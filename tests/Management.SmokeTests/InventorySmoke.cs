using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;

public static class InventorySmoke
{
    public static async Task Run(RestaurantDbContext db, HttpClient admin, Func<HttpClient> newClient,
        Func<string, string, string> hidden, Action<bool, string> check)
    {
        async Task<string> Get(HttpClient client, string path)
        {
            using var response = await client.GetAsync(path);
            check(response.StatusCode == HttpStatusCode.OK, "Inventory GET " + path);
            return await response.Content.ReadAsStringAsync();
        }

        async Task Post(HttpClient client, string path, string form, Dictionary<string, string> fields, bool expectRedirect = true)
        {
            fields["__RequestVerificationToken"] = hidden(form, "__RequestVerificationToken");
            using var response = await client.PostAsync(path, new FormUrlEncodedContent(fields));
            if (expectRedirect)
            {
                check(response.StatusCode == HttpStatusCode.Redirect, "Inventory POST (Redirect) " + path + $" got {response.StatusCode}");
            }
            else
            {
                check(response.StatusCode == HttpStatusCode.OK, "Inventory POST (OK rejection) " + path + $" got {response.StatusCode}");
            }
        }

        Console.WriteLine("-> Running InventorySmoke: Supplier, NhapKho, XuatKho, ThanhLy, TonKho, BaoCao...");

        // 1. Check navigation access for Kho role
        using var khoUser = newClient();
        var khoLoginForm = await Get(khoUser, "/admin");
        using (var loginResp = await khoUser.PostAsync("/admin", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = hidden(khoLoginForm, "__RequestVerificationToken"),
            ["Email"] = "kho.demo@example.test",
            ["Password"] = "Demo@2026!"
        })))
        {
            check(loginResp.StatusCode == HttpStatusCode.Redirect, "Warehouse staff can log in");
        }

        var khoTonKhoHtml = await Get(khoUser, "/Kho/TonKho");
        check(khoTonKhoHtml.Contains("Theo dõi Tồn kho & Cảnh báo"), "Warehouse staff can access /Kho/TonKho");
        await Get(khoUser, "/NhapKho");
        await Get(khoUser, "/XuatKho");
        await Get(khoUser, "/ThanhLy");
        await Get(khoUser, "/Kho/BaoCao");
        await Get(khoUser, "/NhaCungCap");

        // 2. Test Nhà Cung Cấp CRUD
        var nccIndexHtml = await Get(admin, "/NhaCungCap");
        check(nccIndexHtml.Contains("Quản lý Nhà cung cấp"), "Admin can access /NhaCungCap");

        var nccCreateForm = await Get(admin, "/NhaCungCap/Create");
        var testSupplierName = "Công ty TNHH Cung Ứng Test " + Guid.NewGuid().ToString("N")[..6];
        var testSupplierPhone = "098" + Random.Shared.Next(1000000, 9999999);

        await Post(admin, "/NhaCungCap/Create", nccCreateForm, new()
        {
            ["TenNhaCungCap"] = testSupplierName,
            ["SoDienThoai"] = testSupplierPhone,
            ["Email"] = "test.supplier@example.test",
            ["DiaChi"] = "Khu Công Nghiệp Tân Bình",
            ["MaSoThue"] = "0399887766",
            ["NguoiLienHe"] = "Anh Long",
            ["DangSuDung"] = "true"
        });

        var createdNcc = await db.NhaCungCap.SingleOrDefaultAsync(x => x.TenNhaCungCap == testSupplierName);
        check(createdNcc != null && createdNcc.SoDienThoai == testSupplierPhone, "Supplier created successfully in database");

        // Duplicate prevention test
        var nccDuplicateForm = await Get(admin, "/NhaCungCap/Create");
        await Post(admin, "/NhaCungCap/Create", nccDuplicateForm, new()
        {
            ["TenNhaCungCap"] = testSupplierName,
            ["SoDienThoai"] = "0911223344"
        }, expectRedirect: false);

        // Toggle status test
        var toggleForm = await Get(admin, "/NhaCungCap");
        await Post(admin, $"/NhaCungCap/ToggleStatus/{createdNcc!.Id}", toggleForm, new());
        await db.Entry(createdNcc).ReloadAsync();
        check(!createdNcc.DangSuDung, "Supplier DangSuDung successfully toggled to false");

        // Toggle back to active
        await Post(admin, $"/NhaCungCap/ToggleStatus/{createdNcc.Id}", toggleForm, new());
        await db.Entry(createdNcc).ReloadAsync();
        check(createdNcc.DangSuDung, "Supplier DangSuDung toggled back to true");

        // 3. Test Nhập Kho Transaction (Stock increment & Price update)
        var testIngredient = await db.NguyenLieu.FirstAsync(x => x.DangSuDung);
        var initialStock = testIngredient.SoLuongTon;
        var newImportPrice = 45000m;
        var importQty = 50m;

        var nhapKhoCreateForm = await Get(admin, "/NhapKho/Create");
        await Post(admin, "/NhapKho/Create", nhapKhoCreateForm, new()
        {
            ["MaNhaCungCap"] = createdNcc.Id.ToString(),
            ["NgayNhap"] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm"),
            ["SoHoaDon"] = "HD-TEST-999",
            ["GhiChu"] = "Kiểm thử nhập kho tự động",
            ["ChiTiets[0].MaNguyenLieu"] = testIngredient.Id.ToString(),
            ["ChiTiets[0].SoLuong"] = importQty.ToString(),
            ["ChiTiets[0].DonGiaNhap"] = newImportPrice.ToString(),
            ["ChiTiets[0].SoLo"] = "LOT-2026-X"
        });

        await db.Entry(testIngredient).ReloadAsync();
        check(testIngredient.SoLuongTon == initialStock + importQty,
            $"Nhập kho transaction increases SoLuongTon: expected {initialStock + importQty}, got {testIngredient.SoLuongTon}");
        check(testIngredient.DonGia == newImportPrice,
            $"Nhập kho updates DonGia: expected {newImportPrice}, got {testIngredient.DonGia}");

        var latestPhieuNhap = await db.PhieuNhap
            .Include(x => x.ChiTiet)
            .OrderByDescending(x => x.Id)
            .FirstAsync();
        check(latestPhieuNhap.NhaCungCapId == createdNcc.Id, "PhieuNhap linked to correct supplier");
        check(latestPhieuNhap.TongTien == importQty * newImportPrice, "PhieuNhap TongTien calculated correctly");

        var nhapKhoDetailsHtml = await Get(admin, $"/NhapKho/Details/{latestPhieuNhap.Id}");
        check(nhapKhoDetailsHtml.Contains(latestPhieuNhap.MaPhieu), "NhapKho Details renders receipt number");

        // 4. Test Xuất Kho Transaction (Anti-negative stock check & Stock reduction)
        var xuatKhoCreateForm = await Get(admin, "/XuatKho/Create");

        // A. Attempt to export more than available stock -> Must be rejected
        var excessQty = testIngredient.SoLuongTon + 1000m;
        await Post(admin, "/XuatKho/Create", xuatKhoCreateForm, new()
        {
            ["BoPhanNhan"] = "Bếp nóng",
            ["NguoiNhan"] = "Bếp trưởng",
            ["NgayXuat"] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm"),
            ["LyDoXuat"] = "Phục vụ chế biến",
            ["ChiTiets[0].MaNguyenLieu"] = testIngredient.Id.ToString(),
            ["ChiTiets[0].SoLuong"] = excessQty.ToString()
        }, expectRedirect: false);

        await db.Entry(testIngredient).ReloadAsync();
        check(testIngredient.SoLuongTon == initialStock + importQty, "Stock untouched after invalid excess export attempt");

        // B. Valid export
        var validExportQty = 15m;
        var currentStockBeforeExport = testIngredient.SoLuongTon;
        await Post(admin, "/XuatKho/Create", xuatKhoCreateForm, new()
        {
            ["BoPhanNhan"] = "Bếp nóng",
            ["NguoiNhan"] = "Bếp trưởng",
            ["NgayXuat"] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm"),
            ["LyDoXuat"] = "Phục vụ chế biến",
            ["ChiTiets[0].MaNguyenLieu"] = testIngredient.Id.ToString(),
            ["ChiTiets[0].SoLuong"] = validExportQty.ToString()
        });

        await db.Entry(testIngredient).ReloadAsync();
        check(testIngredient.SoLuongTon == currentStockBeforeExport - validExportQty,
            $"Xuất kho transaction reduces SoLuongTon: expected {currentStockBeforeExport - validExportQty}, got {testIngredient.SoLuongTon}");

        var latestPhieuXuat = await db.PhieuXuat.OrderByDescending(x => x.Id).FirstAsync();
        var xuatKhoDetailsHtml = await Get(admin, $"/XuatKho/Details/{latestPhieuXuat.Id}");
        check(xuatKhoDetailsHtml.Contains(latestPhieuXuat.MaPhieu), "XuatKho Details renders export slip");

        // 5. Test Thanh Lý Transaction (Damage loss tracking & Stock reduction)
        var thanhLyCreateForm = await Get(admin, "/ThanhLy/Create");
        var currentStockBeforeDisposal = testIngredient.SoLuongTon;
        var disposalQty = 5m;

        await Post(admin, "/ThanhLy/Create", thanhLyCreateForm, new()
        {
            ["NgayThanhLy"] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm"),
            ["LyDoThanhLy"] = "Hết hạn sử dụng",
            ["GhiChu"] = "Kiểm thử tiêu hủy",
            ["ChiTiets[0].MaNguyenLieu"] = testIngredient.Id.ToString(),
            ["ChiTiets[0].SoLuong"] = disposalQty.ToString()
        });

        await db.Entry(testIngredient).ReloadAsync();
        check(testIngredient.SoLuongTon == currentStockBeforeDisposal - disposalQty,
            $"Thanh lý transaction reduces SoLuongTon: expected {currentStockBeforeDisposal - disposalQty}, got {testIngredient.SoLuongTon}");

        var latestPhieuThanhLy = await db.PhieuThanhLy.OrderByDescending(x => x.Id).FirstAsync();
        check(latestPhieuThanhLy.TongTienThietHai == disposalQty * testIngredient.DonGia,
            "Thanh lý calculates damage loss value correctly");

        var thanhLyDetailsHtml = await Get(admin, $"/ThanhLy/Details/{latestPhieuThanhLy.Id}");
        check(thanhLyDetailsHtml.Contains(latestPhieuThanhLy.MaPhieu), "ThanhLy Details renders disposal report");

        // 6. Test Tồn Kho & Cảnh Báo UI
        var tonKhoHtml = await Get(admin, "/Kho/TonKho");
        check(tonKhoHtml.Contains("Tổng Mặt Hàng") && tonKhoHtml.Contains("Tổng Giá Trị Tồn Kho"),
            "TonKho page renders KPI statistics");

        // 7. Test Báo Cáo Nhập - Xuất - Tồn and CSV Export
        var baoCaoHtml = await Get(admin, "/Kho/BaoCao?preset=thangnay");
        check(baoCaoHtml.Contains("BÁO CÁO NHẬP - XUẤT - TỒN KHO") || baoCaoHtml.Contains("Báo Cáo Nhập - Xuất - Tồn"),
            "BaoCao page renders correctly");
        check(baoCaoHtml.Contains("Tồn Đầu Kỳ") && baoCaoHtml.Contains("Tồn Cuối Kỳ"),
            "BaoCao renders key metric sections");

        // CSV export verification
        using var csvResponse = await admin.GetAsync("/Kho/ExportCsv?preset=thangnay");
        check(csvResponse.StatusCode == HttpStatusCode.OK, "CSV export returns HTTP 200");
        check(csvResponse.Content.Headers.ContentType?.MediaType == "text/csv", "CSV export content type is text/csv");

        var csvBytes = await csvResponse.Content.ReadAsByteArrayAsync();
        check(csvBytes.Length >= 3 && csvBytes[0] == 0xEF && csvBytes[1] == 0xBB && csvBytes[2] == 0xBF,
            "CSV export has UTF-8 BOM for Microsoft Excel Vietnamese compatibility");

        var csvText = System.Text.Encoding.UTF8.GetString(csvBytes);
        check(csvText.Contains("BÁO CÁO") || csvText.Contains("NHẬP - XUẤT - TỒN"), "CSV header matches title");
        check(csvText.Contains("TỔNG") || csvText.Contains("CỘNG") || csvText.Contains("NL001"), "CSV footer contains summary row");

        // 8. Test AJAX Quick-Add Endpoints
        // 8.1. QuickAdd Category
        var qCategoryName = "Danh mục QuickAdd " + Guid.NewGuid().ToString("N")[..6];
        var qCategoryResp = await admin.PostAsJsonAsync("/DanhMuc/QuickCreate", new { tenDanhMuc = qCategoryName, moTa = "Mô tả test" });
        check(qCategoryResp.StatusCode == HttpStatusCode.OK, "QuickCreate Category returns 200 OK");
        var qCategoryJson = await qCategoryResp.Content.ReadFromJsonAsync<JsonElement>();
        check(qCategoryJson.GetProperty("success").GetBoolean(), "QuickCreate Category success is true");
        var qCatId = qCategoryJson.GetProperty("id").GetInt32();
        check(await db.DanhMuc.AnyAsync(x => x.Id == qCatId && x.TenDanhMuc == qCategoryName), "QuickCreate Category saved to db");

        // 8.2. QuickAdd Ingredient
        var qIngredientName = "Nguyên liệu QuickAdd " + Guid.NewGuid().ToString("N")[..6];
        var qIngredientResp = await admin.PostAsJsonAsync("/NguyenLieu/QuickCreate", new { tenNguyenLieu = qIngredientName, donViTinh = "kg", donGia = 65000m, dinhMucToiThieu = 2m });
        check(qIngredientResp.StatusCode == HttpStatusCode.OK, "QuickCreate Ingredient returns 200 OK");
        var qIngJson = await qIngredientResp.Content.ReadFromJsonAsync<JsonElement>();
        check(qIngJson.GetProperty("success").GetBoolean(), "QuickCreate Ingredient success is true");
        var qIngId = qIngJson.GetProperty("id").GetInt32();
        check(await db.NguyenLieu.AnyAsync(x => x.Id == qIngId && x.TenNguyenLieu == qIngredientName), "QuickCreate Ingredient saved to db");

        // 8.3. QuickAdd Supplier
        var qSupplierName = "NCC QuickAdd " + Guid.NewGuid().ToString("N")[..6];
        var qSupplierPhone = "097" + Random.Shared.Next(1000000, 9999999);
        var qSupplierResp = await admin.PostAsJsonAsync("/NhaCungCap/QuickCreate", new { tenNhaCungCap = qSupplierName, soDienThoai = qSupplierPhone });
        check(qSupplierResp.StatusCode == HttpStatusCode.OK, "QuickCreate Supplier returns 200 OK");
        var qSuppJson = await qSupplierResp.Content.ReadFromJsonAsync<JsonElement>();
        check(qSuppJson.GetProperty("success").GetBoolean(), "QuickCreate Supplier success is true");
        var qSuppId = qSuppJson.GetProperty("id").GetInt32();
        check(await db.NhaCungCap.AnyAsync(x => x.Id == qSuppId && x.TenNhaCungCap == qSupplierName), "QuickCreate Supplier saved to db");

        // 9. Test Supplier-Ingredient Many-to-Many Linking
        var linkResp = await admin.PostAsJsonAsync($"/NguyenLieu/LinkSuppliersToIngredient?ingredientId={qIngId}", new[]
        {
            new { maNhaCungCap = qSuppId, donGiaCungUng = 62000m, maHangNCC = "MH-01", ghiChu = "Thỏa thuận test" }
        });
        check(linkResp.StatusCode == HttpStatusCode.OK, "LinkSuppliersToIngredient returns 200 OK");
        check(await db.NhaCungCapNguyenLieus.AnyAsync(x => x.MaNguyenLieu == qIngId && x.MaNhaCungCap == qSuppId && x.DonGiaCungUng == 62000m), "NhaCungCapNguyenLieu composite relation saved in db");

        // Verify GetSuppliersByIngredient
        var getSuppResp = await admin.GetAsync($"/NguyenLieu/GetSuppliersByIngredient?ingredientId={qIngId}");
        check(getSuppResp.StatusCode == HttpStatusCode.OK, "GetSuppliersByIngredient returns 200 OK");
        var getSuppJson = await getSuppResp.Content.ReadFromJsonAsync<JsonElement>();
        check(getSuppJson.GetProperty("suppliers").GetArrayLength() == 1, "GetSuppliersByIngredient returns linked supplier");

        // 10. Test Multi-size BOM Configuration (DinhMucTheoSize)
        var testDish = await db.MonAn.Include(x => x.Sizes).FirstAsync(x => x.Sizes.Count >= 2);
        var dinhmucHtml = await Get(admin, $"/MonAn/DinhMuc/{testDish.Id}");
        check(dinhmucHtml.Contains("Định Mức Đa Kích Cỡ (BOM)") && dinhmucHtml.Contains("Size"), "DinhMuc page rendered successfully");

        var size1 = testDish.Sizes.First();
        var size2 = testDish.Sizes.Skip(1).First();
        var bomForm = dinhmucHtml;
        await Post(admin, "/MonAn/SaveDinhMucTheoSize", bomForm, new()
        {
            ["MonAnId"] = testDish.Id.ToString(),
            ["Sizes[0].MonAnSizeId"] = size1.Id.ToString(),
            ["Sizes[0].TenSize"] = size1.TenSize,
            ["Sizes[0].GiaBan"] = size1.GiaBan.ToString(),
            ["Sizes[0].DangSuDung"] = "true",
            ["Sizes[0].Items[0].NguyenLieuId"] = qIngId.ToString(),
            ["Sizes[0].Items[0].SoLuong"] = "0.25",
            ["Sizes[1].MonAnSizeId"] = size2.Id.ToString(),
            ["Sizes[1].TenSize"] = size2.TenSize,
            ["Sizes[1].GiaBan"] = size2.GiaBan.ToString(),
            ["Sizes[1].DangSuDung"] = "true",
            ["Sizes[1].Items[0].NguyenLieuId"] = qIngId.ToString(),
            ["Sizes[1].Items[0].SoLuong"] = "0.50"
        });

        check(await db.DinhMucMon.AnyAsync(x => x.MonAnId == testDish.Id && x.MaKichCo == size1.Id && x.NguyenLieuId == qIngId && x.SoLuong == 0.25m), "BOM Size 1 saved with composite key");
        check(await db.DinhMucMon.AnyAsync(x => x.MonAnId == testDish.Id && x.MaKichCo == size2.Id && x.NguyenLieuId == qIngId && x.SoLuong == 0.50m), "BOM Size 2 saved with composite key");

        Console.WriteLine("-> InventorySmoke passed all validations!");
    }
}
