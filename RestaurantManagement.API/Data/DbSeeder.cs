using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.API.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(RestaurantDbContext db)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        await AddMissingAsync(db, db.Set<NhanVien>(), x => x.MaNhanVien,
            new NhanVien { MaNhanVien = "NV001", HoTen = "Nguyễn Minh Quản", SoDienThoai = "0900000001", Email = "quan.ly@example.test", ChucVu = "Quản lý", NgayVaoLam = new DateOnly(2025, 1, 2) },
            new NhanVien { MaNhanVien = "NV002", HoTen = "Trần Thu Lễ", SoDienThoai = "0900000002", Email = "le.tan@example.test", ChucVu = "Lễ tân", NgayVaoLam = new DateOnly(2025, 2, 3) },
            new NhanVien { MaNhanVien = "NV003", HoTen = "Lê Văn Kho", SoDienThoai = "0900000003", Email = "nhan.vien.kho@example.test", ChucVu = "Nhân viên kho", NgayVaoLam = new DateOnly(2025, 3, 4) },
            new NhanVien { MaNhanVien = "NV015", HoTen = "Nguyễn Minh Thu", SoDienThoai = "0900000015", Email = "thu.ngan@example.test", ChucVu = "Thu ngân", NgayVaoLam = new DateOnly(2025, 6, 2) });

        await AddMissingAsync(db, db.Set<KhuVuc>(), x => x.TenKhuVuc,
            new KhuVuc { TenKhuVuc = "Sảnh chung", LaPhongVip = false },
            new KhuVuc { TenKhuVuc = "Phòng VIP", LaPhongVip = true });
        await db.SaveChangesAsync();

        var sanh = await db.Set<KhuVuc>().SingleAsync(x => x.TenKhuVuc == "Sảnh chung");
        var vip = await db.Set<KhuVuc>().SingleAsync(x => x.TenKhuVuc == "Phòng VIP");
        await AddMissingAsync(db, db.Set<BanAn>(), x => x.MaBan,
            new BanAn { MaBan = "S01", KhuVucId = sanh.Id, SoChoNgoi = 2, TrangThai = TrangThaiBan.SanSang },
            new BanAn { MaBan = "S02", KhuVucId = sanh.Id, SoChoNgoi = 4, TrangThai = TrangThaiBan.SanSang },
            new BanAn { MaBan = "S03", KhuVucId = sanh.Id, SoChoNgoi = 6, TrangThai = TrangThaiBan.SanSang },
            new BanAn { MaBan = "V01", KhuVucId = vip.Id, SoChoNgoi = 8, TrangThai = TrangThaiBan.SanSang },
            new BanAn { MaBan = "V02", KhuVucId = vip.Id, SoChoNgoi = 10, TrangThai = TrangThaiBan.SanSang });

        await AddMissingAsync(db, db.Set<DanhMuc>(), x => x.TenDanhMuc,
            new DanhMuc { TenDanhMuc = "Khai vị", MoTa = "Dữ liệu minh họa cho CRUD" },
            new DanhMuc { TenDanhMuc = "Món chính", MoTa = "Dữ liệu minh họa cho CRUD" },
            new DanhMuc { TenDanhMuc = "Thức uống", MoTa = "Dữ liệu minh họa cho CRUD" },
            new DanhMuc { TenDanhMuc = "Set món", MoTa = "Dữ liệu minh họa cho CRUD" });
        await db.SaveChangesAsync();

        var dmKhaiVi = await db.Set<DanhMuc>().SingleAsync(x => x.TenDanhMuc == "Khai vị");
        var dmMonChinh = await db.Set<DanhMuc>().SingleAsync(x => x.TenDanhMuc == "Món chính");
        var dmThucUong = await db.Set<DanhMuc>().SingleAsync(x => x.TenDanhMuc == "Thức uống");
        var dmSet = await db.Set<DanhMuc>().SingleAsync(x => x.TenDanhMuc == "Set món");
        await AddMissingAsync(db, db.Set<MonAn>(), x => x.TenMon,
            new MonAn { TenMon = "Salad rau", DanhMucId = dmKhaiVi.Id, Loai = LoaiMon.MonLe, TrangThai = TrangThaiMon.DangPhucVu, LaMonMoi = false, LaMonNoiBat = false },
            new MonAn { TenMon = "Bò lúc lắc", DanhMucId = dmMonChinh.Id, Loai = LoaiMon.MonLe, TrangThai = TrangThaiMon.DangPhucVu, LaMonMoi = false, LaMonNoiBat = true },
            new MonAn { TenMon = "Cơm trắng", DanhMucId = dmMonChinh.Id, Loai = LoaiMon.MonLe, TrangThai = TrangThaiMon.DangPhucVu, LaMonMoi = false, LaMonNoiBat = false },
            new MonAn { TenMon = "Nước cam", DanhMucId = dmThucUong.Id, Loai = LoaiMon.ThucUong, TrangThai = TrangThaiMon.DangPhucVu, LaMonMoi = true, LaMonNoiBat = false },
            new MonAn { TenMon = "Set gia đình", DanhMucId = dmSet.Id, Loai = LoaiMon.Set, TrangThai = TrangThaiMon.DangPhucVu, LaMonMoi = true, LaMonNoiBat = true });
        await db.SaveChangesAsync();

        var dishes = await db.Set<MonAn>().ToDictionaryAsync(x => x.TenMon);
        var sizeSaladS = await AddSizeAsync(db, dishes["Salad rau"].Id, "Nhỏ (S)", 49000);
        var sizeSaladM = await AddSizeAsync(db, dishes["Salad rau"].Id, "Vừa (M)", 59000);
        var sizeSaladL = await AddSizeAsync(db, dishes["Salad rau"].Id, "Lớn (L)", 79000);
        await AddSizeAsync(db, dishes["Salad rau"].Id, "Mặc định", 59000);

        var sizeBoS = await AddSizeAsync(db, dishes["Bò lúc lắc"].Id, "Nhỏ (S)", 139000);
        var sizeBoM = await AddSizeAsync(db, dishes["Bò lúc lắc"].Id, "Vừa (M)", 159000);
        var sizeBoL = await AddSizeAsync(db, dishes["Bò lúc lắc"].Id, "Lớn (L)", 199000);
        await AddSizeAsync(db, dishes["Bò lúc lắc"].Id, "Mặc định", 159000);

        var sizeCom = await AddSizeAsync(db, dishes["Cơm trắng"].Id, "Mặc định", 19000);
        var sizeNuocCam = await AddSizeAsync(db, dishes["Nước cam"].Id, "Mặc định", 39000);
        var sizeCombo = await AddSizeAsync(db, dishes["Set gia đình"].Id, "Mặc định", 229000);

        await AddMissingAsync(db, db.Set<NguyenLieu>(), x => x.TenNguyenLieu,
            new NguyenLieu { TenNguyenLieu = "Thịt bò", DonViTinh = "g", DanhMuc = "Thịt", NguongCanhBao = 1000, SoLuongTon = 10000, DonGia = 220 },
            new NguyenLieu { TenNguyenLieu = "Rau xà lách", DonViTinh = "g", DanhMuc = "Rau củ", NguongCanhBao = 500, SoLuongTon = 5000, DonGia = 30 },
            new NguyenLieu { TenNguyenLieu = "Gạo", DonViTinh = "g", DanhMuc = "Đồ khô", NguongCanhBao = 2000, SoLuongTon = 20000, DonGia = 22 },
            new NguyenLieu { TenNguyenLieu = "Nước cam", DonViTinh = "ml", DanhMuc = "Đồ uống", NguongCanhBao = 2000, SoLuongTon = 12000, DonGia = 50 },
            new NguyenLieu { TenNguyenLieu = "Trứng gà", DonViTinh = "cái", DanhMuc = "Thực phẩm khác", NguongCanhBao = 20, SoLuongTon = 100, DonGia = 3000 });

        await AddMissingAsync(db, db.Set<NhaCungCap>(), x => x.TenNhaCungCap,
            new NhaCungCap { TenNhaCungCap = "Công ty TNHH Thực phẩm Tươi Sạch Sài Gòn", SoDienThoai = "0901234567", Email = "tuoisach@sgfood.vn", DiaChi = "Q. Bình Thạnh, TP.HCM", MaSoThue = "0312345678", NguoiLienHe = "Nguyễn Văn Tuấn" },
            new NhaCungCap { TenNhaCungCap = "Nông trại Rau Củ Đà Lạt Xanh", SoDienThoai = "0908765432", Email = "lienhe@dalatgreen.vn", DiaChi = "TP. Đà Lạt, Lâm Đồng", MaSoThue = "5801234567", NguoiLienHe = "Trần Thị Mai" },
            new NhaCungCap { TenNhaCungCap = "Đại lý Gia Vị & Đồ Khô Phú Thịnh", SoDienThoai = "0912345678", Email = "giavi.phuthinh@gmail.com", DiaChi = "Q.5, TP.HCM", MaSoThue = "0309876543", NguoiLienHe = "Lê Phú Thịnh" });
        await db.SaveChangesAsync();

        var ingredients = await db.Set<NguyenLieu>().ToDictionaryAsync(x => x.TenNguyenLieu);
        foreach (var (ten, item) in ingredients)
        {
            if (item.SoLuongTon == 0)
            {
                if (ten == "Thịt bò") { item.SoLuongTon = 10000; item.DonGia = 220; item.DanhMuc = "Thịt"; }
                else if (ten == "Rau xà lách") { item.SoLuongTon = 5000; item.DonGia = 30; item.DanhMuc = "Rau củ"; }
                else if (ten == "Gạo") { item.SoLuongTon = 20000; item.DonGia = 22; item.DanhMuc = "Đồ khô"; }
                else if (ten == "Nước cam") { item.SoLuongTon = 12000; item.DonGia = 50; item.DanhMuc = "Đồ uống"; }
                else if (ten == "Trứng gà") { item.SoLuongTon = 100; item.DonGia = 3000; item.DanhMuc = "Thực phẩm khác"; }
            }
        }
        await db.SaveChangesAsync();

        var suppliers = await db.Set<NhaCungCap>().ToListAsync();
        var nccTuoiSach = suppliers.FirstOrDefault(x => x.TenNhaCungCap.Contains("Tươi Sạch")) ?? suppliers.First();
        var nccDaLat = suppliers.FirstOrDefault(x => x.TenNhaCungCap.Contains("Đà Lạt")) ?? suppliers.First();
        var nccPhuThinh = suppliers.FirstOrDefault(x => x.TenNhaCungCap.Contains("Phú Thịnh")) ?? suppliers.First();

        // Seed supply relationships
        await AddSupplyAsync(db, nccTuoiSach.Id, ingredients["Thịt bò"].Id, 220, "BO-WAGYU");
        await AddSupplyAsync(db, nccTuoiSach.Id, ingredients["Rau xà lách"].Id, 30, "RAU-XL01");
        await AddSupplyAsync(db, nccDaLat.Id, ingredients["Rau xà lách"].Id, 28, "RAU-DL-02");
        await AddSupplyAsync(db, nccPhuThinh.Id, ingredients["Gạo"].Id, 22, "GAO-ST25");
        await AddSupplyAsync(db, nccPhuThinh.Id, ingredients["Nước cam"].Id, 50, "CAM-EP-01");
        await AddSupplyAsync(db, nccPhuThinh.Id, ingredients["Trứng gà"].Id, 3000, "TRUNG-GA-TA");
        await db.SaveChangesAsync();

        dishes = await db.Set<MonAn>().ToDictionaryAsync(x => x.TenMon);

        // Multi-size recipes (BOM)
        await AddRecipeAsync(db, dishes["Salad rau"].Id, sizeSaladS, ingredients["Rau xà lách"].Id, 80);
        await AddRecipeAsync(db, dishes["Salad rau"].Id, sizeSaladM, ingredients["Rau xà lách"].Id, 120);
        await AddRecipeAsync(db, dishes["Salad rau"].Id, sizeSaladL, ingredients["Rau xà lách"].Id, 180);

        await AddRecipeAsync(db, dishes["Bò lúc lắc"].Id, sizeBoS, ingredients["Thịt bò"].Id, 120);
        await AddRecipeAsync(db, dishes["Bò lúc lắc"].Id, sizeBoM, ingredients["Thịt bò"].Id, 180);
        await AddRecipeAsync(db, dishes["Bò lúc lắc"].Id, sizeBoL, ingredients["Thịt bò"].Id, 250);

        await AddRecipeAsync(db, dishes["Cơm trắng"].Id, sizeCom, ingredients["Gạo"].Id, 100);
        await AddRecipeAsync(db, dishes["Nước cam"].Id, sizeNuocCam, ingredients["Nước cam"].Id, 250);

        await AddComboItemAsync(db, dishes["Set gia đình"].Id, dishes["Bò lúc lắc"].Id, 1);
        await AddComboItemAsync(db, dishes["Set gia đình"].Id, dishes["Salad rau"].Id, 1);
        await AddComboItemAsync(db, dishes["Set gia đình"].Id, dishes["Cơm trắng"].Id, 2);

        if (!await db.Set<PhieuNhap>().AnyAsync(x => x.MaPhieu == "PN-TONDAUKY-001"))
        {
            var warehouseEmployee = await db.Set<NhanVien>().SingleAsync(x => x.MaNhanVien == "NV003");
            db.Add(new PhieuNhap
            {
                MaPhieu = "PN-TONDAUKY-001",
                NhanVienId = warehouseEmployee.Id,
                NhaCungCapId = nccTuoiSach.Id,
                ThoiDiem = new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.FromHours(7)),
                TrangThai = TrangThaiPhieu.DaGhiSo,
                LyDo = LyDoNhap.TonDauKy,
                TongTien = 3690000,
                GhiChu = "Dữ liệu tồn đầu kỳ và minh họa phục vụ kiểm thử phân hệ kho",
                ChiTiet =
                {
                    new ChiTietPhieuNhap { NguyenLieuId = ingredients["Thịt bò"].Id, SoLuong = 10000, DonGia = 220, ThanhTien = 2200000 },
                    new ChiTietPhieuNhap { NguyenLieuId = ingredients["Rau xà lách"].Id, SoLuong = 5000, DonGia = 30, ThanhTien = 150000 },
                    new ChiTietPhieuNhap { NguyenLieuId = ingredients["Gạo"].Id, SoLuong = 20000, DonGia = 22, ThanhTien = 440000 },
                    new ChiTietPhieuNhap { NguyenLieuId = ingredients["Nước cam"].Id, SoLuong = 12000, DonGia = 50, ThanhTien = 600000 },
                    new ChiTietPhieuNhap { NguyenLieuId = ingredients["Trứng gà"].Id, SoLuong = 100, DonGia = 3000, ThanhTien = 300000 }
                }
            });
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static async Task AddMissingAsync<TEntity, TKey>(RestaurantDbContext db, DbSet<TEntity> set,
        Func<TEntity, TKey> key, params TEntity[] values) where TEntity : class
    {
        var existing = (await set.AsNoTracking().ToListAsync()).Select(key).ToHashSet();
        await set.AddRangeAsync(values.Where(x => !existing.Contains(key(x))));
        await db.SaveChangesAsync();
    }

    private static async Task<int> AddSizeAsync(RestaurantDbContext db, int dishId, string size, decimal price)
    {
        var existing = await db.Set<MonAnSize>().FirstOrDefaultAsync(x => x.MonAnId == dishId && x.TenSize == size);
        if (existing != null) return existing.Id;
        var entity = new MonAnSize { MonAnId = dishId, TenSize = size, GiaBan = price };
        db.Add(entity);
        await db.SaveChangesAsync();
        return entity.Id;
    }

    private static async Task AddRecipeAsync(RestaurantDbContext db, int dishId, int sizeId, int ingredientId, decimal quantity)
    {
        if (!await db.Set<DinhMucMon>().AnyAsync(x => x.MonAnId == dishId && x.MaKichCo == sizeId && x.NguyenLieuId == ingredientId))
            db.Add(new DinhMucMon { MonAnId = dishId, MaKichCo = sizeId, NguyenLieuId = ingredientId, SoLuong = quantity });
    }

    private static async Task AddSupplyAsync(RestaurantDbContext db, int supplierId, int ingredientId, decimal price, string? productCode = null)
    {
        if (!await db.Set<NhaCungCapNguyenLieu>().AnyAsync(x => x.MaNhaCungCap == supplierId && x.MaNguyenLieu == ingredientId))
        {
            db.Add(new NhaCungCapNguyenLieu
            {
                MaNhaCungCap = supplierId,
                MaNguyenLieu = ingredientId,
                DonGiaCungUng = price,
                MaHangNCC = productCode,
                NgayLienKet = DateTime.Now,
                TrangThai = true
            });
        }
    }

    private static async Task AddComboItemAsync(RestaurantDbContext db, int comboId, int dishId, int quantity)
    {
        if (!await db.Set<ChiTietCombo>().AnyAsync(x => x.ComboId == comboId && x.MonAnId == dishId))
            db.Add(new ChiTietCombo { ComboId = comboId, MonAnId = dishId, SoLuong = quantity });
    }
}
