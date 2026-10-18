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
            new KhuVuc { TenKhuVuc = "Phòng VIP", LaPhongVip = true },
            new KhuVuc { TenKhuVuc = "Sân Vườn", LaPhongVip = false });
        await db.SaveChangesAsync();

        var sanh = await db.Set<KhuVuc>().SingleAsync(x => x.TenKhuVuc == "Sảnh chung");
        var vip = await db.Set<KhuVuc>().SingleAsync(x => x.TenKhuVuc == "Phòng VIP");
        var sanVuon = await db.Set<KhuVuc>().SingleAsync(x => x.TenKhuVuc == "Sân Vườn");
        await AddMissingAsync(db, db.Set<BanAn>(), x => x.MaBan,
            new BanAn { MaBan = "S01", KhuVucId = sanh.Id, SoChoNgoi = 2, TrangThai = TrangThaiBan.SanSang },
            new BanAn { MaBan = "S02", KhuVucId = sanh.Id, SoChoNgoi = 4, TrangThai = TrangThaiBan.SanSang },
            new BanAn { MaBan = "S03", KhuVucId = sanh.Id, SoChoNgoi = 6, TrangThai = TrangThaiBan.SanSang },
            new BanAn { MaBan = "V01", KhuVucId = vip.Id, SoChoNgoi = 8, TrangThai = TrangThaiBan.SanSang },
            new BanAn { MaBan = "V02", KhuVucId = vip.Id, SoChoNgoi = 10, TrangThai = TrangThaiBan.SanSang },
            new BanAn { MaBan = "SV01", KhuVucId = sanVuon.Id, SoChoNgoi = 4, TrangThai = TrangThaiBan.SanSang },
            new BanAn { MaBan = "SV02", KhuVucId = sanVuon.Id, SoChoNgoi = 6, TrangThai = TrangThaiBan.SanSang });

        await AddMissingAsync(db, db.Set<DanhMuc>(), x => x.TenDanhMuc,
            new DanhMuc { TenDanhMuc = "Khai vị", MoTa = "Dữ liệu minh họa cho CRUD" },
            new DanhMuc { TenDanhMuc = "Món chính", MoTa = "Dữ liệu minh họa cho CRUD" },
            new DanhMuc { TenDanhMuc = "Thức uống", MoTa = "Dữ liệu minh họa cho CRUD" },
            new DanhMuc { TenDanhMuc = "Set món", MoTa = "Dữ liệu minh họa cho CRUD" });
        await db.SaveChangesAsync();

        // Seed nghiệp vụ thực đơn chỉ chạy khi schema đã có các cột của Tuần 9.
        // (PromotionSmoke.MigrationProof seed trên schema cũ trước khi migrate lên bản mới.)
        var coCotWeek9 = await db.Database
            .SqlQueryRaw<int?>("SELECT CAST(COL_LENGTH('MonAnSize', 'HeSoTyLe') AS int) AS [Value]")
            .SingleAsync() is not null;
        if (coCotWeek9) await SeedMenuAsync(db);

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static async Task SeedMenuAsync(RestaurantDbContext db)
    {
        var dmKhaiVi = await db.Set<DanhMuc>().SingleAsync(x => x.TenDanhMuc == "Khai vị");
        var dmMonChinh = await db.Set<DanhMuc>().SingleAsync(x => x.TenDanhMuc == "Món chính");
        var dmThucUong = await db.Set<DanhMuc>().SingleAsync(x => x.TenDanhMuc == "Thức uống");
        var dmSet = await db.Set<DanhMuc>().SingleAsync(x => x.TenDanhMuc == "Set món");
        await AddMissingAsync(db, db.Set<MonAn>(), x => x.TenMon,
            new MonAn { TenMon = "Salad rau", DanhMucId = dmKhaiVi.Id, Loai = LoaiMon.MonLe, TrangThai = TrangThaiMon.DangPhucVu, LaMonMoi = false, LaMonNoiBat = false },
            new MonAn { TenMon = "Bò lúc lắc", DanhMucId = dmMonChinh.Id, Loai = LoaiMon.MonLe, TrangThai = TrangThaiMon.DangPhucVu, LaMonMoi = false, LaMonNoiBat = true },
            new MonAn { TenMon = "Cơm trắng", DanhMucId = dmMonChinh.Id, Loai = LoaiMon.MonLe, TrangThai = TrangThaiMon.DangPhucVu, LaMonMoi = false, LaMonNoiBat = false },
            new MonAn { TenMon = "Nước cam", DanhMucId = dmThucUong.Id, Loai = LoaiMon.ThucUong, TrangThai = TrangThaiMon.DangPhucVu, LaMonMoi = true, LaMonNoiBat = false },
            new MonAn { TenMon = "Gỏi ngó sen tôm thịt", DanhMucId = dmKhaiVi.Id, Loai = LoaiMon.MonLe, TrangThai = TrangThaiMon.DangPhucVu, LaMonMoi = true, LaMonNoiBat = true },
            new MonAn { TenMon = "Gỏi bò bóp thấu", DanhMucId = dmKhaiVi.Id, Loai = LoaiMon.MonLe, TrangThai = TrangThaiMon.DangPhucVu, LaMonMoi = false, LaMonNoiBat = true },
            new MonAn { TenMon = "Set gia đình", DanhMucId = dmSet.Id, Loai = LoaiMon.Set, TrangThai = TrangThaiMon.DangPhucVu, LaMonMoi = true, LaMonNoiBat = true });
        await db.SaveChangesAsync();

        var dishes = await db.Set<MonAn>().ToDictionaryAsync(x => x.TenMon);
        var sizeSaladS = await AddSizeAsync(db, dishes["Salad rau"].Id, "Nhỏ (S)", 49000, 1.0m);
        var sizeSaladM = await AddSizeAsync(db, dishes["Salad rau"].Id, "Vừa (M)", 59000, 1.5m);
        var sizeSaladL = await AddSizeAsync(db, dishes["Salad rau"].Id, "Lớn (L)", 79000, 2.25m);
        await AddSizeAsync(db, dishes["Salad rau"].Id, "Mặc định", 59000);

        var sizeBoS = await AddSizeAsync(db, dishes["Bò lúc lắc"].Id, "Nhỏ (S)", 139000, 1.0m);
        var sizeBoM = await AddSizeAsync(db, dishes["Bò lúc lắc"].Id, "Vừa (M)", 159000, 1.5m);
        var sizeBoL = await AddSizeAsync(db, dishes["Bò lúc lắc"].Id, "Lớn (L)", 199000, 2.083333m);
        await AddSizeAsync(db, dishes["Bò lúc lắc"].Id, "Mặc định", 159000);

        var sizeGoiNgoSenS = await AddSizeAsync(db, dishes["Gỏi ngó sen tôm thịt"].Id, "Nhỏ (S)", 89000, 1.0m);
        var sizeGoiNgoSenM = await AddSizeAsync(db, dishes["Gỏi ngó sen tôm thịt"].Id, "Vừa (M)", 119000, 1.3m);
        var sizeGoiNgoSenL = await AddSizeAsync(db, dishes["Gỏi ngó sen tôm thịt"].Id, "Lớn (L)", 149000, 1.5m,
            "Size lớn kèm thêm bánh phồng tôm và tôm sú tăng cường");

        var sizeGoiBoS = await AddSizeAsync(db, dishes["Gỏi bò bóp thấu"].Id, "Nhỏ (S)", 99000, 1.0m);
        var sizeGoiBoM = await AddSizeAsync(db, dishes["Gỏi bò bóp thấu"].Id, "Vừa (M)", 129000, 1.3m);
        var sizeGoiBoL = await AddSizeAsync(db, dishes["Gỏi bò bóp thấu"].Id, "Lớn (L)", 159000, 1.5m,
            "Size lớn kèm thêm bánh phồng tôm; nước mắm chua ngọt tách riêng");

        var sizeCom = await AddSizeAsync(db, dishes["Cơm trắng"].Id, "Mặc định", 19000);
        var sizeNuocCam = await AddSizeAsync(db, dishes["Nước cam"].Id, "Mặc định", 39000);
        var sizeCombo = await AddSizeAsync(db, dishes["Set gia đình"].Id, "Mặc định", 229000);

        await AddMissingAsync(db, db.Set<NguyenLieu>(), x => x.TenNguyenLieu,
            new NguyenLieu { TenNguyenLieu = "Thịt bò", DonViTinh = "g", DanhMuc = "Thịt", NguongCanhBao = 1000, SoLuongTon = 10000, DonGia = 220 },
            new NguyenLieu { TenNguyenLieu = "Rau xà lách", DonViTinh = "g", DanhMuc = "Rau củ", NguongCanhBao = 500, SoLuongTon = 5000, DonGia = 30 },
            new NguyenLieu { TenNguyenLieu = "Gạo", DonViTinh = "g", DanhMuc = "Đồ khô", NguongCanhBao = 2000, SoLuongTon = 20000, DonGia = 22 },
            new NguyenLieu { TenNguyenLieu = "Nước cam", DonViTinh = "ml", DanhMuc = "Đồ uống", NguongCanhBao = 2000, SoLuongTon = 12000, DonGia = 50 },
            new NguyenLieu { TenNguyenLieu = "Trứng gà", DonViTinh = "cái", DanhMuc = "Thực phẩm khác", NguongCanhBao = 20, SoLuongTon = 100, DonGia = 3000 },
            new NguyenLieu { TenNguyenLieu = "Ngó sen", DonViTinh = "g", DanhMuc = "Rau củ", NguongCanhBao = 300, SoLuongTon = 3000, DonGia = 45 },
            new NguyenLieu { TenNguyenLieu = "Tôm sú tươi", DonViTinh = "g", DanhMuc = "Hải sản", NguongCanhBao = 500, SoLuongTon = 4000, DonGia = 320 },
            new NguyenLieu { TenNguyenLieu = "Thịt ba chỉ", DonViTinh = "g", DanhMuc = "Thịt", NguongCanhBao = 500, SoLuongTon = 5000, DonGia = 150 },
            new NguyenLieu { TenNguyenLieu = "Khế", DonViTinh = "g", DanhMuc = "Rau củ", NguongCanhBao = 200, SoLuongTon = 1500, DonGia = 25 },
            new NguyenLieu { TenNguyenLieu = "Chuối chát", DonViTinh = "g", DanhMuc = "Rau củ", NguongCanhBao = 200, SoLuongTon = 1500, DonGia = 20 },
            new NguyenLieu { TenNguyenLieu = "Rau răm", DonViTinh = "g", DanhMuc = "Rau củ", NguongCanhBao = 100, SoLuongTon = 800, DonGia = 40 },
            new NguyenLieu { TenNguyenLieu = "Đậu phộng rang", DonViTinh = "g", DanhMuc = "Đồ khô", NguongCanhBao = 300, SoLuongTon = 2000, DonGia = 90 },
            new NguyenLieu { TenNguyenLieu = "Bánh phồng tôm", DonViTinh = "cái", DanhMuc = "Đồ khô", NguongCanhBao = 50, SoLuongTon = 400, DonGia = 1500 },
            new NguyenLieu { TenNguyenLieu = "Nước mắm", DonViTinh = "ml", DanhMuc = "Gia vị", NguongCanhBao = 500, SoLuongTon = 5000, DonGia = 60 },
            new NguyenLieu { TenNguyenLieu = "Đường", DonViTinh = "g", DanhMuc = "Gia vị", NguongCanhBao = 500, SoLuongTon = 10000, DonGia = 20 },
            new NguyenLieu { TenNguyenLieu = "Chanh", DonViTinh = "g", DanhMuc = "Rau củ", NguongCanhBao = 200, SoLuongTon = 1200, DonGia = 30 },
            new NguyenLieu { TenNguyenLieu = "Ớt", DonViTinh = "g", DanhMuc = "Gia vị", NguongCanhBao = 100, SoLuongTon = 600, DonGia = 50 });

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

        // W9: BOM món mới — định lượng gốc theo size S, các size sau nhân hệ số tỷ lệ.
        await AddRecipeTheoTyLeAsync(db, dishes["Gỏi ngó sen tôm thịt"].Id,
            new[] { sizeGoiNgoSenS, sizeGoiNgoSenM, sizeGoiNgoSenL }, new[] { 1.0m, 1.3m, 1.5m },
            new (int, decimal)[]
            {
                (ingredients["Tôm sú tươi"].Id, 100),
                (ingredients["Thịt ba chỉ"].Id, 80),
                (ingredients["Ngó sen"].Id, 150),
                (ingredients["Rau răm"].Id, 20),
                (ingredients["Đậu phộng rang"].Id, 30),
                (ingredients["Nước mắm"].Id, 15),
                (ingredients["Chanh"].Id, 10),
                (ingredients["Ớt"].Id, 5)
            });
        await AddExtraRecipeAsync(db, dishes["Gỏi ngó sen tôm thịt"].Id, sizeGoiNgoSenL, ingredients["Bánh phồng tôm"].Id, 4);

        await AddRecipeTheoTyLeAsync(db, dishes["Gỏi bò bóp thấu"].Id,
            new[] { sizeGoiBoS, sizeGoiBoM, sizeGoiBoL }, new[] { 1.0m, 1.3m, 1.5m },
            new (int, decimal)[]
            {
                (ingredients["Thịt bò"].Id, 100),
                (ingredients["Khế"].Id, 40),
                (ingredients["Chuối chát"].Id, 40),
                (ingredients["Rau răm"].Id, 15),
                (ingredients["Đậu phộng rang"].Id, 25),
                (ingredients["Nước mắm"].Id, 20),
                (ingredients["Đường"].Id, 10),
                (ingredients["Chanh"].Id, 10),
                (ingredients["Ớt"].Id, 5)
            });
        await AddExtraRecipeAsync(db, dishes["Gỏi bò bóp thấu"].Id, sizeGoiBoL, ingredients["Bánh phồng tôm"].Id, 4);

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

        // Chứng từ tồn kho cho các nguyên liệu mới (bình đẳng với dữ liệu tồn đầu kỳ, không sửa chứng từ cũ).
        if (!await db.Set<PhieuNhap>().AnyAsync(x => x.MaPhieu == "PN-W9-TONKHO-001"))
        {
            var warehouseman = await db.Set<NhanVien>().SingleAsync(x => x.MaNhanVien == "NV003");
            var receipt = new PhieuNhap
            {
                MaPhieu = "PN-W9-TONKHO-001",
                NhanVienId = warehouseman.Id,
                NhaCungCapId = nccTuoiSach.Id,
                ThoiDiem = new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.FromHours(7)),
                TrangThai = TrangThaiPhieu.DaGhiSo,
                LyDo = LyDoNhap.MuaHang,
                GhiChu = "Nhập kho nguyên liệu cho các món mới Tuần 9 (gỏi ngó sen tôm thịt, gỏi bò bóp thấu)"
            };
            var lines = new (string Ten, decimal SoLuong)[]
            {
                ("Ngó sen", 3000), ("Tôm sú tươi", 4000), ("Thịt ba chỉ", 5000),
                ("Khế", 1500), ("Chuối chát", 1500), ("Rau răm", 800),
                ("Đậu phộng rang", 2000), ("Bánh phồng tôm", 400), ("Nước mắm", 5000),
                ("Đường", 10000), ("Chanh", 1200), ("Ớt", 600)
            };
            foreach (var (ten, soLuong) in lines)
            {
                receipt.ChiTiet.Add(new ChiTietPhieuNhap
                {
                    NguyenLieuId = ingredients[ten].Id,
                    SoLuong = soLuong,
                    DonGia = ingredients[ten].DonGia,
                    ThanhTien = soLuong * ingredients[ten].DonGia
                });
            }
            receipt.TongTien = receipt.ChiTiet.Sum(x => x.ThanhTien);
            db.Add(receipt);
        }

        await db.SaveChangesAsync();
    }

    private static async Task AddMissingAsync<TEntity, TKey>(RestaurantDbContext db, DbSet<TEntity> set,
        Func<TEntity, TKey> key, params TEntity[] values) where TEntity : class
    {
        var comparer = typeof(TKey) == typeof(string)
            ? (IEqualityComparer<TKey>)StringComparer.OrdinalIgnoreCase
            : EqualityComparer<TKey>.Default;
        var existing = (await set.AsNoTracking().ToListAsync()).Select(key).ToHashSet(comparer);
        await set.AddRangeAsync(values.Where(x => !existing.Contains(key(x))));
        await db.SaveChangesAsync();
    }

    private static async Task<int> AddSizeAsync(RestaurantDbContext db, int dishId, string size, decimal price,
        decimal heSoTyLe = 1.0m, string? ghiChu = null)
    {
        var existing = await db.Set<MonAnSize>().FirstOrDefaultAsync(x => x.MonAnId == dishId && x.TenSize == size);
        if (existing != null)
        {
            if (existing.HeSoTyLe <= 0) existing.HeSoTyLe = heSoTyLe;
            if (string.IsNullOrWhiteSpace(existing.GhiChuThanhPhanKhacBiet) && !string.IsNullOrWhiteSpace(ghiChu))
                existing.GhiChuThanhPhanKhacBiet = ghiChu;
            await db.SaveChangesAsync();
            return existing.Id;
        }
        var entity = new MonAnSize { MonAnId = dishId, TenSize = size, GiaBan = price, HeSoTyLe = heSoTyLe, GhiChuThanhPhanKhacBiet = ghiChu };
        db.Add(entity);
        await db.SaveChangesAsync();
        return entity.Id;
    }

    private static async Task AddRecipeAsync(RestaurantDbContext db, int dishId, int sizeId, int ingredientId, decimal quantity)
    {
        if (!await db.Set<DinhMucMon>().AnyAsync(x => x.MonAnId == dishId && x.MaKichCo == sizeId && x.NguyenLieuId == ingredientId))
            db.Add(new DinhMucMon { MonAnId = dishId, MaKichCo = sizeId, NguyenLieuId = ingredientId, SoLuong = quantity });
    }

    private static async Task AddRecipeTheoTyLeAsync(RestaurantDbContext db, int dishId, int[] sizeIds, decimal[] heSoTyLe,
        (int NguyenLieuId, decimal SoLuongGoc)[] dinhMucGoc)
    {
        for (var i = 0; i < sizeIds.Length; i++)
            foreach (var (nguyenLieuId, soLuongGoc) in dinhMucGoc)
                await AddRecipeAsync(db, dishId, sizeIds[i], nguyenLieuId,
                    Math.Round(soLuongGoc * heSoTyLe[i], 4, MidpointRounding.AwayFromZero));
    }

    private static async Task AddExtraRecipeAsync(RestaurantDbContext db, int dishId, int sizeId, int ingredientId, decimal quantity)
    {
        if (!await db.Set<DinhMucMon>().AnyAsync(x => x.MonAnId == dishId && x.MaKichCo == sizeId && x.NguyenLieuId == ingredientId))
            db.Add(new DinhMucMon { MonAnId = dishId, MaKichCo = sizeId, NguyenLieuId = ingredientId, SoLuong = quantity, LaNguyenLieuPhu = true });
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
