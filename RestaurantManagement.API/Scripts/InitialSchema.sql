IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [DanhMuc] (
        [Id] int NOT NULL IDENTITY,
        [TenDanhMuc] nvarchar(100) NOT NULL,
        [MoTa] nvarchar(500) NULL,
        [DangSuDung] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DanhMuc] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [DonViTinh] (
        [Id] int NOT NULL IDENTITY,
        [TenDonVi] nvarchar(30) NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DonViTinh] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [KhuVuc] (
        [Id] int NOT NULL IDENTITY,
        [TenKhuVuc] nvarchar(100) NOT NULL,
        [LaPhongVip] bit NOT NULL,
        [DangSuDung] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_KhuVuc] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [KhuyenMai] (
        [Id] int NOT NULL IDENTITY,
        [TenChuongTrinh] nvarchar(150) NOT NULL,
        [BatDau] datetimeoffset NOT NULL,
        [KetThuc] datetimeoffset NOT NULL,
        [PhamVi] nvarchar(40) NOT NULL,
        [KieuGiam] nvarchar(40) NOT NULL,
        [GiaTri] decimal(18,2) NOT NULL,
        [MucGiamToiDa] decimal(18,2) NULL,
        [GiaTriToiThieu] decimal(18,2) NOT NULL,
        [ChoPhepKetHop] bit NOT NULL,
        [ThuTuApDung] int NOT NULL,
        [CanVoucher] bit NOT NULL,
        [DangSuDung] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_KhuyenMai] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_KhuyenMai_DieuKien] CHECK ([KetThuc]>[BatDau] AND [GiaTri]>0 AND ([KieuGiam]<>'PhanTram' OR [GiaTri]<=100) AND [GiaTriToiThieu]>=0 AND ([MucGiamToiDa] IS NULL OR [MucGiamToiDa]>0) AND [ThuTuApDung]>=0),
        CONSTRAINT [CK_KhuyenMai_KieuGiam_Enum] CHECK ([KieuGiam] IN ('PhanTram','SoTien')),
        CONSTRAINT [CK_KhuyenMai_PhamVi_Enum] CHECK ([PhamVi] IN ('MonAn','HoaDon'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [NhaCungCap] (
        [Id] int NOT NULL IDENTITY,
        [TenNhaCungCap] nvarchar(150) NOT NULL,
        [SoDienThoai] nvarchar(20) NULL,
        [DiaChi] nvarchar(300) NULL,
        [MaSoThue] nvarchar(30) NULL,
        [DangSuDung] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_NhaCungCap] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [TaiKhoan] (
        [Id] int NOT NULL IDENTITY,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_TaiKhoan] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [VaiTro] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_VaiTro] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [MonAn] (
        [Id] int NOT NULL IDENTITY,
        [DanhMucId] int NOT NULL,
        [TenMon] nvarchar(150) NOT NULL,
        [MoTa] nvarchar(1000) NULL,
        [HinhAnh] nvarchar(500) NULL,
        [Loai] nvarchar(40) NOT NULL,
        [GiaBan] decimal(18,2) NOT NULL,
        [TrangThai] nvarchar(40) NOT NULL,
        [LaMonMoi] bit NOT NULL,
        [LaMonNoiBat] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_MonAn] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_MonAn_Gia] CHECK ([GiaBan]>=0),
        CONSTRAINT [CK_MonAn_Loai_Enum] CHECK ([Loai] IN ('MonLe','ThucUong','Set')),
        CONSTRAINT [CK_MonAn_TrangThai_Enum] CHECK ([TrangThai] IN ('DangPhucVu','TamHet','NgungKinhDoanh')),
        CONSTRAINT [FK_MonAn_DanhMuc_DanhMucId] FOREIGN KEY ([DanhMucId]) REFERENCES [DanhMuc] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [NguyenLieu] (
        [Id] int NOT NULL IDENTITY,
        [TenNguyenLieu] nvarchar(120) NOT NULL,
        [DonViCoSoId] int NOT NULL,
        [NguongCanhBao] decimal(18,6) NOT NULL,
        [DangSuDung] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_NguyenLieu] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_NguyenLieu_Nguong] CHECK ([NguongCanhBao]>=0),
        CONSTRAINT [FK_NguyenLieu_DonViTinh_DonViCoSoId] FOREIGN KEY ([DonViCoSoId]) REFERENCES [DonViTinh] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [BanAn] (
        [Id] int NOT NULL IDENTITY,
        [MaBan] nvarchar(20) NOT NULL,
        [KhuVucId] int NOT NULL,
        [SoChoNgoi] int NOT NULL,
        [TrangThai] nvarchar(40) NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_BanAn] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_BanAn_SoCho] CHECK ([SoChoNgoi]>0),
        CONSTRAINT [CK_BanAn_TrangThai_Enum] CHECK ([TrangThai] IN ('SanSang','DangPhucVu','CanDon','NgungSuDung')),
        CONSTRAINT [FK_BanAn_KhuVuc_KhuVucId] FOREIGN KEY ([KhuVucId]) REFERENCES [KhuVuc] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [DangNhapNgoai] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] int NOT NULL,
        CONSTRAINT [PK_DangNhapNgoai] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_DangNhapNgoai_TaiKhoan_UserId] FOREIGN KEY ([UserId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [KhachHang] (
        [Id] int NOT NULL IDENTITY,
        [HoTen] nvarchar(120) NOT NULL,
        [SoDienThoai] nvarchar(20) NOT NULL,
        [Email] nvarchar(256) NULL,
        [NgayDangKy] datetimeoffset NOT NULL,
        [DongYNhanUuDai] bit NOT NULL,
        [ThoiDiemDongYNhanUuDai] datetimeoffset NULL,
        [DangSuDung] bit NOT NULL,
        [TaiKhoanId] int NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_KhachHang] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_KhachHang_TaiKhoan_TaiKhoanId] FOREIGN KEY ([TaiKhoanId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [NhanVien] (
        [Id] int NOT NULL IDENTITY,
        [MaNhanVien] nvarchar(20) NOT NULL,
        [HoTen] nvarchar(120) NOT NULL,
        [SoDienThoai] nvarchar(20) NOT NULL,
        [Email] nvarchar(256) NULL,
        [DiaChi] nvarchar(300) NULL,
        [ChucVu] nvarchar(80) NOT NULL,
        [NgayVaoLam] date NOT NULL,
        [DangLamViec] bit NOT NULL,
        [TaiKhoanId] int NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_NhanVien] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_NhanVien_TaiKhoan_TaiKhoanId] FOREIGN KEY ([TaiKhoanId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [TaiKhoanClaim] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_TaiKhoanClaim] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TaiKhoanClaim_TaiKhoan_UserId] FOREIGN KEY ([UserId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [TokenTaiKhoan] (
        [UserId] int NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_TokenTaiKhoan] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_TokenTaiKhoan_TaiKhoan_UserId] FOREIGN KEY ([UserId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [TaiKhoanVaiTro] (
        [UserId] int NOT NULL,
        [RoleId] int NOT NULL,
        CONSTRAINT [PK_TaiKhoanVaiTro] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_TaiKhoanVaiTro_TaiKhoan_UserId] FOREIGN KEY ([UserId]) REFERENCES [TaiKhoan] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TaiKhoanVaiTro_VaiTro_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [VaiTro] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [VaiTroClaim] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] int NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_VaiTroClaim] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VaiTroClaim_VaiTro_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [VaiTro] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [KhuyenMaiMon] (
        [KhuyenMaiId] int NOT NULL,
        [MonAnId] int NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_KhuyenMaiMon] PRIMARY KEY ([KhuyenMaiId], [MonAnId]),
        CONSTRAINT [FK_KhuyenMaiMon_KhuyenMai_KhuyenMaiId] FOREIGN KEY ([KhuyenMaiId]) REFERENCES [KhuyenMai] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_KhuyenMaiMon_MonAn_MonAnId] FOREIGN KEY ([MonAnId]) REFERENCES [MonAn] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [ThanhPhanSet] (
        [SetId] int NOT NULL,
        [MonAnId] int NOT NULL,
        [SoLuong] int NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_ThanhPhanSet] PRIMARY KEY ([SetId], [MonAnId]),
        CONSTRAINT [CK_ThanhPhanSet_ThanhPhan] CHECK ([SetId]<>[MonAnId] AND [SoLuong]>0),
        CONSTRAINT [FK_ThanhPhanSet_MonAn_MonAnId] FOREIGN KEY ([MonAnId]) REFERENCES [MonAn] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ThanhPhanSet_MonAn_SetId] FOREIGN KEY ([SetId]) REFERENCES [MonAn] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [DinhLuongMon] (
        [MonAnId] int NOT NULL,
        [NguyenLieuId] int NOT NULL,
        [SoLuongCoSo] decimal(18,6) NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DinhLuongMon] PRIMARY KEY ([MonAnId], [NguyenLieuId]),
        CONSTRAINT [CK_DinhLuongMon_SoLuong] CHECK ([SoLuongCoSo]>0),
        CONSTRAINT [FK_DinhLuongMon_MonAn_MonAnId] FOREIGN KEY ([MonAnId]) REFERENCES [MonAn] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DinhLuongMon_NguyenLieu_NguyenLieuId] FOREIGN KEY ([NguyenLieuId]) REFERENCES [NguyenLieu] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [QuyDoiNguyenLieu] (
        [NguyenLieuId] int NOT NULL,
        [DonViTinhId] int NOT NULL,
        [HeSoVeDonViCoSo] decimal(18,6) NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_QuyDoiNguyenLieu] PRIMARY KEY ([NguyenLieuId], [DonViTinhId]),
        CONSTRAINT [CK_QuyDoiNguyenLieu_HeSo] CHECK ([HeSoVeDonViCoSo]>0),
        CONSTRAINT [FK_QuyDoiNguyenLieu_DonViTinh_DonViTinhId] FOREIGN KEY ([DonViTinhId]) REFERENCES [DonViTinh] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QuyDoiNguyenLieu_NguyenLieu_NguyenLieuId] FOREIGN KEY ([NguyenLieuId]) REFERENCES [NguyenLieu] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [Voucher] (
        [Id] int NOT NULL IDENTITY,
        [Ma] nvarchar(40) NOT NULL,
        [KhuyenMaiId] int NOT NULL,
        [KhachHangId] int NULL,
        [GioiHanTongLuot] int NOT NULL,
        [DangSuDung] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Voucher] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Voucher_Luot] CHECK ([GioiHanTongLuot]>0),
        CONSTRAINT [FK_Voucher_KhachHang_KhachHangId] FOREIGN KEY ([KhachHangId]) REFERENCES [KhachHang] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Voucher_KhuyenMai_KhuyenMaiId] FOREIGN KEY ([KhuyenMaiId]) REFERENCES [KhuyenMai] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [DatBan] (
        [Id] int NOT NULL IDENTITY,
        [MaDatBan] nvarchar(30) NOT NULL,
        [KhachHangId] int NULL,
        [HoTenLienHe] nvarchar(120) NOT NULL,
        [SoDienThoaiLienHe] nvarchar(20) NULL,
        [LaKhachTrucTiep] bit NOT NULL,
        [ThoiDiemTao] datetimeoffset NOT NULL,
        [GioDen] datetimeoffset NOT NULL,
        [GioKetThucDuKien] datetimeoffset NOT NULL,
        [SoNguoiLon] int NOT NULL,
        [SoTreEm] int NOT NULL,
        [YeuCau] nvarchar(1000) NULL,
        [YeuCauTrangTri] bit NOT NULL,
        [YeuCauVip] bit NOT NULL,
        [TienCocYeuCau] decimal(18,2) NOT NULL,
        [DieuKienCocDaThoaThuan] nvarchar(1000) NULL,
        [TrangThai] nvarchar(40) NOT NULL,
        [ThoiDiemNhanBan] datetimeoffset NULL,
        [ThoiDiemHuy] datetimeoffset NULL,
        [LyDoHuy] nvarchar(500) NULL,
        [NhanVienTiepNhanId] int NULL,
        [NhanVienHuyId] int NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DatBan] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_DatBan_Coc] CHECK ([TienCocYeuCau]>=0),
        CONSTRAINT [CK_DatBan_LienHe] CHECK ([LaKhachTrucTiep]=1 OR ([SoDienThoaiLienHe] IS NOT NULL AND LEN([SoDienThoaiLienHe])>0)),
        CONSTRAINT [CK_DatBan_NhanHuy] CHECK (([TrangThai]<>'DaNhanBan' OR [ThoiDiemNhanBan] IS NOT NULL) AND ([TrangThai]<>'DaHuy' OR [ThoiDiemHuy] IS NOT NULL)),
        CONSTRAINT [CK_DatBan_SoKhach] CHECK ([SoNguoiLon]>=0 AND [SoTreEm]>=0 AND [SoNguoiLon]+[SoTreEm]>0),
        CONSTRAINT [CK_DatBan_ThoiGian] CHECK ([GioKetThucDuKien]>[GioDen]),
        CONSTRAINT [CK_DatBan_TrangThai_Enum] CHECK ([TrangThai] IN ('ChoXacNhan','ChoCoc','DaXacNhan','DaNhanBan','DaHuy','KhongDen')),
        CONSTRAINT [FK_DatBan_KhachHang_KhachHangId] FOREIGN KEY ([KhachHangId]) REFERENCES [KhachHang] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DatBan_NhanVien_NhanVienHuyId] FOREIGN KEY ([NhanVienHuyId]) REFERENCES [NhanVien] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DatBan_NhanVien_NhanVienTiepNhanId] FOREIGN KEY ([NhanVienTiepNhanId]) REFERENCES [NhanVien] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [PhieuNhap] (
        [Id] int NOT NULL IDENTITY,
        [MaPhieu] nvarchar(30) NOT NULL,
        [NhaCungCapId] int NULL,
        [NhanVienId] int NOT NULL,
        [ThoiDiem] datetimeoffset NOT NULL,
        [TrangThai] nvarchar(40) NOT NULL,
        [LyDo] nvarchar(40) NOT NULL,
        [GhiChu] nvarchar(500) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_PhieuNhap] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PhieuNhap_LyDo_Enum] CHECK ([LyDo] IN ('MuaHang','TonDauKy','DieuChinhTang')),
        CONSTRAINT [CK_PhieuNhap_NhaCungCap] CHECK ([LyDo]<>'MuaHang' OR [NhaCungCapId] IS NOT NULL),
        CONSTRAINT [CK_PhieuNhap_TrangThai_Enum] CHECK ([TrangThai] IN ('Nhap','DaGhiSo','DaHuy')),
        CONSTRAINT [FK_PhieuNhap_NhaCungCap_NhaCungCapId] FOREIGN KEY ([NhaCungCapId]) REFERENCES [NhaCungCap] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PhieuNhap_NhanVien_NhanVienId] FOREIGN KEY ([NhanVienId]) REFERENCES [NhanVien] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [ChiTietDatBan] (
        [DatBanId] int NOT NULL,
        [BanAnId] int NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_ChiTietDatBan] PRIMARY KEY ([DatBanId], [BanAnId]),
        CONSTRAINT [FK_ChiTietDatBan_BanAn_BanAnId] FOREIGN KEY ([BanAnId]) REFERENCES [BanAn] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiTietDatBan_DatBan_DatBanId] FOREIGN KEY ([DatBanId]) REFERENCES [DatBan] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [DonHang] (
        [Id] int NOT NULL IDENTITY,
        [MaDonHang] nvarchar(30) NOT NULL,
        [DatBanId] int NULL,
        [KhachHangId] int NULL,
        [NhanVienLapId] int NULL,
        [Loai] nvarchar(40) NOT NULL,
        [TrangThai] nvarchar(40) NOT NULL,
        [ThoiDiemTao] datetimeoffset NOT NULL,
        [TenNguoiNhan] nvarchar(120) NULL,
        [DienThoaiGiaoHang] nvarchar(20) NULL,
        [DiaChiGiaoHang] nvarchar(500) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DonHang] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_DonHang_Loai_Enum] CHECK ([Loai] IN ('TaiCho','GiaoHang')),
        CONSTRAINT [CK_DonHang_LoaiDon] CHECK (([Loai]='TaiCho' AND [DatBanId] IS NOT NULL) OR ([Loai]='GiaoHang' AND [DatBanId] IS NULL AND [TenNguoiNhan] IS NOT NULL AND [DienThoaiGiaoHang] IS NOT NULL AND [DiaChiGiaoHang] IS NOT NULL)),
        CONSTRAINT [CK_DonHang_TrangThai_Enum] CHECK ([TrangThai] IN ('Moi','DaXacNhan','DangPhucVu','DangGiao','HoanTat','DaHuy')),
        CONSTRAINT [FK_DonHang_DatBan_DatBanId] FOREIGN KEY ([DatBanId]) REFERENCES [DatBan] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DonHang_KhachHang_KhachHangId] FOREIGN KEY ([KhachHangId]) REFERENCES [KhachHang] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DonHang_NhanVien_NhanVienLapId] FOREIGN KEY ([NhanVienLapId]) REFERENCES [NhanVien] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [MonDatTruoc] (
        [Id] int NOT NULL IDENTITY,
        [DatBanId] int NOT NULL,
        [MonAnId] int NOT NULL,
        [TenMonLucDat] nvarchar(150) NOT NULL,
        [SoLuong] int NOT NULL,
        [DonGiaThoaThuan] decimal(18,2) NOT NULL,
        [YeuCauCheBien] nvarchar(500) NULL,
        [ThanhPhanSetSnapshot] nvarchar(4000) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_MonDatTruoc] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_MonDatTruoc_LuongGia] CHECK ([SoLuong]>0 AND [DonGiaThoaThuan]>=0),
        CONSTRAINT [FK_MonDatTruoc_DatBan_DatBanId] FOREIGN KEY ([DatBanId]) REFERENCES [DatBan] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MonDatTruoc_MonAn_MonAnId] FOREIGN KEY ([MonAnId]) REFERENCES [MonAn] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [ChiTietPhieuNhap] (
        [Id] int NOT NULL IDENTITY,
        [PhieuNhapId] int NOT NULL,
        [NguyenLieuId] int NOT NULL,
        [DonViTinhId] int NOT NULL,
        [SoLuong] decimal(18,6) NOT NULL,
        [HeSoQuyDoi] decimal(18,6) NOT NULL,
        [SoLuongCoSo] AS CONVERT(decimal(18,6),[SoLuong]*[HeSoQuyDoi]) PERSISTED,
        [DonGia] decimal(18,2) NOT NULL,
        [HanSuDung] date NULL,
        [MaLo] nvarchar(50) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_ChiTietPhieuNhap] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ChiTietPhieuNhap_LuongGia] CHECK ([SoLuong]>0 AND [HeSoQuyDoi]>0 AND [DonGia]>=0),
        CONSTRAINT [FK_ChiTietPhieuNhap_DonViTinh_DonViTinhId] FOREIGN KEY ([DonViTinhId]) REFERENCES [DonViTinh] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiTietPhieuNhap_NguyenLieu_NguyenLieuId] FOREIGN KEY ([NguyenLieuId]) REFERENCES [NguyenLieu] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiTietPhieuNhap_PhieuNhap_PhieuNhapId] FOREIGN KEY ([PhieuNhapId]) REFERENCES [PhieuNhap] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [DonHangBan] (
        [Id] int NOT NULL IDENTITY,
        [DonHangId] int NOT NULL,
        [BanAnId] int NOT NULL,
        [BatDau] datetimeoffset NOT NULL,
        [KetThuc] datetimeoffset NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DonHangBan] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_DonHangBan_ThoiGian] CHECK ([KetThuc] IS NULL OR [KetThuc]>=[BatDau]),
        CONSTRAINT [FK_DonHangBan_BanAn_BanAnId] FOREIGN KEY ([BanAnId]) REFERENCES [BanAn] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DonHangBan_DonHang_DonHangId] FOREIGN KEY ([DonHangId]) REFERENCES [DonHang] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [HoaDon] (
        [Id] int NOT NULL IDENTITY,
        [SoHoaDon] nvarchar(30) NOT NULL,
        [DonHangId] int NOT NULL,
        [ThuNganId] int NOT NULL,
        [ThoiDiemLap] datetimeoffset NOT NULL,
        [TienMon] decimal(18,2) NOT NULL,
        [TienGiam] decimal(18,2) NOT NULL,
        [TienThue] decimal(18,2) NOT NULL,
        [PhiDichVu] decimal(18,2) NOT NULL,
        [PhiGiaoHang] decimal(18,2) NOT NULL,
        [TongThanhToan] AS [TienMon]-[TienGiam]+[TienThue]+[PhiDichVu]+[PhiGiaoHang] PERSISTED,
        [TrangThai] nvarchar(40) NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_HoaDon] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_HoaDon_Id_DonHangId] UNIQUE ([Id], [DonHangId]),
        CONSTRAINT [CK_HoaDon_SoTien] CHECK ([TienMon]>=0 AND [TienGiam]>=0 AND [TienGiam]<=[TienMon] AND [TienThue]>=0 AND [PhiDichVu]>=0 AND [PhiGiaoHang]>=0),
        CONSTRAINT [CK_HoaDon_TrangThai_Enum] CHECK ([TrangThai] IN ('ChuaThanhToan','ThanhToanMotPhan','DaThanhToan','DaHuy')),
        CONSTRAINT [FK_HoaDon_DonHang_DonHangId] FOREIGN KEY ([DonHangId]) REFERENCES [DonHang] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_HoaDon_NhanVien_ThuNganId] FOREIGN KEY ([ThuNganId]) REFERENCES [NhanVien] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [PhieuXuat] (
        [Id] int NOT NULL IDENTITY,
        [MaPhieu] nvarchar(30) NOT NULL,
        [NhanVienId] int NOT NULL,
        [DonHangId] int NULL,
        [ThoiDiem] datetimeoffset NOT NULL,
        [TrangThai] nvarchar(40) NOT NULL,
        [LyDo] nvarchar(40) NOT NULL,
        [GhiChu] nvarchar(500) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_PhieuXuat] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PhieuXuat_LyDo_Enum] CHECK ([LyDo] IN ('CheBien','ThanhLy','HuyHong','TraNhaCungCap','DieuChinhGiam')),
        CONSTRAINT [CK_PhieuXuat_TrangThai_Enum] CHECK ([TrangThai] IN ('Nhap','DaGhiSo','DaHuy')),
        CONSTRAINT [FK_PhieuXuat_DonHang_DonHangId] FOREIGN KEY ([DonHangId]) REFERENCES [DonHang] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PhieuXuat_NhanVien_NhanVienId] FOREIGN KEY ([NhanVienId]) REFERENCES [NhanVien] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [ChiTietDonHang] (
        [Id] int NOT NULL IDENTITY,
        [DonHangId] int NOT NULL,
        [MonAnId] int NOT NULL,
        [MonDatTruocId] int NULL,
        [TenMonLucBan] nvarchar(150) NOT NULL,
        [SoLuong] int NOT NULL,
        [DonGia] decimal(18,2) NOT NULL,
        [YeuCauCheBien] nvarchar(500) NULL,
        [ThanhPhanSetSnapshot] nvarchar(4000) NULL,
        [TrangThai] nvarchar(40) NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_ChiTietDonHang] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_ChiTietDonHang_Id_DonHangId] UNIQUE ([Id], [DonHangId]),
        CONSTRAINT [CK_ChiTietDonHang_LuongGia] CHECK ([SoLuong]>0 AND [DonGia]>=0),
        CONSTRAINT [CK_ChiTietDonHang_TrangThai_Enum] CHECK ([TrangThai] IN ('ChoCheBien','DangCheBien','SanSang','DaPhucVu','DaHuy')),
        CONSTRAINT [FK_ChiTietDonHang_DonHang_DonHangId] FOREIGN KEY ([DonHangId]) REFERENCES [DonHang] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiTietDonHang_MonAn_MonAnId] FOREIGN KEY ([MonAnId]) REFERENCES [MonAn] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiTietDonHang_MonDatTruoc_MonDatTruocId] FOREIGN KEY ([MonDatTruocId]) REFERENCES [MonDatTruoc] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [GiaoDichThanhToan] (
        [Id] int NOT NULL IDENTITY,
        [KhoaChongLap] uniqueidentifier NOT NULL,
        [DatBanId] int NULL,
        [HoaDonId] int NULL,
        [GiaoDichGocId] int NULL,
        [Loai] nvarchar(40) NOT NULL,
        [TrangThai] nvarchar(40) NOT NULL,
        [PhuongThuc] nvarchar(40) NOT NULL,
        [SoTien] decimal(18,2) NOT NULL,
        [ThoiDiem] datetimeoffset NOT NULL,
        [NhanVienId] int NULL,
        [MaThamChieu] nvarchar(150) NULL,
        [GhiChu] nvarchar(500) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_GiaoDichThanhToan] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_GiaoDichThanhToan_Dich] CHECK (([Loai] IN ('ThuCoc','HoanCoc') AND [DatBanId] IS NOT NULL AND [HoaDonId] IS NULL) OR ([Loai] IN ('ThuHoaDon','HoanThanhToan') AND [HoaDonId] IS NOT NULL AND [DatBanId] IS NULL)),
        CONSTRAINT [CK_GiaoDichThanhToan_Hoan] CHECK (([Loai] IN ('ThuCoc','ThuHoaDon') AND [GiaoDichGocId] IS NULL) OR ([Loai] IN ('HoanCoc','HoanThanhToan') AND [GiaoDichGocId] IS NOT NULL AND [GiaoDichGocId]<>[Id])),
        CONSTRAINT [CK_GiaoDichThanhToan_Loai_Enum] CHECK ([Loai] IN ('ThuCoc','ThuHoaDon','HoanCoc','HoanThanhToan')),
        CONSTRAINT [CK_GiaoDichThanhToan_PhuongThuc_Enum] CHECK ([PhuongThuc] IN ('TienMat','ChuyenKhoan','The','ViDienTu')),
        CONSTRAINT [CK_GiaoDichThanhToan_SoTien] CHECK ([SoTien]>0),
        CONSTRAINT [CK_GiaoDichThanhToan_TrangThai_Enum] CHECK ([TrangThai] IN ('ChoXuLy','ThanhCong','ThatBai')),
        CONSTRAINT [FK_GiaoDichThanhToan_DatBan_DatBanId] FOREIGN KEY ([DatBanId]) REFERENCES [DatBan] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_GiaoDichThanhToan_GiaoDichThanhToan_GiaoDichGocId] FOREIGN KEY ([GiaoDichGocId]) REFERENCES [GiaoDichThanhToan] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_GiaoDichThanhToan_HoaDon_HoaDonId] FOREIGN KEY ([HoaDonId]) REFERENCES [HoaDon] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_GiaoDichThanhToan_NhanVien_NhanVienId] FOREIGN KEY ([NhanVienId]) REFERENCES [NhanVien] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [SuDungVoucher] (
        [Id] int NOT NULL IDENTITY,
        [VoucherId] int NOT NULL,
        [HoaDonId] int NOT NULL,
        [ThoiDiem] datetimeoffset NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_SuDungVoucher] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_SuDungVoucher_Id_HoaDonId] UNIQUE ([Id], [HoaDonId]),
        CONSTRAINT [FK_SuDungVoucher_HoaDon_HoaDonId] FOREIGN KEY ([HoaDonId]) REFERENCES [HoaDon] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SuDungVoucher_Voucher_VoucherId] FOREIGN KEY ([VoucherId]) REFERENCES [Voucher] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [ChiTietPhieuXuat] (
        [Id] int NOT NULL IDENTITY,
        [PhieuXuatId] int NOT NULL,
        [NguyenLieuId] int NOT NULL,
        [DonViTinhId] int NOT NULL,
        [SoLuong] decimal(18,6) NOT NULL,
        [HeSoQuyDoi] decimal(18,6) NOT NULL,
        [SoLuongCoSo] AS CONVERT(decimal(18,6),[SoLuong]*[HeSoQuyDoi]) PERSISTED,
        [ChiTietPhieuNhapId] int NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_ChiTietPhieuXuat] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ChiTietPhieuXuat_SoLuong] CHECK ([SoLuong]>0 AND [HeSoQuyDoi]>0),
        CONSTRAINT [FK_ChiTietPhieuXuat_ChiTietPhieuNhap_ChiTietPhieuNhapId] FOREIGN KEY ([ChiTietPhieuNhapId]) REFERENCES [ChiTietPhieuNhap] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiTietPhieuXuat_DonViTinh_DonViTinhId] FOREIGN KEY ([DonViTinhId]) REFERENCES [DonViTinh] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiTietPhieuXuat_NguyenLieu_NguyenLieuId] FOREIGN KEY ([NguyenLieuId]) REFERENCES [NguyenLieu] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiTietPhieuXuat_PhieuXuat_PhieuXuatId] FOREIGN KEY ([PhieuXuatId]) REFERENCES [PhieuXuat] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [DanhGia] (
        [Id] int NOT NULL IDENTITY,
        [DonHangId] int NOT NULL,
        [ChiTietDonHangId] int NULL,
        [KhachHangId] int NULL,
        [Diem] int NOT NULL,
        [NoiDung] nvarchar(2000) NULL,
        [ThoiDiem] datetimeoffset NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DanhGia] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_DanhGia_Diem] CHECK ([Diem] BETWEEN 1 AND 5),
        CONSTRAINT [FK_DanhGia_ChiTietDonHang_ChiTietDonHangId_DonHangId] FOREIGN KEY ([ChiTietDonHangId], [DonHangId]) REFERENCES [ChiTietDonHang] ([Id], [DonHangId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DanhGia_DonHang_DonHangId] FOREIGN KEY ([DonHangId]) REFERENCES [DonHang] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DanhGia_KhachHang_KhachHangId] FOREIGN KEY ([KhachHangId]) REFERENCES [KhachHang] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [DoiTruCoc] (
        [GiaoDichCocId] int NOT NULL,
        [HoaDonId] int NOT NULL,
        [SoTien] decimal(18,2) NOT NULL,
        [ThoiDiem] datetimeoffset NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DoiTruCoc] PRIMARY KEY ([GiaoDichCocId]),
        CONSTRAINT [CK_DoiTruCoc_SoTien] CHECK ([SoTien]>0),
        CONSTRAINT [FK_DoiTruCoc_GiaoDichThanhToan_GiaoDichCocId] FOREIGN KEY ([GiaoDichCocId]) REFERENCES [GiaoDichThanhToan] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DoiTruCoc_HoaDon_HoaDonId] FOREIGN KEY ([HoaDonId]) REFERENCES [HoaDon] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE TABLE [ApDungKhuyenMai] (
        [Id] int NOT NULL IDENTITY,
        [HoaDonId] int NOT NULL,
        [DonHangId] int NOT NULL,
        [ChiTietDonHangId] int NULL,
        [KhuyenMaiId] int NOT NULL,
        [SuDungVoucherId] int NULL,
        [TenChuongTrinhLucApDung] nvarchar(150) NOT NULL,
        [KieuGiamLucApDung] nvarchar(40) NOT NULL,
        [GiaTriLucApDung] decimal(18,2) NOT NULL,
        [ThuTu] int NOT NULL,
        [CoSoTinhGiam] decimal(18,2) NOT NULL,
        [SoTienGiam] decimal(18,2) NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_ApDungKhuyenMai] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ApDungKhuyenMai_KieuGiamLucApDung_Enum] CHECK ([KieuGiamLucApDung] IN ('PhanTram','SoTien')),
        CONSTRAINT [CK_ApDungKhuyenMai_SoTien] CHECK ([SoTienGiam]>=0 AND [CoSoTinhGiam]>=[SoTienGiam] AND [GiaTriLucApDung]>0 AND [ThuTu]>=0),
        CONSTRAINT [FK_ApDungKhuyenMai_ChiTietDonHang_ChiTietDonHangId_DonHangId] FOREIGN KEY ([ChiTietDonHangId], [DonHangId]) REFERENCES [ChiTietDonHang] ([Id], [DonHangId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ApDungKhuyenMai_HoaDon_HoaDonId_DonHangId] FOREIGN KEY ([HoaDonId], [DonHangId]) REFERENCES [HoaDon] ([Id], [DonHangId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ApDungKhuyenMai_KhuyenMai_KhuyenMaiId] FOREIGN KEY ([KhuyenMaiId]) REFERENCES [KhuyenMai] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ApDungKhuyenMai_SuDungVoucher_SuDungVoucherId_HoaDonId] FOREIGN KEY ([SuDungVoucherId], [HoaDonId]) REFERENCES [SuDungVoucher] ([Id], [HoaDonId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ApDungKhuyenMai_ChiTietDonHangId_DonHangId] ON [ApDungKhuyenMai] ([ChiTietDonHangId], [DonHangId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ApDungKhuyenMai_HoaDonId_DonHangId] ON [ApDungKhuyenMai] ([HoaDonId], [DonHangId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ApDungKhuyenMai_HoaDonId_KhuyenMaiId_ChiTietDonHangId] ON [ApDungKhuyenMai] ([HoaDonId], [KhuyenMaiId], [ChiTietDonHangId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ApDungKhuyenMai_KhuyenMaiId] ON [ApDungKhuyenMai] ([KhuyenMaiId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ApDungKhuyenMai_SuDungVoucherId_HoaDonId] ON [ApDungKhuyenMai] ([SuDungVoucherId], [HoaDonId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_BanAn_KhuVucId] ON [BanAn] ([KhuVucId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BanAn_MaBan] ON [BanAn] ([MaBan]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ChiTietDatBan_BanAnId] ON [ChiTietDatBan] ([BanAnId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ChiTietDonHang_DonHangId] ON [ChiTietDonHang] ([DonHangId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ChiTietDonHang_MonAnId] ON [ChiTietDonHang] ([MonAnId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ChiTietDonHang_MonDatTruocId] ON [ChiTietDonHang] ([MonDatTruocId]) WHERE [MonDatTruocId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ChiTietPhieuNhap_DonViTinhId] ON [ChiTietPhieuNhap] ([DonViTinhId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ChiTietPhieuNhap_NguyenLieuId] ON [ChiTietPhieuNhap] ([NguyenLieuId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ChiTietPhieuNhap_PhieuNhapId] ON [ChiTietPhieuNhap] ([PhieuNhapId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ChiTietPhieuXuat_ChiTietPhieuNhapId] ON [ChiTietPhieuXuat] ([ChiTietPhieuNhapId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ChiTietPhieuXuat_DonViTinhId] ON [ChiTietPhieuXuat] ([DonViTinhId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ChiTietPhieuXuat_NguyenLieuId] ON [ChiTietPhieuXuat] ([NguyenLieuId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ChiTietPhieuXuat_PhieuXuatId] ON [ChiTietPhieuXuat] ([PhieuXuatId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_DangNhapNgoai_UserId] ON [DangNhapNgoai] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_DanhGia_ChiTietDonHangId_DonHangId] ON [DanhGia] ([ChiTietDonHangId], [DonHangId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DanhGia_DonHangId_ChiTietDonHangId] ON [DanhGia] ([DonHangId], [ChiTietDonHangId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_DanhGia_KhachHangId] ON [DanhGia] ([KhachHangId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DanhMuc_TenDanhMuc] ON [DanhMuc] ([TenDanhMuc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_DatBan_KhachHangId] ON [DatBan] ([KhachHangId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DatBan_MaDatBan] ON [DatBan] ([MaDatBan]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_DatBan_NhanVienHuyId] ON [DatBan] ([NhanVienHuyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_DatBan_NhanVienTiepNhanId] ON [DatBan] ([NhanVienTiepNhanId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_DatBan_TrangThai_GioDen_GioKetThucDuKien] ON [DatBan] ([TrangThai], [GioDen], [GioKetThucDuKien]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_DinhLuongMon_NguyenLieuId] ON [DinhLuongMon] ([NguyenLieuId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_DoiTruCoc_HoaDonId] ON [DoiTruCoc] ([HoaDonId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_DonHang_DatBanId] ON [DonHang] ([DatBanId]) WHERE [DatBanId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_DonHang_KhachHangId] ON [DonHang] ([KhachHangId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DonHang_MaDonHang] ON [DonHang] ([MaDonHang]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_DonHang_NhanVienLapId] ON [DonHang] ([NhanVienLapId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_DonHangBan_BanAnId] ON [DonHangBan] ([BanAnId]) WHERE [KetThuc] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DonHangBan_DonHangId_BanAnId_BatDau] ON [DonHangBan] ([DonHangId], [BanAnId], [BatDau]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DonViTinh_TenDonVi] ON [DonViTinh] ([TenDonVi]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_GiaoDichThanhToan_DatBanId] ON [GiaoDichThanhToan] ([DatBanId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_GiaoDichThanhToan_GiaoDichGocId] ON [GiaoDichThanhToan] ([GiaoDichGocId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_GiaoDichThanhToan_HoaDonId] ON [GiaoDichThanhToan] ([HoaDonId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_GiaoDichThanhToan_KhoaChongLap] ON [GiaoDichThanhToan] ([KhoaChongLap]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_GiaoDichThanhToan_NhanVienId] ON [GiaoDichThanhToan] ([NhanVienId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_HoaDon_DonHangId] ON [HoaDon] ([DonHangId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_HoaDon_SoHoaDon] ON [HoaDon] ([SoHoaDon]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_HoaDon_ThuNganId] ON [HoaDon] ([ThuNganId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_KhachHang_SoDienThoai] ON [KhachHang] ([SoDienThoai]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_KhachHang_TaiKhoanId] ON [KhachHang] ([TaiKhoanId]) WHERE [TaiKhoanId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_KhuyenMaiMon_MonAnId] ON [KhuyenMaiMon] ([MonAnId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_MonAn_DanhMucId] ON [MonAn] ([DanhMucId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_MonDatTruoc_DatBanId] ON [MonDatTruoc] ([DatBanId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_MonDatTruoc_MonAnId] ON [MonDatTruoc] ([MonAnId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_NguyenLieu_DonViCoSoId] ON [NguyenLieu] ([DonViCoSoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NhanVien_MaNhanVien] ON [NhanVien] ([MaNhanVien]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_NhanVien_TaiKhoanId] ON [NhanVien] ([TaiKhoanId]) WHERE [TaiKhoanId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PhieuNhap_MaPhieu] ON [PhieuNhap] ([MaPhieu]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_PhieuNhap_NhaCungCapId] ON [PhieuNhap] ([NhaCungCapId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_PhieuNhap_NhanVienId] ON [PhieuNhap] ([NhanVienId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_PhieuXuat_DonHangId] ON [PhieuXuat] ([DonHangId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PhieuXuat_MaPhieu] ON [PhieuXuat] ([MaPhieu]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_PhieuXuat_NhanVienId] ON [PhieuXuat] ([NhanVienId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_QuyDoiNguyenLieu_DonViTinhId] ON [QuyDoiNguyenLieu] ([DonViTinhId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_SuDungVoucher_HoaDonId] ON [SuDungVoucher] ([HoaDonId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SuDungVoucher_VoucherId_HoaDonId] ON [SuDungVoucher] ([VoucherId], [HoaDonId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [TaiKhoan] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [TaiKhoan] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_TaiKhoanClaim_UserId] ON [TaiKhoanClaim] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_TaiKhoanVaiTro_RoleId] ON [TaiKhoanVaiTro] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ThanhPhanSet_MonAnId] ON [ThanhPhanSet] ([MonAnId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [VaiTro] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_VaiTroClaim_RoleId] ON [VaiTroClaim] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Voucher_KhachHangId] ON [Voucher] ([KhachHangId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Voucher_KhuyenMaiId] ON [Voucher] ([KhuyenMaiId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Voucher_Ma] ON [Voucher] ([Ma]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918120613_InitialSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260918120613_InitialSchema', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [ChiTietPhieuNhap] DROP CONSTRAINT [FK_ChiTietPhieuNhap_DonViTinh_DonViTinhId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [ChiTietPhieuXuat] DROP CONSTRAINT [FK_ChiTietPhieuXuat_DonViTinh_DonViTinhId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [DanhGia] DROP CONSTRAINT [FK_DanhGia_ChiTietDonHang_ChiTietDonHangId_DonHangId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [DanhGia] DROP CONSTRAINT [FK_DanhGia_DonHang_DonHangId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [HoaDon] DROP CONSTRAINT [FK_HoaDon_DonHang_DonHangId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [HoaDon] DROP CONSTRAINT [FK_HoaDon_NhanVien_ThuNganId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [NguyenLieu] DROP CONSTRAINT [FK_NguyenLieu_DonViTinh_DonViCoSoId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [PhieuXuat] DROP CONSTRAINT [FK_PhieuXuat_DonHang_DonHangId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    DROP TABLE [ApDungKhuyenMai];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    DROP TABLE [DinhLuongMon];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    DROP TABLE [DoiTruCoc];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    DROP TABLE [DonHangBan];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    DROP TABLE [QuyDoiNguyenLieu];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    DROP TABLE [ThanhPhanSet];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    DROP TABLE [ChiTietDonHang];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    DROP TABLE [SuDungVoucher];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    DROP TABLE [GiaoDichThanhToan];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    DROP TABLE [DonViTinh];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    DROP TABLE [DonHang];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    DROP INDEX [IX_NguyenLieu_DonViCoSoId] ON [NguyenLieu];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [MonAn] DROP CONSTRAINT [CK_MonAn_Gia];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [HoaDon] DROP CONSTRAINT [AK_HoaDon_Id_DonHangId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    DROP INDEX [IX_HoaDon_DonHangId] ON [HoaDon];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [HoaDon] DROP CONSTRAINT [CK_HoaDon_SoTien];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [DatBan] DROP CONSTRAINT [CK_DatBan_Coc];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    DROP INDEX [IX_ChiTietPhieuXuat_DonViTinhId] ON [ChiTietPhieuXuat];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [ChiTietPhieuXuat] DROP CONSTRAINT [CK_ChiTietPhieuXuat_SoLuong];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    DROP INDEX [IX_ChiTietPhieuNhap_DonViTinhId] ON [ChiTietPhieuNhap];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [ChiTietPhieuNhap] DROP CONSTRAINT [CK_ChiTietPhieuNhap_LuongGia];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ChiTietPhieuXuat]') AND [c].[name] = N'SoLuongCoSo');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [ChiTietPhieuXuat] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [ChiTietPhieuXuat] DROP COLUMN [SoLuongCoSo];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ChiTietPhieuNhap]') AND [c].[name] = N'SoLuongCoSo');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [ChiTietPhieuNhap] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [ChiTietPhieuNhap] DROP COLUMN [SoLuongCoSo];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[NguyenLieu]') AND [c].[name] = N'DonViCoSoId');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [NguyenLieu] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [NguyenLieu] DROP COLUMN [DonViCoSoId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MonAn]') AND [c].[name] = N'GiaBan');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [MonAn] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [MonAn] DROP COLUMN [GiaBan];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[HoaDon]') AND [c].[name] = N'TongThanhToan');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [HoaDon] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [HoaDon] DROP COLUMN [TongThanhToan];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[HoaDon]') AND [c].[name] = N'DonHangId');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [HoaDon] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [HoaDon] DROP COLUMN [DonHangId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[HoaDon]') AND [c].[name] = N'PhiDichVu');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [HoaDon] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [HoaDon] DROP COLUMN [PhiDichVu];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[HoaDon]') AND [c].[name] = N'PhiGiaoHang');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [HoaDon] DROP CONSTRAINT ' + @var7 + ';');
    ALTER TABLE [HoaDon] DROP COLUMN [PhiGiaoHang];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    DECLARE @var8 nvarchar(max);
    SELECT @var8 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ChiTietPhieuXuat]') AND [c].[name] = N'DonViTinhId');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [ChiTietPhieuXuat] DROP CONSTRAINT ' + @var8 + ';');
    ALTER TABLE [ChiTietPhieuXuat] DROP COLUMN [DonViTinhId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    DECLARE @var9 nvarchar(max);
    SELECT @var9 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ChiTietPhieuXuat]') AND [c].[name] = N'HeSoQuyDoi');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [ChiTietPhieuXuat] DROP CONSTRAINT ' + @var9 + ';');
    ALTER TABLE [ChiTietPhieuXuat] DROP COLUMN [HeSoQuyDoi];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    DECLARE @var10 nvarchar(max);
    SELECT @var10 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ChiTietPhieuNhap]') AND [c].[name] = N'DonViTinhId');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [ChiTietPhieuNhap] DROP CONSTRAINT ' + @var10 + ';');
    ALTER TABLE [ChiTietPhieuNhap] DROP COLUMN [DonViTinhId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    DECLARE @var11 nvarchar(max);
    SELECT @var11 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ChiTietPhieuNhap]') AND [c].[name] = N'HeSoQuyDoi');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [ChiTietPhieuNhap] DROP CONSTRAINT ' + @var11 + ';');
    ALTER TABLE [ChiTietPhieuNhap] DROP COLUMN [HeSoQuyDoi];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    EXEC sp_rename N'[PhieuXuat].[DonHangId]', N'HoaDonId', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    EXEC sp_rename N'[PhieuXuat].[IX_PhieuXuat_DonHangId]', N'IX_PhieuXuat_HoaDonId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    EXEC sp_rename N'[MonDatTruoc].[ThanhPhanSetSnapshot]', N'ChiTietComboSnapshot', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    EXEC sp_rename N'[HoaDon].[TienThue]', N'TongTienHang', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    EXEC sp_rename N'[HoaDon].[TienMon]', N'TienCocDaTru', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    EXEC sp_rename N'[HoaDon].[ThuNganId]', N'NhanVienId', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    EXEC sp_rename N'[HoaDon].[SoHoaDon]', N'MaHoaDon', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    EXEC sp_rename N'[HoaDon].[IX_HoaDon_ThuNganId]', N'IX_HoaDon_NhanVienId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    EXEC sp_rename N'[HoaDon].[IX_HoaDon_SoHoaDon]', N'IX_HoaDon_MaHoaDon', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    EXEC sp_rename N'[DanhGia].[DonHangId]', N'HoaDonId', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    EXEC sp_rename N'[DanhGia].[ChiTietDonHangId]', N'ChiTietHoaDonId', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    EXEC sp_rename N'[DanhGia].[IX_DanhGia_DonHangId_ChiTietDonHangId]', N'IX_DanhGia_HoaDonId_ChiTietHoaDonId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    EXEC sp_rename N'[DanhGia].[IX_DanhGia_ChiTietDonHangId_DonHangId]', N'IX_DanhGia_ChiTietHoaDonId_HoaDonId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [NguyenLieu] ADD [DonViTinh] nvarchar(30) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [MonDatTruoc] ADD [MonAnSizeId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [HoaDon] ADD [DatBanId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [HoaDon] ADD [KhachHangId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [HoaDon] ADD [PhuongThucThanhToan] nvarchar(40) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [HoaDon] ADD [ThoiDiemThanhToan] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [HoaDon] ADD [VoucherId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [DatBan] ADD [ThoiDiemCoc] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [DatBan] ADD [TienCocDaNop] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [DatBan] ADD [TrangThaiCoc] nvarchar(40) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    EXEC(N'ALTER TABLE [HoaDon] ADD [TongThanhToan] AS [TongTienHang]-[TienGiam]-[TienCocDaTru] PERSISTED');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    CREATE TABLE [ChiTietCombo] (
        [ComboId] int NOT NULL,
        [MonAnId] int NOT NULL,
        [SoLuong] int NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_ChiTietCombo] PRIMARY KEY ([ComboId], [MonAnId]),
        CONSTRAINT [CK_ChiTietCombo_ThanhPhan] CHECK ([ComboId]<>[MonAnId] AND [SoLuong]>0),
        CONSTRAINT [FK_ChiTietCombo_MonAn_ComboId] FOREIGN KEY ([ComboId]) REFERENCES [MonAn] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiTietCombo_MonAn_MonAnId] FOREIGN KEY ([MonAnId]) REFERENCES [MonAn] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    CREATE TABLE [DinhMucMon] (
        [MonAnId] int NOT NULL,
        [NguyenLieuId] int NOT NULL,
        [SoLuong] decimal(18,6) NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DinhMucMon] PRIMARY KEY ([MonAnId], [NguyenLieuId]),
        CONSTRAINT [CK_DinhMucMon_SoLuong] CHECK ([SoLuong]>0),
        CONSTRAINT [FK_DinhMucMon_MonAn_MonAnId] FOREIGN KEY ([MonAnId]) REFERENCES [MonAn] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DinhMucMon_NguyenLieu_NguyenLieuId] FOREIGN KEY ([NguyenLieuId]) REFERENCES [NguyenLieu] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    CREATE TABLE [MonAnSize] (
        [Id] int NOT NULL IDENTITY,
        [MonAnId] int NOT NULL,
        [TenSize] nvarchar(50) NOT NULL,
        [GiaBan] decimal(18,2) NOT NULL,
        [DangSuDung] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_MonAnSize] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_MonAnSize_Gia] CHECK ([GiaBan]>=0),
        CONSTRAINT [FK_MonAnSize_MonAn_MonAnId] FOREIGN KEY ([MonAnId]) REFERENCES [MonAn] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    CREATE TABLE [ChiTietHoaDon] (
        [Id] int NOT NULL IDENTITY,
        [HoaDonId] int NOT NULL,
        [MonAnId] int NOT NULL,
        [MonAnSizeId] int NULL,
        [TenMonLucBan] nvarchar(150) NOT NULL,
        [SoLuong] int NOT NULL,
        [DonGia] decimal(18,2) NOT NULL,
        [YeuCauCheBien] nvarchar(500) NULL,
        [ChiTietComboSnapshot] nvarchar(4000) NULL,
        [TrangThai] nvarchar(40) NOT NULL,
        [MonDatTruocId] int NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_ChiTietHoaDon] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_ChiTietHoaDon_Id_HoaDonId] UNIQUE ([Id], [HoaDonId]),
        CONSTRAINT [CK_ChiTietHoaDon_LuongGia] CHECK ([SoLuong]>0 AND [DonGia]>=0),
        CONSTRAINT [CK_ChiTietHoaDon_TrangThai_Enum] CHECK ([TrangThai] IN ('ChoCheBien','DangCheBien','SanSang','DaPhucVu','DaHuy')),
        CONSTRAINT [FK_ChiTietHoaDon_HoaDon_HoaDonId] FOREIGN KEY ([HoaDonId]) REFERENCES [HoaDon] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiTietHoaDon_MonAnSize_MonAnSizeId] FOREIGN KEY ([MonAnSizeId]) REFERENCES [MonAnSize] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiTietHoaDon_MonAn_MonAnId] FOREIGN KEY ([MonAnId]) REFERENCES [MonAn] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiTietHoaDon_MonDatTruoc_MonDatTruocId] FOREIGN KEY ([MonDatTruocId]) REFERENCES [MonDatTruoc] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NguyenLieu_TenNguyenLieu] ON [NguyenLieu] ([TenNguyenLieu]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    CREATE INDEX [IX_MonDatTruoc_MonAnSizeId] ON [MonDatTruoc] ([MonAnSizeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    CREATE INDEX [IX_HoaDon_DatBanId] ON [HoaDon] ([DatBanId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    CREATE INDEX [IX_HoaDon_KhachHangId] ON [HoaDon] ([KhachHangId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    CREATE INDEX [IX_HoaDon_VoucherId] ON [HoaDon] ([VoucherId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    EXEC(N'ALTER TABLE [HoaDon] ADD CONSTRAINT [CK_HoaDon_PhuongThucThanhToan_Enum] CHECK ([PhuongThucThanhToan] IN (''TienMat'',''ChuyenKhoan'',''The'',''ViDienTu''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    EXEC(N'ALTER TABLE [HoaDon] ADD CONSTRAINT [CK_HoaDon_SoTien] CHECK ([TongTienHang]>=0 AND [TienGiam]>=0 AND [TienGiam]<=[TongTienHang] AND [TienCocDaTru]>=0 AND [TienCocDaTru]<=[TongTienHang]-[TienGiam])');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    EXEC(N'ALTER TABLE [DatBan] ADD CONSTRAINT [CK_DatBan_Coc] CHECK ([TienCocYeuCau]>=0 AND [TienCocDaNop]>=0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    EXEC(N'ALTER TABLE [DatBan] ADD CONSTRAINT [CK_DatBan_TrangThaiCoc_Enum] CHECK ([TrangThaiCoc] IN (''ChuaCoc'',''DaCoc'',''DaHoan'',''DaDoiTru''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    EXEC(N'ALTER TABLE [ChiTietPhieuXuat] ADD CONSTRAINT [CK_ChiTietPhieuXuat_SoLuong] CHECK ([SoLuong]>0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    EXEC(N'ALTER TABLE [ChiTietPhieuNhap] ADD CONSTRAINT [CK_ChiTietPhieuNhap_LuongGia] CHECK ([SoLuong]>0 AND [DonGia]>=0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    CREATE INDEX [IX_ChiTietCombo_MonAnId] ON [ChiTietCombo] ([MonAnId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    CREATE INDEX [IX_ChiTietHoaDon_HoaDonId] ON [ChiTietHoaDon] ([HoaDonId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    CREATE INDEX [IX_ChiTietHoaDon_MonAnId] ON [ChiTietHoaDon] ([MonAnId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    CREATE INDEX [IX_ChiTietHoaDon_MonAnSizeId] ON [ChiTietHoaDon] ([MonAnSizeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ChiTietHoaDon_MonDatTruocId] ON [ChiTietHoaDon] ([MonDatTruocId]) WHERE [MonDatTruocId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    CREATE INDEX [IX_DinhMucMon_NguyenLieuId] ON [DinhMucMon] ([NguyenLieuId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MonAnSize_MonAnId_TenSize] ON [MonAnSize] ([MonAnId], [TenSize]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [DanhGia] ADD CONSTRAINT [FK_DanhGia_ChiTietHoaDon_ChiTietHoaDonId_HoaDonId] FOREIGN KEY ([ChiTietHoaDonId], [HoaDonId]) REFERENCES [ChiTietHoaDon] ([Id], [HoaDonId]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [DanhGia] ADD CONSTRAINT [FK_DanhGia_HoaDon_HoaDonId] FOREIGN KEY ([HoaDonId]) REFERENCES [HoaDon] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [HoaDon] ADD CONSTRAINT [FK_HoaDon_DatBan_DatBanId] FOREIGN KEY ([DatBanId]) REFERENCES [DatBan] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [HoaDon] ADD CONSTRAINT [FK_HoaDon_KhachHang_KhachHangId] FOREIGN KEY ([KhachHangId]) REFERENCES [KhachHang] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [HoaDon] ADD CONSTRAINT [FK_HoaDon_NhanVien_NhanVienId] FOREIGN KEY ([NhanVienId]) REFERENCES [NhanVien] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [HoaDon] ADD CONSTRAINT [FK_HoaDon_Voucher_VoucherId] FOREIGN KEY ([VoucherId]) REFERENCES [Voucher] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [MonDatTruoc] ADD CONSTRAINT [FK_MonDatTruoc_MonAnSize_MonAnSizeId] FOREIGN KEY ([MonAnSizeId]) REFERENCES [MonAnSize] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    ALTER TABLE [PhieuXuat] ADD CONSTRAINT [FK_PhieuXuat_HoaDon_HoaDonId] FOREIGN KEY ([HoaDonId]) REFERENCES [HoaDon] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919024114_SimplifySchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260919024114_SimplifySchema', N'10.0.12');
END;

COMMIT;
GO

