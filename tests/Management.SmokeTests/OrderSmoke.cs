using System.Net;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Services;

namespace Management.SmokeTests;

public static class OrderSmoke
{
    public static async Task Run(RestaurantDbContext db, HttpClient admin, Func<HttpClient> newClient,
        Func<string, string, string> hidden, Action<bool, string> check)
    {
        async Task<string> Get(HttpClient client, string path)
        {
            using var response = await client.GetAsync(path);
            check(response.StatusCode == HttpStatusCode.OK, "Order GET " + path);
            return await response.Content.ReadAsStringAsync();
        }

        async Task Post(HttpClient client, string path, string form, Dictionary<string, string> fields)
        {
            fields["__RequestVerificationToken"] = hidden(form, "__RequestVerificationToken");
            using var response = await client.PostAsync(path, new FormUrlEncodedContent(fields));
            check(response.StatusCode == HttpStatusCode.Redirect, "Order POST " + path);
        }

        async Task<string> Version<T>(T entity) where T : class
        {
            await db.Entry(entity).ReloadAsync();
            return Convert.ToBase64String(db.Entry(entity).Property<byte[]>("RowVersion").CurrentValue!);
        }

        // 1. Prepare test category, dish, and sizes
        var category = await db.DanhMuc.FirstOrDefaultAsync(x => x.TenDanhMuc == "TEST-CAT-ORDER");
        if (category == null)
        {
            category = new DanhMuc { TenDanhMuc = "TEST-CAT-ORDER", DangSuDung = true };
            db.DanhMuc.Add(category);
            await db.SaveChangesAsync();
        }

        var dish = new MonAn
        {
            TenMon = "Bò né đặc biệt TEST",
            DanhMucId = category.Id,
            Loai = LoaiMon.MonLe,
            TrangThai = TrangThaiMon.DangPhucVu,
            DaDuyet = true,
            Sizes =
            {
                new MonAnSize { TenSize = "Nhỏ (S)", GiaBan = 65000, DangSuDung = true },
                new MonAnSize { TenSize = "Lớn (L)", GiaBan = 85000, DangSuDung = true }
            }
        };
        db.MonAn.Add(dish);
        await db.SaveChangesAsync();

        var sizeS = dish.Sizes.First(x => x.TenSize == "Nhỏ (S)");
        var sizeL = dish.Sizes.First(x => x.TenSize == "Lớn (L)");

        // 2. Test Takeaway Order Creation
        var createForm = await Get(admin, "/DonHang/Create");
        var createFormText = WebUtility.HtmlDecode(createForm);
        check(createFormText.Contains("Ghi nhận gọi món") && createFormText.Contains("Mang đi"), "Admin/Staff views order creation page");

        await Post(admin, "/DonHang/Create", createForm, new()
        {
            ["LoaiDonHang"] = "MangDi",
            ["TenNguoiNhan"] = "Anh Hoàng Thử Nghiệm",
            ["SoDienThoaiNhan"] = "0988112233",
            ["GhiChuDonHang"] = "Khách mang đi cần hộp xốp riêng",
            ["Items[0].MonAnId"] = dish.Id.ToString(),
            ["Items[0].MonAnSizeId"] = sizeS.Id.ToString(),
            ["Items[0].SoLuong"] = "2",
            ["Items[0].YeuCauCheBien"] = "Ít cay"
        });

        var takeawayBill = await db.HoaDon
            .Include(x => x.ChiTiet)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(x => x.TenNguoiNhan == "Anh Hoàng Thử Nghiệm");

        check(takeawayBill != null && takeawayBill.LoaiDonHang == LoaiDonHang.MangDi, "Takeaway order created successfully");
        check(takeawayBill!.TongTienHang == 130000m, "Takeaway order calculates correct total (2 * 65.000đ)");
        check(takeawayBill.ChiTiet.Count == 1, "Takeaway order creates dish detail");
        var takeawayItem = takeawayBill.ChiTiet.First();
        check(takeawayItem.TenSizeLucBan == "Nhỏ (S)" && takeawayItem.DonGia == 65000m && takeawayItem.TrangThai == TrangThaiCheBien.ChoCheBien,
            "Dish detail stores size snapshot and initial cooking state ChoCheBien");

        // 3. Test Delivery Order Creation
        await Post(admin, "/DonHang/Create", createForm, new()
        {
            ["LoaiDonHang"] = "GiaoHang",
            ["TenNguoiNhan"] = "Chị Mai Giao Tận Nơi",
            ["SoDienThoaiNhan"] = "0911223344",
            ["DiaChiGiaoHang"] = "Tòa nhà Landmark 81, P.22, Bình Thạnh",
            ["GhiChuDonHang"] = "Giao trước 12h trưa",
            ["Items[0].MonAnId"] = dish.Id.ToString(),
            ["Items[0].MonAnSizeId"] = sizeL.Id.ToString(),
            ["Items[0].SoLuong"] = "3",
            ["Items[0].YeuCauCheBien"] = "Nhiều rau thơm"
        });

        var deliveryBill = await db.HoaDon
            .Include(x => x.ChiTiet)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(x => x.TenNguoiNhan == "Chị Mai Giao Tận Nơi");

        check(deliveryBill != null && deliveryBill.LoaiDonHang == LoaiDonHang.GiaoHang
            && deliveryBill.DiaChiGiaoHang == "Tòa nhà Landmark 81, P.22, Bình Thạnh"
            && deliveryBill.TongTienHang == 255000m, "Delivery order created with address and correct total (3 * 85.000đ)");

        // 4. Test Table Order Creation for Occupied Table
        var area = new KhuVuc { TenKhuVuc = "ORDER-TEST-ZONE", Tang = 1 };
        db.KhuVuc.Add(area);
        await db.SaveChangesAsync();

        var table = new BanAn { MaBan = "TB-ORDER-01", KhuVucId = area.Id, SoChoNgoi = 4, TrangThai = TrangThaiBan.DangPhucVu };
        db.BanAn.Add(table);
        await db.SaveChangesAsync();

        var booking = new DatBan
        {
            MaDatBan = "DB-ORDER-TEST",
            HoTenLienHe = "Khách ăn tại bàn",
            SoDienThoaiLienHe = "0933445566",
            LaKhachTrucTiep = true,
            GioDen = DateTimeOffset.UtcNow,
            GioKetThucDuKien = DateTimeOffset.UtcNow.AddHours(2),
            SoNguoiLon = 2,
            TrangThai = TrangThaiDatBan.DaNhanBan,
            ThoiDiemNhanBan = DateTimeOffset.UtcNow,
            Ban = { new ChiTietDatBan { BanAnId = table.Id } }
        };
        db.DatBan.Add(booking);
        await db.SaveChangesAsync();

        await Post(admin, "/DonHang/Create", createForm, new()
        {
            ["LoaiDonHang"] = "TaiBan",
            ["BanAnId"] = table.Id.ToString(),
            ["Items[0].MonAnId"] = dish.Id.ToString(),
            ["Items[0].MonAnSizeId"] = sizeS.Id.ToString(),
            ["Items[0].SoLuong"] = "1",
            ["Items[0].YeuCauCheBien"] = "Không hành"
        });

        var tableBill = await db.HoaDon
            .Include(x => x.ChiTiet)
            .FirstOrDefaultAsync(x => x.DatBanId == booking.Id);

        check(tableBill != null && tableBill.LoaiDonHang == LoaiDonHang.TaiBan && tableBill.TongTienHang == 65000m,
            "Table order created for occupied table with correct bill link");

        // 5. Test Add Dishes (Gọi thêm món) to Table Bill
        var addDishesForm = await Get(admin, $"/DonHang/AddDishes/{tableBill!.Id}");
        check(addDishesForm.Contains("Gọi thêm món") && addDishesForm.Contains(tableBill.MaHoaDon),
            "Staff opens AddDishes view for existing bill");

        await Post(admin, $"/DonHang/AddDishes/{tableBill.Id}", addDishesForm, new()
        {
            ["items[0].MonAnId"] = dish.Id.ToString(),
            ["items[0].MonAnSizeId"] = sizeL.Id.ToString(),
            ["items[0].SoLuong"] = "2",
            ["items[0].YeuCauCheBien"] = "Thêm bánh mì giòn"
        });

        await db.Entry(tableBill).ReloadAsync();
        var tableDetails = await db.ChiTietHoaDon.Where(x => x.HoaDonId == tableBill.Id).ToListAsync();
        check(tableDetails.Count == 2 && tableBill.TongTienHang == 65000m + (2 * 85000m),
            "AddDishes appends new items and recalculates bill total correctly (235.000đ)");

        // 6. Test Kitchen KDS FIFO & Status Transitions
        using var cook = newClient();
        await Post(cook, "/admin", await Get(cook, "/admin"), new()
        {
            ["Email"] = "bep.demo@example.test",
            ["Password"] = "Demo@2026!"
        });

        var kdsView = await Get(cook, "/Bep");
        var kdsText = WebUtility.HtmlDecode(kdsView);
        check(kdsText.Contains("Bò né đặc biệt TEST") && kdsText.Contains("Nhỏ (S)"),
            "Kitchen KDS displays dish name and size snapshot");

        var firstDetail = tableDetails.First(x => x.MonAnSizeId == sizeS.Id);
        // Kitchen begins cooking: ChoCheBien -> DangCheBien
        await Post(cook, "/Bep/CapNhat", kdsView, new()
        {
            ["id"] = firstDetail.Id.ToString(),
            ["trangThai"] = "DangCheBien"
        });
        await db.Entry(firstDetail).ReloadAsync();
        check(firstDetail.TrangThai == TrangThaiCheBien.DangCheBien, "Kitchen updates status to DangCheBien");

        // Kitchen finishes cooking: DangCheBien -> SanSang
        await Post(cook, "/Bep/CapNhat", kdsView, new()
        {
            ["id"] = firstDetail.Id.ToString(),
            ["trangThai"] = "SanSang"
        });
        await db.Entry(firstDetail).ReloadAsync();
        check(firstDetail.TrangThai == TrangThaiCheBien.SanSang, "Kitchen updates status to SanSang (ready to serve)");

        // 7. Test Waiter Serves Dish: SanSang -> DaPhucVu
        using var waiter = newClient();
        await Post(waiter, "/admin", await Get(waiter, "/admin"), new()
        {
            ["Email"] = "boiban.demo@example.test",
            ["Password"] = "Demo@2026!"
        });

        var orderDetailView = await Get(waiter, $"/DonHang/Details/{tableBill.Id}");
        var orderDetailText = WebUtility.HtmlDecode(orderDetailView);
        check(orderDetailText.Contains("Đã xong (Sẵn sàng)"), "Waiter sees ready dish on order details");

        await Post(waiter, "/DonHang/CapNhatMon", orderDetailView, new()
        {
            ["id"] = firstDetail.Id.ToString(),
            ["hoaDonId"] = tableBill.Id.ToString(),
            ["trangThai"] = "DaPhucVu"
        });
        await db.Entry(firstDetail).ReloadAsync();
        check(firstDetail.TrangThai == TrangThaiCheBien.DaPhucVu, "Waiter marks dish as served (DaPhucVu)");

        // 8. Test Cancel Dish in ChoCheBien: Second detail cancelled by customer
        var secondDetail = tableDetails.First(x => x.MonAnSizeId == sizeL.Id);
        await Post(waiter, "/DonHang/CapNhatMon", orderDetailView, new()
        {
            ["id"] = secondDetail.Id.ToString(),
            ["hoaDonId"] = tableBill.Id.ToString(),
            ["trangThai"] = "DaHuy"
        });
        await db.Entry(secondDetail).ReloadAsync();
        await db.Entry(tableBill).ReloadAsync();
        check(secondDetail.TrangThai == TrangThaiCheBien.DaHuy, "Customer cancellation changes dish to DaHuy");
        check(tableBill.TongTienHang == 65000m, "Invoice total automatically adjusted after dish cancellation");

        // 9. Test Multi-Payment: Bank Transfer (QR) on Delivery Order
        using var cashier = newClient();
        await Post(cashier, "/admin", await Get(cashier, "/admin"), new()
        {
            ["Email"] = "thungan.demo@example.test",
            ["Password"] = "Demo@2026!"
        });

        var deliveryPaymentPage = await Get(cashier, $"/HoaDon/Details/{deliveryBill!.Id}");
        var deliveryPaymentText = WebUtility.HtmlDecode(deliveryPaymentPage);
        check(deliveryPaymentText.Contains("Chuyển khoản QR") && deliveryPaymentText.Contains("VietQR"),
            "Cashier views VietQR payment option on invoice page");

        await Post(cashier, "/HoaDon/ThanhToan", deliveryPaymentPage, new()
        {
            ["Id"] = deliveryBill.Id.ToString(),
            ["RowVersion"] = await Version(deliveryBill),
            ["PhuongThuc"] = "ChuyenKhoan",
            ["MaGiaoDich"] = "MB-TRANS-987654"
        });
        await db.Entry(deliveryBill).ReloadAsync();
        check(deliveryBill.TrangThai == TrangThaiHoaDon.DaThanhToan && deliveryBill.PhuongThucThanhToan == PhuongThucThanhToan.ChuyenKhoan
            && deliveryBill.MaGiaoDich == "MB-TRANS-987654", "Delivery order paid via bank transfer with transaction ref");

        // 10. Test Multi-Payment: POS Card on Takeaway Order
        var takeawayPaymentPage = await Get(cashier, $"/HoaDon/Details/{takeawayBill!.Id}");
        var takeawayPaymentText = WebUtility.HtmlDecode(takeawayPaymentPage);
        check(takeawayPaymentText.Contains("Thẻ ngân hàng / POS"), "Cashier views POS card payment option");

        await Post(cashier, "/HoaDon/ThanhToan", takeawayPaymentPage, new()
        {
            ["Id"] = takeawayBill.Id.ToString(),
            ["RowVersion"] = await Version(takeawayBill),
            ["PhuongThuc"] = "The",
            ["LoaiThe"] = "Visa",
            ["SoThe4SoCuoi"] = "1234",
            ["MaGiaoDich"] = "POS-AUTH-888"
        });
        await db.Entry(takeawayBill).ReloadAsync();
        check(takeawayBill.TrangThai == TrangThaiHoaDon.DaThanhToan && takeawayBill.PhuongThucThanhToan == PhuongThucThanhToan.The
            && takeawayBill.MaGiaoDich != null && takeawayBill.MaGiaoDich.Contains("Visa") && takeawayBill.MaGiaoDich.Contains("1234"),
            "Takeaway order paid via POS card with card details and approval code");

        // 11. Test Multi-Payment: Cash on Table Order with Change Calculation
        var tablePaymentPage = await Get(cashier, $"/HoaDon/Details/{tableBill.Id}");
        await Post(cashier, "/HoaDon/ThanhToan", tablePaymentPage, new()
        {
            ["Id"] = tableBill.Id.ToString(),
            ["RowVersion"] = await Version(tableBill),
            ["PhuongThuc"] = "TienMat",
            ["TienKhachDua"] = "100000",
            ["TienGiam"] = "5000"
        });
        await db.Entry(tableBill).ReloadAsync();
        check(tableBill.TrangThai == TrangThaiHoaDon.DaThanhToan && tableBill.PhuongThucThanhToan == PhuongThucThanhToan.TienMat
            && tableBill.TienKhachDua == 100000m && tableBill.TienThoiLai == 40000m,
            "Table order paid via Cash with change computed correctly (100.000đ - 60.000đ = 40.000đ)");

        // 12. Test Table Session Can Now Cleanly End via TableService
        // Table has all dishes served or cancelled, and bill is paid!
        var tableService = new TableService(db);
        var endError = await tableService.ChangeTable(table.Id, await Version(table), clean: false);
        check(endError == null, "Table service allows ending session now that dishes are served and bill is paid");
        await db.Entry(table).ReloadAsync();
        check(table.TrangThai == TrangThaiBan.CanDon, "Table transitioned to CanDon (cleaning needed)");

        var cleanError = await tableService.ChangeTable(table.Id, await Version(table), clean: true);
        check(cleanError == null, "Table service marks table ready after cleaning");
        await db.Entry(table).ReloadAsync();
        check(table.TrangThai == TrangThaiBan.SanSang, "Table returned to SanSang status");

        // 13. Test Printable Invoice Details
        var paidReceipt = await Get(admin, $"/HoaDon/Details/{tableBill.Id}");
        var paidReceiptText = WebUtility.HtmlDecode(paidReceipt);
        check(paidReceiptText.Contains("HÓA ĐƠN THANH TOÁN") && paidReceiptText.Contains("Bò né đặc biệt TEST")
            && paidReceiptText.Contains("Nhỏ (S)") && paidReceiptText.Contains("Tiền khách đưa"),
            "Printable receipt includes restaurant header, dish name, size snapshot, and cash details");
    }
}
