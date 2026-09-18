using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DanhMuc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDanhMuc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DangSuDung = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhMuc", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DonViTinh",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDonVi = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonViTinh", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KhuVuc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenKhuVuc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LaPhongVip = table.Column<bool>(type: "bit", nullable: false),
                    DangSuDung = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhuVuc", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KhuyenMai",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenChuongTrinh = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    BatDau = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    KetThuc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PhamVi = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    KieuGiam = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    GiaTri = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MucGiamToiDa = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    GiaTriToiThieu = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ChoPhepKetHop = table.Column<bool>(type: "bit", nullable: false),
                    ThuTuApDung = table.Column<int>(type: "int", nullable: false),
                    CanVoucher = table.Column<bool>(type: "bit", nullable: false),
                    DangSuDung = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhuyenMai", x => x.Id);
                    table.CheckConstraint("CK_KhuyenMai_DieuKien", "[KetThuc]>[BatDau] AND [GiaTri]>0 AND ([KieuGiam]<>'PhanTram' OR [GiaTri]<=100) AND [GiaTriToiThieu]>=0 AND ([MucGiamToiDa] IS NULL OR [MucGiamToiDa]>0) AND [ThuTuApDung]>=0");
                    table.CheckConstraint("CK_KhuyenMai_KieuGiam_Enum", "[KieuGiam] IN ('PhanTram','SoTien')");
                    table.CheckConstraint("CK_KhuyenMai_PhamVi_Enum", "[PhamVi] IN ('MonAn','HoaDon')");
                });

            migrationBuilder.CreateTable(
                name: "NhaCungCap",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenNhaCungCap = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    MaSoThue = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DangSuDung = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhaCungCap", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VaiTro",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaiTro", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MonAn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DanhMucId = table.Column<int>(type: "int", nullable: false),
                    TenMon = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    HinhAnh = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Loai = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    GiaBan = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    LaMonMoi = table.Column<bool>(type: "bit", nullable: false),
                    LaMonNoiBat = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonAn", x => x.Id);
                    table.CheckConstraint("CK_MonAn_Gia", "[GiaBan]>=0");
                    table.CheckConstraint("CK_MonAn_Loai_Enum", "[Loai] IN ('MonLe','ThucUong','Set')");
                    table.CheckConstraint("CK_MonAn_TrangThai_Enum", "[TrangThai] IN ('DangPhucVu','TamHet','NgungKinhDoanh')");
                    table.ForeignKey(
                        name: "FK_MonAn_DanhMuc_DanhMucId",
                        column: x => x.DanhMucId,
                        principalTable: "DanhMuc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NguyenLieu",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenNguyenLieu = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DonViCoSoId = table.Column<int>(type: "int", nullable: false),
                    NguongCanhBao = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    DangSuDung = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguyenLieu", x => x.Id);
                    table.CheckConstraint("CK_NguyenLieu_Nguong", "[NguongCanhBao]>=0");
                    table.ForeignKey(
                        name: "FK_NguyenLieu_DonViTinh_DonViCoSoId",
                        column: x => x.DonViCoSoId,
                        principalTable: "DonViTinh",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BanAn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaBan = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    KhuVucId = table.Column<int>(type: "int", nullable: false),
                    SoChoNgoi = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanAn", x => x.Id);
                    table.CheckConstraint("CK_BanAn_SoCho", "[SoChoNgoi]>0");
                    table.CheckConstraint("CK_BanAn_TrangThai_Enum", "[TrangThai] IN ('SanSang','DangPhucVu','CanDon','NgungSuDung')");
                    table.ForeignKey(
                        name: "FK_BanAn_KhuVuc_KhuVucId",
                        column: x => x.KhuVucId,
                        principalTable: "KhuVuc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DangNhapNgoai",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DangNhapNgoai", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_DangNhapNgoai_TaiKhoan_UserId",
                        column: x => x.UserId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KhachHang",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoTen = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NgayDangKy = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DongYNhanUuDai = table.Column<bool>(type: "bit", nullable: false),
                    ThoiDiemDongYNhanUuDai = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DangSuDung = table.Column<bool>(type: "bit", nullable: false),
                    TaiKhoanId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhachHang", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KhachHang_TaiKhoan_TaiKhoanId",
                        column: x => x.TaiKhoanId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NhanVien",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNhanVien = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ChucVu = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    NgayVaoLam = table.Column<DateOnly>(type: "date", nullable: false),
                    DangLamViec = table.Column<bool>(type: "bit", nullable: false),
                    TaiKhoanId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhanVien", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NhanVien_TaiKhoan_TaiKhoanId",
                        column: x => x.TaiKhoanId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoanClaim",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoanClaim", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaiKhoanClaim_TaiKhoan_UserId",
                        column: x => x.UserId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TokenTaiKhoan",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenTaiKhoan", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_TokenTaiKhoan_TaiKhoan_UserId",
                        column: x => x.UserId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoanVaiTro",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoanVaiTro", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_TaiKhoanVaiTro_TaiKhoan_UserId",
                        column: x => x.UserId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaiKhoanVaiTro_VaiTro_RoleId",
                        column: x => x.RoleId,
                        principalTable: "VaiTro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VaiTroClaim",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaiTroClaim", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaiTroClaim_VaiTro_RoleId",
                        column: x => x.RoleId,
                        principalTable: "VaiTro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KhuyenMaiMon",
                columns: table => new
                {
                    KhuyenMaiId = table.Column<int>(type: "int", nullable: false),
                    MonAnId = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhuyenMaiMon", x => new { x.KhuyenMaiId, x.MonAnId });
                    table.ForeignKey(
                        name: "FK_KhuyenMaiMon_KhuyenMai_KhuyenMaiId",
                        column: x => x.KhuyenMaiId,
                        principalTable: "KhuyenMai",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KhuyenMaiMon_MonAn_MonAnId",
                        column: x => x.MonAnId,
                        principalTable: "MonAn",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ThanhPhanSet",
                columns: table => new
                {
                    SetId = table.Column<int>(type: "int", nullable: false),
                    MonAnId = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThanhPhanSet", x => new { x.SetId, x.MonAnId });
                    table.CheckConstraint("CK_ThanhPhanSet_ThanhPhan", "[SetId]<>[MonAnId] AND [SoLuong]>0");
                    table.ForeignKey(
                        name: "FK_ThanhPhanSet_MonAn_MonAnId",
                        column: x => x.MonAnId,
                        principalTable: "MonAn",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ThanhPhanSet_MonAn_SetId",
                        column: x => x.SetId,
                        principalTable: "MonAn",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DinhLuongMon",
                columns: table => new
                {
                    MonAnId = table.Column<int>(type: "int", nullable: false),
                    NguyenLieuId = table.Column<int>(type: "int", nullable: false),
                    SoLuongCoSo = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DinhLuongMon", x => new { x.MonAnId, x.NguyenLieuId });
                    table.CheckConstraint("CK_DinhLuongMon_SoLuong", "[SoLuongCoSo]>0");
                    table.ForeignKey(
                        name: "FK_DinhLuongMon_MonAn_MonAnId",
                        column: x => x.MonAnId,
                        principalTable: "MonAn",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DinhLuongMon_NguyenLieu_NguyenLieuId",
                        column: x => x.NguyenLieuId,
                        principalTable: "NguyenLieu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuyDoiNguyenLieu",
                columns: table => new
                {
                    NguyenLieuId = table.Column<int>(type: "int", nullable: false),
                    DonViTinhId = table.Column<int>(type: "int", nullable: false),
                    HeSoVeDonViCoSo = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuyDoiNguyenLieu", x => new { x.NguyenLieuId, x.DonViTinhId });
                    table.CheckConstraint("CK_QuyDoiNguyenLieu_HeSo", "[HeSoVeDonViCoSo]>0");
                    table.ForeignKey(
                        name: "FK_QuyDoiNguyenLieu_DonViTinh_DonViTinhId",
                        column: x => x.DonViTinhId,
                        principalTable: "DonViTinh",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuyDoiNguyenLieu_NguyenLieu_NguyenLieuId",
                        column: x => x.NguyenLieuId,
                        principalTable: "NguyenLieu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Voucher",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ma = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    KhuyenMaiId = table.Column<int>(type: "int", nullable: false),
                    KhachHangId = table.Column<int>(type: "int", nullable: true),
                    GioiHanTongLuot = table.Column<int>(type: "int", nullable: false),
                    DangSuDung = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Voucher", x => x.Id);
                    table.CheckConstraint("CK_Voucher_Luot", "[GioiHanTongLuot]>0");
                    table.ForeignKey(
                        name: "FK_Voucher_KhachHang_KhachHangId",
                        column: x => x.KhachHangId,
                        principalTable: "KhachHang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Voucher_KhuyenMai_KhuyenMaiId",
                        column: x => x.KhuyenMaiId,
                        principalTable: "KhuyenMai",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DatBan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDatBan = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    KhachHangId = table.Column<int>(type: "int", nullable: true),
                    HoTenLienHe = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SoDienThoaiLienHe = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LaKhachTrucTiep = table.Column<bool>(type: "bit", nullable: false),
                    ThoiDiemTao = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    GioDen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    GioKetThucDuKien = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SoNguoiLon = table.Column<int>(type: "int", nullable: false),
                    SoTreEm = table.Column<int>(type: "int", nullable: false),
                    YeuCau = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    YeuCauTrangTri = table.Column<bool>(type: "bit", nullable: false),
                    YeuCauVip = table.Column<bool>(type: "bit", nullable: false),
                    TienCocYeuCau = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DieuKienCocDaThoaThuan = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ThoiDiemNhanBan = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ThoiDiemHuy = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LyDoHuy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NhanVienTiepNhanId = table.Column<int>(type: "int", nullable: true),
                    NhanVienHuyId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatBan", x => x.Id);
                    table.CheckConstraint("CK_DatBan_Coc", "[TienCocYeuCau]>=0");
                    table.CheckConstraint("CK_DatBan_LienHe", "[LaKhachTrucTiep]=1 OR ([SoDienThoaiLienHe] IS NOT NULL AND LEN([SoDienThoaiLienHe])>0)");
                    table.CheckConstraint("CK_DatBan_NhanHuy", "([TrangThai]<>'DaNhanBan' OR [ThoiDiemNhanBan] IS NOT NULL) AND ([TrangThai]<>'DaHuy' OR [ThoiDiemHuy] IS NOT NULL)");
                    table.CheckConstraint("CK_DatBan_SoKhach", "[SoNguoiLon]>=0 AND [SoTreEm]>=0 AND [SoNguoiLon]+[SoTreEm]>0");
                    table.CheckConstraint("CK_DatBan_ThoiGian", "[GioKetThucDuKien]>[GioDen]");
                    table.CheckConstraint("CK_DatBan_TrangThai_Enum", "[TrangThai] IN ('ChoXacNhan','ChoCoc','DaXacNhan','DaNhanBan','DaHuy','KhongDen')");
                    table.ForeignKey(
                        name: "FK_DatBan_KhachHang_KhachHangId",
                        column: x => x.KhachHangId,
                        principalTable: "KhachHang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DatBan_NhanVien_NhanVienHuyId",
                        column: x => x.NhanVienHuyId,
                        principalTable: "NhanVien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DatBan_NhanVien_NhanVienTiepNhanId",
                        column: x => x.NhanVienTiepNhanId,
                        principalTable: "NhanVien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhieuNhap",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaPhieu = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NhaCungCapId = table.Column<int>(type: "int", nullable: true),
                    NhanVienId = table.Column<int>(type: "int", nullable: false),
                    ThoiDiem = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuNhap", x => x.Id);
                    table.CheckConstraint("CK_PhieuNhap_LyDo_Enum", "[LyDo] IN ('MuaHang','TonDauKy','DieuChinhTang')");
                    table.CheckConstraint("CK_PhieuNhap_NhaCungCap", "[LyDo]<>'MuaHang' OR [NhaCungCapId] IS NOT NULL");
                    table.CheckConstraint("CK_PhieuNhap_TrangThai_Enum", "[TrangThai] IN ('Nhap','DaGhiSo','DaHuy')");
                    table.ForeignKey(
                        name: "FK_PhieuNhap_NhaCungCap_NhaCungCapId",
                        column: x => x.NhaCungCapId,
                        principalTable: "NhaCungCap",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuNhap_NhanVien_NhanVienId",
                        column: x => x.NhanVienId,
                        principalTable: "NhanVien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietDatBan",
                columns: table => new
                {
                    DatBanId = table.Column<int>(type: "int", nullable: false),
                    BanAnId = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietDatBan", x => new { x.DatBanId, x.BanAnId });
                    table.ForeignKey(
                        name: "FK_ChiTietDatBan_BanAn_BanAnId",
                        column: x => x.BanAnId,
                        principalTable: "BanAn",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietDatBan_DatBan_DatBanId",
                        column: x => x.DatBanId,
                        principalTable: "DatBan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DonHang",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDonHang = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DatBanId = table.Column<int>(type: "int", nullable: true),
                    KhachHangId = table.Column<int>(type: "int", nullable: true),
                    NhanVienLapId = table.Column<int>(type: "int", nullable: true),
                    Loai = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ThoiDiemTao = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenNguoiNhan = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    DienThoaiGiaoHang = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DiaChiGiaoHang = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonHang", x => x.Id);
                    table.CheckConstraint("CK_DonHang_Loai_Enum", "[Loai] IN ('TaiCho','GiaoHang')");
                    table.CheckConstraint("CK_DonHang_LoaiDon", "([Loai]='TaiCho' AND [DatBanId] IS NOT NULL) OR ([Loai]='GiaoHang' AND [DatBanId] IS NULL AND [TenNguoiNhan] IS NOT NULL AND [DienThoaiGiaoHang] IS NOT NULL AND [DiaChiGiaoHang] IS NOT NULL)");
                    table.CheckConstraint("CK_DonHang_TrangThai_Enum", "[TrangThai] IN ('Moi','DaXacNhan','DangPhucVu','DangGiao','HoanTat','DaHuy')");
                    table.ForeignKey(
                        name: "FK_DonHang_DatBan_DatBanId",
                        column: x => x.DatBanId,
                        principalTable: "DatBan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonHang_KhachHang_KhachHangId",
                        column: x => x.KhachHangId,
                        principalTable: "KhachHang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonHang_NhanVien_NhanVienLapId",
                        column: x => x.NhanVienLapId,
                        principalTable: "NhanVien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MonDatTruoc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DatBanId = table.Column<int>(type: "int", nullable: false),
                    MonAnId = table.Column<int>(type: "int", nullable: false),
                    TenMonLucDat = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    DonGiaThoaThuan = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    YeuCauCheBien = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ThanhPhanSetSnapshot = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonDatTruoc", x => x.Id);
                    table.CheckConstraint("CK_MonDatTruoc_LuongGia", "[SoLuong]>0 AND [DonGiaThoaThuan]>=0");
                    table.ForeignKey(
                        name: "FK_MonDatTruoc_DatBan_DatBanId",
                        column: x => x.DatBanId,
                        principalTable: "DatBan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonDatTruoc_MonAn_MonAnId",
                        column: x => x.MonAnId,
                        principalTable: "MonAn",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietPhieuNhap",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhieuNhapId = table.Column<int>(type: "int", nullable: false),
                    NguyenLieuId = table.Column<int>(type: "int", nullable: false),
                    DonViTinhId = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    HeSoQuyDoi = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    SoLuongCoSo = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, computedColumnSql: "CONVERT(decimal(18,6),[SoLuong]*[HeSoQuyDoi])", stored: true),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    HanSuDung = table.Column<DateOnly>(type: "date", nullable: true),
                    MaLo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietPhieuNhap", x => x.Id);
                    table.CheckConstraint("CK_ChiTietPhieuNhap_LuongGia", "[SoLuong]>0 AND [HeSoQuyDoi]>0 AND [DonGia]>=0");
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuNhap_DonViTinh_DonViTinhId",
                        column: x => x.DonViTinhId,
                        principalTable: "DonViTinh",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuNhap_NguyenLieu_NguyenLieuId",
                        column: x => x.NguyenLieuId,
                        principalTable: "NguyenLieu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuNhap_PhieuNhap_PhieuNhapId",
                        column: x => x.PhieuNhapId,
                        principalTable: "PhieuNhap",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DonHangBan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonHangId = table.Column<int>(type: "int", nullable: false),
                    BanAnId = table.Column<int>(type: "int", nullable: false),
                    BatDau = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    KetThuc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonHangBan", x => x.Id);
                    table.CheckConstraint("CK_DonHangBan_ThoiGian", "[KetThuc] IS NULL OR [KetThuc]>=[BatDau]");
                    table.ForeignKey(
                        name: "FK_DonHangBan_BanAn_BanAnId",
                        column: x => x.BanAnId,
                        principalTable: "BanAn",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonHangBan_DonHang_DonHangId",
                        column: x => x.DonHangId,
                        principalTable: "DonHang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HoaDon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoHoaDon = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DonHangId = table.Column<int>(type: "int", nullable: false),
                    ThuNganId = table.Column<int>(type: "int", nullable: false),
                    ThoiDiemLap = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TienMon = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TienGiam = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TienThue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PhiDichVu = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PhiGiaoHang = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TongThanhToan = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, computedColumnSql: "[TienMon]-[TienGiam]+[TienThue]+[PhiDichVu]+[PhiGiaoHang]", stored: true),
                    TrangThai = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoaDon", x => x.Id);
                    table.UniqueConstraint("AK_HoaDon_Id_DonHangId", x => new { x.Id, x.DonHangId });
                    table.CheckConstraint("CK_HoaDon_SoTien", "[TienMon]>=0 AND [TienGiam]>=0 AND [TienGiam]<=[TienMon] AND [TienThue]>=0 AND [PhiDichVu]>=0 AND [PhiGiaoHang]>=0");
                    table.CheckConstraint("CK_HoaDon_TrangThai_Enum", "[TrangThai] IN ('ChuaThanhToan','ThanhToanMotPhan','DaThanhToan','DaHuy')");
                    table.ForeignKey(
                        name: "FK_HoaDon_DonHang_DonHangId",
                        column: x => x.DonHangId,
                        principalTable: "DonHang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HoaDon_NhanVien_ThuNganId",
                        column: x => x.ThuNganId,
                        principalTable: "NhanVien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhieuXuat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaPhieu = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NhanVienId = table.Column<int>(type: "int", nullable: false),
                    DonHangId = table.Column<int>(type: "int", nullable: true),
                    ThoiDiem = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuXuat", x => x.Id);
                    table.CheckConstraint("CK_PhieuXuat_LyDo_Enum", "[LyDo] IN ('CheBien','ThanhLy','HuyHong','TraNhaCungCap','DieuChinhGiam')");
                    table.CheckConstraint("CK_PhieuXuat_TrangThai_Enum", "[TrangThai] IN ('Nhap','DaGhiSo','DaHuy')");
                    table.ForeignKey(
                        name: "FK_PhieuXuat_DonHang_DonHangId",
                        column: x => x.DonHangId,
                        principalTable: "DonHang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuXuat_NhanVien_NhanVienId",
                        column: x => x.NhanVienId,
                        principalTable: "NhanVien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietDonHang",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonHangId = table.Column<int>(type: "int", nullable: false),
                    MonAnId = table.Column<int>(type: "int", nullable: false),
                    MonDatTruocId = table.Column<int>(type: "int", nullable: true),
                    TenMonLucBan = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    YeuCauCheBien = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ThanhPhanSetSnapshot = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietDonHang", x => x.Id);
                    table.UniqueConstraint("AK_ChiTietDonHang_Id_DonHangId", x => new { x.Id, x.DonHangId });
                    table.CheckConstraint("CK_ChiTietDonHang_LuongGia", "[SoLuong]>0 AND [DonGia]>=0");
                    table.CheckConstraint("CK_ChiTietDonHang_TrangThai_Enum", "[TrangThai] IN ('ChoCheBien','DangCheBien','SanSang','DaPhucVu','DaHuy')");
                    table.ForeignKey(
                        name: "FK_ChiTietDonHang_DonHang_DonHangId",
                        column: x => x.DonHangId,
                        principalTable: "DonHang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietDonHang_MonAn_MonAnId",
                        column: x => x.MonAnId,
                        principalTable: "MonAn",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietDonHang_MonDatTruoc_MonDatTruocId",
                        column: x => x.MonDatTruocId,
                        principalTable: "MonDatTruoc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GiaoDichThanhToan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KhoaChongLap = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DatBanId = table.Column<int>(type: "int", nullable: true),
                    HoaDonId = table.Column<int>(type: "int", nullable: true),
                    GiaoDichGocId = table.Column<int>(type: "int", nullable: true),
                    Loai = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PhuongThuc = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ThoiDiem = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    NhanVienId = table.Column<int>(type: "int", nullable: true),
                    MaThamChieu = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiaoDichThanhToan", x => x.Id);
                    table.CheckConstraint("CK_GiaoDichThanhToan_Dich", "([Loai] IN ('ThuCoc','HoanCoc') AND [DatBanId] IS NOT NULL AND [HoaDonId] IS NULL) OR ([Loai] IN ('ThuHoaDon','HoanThanhToan') AND [HoaDonId] IS NOT NULL AND [DatBanId] IS NULL)");
                    table.CheckConstraint("CK_GiaoDichThanhToan_Hoan", "([Loai] IN ('ThuCoc','ThuHoaDon') AND [GiaoDichGocId] IS NULL) OR ([Loai] IN ('HoanCoc','HoanThanhToan') AND [GiaoDichGocId] IS NOT NULL AND [GiaoDichGocId]<>[Id])");
                    table.CheckConstraint("CK_GiaoDichThanhToan_Loai_Enum", "[Loai] IN ('ThuCoc','ThuHoaDon','HoanCoc','HoanThanhToan')");
                    table.CheckConstraint("CK_GiaoDichThanhToan_PhuongThuc_Enum", "[PhuongThuc] IN ('TienMat','ChuyenKhoan','The','ViDienTu')");
                    table.CheckConstraint("CK_GiaoDichThanhToan_SoTien", "[SoTien]>0");
                    table.CheckConstraint("CK_GiaoDichThanhToan_TrangThai_Enum", "[TrangThai] IN ('ChoXuLy','ThanhCong','ThatBai')");
                    table.ForeignKey(
                        name: "FK_GiaoDichThanhToan_DatBan_DatBanId",
                        column: x => x.DatBanId,
                        principalTable: "DatBan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiaoDichThanhToan_GiaoDichThanhToan_GiaoDichGocId",
                        column: x => x.GiaoDichGocId,
                        principalTable: "GiaoDichThanhToan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiaoDichThanhToan_HoaDon_HoaDonId",
                        column: x => x.HoaDonId,
                        principalTable: "HoaDon",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiaoDichThanhToan_NhanVien_NhanVienId",
                        column: x => x.NhanVienId,
                        principalTable: "NhanVien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SuDungVoucher",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VoucherId = table.Column<int>(type: "int", nullable: false),
                    HoaDonId = table.Column<int>(type: "int", nullable: false),
                    ThoiDiem = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuDungVoucher", x => x.Id);
                    table.UniqueConstraint("AK_SuDungVoucher_Id_HoaDonId", x => new { x.Id, x.HoaDonId });
                    table.ForeignKey(
                        name: "FK_SuDungVoucher_HoaDon_HoaDonId",
                        column: x => x.HoaDonId,
                        principalTable: "HoaDon",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SuDungVoucher_Voucher_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Voucher",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietPhieuXuat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhieuXuatId = table.Column<int>(type: "int", nullable: false),
                    NguyenLieuId = table.Column<int>(type: "int", nullable: false),
                    DonViTinhId = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    HeSoQuyDoi = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    SoLuongCoSo = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, computedColumnSql: "CONVERT(decimal(18,6),[SoLuong]*[HeSoQuyDoi])", stored: true),
                    ChiTietPhieuNhapId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietPhieuXuat", x => x.Id);
                    table.CheckConstraint("CK_ChiTietPhieuXuat_SoLuong", "[SoLuong]>0 AND [HeSoQuyDoi]>0");
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuXuat_ChiTietPhieuNhap_ChiTietPhieuNhapId",
                        column: x => x.ChiTietPhieuNhapId,
                        principalTable: "ChiTietPhieuNhap",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuXuat_DonViTinh_DonViTinhId",
                        column: x => x.DonViTinhId,
                        principalTable: "DonViTinh",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuXuat_NguyenLieu_NguyenLieuId",
                        column: x => x.NguyenLieuId,
                        principalTable: "NguyenLieu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuXuat_PhieuXuat_PhieuXuatId",
                        column: x => x.PhieuXuatId,
                        principalTable: "PhieuXuat",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DanhGia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonHangId = table.Column<int>(type: "int", nullable: false),
                    ChiTietDonHangId = table.Column<int>(type: "int", nullable: true),
                    KhachHangId = table.Column<int>(type: "int", nullable: true),
                    Diem = table.Column<int>(type: "int", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ThoiDiem = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhGia", x => x.Id);
                    table.CheckConstraint("CK_DanhGia_Diem", "[Diem] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_DanhGia_ChiTietDonHang_ChiTietDonHangId_DonHangId",
                        columns: x => new { x.ChiTietDonHangId, x.DonHangId },
                        principalTable: "ChiTietDonHang",
                        principalColumns: new[] { "Id", "DonHangId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DanhGia_DonHang_DonHangId",
                        column: x => x.DonHangId,
                        principalTable: "DonHang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DanhGia_KhachHang_KhachHangId",
                        column: x => x.KhachHangId,
                        principalTable: "KhachHang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DoiTruCoc",
                columns: table => new
                {
                    GiaoDichCocId = table.Column<int>(type: "int", nullable: false),
                    HoaDonId = table.Column<int>(type: "int", nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ThoiDiem = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoiTruCoc", x => x.GiaoDichCocId);
                    table.CheckConstraint("CK_DoiTruCoc_SoTien", "[SoTien]>0");
                    table.ForeignKey(
                        name: "FK_DoiTruCoc_GiaoDichThanhToan_GiaoDichCocId",
                        column: x => x.GiaoDichCocId,
                        principalTable: "GiaoDichThanhToan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DoiTruCoc_HoaDon_HoaDonId",
                        column: x => x.HoaDonId,
                        principalTable: "HoaDon",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApDungKhuyenMai",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoaDonId = table.Column<int>(type: "int", nullable: false),
                    DonHangId = table.Column<int>(type: "int", nullable: false),
                    ChiTietDonHangId = table.Column<int>(type: "int", nullable: true),
                    KhuyenMaiId = table.Column<int>(type: "int", nullable: false),
                    SuDungVoucherId = table.Column<int>(type: "int", nullable: true),
                    TenChuongTrinhLucApDung = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    KieuGiamLucApDung = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    GiaTriLucApDung = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ThuTu = table.Column<int>(type: "int", nullable: false),
                    CoSoTinhGiam = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SoTienGiam = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApDungKhuyenMai", x => x.Id);
                    table.CheckConstraint("CK_ApDungKhuyenMai_KieuGiamLucApDung_Enum", "[KieuGiamLucApDung] IN ('PhanTram','SoTien')");
                    table.CheckConstraint("CK_ApDungKhuyenMai_SoTien", "[SoTienGiam]>=0 AND [CoSoTinhGiam]>=[SoTienGiam] AND [GiaTriLucApDung]>0 AND [ThuTu]>=0");
                    table.ForeignKey(
                        name: "FK_ApDungKhuyenMai_ChiTietDonHang_ChiTietDonHangId_DonHangId",
                        columns: x => new { x.ChiTietDonHangId, x.DonHangId },
                        principalTable: "ChiTietDonHang",
                        principalColumns: new[] { "Id", "DonHangId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApDungKhuyenMai_HoaDon_HoaDonId_DonHangId",
                        columns: x => new { x.HoaDonId, x.DonHangId },
                        principalTable: "HoaDon",
                        principalColumns: new[] { "Id", "DonHangId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApDungKhuyenMai_KhuyenMai_KhuyenMaiId",
                        column: x => x.KhuyenMaiId,
                        principalTable: "KhuyenMai",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApDungKhuyenMai_SuDungVoucher_SuDungVoucherId_HoaDonId",
                        columns: x => new { x.SuDungVoucherId, x.HoaDonId },
                        principalTable: "SuDungVoucher",
                        principalColumns: new[] { "Id", "HoaDonId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApDungKhuyenMai_ChiTietDonHangId_DonHangId",
                table: "ApDungKhuyenMai",
                columns: new[] { "ChiTietDonHangId", "DonHangId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApDungKhuyenMai_HoaDonId_DonHangId",
                table: "ApDungKhuyenMai",
                columns: new[] { "HoaDonId", "DonHangId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApDungKhuyenMai_HoaDonId_KhuyenMaiId_ChiTietDonHangId",
                table: "ApDungKhuyenMai",
                columns: new[] { "HoaDonId", "KhuyenMaiId", "ChiTietDonHangId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApDungKhuyenMai_KhuyenMaiId",
                table: "ApDungKhuyenMai",
                column: "KhuyenMaiId");

            migrationBuilder.CreateIndex(
                name: "IX_ApDungKhuyenMai_SuDungVoucherId_HoaDonId",
                table: "ApDungKhuyenMai",
                columns: new[] { "SuDungVoucherId", "HoaDonId" });

            migrationBuilder.CreateIndex(
                name: "IX_BanAn_KhuVucId",
                table: "BanAn",
                column: "KhuVucId");

            migrationBuilder.CreateIndex(
                name: "IX_BanAn_MaBan",
                table: "BanAn",
                column: "MaBan",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDatBan_BanAnId",
                table: "ChiTietDatBan",
                column: "BanAnId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDonHang_DonHangId",
                table: "ChiTietDonHang",
                column: "DonHangId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDonHang_MonAnId",
                table: "ChiTietDonHang",
                column: "MonAnId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDonHang_MonDatTruocId",
                table: "ChiTietDonHang",
                column: "MonDatTruocId",
                unique: true,
                filter: "[MonDatTruocId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuNhap_DonViTinhId",
                table: "ChiTietPhieuNhap",
                column: "DonViTinhId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuNhap_NguyenLieuId",
                table: "ChiTietPhieuNhap",
                column: "NguyenLieuId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuNhap_PhieuNhapId",
                table: "ChiTietPhieuNhap",
                column: "PhieuNhapId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuXuat_ChiTietPhieuNhapId",
                table: "ChiTietPhieuXuat",
                column: "ChiTietPhieuNhapId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuXuat_DonViTinhId",
                table: "ChiTietPhieuXuat",
                column: "DonViTinhId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuXuat_NguyenLieuId",
                table: "ChiTietPhieuXuat",
                column: "NguyenLieuId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuXuat_PhieuXuatId",
                table: "ChiTietPhieuXuat",
                column: "PhieuXuatId");

            migrationBuilder.CreateIndex(
                name: "IX_DangNhapNgoai_UserId",
                table: "DangNhapNgoai",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGia_ChiTietDonHangId_DonHangId",
                table: "DanhGia",
                columns: new[] { "ChiTietDonHangId", "DonHangId" });

            migrationBuilder.CreateIndex(
                name: "IX_DanhGia_DonHangId_ChiTietDonHangId",
                table: "DanhGia",
                columns: new[] { "DonHangId", "ChiTietDonHangId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DanhGia_KhachHangId",
                table: "DanhGia",
                column: "KhachHangId");

            migrationBuilder.CreateIndex(
                name: "IX_DanhMuc_TenDanhMuc",
                table: "DanhMuc",
                column: "TenDanhMuc",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DatBan_KhachHangId",
                table: "DatBan",
                column: "KhachHangId");

            migrationBuilder.CreateIndex(
                name: "IX_DatBan_MaDatBan",
                table: "DatBan",
                column: "MaDatBan",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DatBan_NhanVienHuyId",
                table: "DatBan",
                column: "NhanVienHuyId");

            migrationBuilder.CreateIndex(
                name: "IX_DatBan_NhanVienTiepNhanId",
                table: "DatBan",
                column: "NhanVienTiepNhanId");

            migrationBuilder.CreateIndex(
                name: "IX_DatBan_TrangThai_GioDen_GioKetThucDuKien",
                table: "DatBan",
                columns: new[] { "TrangThai", "GioDen", "GioKetThucDuKien" });

            migrationBuilder.CreateIndex(
                name: "IX_DinhLuongMon_NguyenLieuId",
                table: "DinhLuongMon",
                column: "NguyenLieuId");

            migrationBuilder.CreateIndex(
                name: "IX_DoiTruCoc_HoaDonId",
                table: "DoiTruCoc",
                column: "HoaDonId");

            migrationBuilder.CreateIndex(
                name: "IX_DonHang_DatBanId",
                table: "DonHang",
                column: "DatBanId",
                unique: true,
                filter: "[DatBanId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DonHang_KhachHangId",
                table: "DonHang",
                column: "KhachHangId");

            migrationBuilder.CreateIndex(
                name: "IX_DonHang_MaDonHang",
                table: "DonHang",
                column: "MaDonHang",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DonHang_NhanVienLapId",
                table: "DonHang",
                column: "NhanVienLapId");

            migrationBuilder.CreateIndex(
                name: "IX_DonHangBan_BanAnId",
                table: "DonHangBan",
                column: "BanAnId",
                unique: true,
                filter: "[KetThuc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DonHangBan_DonHangId_BanAnId_BatDau",
                table: "DonHangBan",
                columns: new[] { "DonHangId", "BanAnId", "BatDau" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DonViTinh_TenDonVi",
                table: "DonViTinh",
                column: "TenDonVi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichThanhToan_DatBanId",
                table: "GiaoDichThanhToan",
                column: "DatBanId");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichThanhToan_GiaoDichGocId",
                table: "GiaoDichThanhToan",
                column: "GiaoDichGocId");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichThanhToan_HoaDonId",
                table: "GiaoDichThanhToan",
                column: "HoaDonId");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichThanhToan_KhoaChongLap",
                table: "GiaoDichThanhToan",
                column: "KhoaChongLap",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichThanhToan_NhanVienId",
                table: "GiaoDichThanhToan",
                column: "NhanVienId");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_DonHangId",
                table: "HoaDon",
                column: "DonHangId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_SoHoaDon",
                table: "HoaDon",
                column: "SoHoaDon",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_ThuNganId",
                table: "HoaDon",
                column: "ThuNganId");

            migrationBuilder.CreateIndex(
                name: "IX_KhachHang_SoDienThoai",
                table: "KhachHang",
                column: "SoDienThoai",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KhachHang_TaiKhoanId",
                table: "KhachHang",
                column: "TaiKhoanId",
                unique: true,
                filter: "[TaiKhoanId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KhuyenMaiMon_MonAnId",
                table: "KhuyenMaiMon",
                column: "MonAnId");

            migrationBuilder.CreateIndex(
                name: "IX_MonAn_DanhMucId",
                table: "MonAn",
                column: "DanhMucId");

            migrationBuilder.CreateIndex(
                name: "IX_MonDatTruoc_DatBanId",
                table: "MonDatTruoc",
                column: "DatBanId");

            migrationBuilder.CreateIndex(
                name: "IX_MonDatTruoc_MonAnId",
                table: "MonDatTruoc",
                column: "MonAnId");

            migrationBuilder.CreateIndex(
                name: "IX_NguyenLieu_DonViCoSoId",
                table: "NguyenLieu",
                column: "DonViCoSoId");

            migrationBuilder.CreateIndex(
                name: "IX_NhanVien_MaNhanVien",
                table: "NhanVien",
                column: "MaNhanVien",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NhanVien_TaiKhoanId",
                table: "NhanVien",
                column: "TaiKhoanId",
                unique: true,
                filter: "[TaiKhoanId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuNhap_MaPhieu",
                table: "PhieuNhap",
                column: "MaPhieu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhieuNhap_NhaCungCapId",
                table: "PhieuNhap",
                column: "NhaCungCapId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuNhap_NhanVienId",
                table: "PhieuNhap",
                column: "NhanVienId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuXuat_DonHangId",
                table: "PhieuXuat",
                column: "DonHangId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuXuat_MaPhieu",
                table: "PhieuXuat",
                column: "MaPhieu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhieuXuat_NhanVienId",
                table: "PhieuXuat",
                column: "NhanVienId");

            migrationBuilder.CreateIndex(
                name: "IX_QuyDoiNguyenLieu_DonViTinhId",
                table: "QuyDoiNguyenLieu",
                column: "DonViTinhId");

            migrationBuilder.CreateIndex(
                name: "IX_SuDungVoucher_HoaDonId",
                table: "SuDungVoucher",
                column: "HoaDonId");

            migrationBuilder.CreateIndex(
                name: "IX_SuDungVoucher_VoucherId_HoaDonId",
                table: "SuDungVoucher",
                columns: new[] { "VoucherId", "HoaDonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "TaiKhoan",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "TaiKhoan",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoanClaim_UserId",
                table: "TaiKhoanClaim",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoanVaiTro_RoleId",
                table: "TaiKhoanVaiTro",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ThanhPhanSet_MonAnId",
                table: "ThanhPhanSet",
                column: "MonAnId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "VaiTro",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VaiTroClaim_RoleId",
                table: "VaiTroClaim",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Voucher_KhachHangId",
                table: "Voucher",
                column: "KhachHangId");

            migrationBuilder.CreateIndex(
                name: "IX_Voucher_KhuyenMaiId",
                table: "Voucher",
                column: "KhuyenMaiId");

            migrationBuilder.CreateIndex(
                name: "IX_Voucher_Ma",
                table: "Voucher",
                column: "Ma",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApDungKhuyenMai");

            migrationBuilder.DropTable(
                name: "ChiTietDatBan");

            migrationBuilder.DropTable(
                name: "ChiTietPhieuXuat");

            migrationBuilder.DropTable(
                name: "DangNhapNgoai");

            migrationBuilder.DropTable(
                name: "DanhGia");

            migrationBuilder.DropTable(
                name: "DinhLuongMon");

            migrationBuilder.DropTable(
                name: "DoiTruCoc");

            migrationBuilder.DropTable(
                name: "DonHangBan");

            migrationBuilder.DropTable(
                name: "KhuyenMaiMon");

            migrationBuilder.DropTable(
                name: "QuyDoiNguyenLieu");

            migrationBuilder.DropTable(
                name: "TaiKhoanClaim");

            migrationBuilder.DropTable(
                name: "TaiKhoanVaiTro");

            migrationBuilder.DropTable(
                name: "ThanhPhanSet");

            migrationBuilder.DropTable(
                name: "TokenTaiKhoan");

            migrationBuilder.DropTable(
                name: "VaiTroClaim");

            migrationBuilder.DropTable(
                name: "SuDungVoucher");

            migrationBuilder.DropTable(
                name: "ChiTietPhieuNhap");

            migrationBuilder.DropTable(
                name: "PhieuXuat");

            migrationBuilder.DropTable(
                name: "ChiTietDonHang");

            migrationBuilder.DropTable(
                name: "GiaoDichThanhToan");

            migrationBuilder.DropTable(
                name: "BanAn");

            migrationBuilder.DropTable(
                name: "VaiTro");

            migrationBuilder.DropTable(
                name: "Voucher");

            migrationBuilder.DropTable(
                name: "NguyenLieu");

            migrationBuilder.DropTable(
                name: "PhieuNhap");

            migrationBuilder.DropTable(
                name: "MonDatTruoc");

            migrationBuilder.DropTable(
                name: "HoaDon");

            migrationBuilder.DropTable(
                name: "KhuVuc");

            migrationBuilder.DropTable(
                name: "KhuyenMai");

            migrationBuilder.DropTable(
                name: "DonViTinh");

            migrationBuilder.DropTable(
                name: "NhaCungCap");

            migrationBuilder.DropTable(
                name: "MonAn");

            migrationBuilder.DropTable(
                name: "DonHang");

            migrationBuilder.DropTable(
                name: "DanhMuc");

            migrationBuilder.DropTable(
                name: "DatBan");

            migrationBuilder.DropTable(
                name: "KhachHang");

            migrationBuilder.DropTable(
                name: "NhanVien");

            migrationBuilder.DropTable(
                name: "TaiKhoan");
        }
    }
}
