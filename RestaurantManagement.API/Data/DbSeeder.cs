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
            new NhanVien { MaNhanVien = "NV003", HoTen = "Lê Văn Kho", SoDienThoai = "0900000003", Email = "nhan.vien.kho@example.test", ChucVu = "Nhân viên kho", NgayVaoLam = new DateOnly(2025, 3, 4) });

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
        await AddSizeAsync(db, dishes["Salad rau"].Id, "Mặc định", 59000);
        await AddSizeAsync(db, dishes["Bò lúc lắc"].Id, "Mặc định", 159000);
        await AddSizeAsync(db, dishes["Cơm trắng"].Id, "Mặc định", 19000);
        await AddSizeAsync(db, dishes["Nước cam"].Id, "Mặc định", 39000);
        await AddSizeAsync(db, dishes["Set gia đình"].Id, "Mặc định", 229000);

        await AddMissingAsync(db, db.Set<NguyenLieu>(), x => x.TenNguyenLieu,
            new NguyenLieu { TenNguyenLieu = "Thịt bò", DonViTinh = "g", NguongCanhBao = 1000 },
            new NguyenLieu { TenNguyenLieu = "Rau xà lách", DonViTinh = "g", NguongCanhBao = 500 },
            new NguyenLieu { TenNguyenLieu = "Gạo", DonViTinh = "g", NguongCanhBao = 2000 },
            new NguyenLieu { TenNguyenLieu = "Nước cam", DonViTinh = "ml", NguongCanhBao = 2000 },
            new NguyenLieu { TenNguyenLieu = "Trứng gà", DonViTinh = "cái", NguongCanhBao = 20 });

        await AddMissingAsync(db, db.Set<NhaCungCap>(), x => x.TenNhaCungCap,
            new NhaCungCap { TenNhaCungCap = "Nhà cung cấp minh họa", SoDienThoai = "0900000010", DiaChi = "TP. Hồ Chí Minh" });
        await db.SaveChangesAsync();

        var ingredients = await db.Set<NguyenLieu>().ToDictionaryAsync(x => x.TenNguyenLieu);
        dishes = await db.Set<MonAn>().ToDictionaryAsync(x => x.TenMon);

        await AddRecipeAsync(db, dishes["Salad rau"].Id, ingredients["Rau xà lách"].Id, 120);
        await AddRecipeAsync(db, dishes["Bò lúc lắc"].Id, ingredients["Thịt bò"].Id, 180);
        await AddRecipeAsync(db, dishes["Cơm trắng"].Id, ingredients["Gạo"].Id, 100);
        // Nước cam đóng chai: không bắt buộc định mức theo yêu cầu cô; giữ ví dụ minh họa tối thiểu.
        await AddRecipeAsync(db, dishes["Nước cam"].Id, ingredients["Nước cam"].Id, 250);

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
                ThoiDiem = new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.FromHours(7)),
                TrangThai = TrangThaiPhieu.DaGhiSo,
                LyDo = LyDoNhap.TonDauKy,
                GhiChu = "Dữ liệu minh họa phục vụ kiểm thử CRUD",
                ChiTiet =
                {
                    new ChiTietPhieuNhap { NguyenLieuId = ingredients["Thịt bò"].Id, SoLuong = 10000, DonGia = 220 },
                    new ChiTietPhieuNhap { NguyenLieuId = ingredients["Rau xà lách"].Id, SoLuong = 5000, DonGia = 30 },
                    new ChiTietPhieuNhap { NguyenLieuId = ingredients["Gạo"].Id, SoLuong = 20000, DonGia = 22 },
                    new ChiTietPhieuNhap { NguyenLieuId = ingredients["Nước cam"].Id, SoLuong = 12000, DonGia = 50 },
                    new ChiTietPhieuNhap { NguyenLieuId = ingredients["Trứng gà"].Id, SoLuong = 100, DonGia = 3000 }
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

    private static async Task AddSizeAsync(RestaurantDbContext db, int dishId, string size, decimal price)
    {
        if (!await db.Set<MonAnSize>().AnyAsync(x => x.MonAnId == dishId && x.TenSize == size))
            db.Add(new MonAnSize { MonAnId = dishId, TenSize = size, GiaBan = price });
        await db.SaveChangesAsync();
    }

    private static async Task AddRecipeAsync(RestaurantDbContext db, int dishId, int ingredientId, decimal quantity)
    {
        if (!await db.Set<DinhMucMon>().AnyAsync(x => x.MonAnId == dishId && x.NguyenLieuId == ingredientId))
            db.Add(new DinhMucMon { MonAnId = dishId, NguyenLieuId = ingredientId, SoLuong = quantity });
    }

    private static async Task AddComboItemAsync(RestaurantDbContext db, int comboId, int dishId, int quantity)
    {
        if (!await db.Set<ChiTietCombo>().AnyAsync(x => x.ComboId == comboId && x.MonAnId == dishId))
            db.Add(new ChiTietCombo { ComboId = comboId, MonAnId = dishId, SoLuong = quantity });
    }
}
