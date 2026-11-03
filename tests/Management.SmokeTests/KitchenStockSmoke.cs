using System.Net;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Services;

namespace Management.SmokeTests;

// Bếp chỉ thấy món trong ngày; gọi món khi thiếu nguyên liệu vẫn lưu đơn nhưng phải báo cho thu ngân.
public static class KitchenStockSmoke
{
    public static async Task Run(RestaurantDbContext db, HttpClient admin, Func<HttpClient> newClient,
        Func<string, string, string> hidden, Action<bool, string> check)
    {
        async Task<string> Get(HttpClient client, string path)
        {
            var response = await client.GetAsync(path);
            check(response.StatusCode == HttpStatusCode.OK, "Kitchen GET " + path + ": " + response.StatusCode);
            return WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        }

        var staffId = await db.NhanVien.Where(x => x.DangLamViec).Select(x => x.Id).FirstAsync();
        var ingredient = new NguyenLieu { TenNguyenLieu = "STOCK-THỊT-BÒ", DonViTinh = "kg", SoLuongTon = 0.5m, DonGia = 200000m, NguongCanhBao = 0.2m };
        var dish = new MonAn { TenMon = "STOCK-BÒ-LÚC-LẮC", DanhMuc = new DanhMuc { TenDanhMuc = "STOCK-MENU", DangSuDung = true },
            Loai = LoaiMon.MonLe, DaDuyet = true, TrangThai = TrangThaiMon.DangPhucVu,
            Sizes = { new MonAnSize { TenSize = "Vừa", GiaBan = 120000m, DangSuDung = true } } };
        db.AddRange(ingredient, dish); await db.SaveChangesAsync();
        var size = dish.Sizes.Single();
        db.DinhMucMon.Add(new DinhMucMon { MonAnId = dish.Id, MaKichCo = size.Id, NguyenLieuId = ingredient.Id, SoLuong = 0.4m });
        await db.SaveChangesAsync();

        // 1. Kitchen screen and automatic day closing.
        var startOfToday = KitchenDayClosing.StartOfToday;
        var bill = new HoaDon
        {
            MaHoaDon = "HD-KDS-DAY", LoaiDonHang = LoaiDonHang.MangDi, TenNguoiNhan = "KDS", NhanVienId = staffId,
            ThoiDiemLap = startOfToday.AddHours(-3), TrangThai = TrangThaiHoaDon.ChuaThanhToan, TongTienHang = 360000m,
            ChiTiet =
            {
                new ChiTietHoaDon { MonAnId = dish.Id, MonAnSizeId = size.Id, TenMonLucBan = "KDS-YESTERDAY-WAITING", SoLuong = 1, DonGia = 120000m,
                    TrangThai = TrangThaiCheBien.ChoCheBien, ThoiDiemGoi = startOfToday.AddHours(-3) },
                new ChiTietHoaDon { MonAnId = dish.Id, MonAnSizeId = size.Id, TenMonLucBan = "KDS-YESTERDAY-READY", SoLuong = 1, DonGia = 120000m,
                    TrangThai = TrangThaiCheBien.SanSang, ThoiDiemGoi = startOfToday.AddHours(-3) },
                new ChiTietHoaDon { MonAnId = dish.Id, MonAnSizeId = size.Id, TenMonLucBan = "KDS-TODAY-WAITING", SoLuong = 1, DonGia = 120000m,
                    TrangThai = TrangThaiCheBien.ChoCheBien, ThoiDiemGoi = DateTimeOffset.UtcNow }
            }
        };
        db.HoaDon.Add(bill); await db.SaveChangesAsync();
        var kitchen = await Get(admin, "/Bep");
        check(kitchen.Contains("KDS-TODAY-WAITING") && !kitchen.Contains("KDS-YESTERDAY-WAITING") && !kitchen.Contains("KDS-YESTERDAY-READY"),
            "Kitchen screen only shows dishes ordered today");
        await KitchenDayClosing.CloseBeforeAsync(db, startOfToday);
        db.ChangeTracker.Clear();
        var lines = await db.ChiTietHoaDon.AsNoTracking().Where(x => x.HoaDonId == bill.Id).ToDictionaryAsync(x => x.TenMonLucBan);
        check(lines["KDS-YESTERDAY-WAITING"].TrangThai == TrangThaiCheBien.DaHuy && lines["KDS-YESTERDAY-WAITING"].ThoiDiemHuy != null
            && lines["KDS-YESTERDAY-READY"].TrangThai == TrangThaiCheBien.DaPhucVu
            && lines["KDS-TODAY-WAITING"].TrangThai == TrangThaiCheBien.ChoCheBien,
            "Day closing cancels yesterday's uncooked dish, hands over the ready one and leaves today's order alone");
        var closedBill = await db.HoaDon.AsNoTracking().SingleAsync(x => x.Id == bill.Id);
        check(closedBill.TongTienHang == 240000m && closedBill.TrangThai == TrangThaiHoaDon.ChuaThanhToan,
            "Bill total drops the cancelled dish and the bill stays open for today's dish");

        // 2. Ordering with too little stock keeps the order but warns the cashier.
        var form = await Get(admin, "/DonHang/Create");
        using (var response = await admin.PostAsync("/DonHang/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = hidden(form, "__RequestVerificationToken"), ["LoaiDonHang"] = "MangDi",
            ["TenNguoiNhan"] = "STOCK-CUSTOMER", ["SoDienThoaiNhan"] = "0988000111",
            ["Items[0].MonAnId"] = dish.Id.ToString(), ["Items[0].MonAnSizeId"] = size.Id.ToString(), ["Items[0].SoLuong"] = "2"
        })))
        {
            check(response.StatusCode == HttpStatusCode.Redirect, "Order with short stock is still saved");
            var next = await Get(admin, response.Headers.Location!.OriginalString);
            check(next.Contains("Lưu ý kho") && next.Contains("STOCK-THỊT-BÒ"), "Cashier sees which ingredient ran short");
        }
        var order = await db.HoaDon.AsNoTracking().Include(x => x.ChiTiet).SingleAsync(x => x.TenNguoiNhan == "STOCK-CUSTOMER");
        var slip = await db.PhieuXuat.AsNoTracking().Include(x => x.ChiTiet).SingleOrDefaultAsync(x => x.HoaDonId == order.Id);
        var stock = await db.NguyenLieu.AsNoTracking().Where(x => x.Id == ingredient.Id).Select(x => x.SoLuongTon).SingleAsync();
        check(order.ChiTiet.Single().SoLuong == 2 && slip != null && slip.ChiTiet.Single().SoLuong == 0.8m && stock == 0,
            "Order lines, the stock issue slip and the remaining stock are all recorded");

        // 3. Enough stock: no warning.
        ingredient = await db.NguyenLieu.SingleAsync(x => x.Id == ingredient.Id);
        ingredient.SoLuongTon = 10m; await db.SaveChangesAsync();
        var orders = new OrderService(db, new InventoryService(db));
        var created = await orders.CreateOrderAsync(new()
        {
            LoaiDonHang = LoaiDonHang.MangDi, TenNguoiNhan = "STOCK-ENOUGH", SoDienThoaiNhan = "0988000222",
            Items = [new() { MonAnId = dish.Id, MonAnSizeId = size.Id, SoLuong = 1 }]
        }, staffId);
        check(created.Error is null && orders.InventoryWarnings.Count == 0, "Order with enough stock produces no stock warning");
    }
}
