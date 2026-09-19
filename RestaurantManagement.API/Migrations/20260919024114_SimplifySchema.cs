using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class SimplifySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietPhieuNhap_DonViTinh_DonViTinhId",
                table: "ChiTietPhieuNhap");

            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietPhieuXuat_DonViTinh_DonViTinhId",
                table: "ChiTietPhieuXuat");

            migrationBuilder.DropForeignKey(
                name: "FK_DanhGia_ChiTietDonHang_ChiTietDonHangId_DonHangId",
                table: "DanhGia");

            migrationBuilder.DropForeignKey(
                name: "FK_DanhGia_DonHang_DonHangId",
                table: "DanhGia");

            migrationBuilder.DropForeignKey(
                name: "FK_HoaDon_DonHang_DonHangId",
                table: "HoaDon");

            migrationBuilder.DropForeignKey(
                name: "FK_HoaDon_NhanVien_ThuNganId",
                table: "HoaDon");

            migrationBuilder.DropForeignKey(
                name: "FK_NguyenLieu_DonViTinh_DonViCoSoId",
                table: "NguyenLieu");

            migrationBuilder.DropForeignKey(
                name: "FK_PhieuXuat_DonHang_DonHangId",
                table: "PhieuXuat");

            migrationBuilder.DropTable(
                name: "ApDungKhuyenMai");

            migrationBuilder.DropTable(
                name: "DinhLuongMon");

            migrationBuilder.DropTable(
                name: "DoiTruCoc");

            migrationBuilder.DropTable(
                name: "DonHangBan");

            migrationBuilder.DropTable(
                name: "QuyDoiNguyenLieu");

            migrationBuilder.DropTable(
                name: "ThanhPhanSet");

            migrationBuilder.DropTable(
                name: "ChiTietDonHang");

            migrationBuilder.DropTable(
                name: "SuDungVoucher");

            migrationBuilder.DropTable(
                name: "GiaoDichThanhToan");

            migrationBuilder.DropTable(
                name: "DonViTinh");

            migrationBuilder.DropTable(
                name: "DonHang");

            migrationBuilder.DropIndex(
                name: "IX_NguyenLieu_DonViCoSoId",
                table: "NguyenLieu");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MonAn_Gia",
                table: "MonAn");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_HoaDon_Id_DonHangId",
                table: "HoaDon");

            migrationBuilder.DropIndex(
                name: "IX_HoaDon_DonHangId",
                table: "HoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HoaDon_SoTien",
                table: "HoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DatBan_Coc",
                table: "DatBan");

            migrationBuilder.DropIndex(
                name: "IX_ChiTietPhieuXuat_DonViTinhId",
                table: "ChiTietPhieuXuat");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChiTietPhieuXuat_SoLuong",
                table: "ChiTietPhieuXuat");

            migrationBuilder.DropIndex(
                name: "IX_ChiTietPhieuNhap_DonViTinhId",
                table: "ChiTietPhieuNhap");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChiTietPhieuNhap_LuongGia",
                table: "ChiTietPhieuNhap");

            migrationBuilder.DropColumn(
                name: "SoLuongCoSo",
                table: "ChiTietPhieuXuat");

            migrationBuilder.DropColumn(
                name: "SoLuongCoSo",
                table: "ChiTietPhieuNhap");

            migrationBuilder.DropColumn(
                name: "DonViCoSoId",
                table: "NguyenLieu");

            migrationBuilder.DropColumn(
                name: "GiaBan",
                table: "MonAn");

            migrationBuilder.DropColumn(
                name: "TongThanhToan",
                table: "HoaDon");

            migrationBuilder.DropColumn(
                name: "DonHangId",
                table: "HoaDon");

            migrationBuilder.DropColumn(
                name: "PhiDichVu",
                table: "HoaDon");

            migrationBuilder.DropColumn(
                name: "PhiGiaoHang",
                table: "HoaDon");

            migrationBuilder.DropColumn(
                name: "DonViTinhId",
                table: "ChiTietPhieuXuat");

            migrationBuilder.DropColumn(
                name: "HeSoQuyDoi",
                table: "ChiTietPhieuXuat");

            migrationBuilder.DropColumn(
                name: "DonViTinhId",
                table: "ChiTietPhieuNhap");

            migrationBuilder.DropColumn(
                name: "HeSoQuyDoi",
                table: "ChiTietPhieuNhap");

            migrationBuilder.RenameColumn(
                name: "DonHangId",
                table: "PhieuXuat",
                newName: "HoaDonId");

            migrationBuilder.RenameIndex(
                name: "IX_PhieuXuat_DonHangId",
                table: "PhieuXuat",
                newName: "IX_PhieuXuat_HoaDonId");

            migrationBuilder.RenameColumn(
                name: "ThanhPhanSetSnapshot",
                table: "MonDatTruoc",
                newName: "ChiTietComboSnapshot");

            migrationBuilder.RenameColumn(
                name: "TienThue",
                table: "HoaDon",
                newName: "TongTienHang");

            migrationBuilder.RenameColumn(
                name: "TienMon",
                table: "HoaDon",
                newName: "TienCocDaTru");

            migrationBuilder.RenameColumn(
                name: "ThuNganId",
                table: "HoaDon",
                newName: "NhanVienId");

            migrationBuilder.RenameColumn(
                name: "SoHoaDon",
                table: "HoaDon",
                newName: "MaHoaDon");

            migrationBuilder.RenameIndex(
                name: "IX_HoaDon_ThuNganId",
                table: "HoaDon",
                newName: "IX_HoaDon_NhanVienId");

            migrationBuilder.RenameIndex(
                name: "IX_HoaDon_SoHoaDon",
                table: "HoaDon",
                newName: "IX_HoaDon_MaHoaDon");

            migrationBuilder.RenameColumn(
                name: "DonHangId",
                table: "DanhGia",
                newName: "HoaDonId");

            migrationBuilder.RenameColumn(
                name: "ChiTietDonHangId",
                table: "DanhGia",
                newName: "ChiTietHoaDonId");

            migrationBuilder.RenameIndex(
                name: "IX_DanhGia_DonHangId_ChiTietDonHangId",
                table: "DanhGia",
                newName: "IX_DanhGia_HoaDonId_ChiTietHoaDonId");

            migrationBuilder.RenameIndex(
                name: "IX_DanhGia_ChiTietDonHangId_DonHangId",
                table: "DanhGia",
                newName: "IX_DanhGia_ChiTietHoaDonId_HoaDonId");

            migrationBuilder.AddColumn<string>(
                name: "DonViTinh",
                table: "NguyenLieu",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MonAnSizeId",
                table: "MonDatTruoc",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DatBanId",
                table: "HoaDon",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KhachHangId",
                table: "HoaDon",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhuongThucThanhToan",
                table: "HoaDon",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ThoiDiemThanhToan",
                table: "HoaDon",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VoucherId",
                table: "HoaDon",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ThoiDiemCoc",
                table: "DatBan",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TienCocDaNop",
                table: "DatBan",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TrangThaiCoc",
                table: "DatBan",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TongThanhToan",
                table: "HoaDon",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                computedColumnSql: "[TongTienHang]-[TienGiam]-[TienCocDaTru]",
                stored: true);

            migrationBuilder.CreateTable(
                name: "ChiTietCombo",
                columns: table => new
                {
                    ComboId = table.Column<int>(type: "int", nullable: false),
                    MonAnId = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietCombo", x => new { x.ComboId, x.MonAnId });
                    table.CheckConstraint("CK_ChiTietCombo_ThanhPhan", "[ComboId]<>[MonAnId] AND [SoLuong]>0");
                    table.ForeignKey(
                        name: "FK_ChiTietCombo_MonAn_ComboId",
                        column: x => x.ComboId,
                        principalTable: "MonAn",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietCombo_MonAn_MonAnId",
                        column: x => x.MonAnId,
                        principalTable: "MonAn",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DinhMucMon",
                columns: table => new
                {
                    MonAnId = table.Column<int>(type: "int", nullable: false),
                    NguyenLieuId = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DinhMucMon", x => new { x.MonAnId, x.NguyenLieuId });
                    table.CheckConstraint("CK_DinhMucMon_SoLuong", "[SoLuong]>0");
                    table.ForeignKey(
                        name: "FK_DinhMucMon_MonAn_MonAnId",
                        column: x => x.MonAnId,
                        principalTable: "MonAn",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DinhMucMon_NguyenLieu_NguyenLieuId",
                        column: x => x.NguyenLieuId,
                        principalTable: "NguyenLieu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MonAnSize",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MonAnId = table.Column<int>(type: "int", nullable: false),
                    TenSize = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GiaBan = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DangSuDung = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonAnSize", x => x.Id);
                    table.CheckConstraint("CK_MonAnSize_Gia", "[GiaBan]>=0");
                    table.ForeignKey(
                        name: "FK_MonAnSize_MonAn_MonAnId",
                        column: x => x.MonAnId,
                        principalTable: "MonAn",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietHoaDon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoaDonId = table.Column<int>(type: "int", nullable: false),
                    MonAnId = table.Column<int>(type: "int", nullable: false),
                    MonAnSizeId = table.Column<int>(type: "int", nullable: true),
                    TenMonLucBan = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    YeuCauCheBien = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChiTietComboSnapshot = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    MonDatTruocId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietHoaDon", x => x.Id);
                    table.UniqueConstraint("AK_ChiTietHoaDon_Id_HoaDonId", x => new { x.Id, x.HoaDonId });
                    table.CheckConstraint("CK_ChiTietHoaDon_LuongGia", "[SoLuong]>0 AND [DonGia]>=0");
                    table.CheckConstraint("CK_ChiTietHoaDon_TrangThai_Enum", "[TrangThai] IN ('ChoCheBien','DangCheBien','SanSang','DaPhucVu','DaHuy')");
                    table.ForeignKey(
                        name: "FK_ChiTietHoaDon_HoaDon_HoaDonId",
                        column: x => x.HoaDonId,
                        principalTable: "HoaDon",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietHoaDon_MonAnSize_MonAnSizeId",
                        column: x => x.MonAnSizeId,
                        principalTable: "MonAnSize",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietHoaDon_MonAn_MonAnId",
                        column: x => x.MonAnId,
                        principalTable: "MonAn",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietHoaDon_MonDatTruoc_MonDatTruocId",
                        column: x => x.MonDatTruocId,
                        principalTable: "MonDatTruoc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NguyenLieu_TenNguyenLieu",
                table: "NguyenLieu",
                column: "TenNguyenLieu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonDatTruoc_MonAnSizeId",
                table: "MonDatTruoc",
                column: "MonAnSizeId");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_DatBanId",
                table: "HoaDon",
                column: "DatBanId");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_KhachHangId",
                table: "HoaDon",
                column: "KhachHangId");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_VoucherId",
                table: "HoaDon",
                column: "VoucherId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HoaDon_PhuongThucThanhToan_Enum",
                table: "HoaDon",
                sql: "[PhuongThucThanhToan] IN ('TienMat','ChuyenKhoan','The','ViDienTu')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HoaDon_SoTien",
                table: "HoaDon",
                sql: "[TongTienHang]>=0 AND [TienGiam]>=0 AND [TienGiam]<=[TongTienHang] AND [TienCocDaTru]>=0 AND [TienCocDaTru]<=[TongTienHang]-[TienGiam]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DatBan_Coc",
                table: "DatBan",
                sql: "[TienCocYeuCau]>=0 AND [TienCocDaNop]>=0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DatBan_TrangThaiCoc_Enum",
                table: "DatBan",
                sql: "[TrangThaiCoc] IN ('ChuaCoc','DaCoc','DaHoan','DaDoiTru')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChiTietPhieuXuat_SoLuong",
                table: "ChiTietPhieuXuat",
                sql: "[SoLuong]>0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChiTietPhieuNhap_LuongGia",
                table: "ChiTietPhieuNhap",
                sql: "[SoLuong]>0 AND [DonGia]>=0");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietCombo_MonAnId",
                table: "ChiTietCombo",
                column: "MonAnId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHoaDon_HoaDonId",
                table: "ChiTietHoaDon",
                column: "HoaDonId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHoaDon_MonAnId",
                table: "ChiTietHoaDon",
                column: "MonAnId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHoaDon_MonAnSizeId",
                table: "ChiTietHoaDon",
                column: "MonAnSizeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHoaDon_MonDatTruocId",
                table: "ChiTietHoaDon",
                column: "MonDatTruocId",
                unique: true,
                filter: "[MonDatTruocId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DinhMucMon_NguyenLieuId",
                table: "DinhMucMon",
                column: "NguyenLieuId");

            migrationBuilder.CreateIndex(
                name: "IX_MonAnSize_MonAnId_TenSize",
                table: "MonAnSize",
                columns: new[] { "MonAnId", "TenSize" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DanhGia_ChiTietHoaDon_ChiTietHoaDonId_HoaDonId",
                table: "DanhGia",
                columns: new[] { "ChiTietHoaDonId", "HoaDonId" },
                principalTable: "ChiTietHoaDon",
                principalColumns: new[] { "Id", "HoaDonId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DanhGia_HoaDon_HoaDonId",
                table: "DanhGia",
                column: "HoaDonId",
                principalTable: "HoaDon",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HoaDon_DatBan_DatBanId",
                table: "HoaDon",
                column: "DatBanId",
                principalTable: "DatBan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HoaDon_KhachHang_KhachHangId",
                table: "HoaDon",
                column: "KhachHangId",
                principalTable: "KhachHang",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HoaDon_NhanVien_NhanVienId",
                table: "HoaDon",
                column: "NhanVienId",
                principalTable: "NhanVien",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HoaDon_Voucher_VoucherId",
                table: "HoaDon",
                column: "VoucherId",
                principalTable: "Voucher",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MonDatTruoc_MonAnSize_MonAnSizeId",
                table: "MonDatTruoc",
                column: "MonAnSizeId",
                principalTable: "MonAnSize",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PhieuXuat_HoaDon_HoaDonId",
                table: "PhieuXuat",
                column: "HoaDonId",
                principalTable: "HoaDon",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DanhGia_ChiTietHoaDon_ChiTietHoaDonId_HoaDonId",
                table: "DanhGia");

            migrationBuilder.DropForeignKey(
                name: "FK_DanhGia_HoaDon_HoaDonId",
                table: "DanhGia");

            migrationBuilder.DropForeignKey(
                name: "FK_HoaDon_DatBan_DatBanId",
                table: "HoaDon");

            migrationBuilder.DropForeignKey(
                name: "FK_HoaDon_KhachHang_KhachHangId",
                table: "HoaDon");

            migrationBuilder.DropForeignKey(
                name: "FK_HoaDon_NhanVien_NhanVienId",
                table: "HoaDon");

            migrationBuilder.DropForeignKey(
                name: "FK_HoaDon_Voucher_VoucherId",
                table: "HoaDon");

            migrationBuilder.DropForeignKey(
                name: "FK_MonDatTruoc_MonAnSize_MonAnSizeId",
                table: "MonDatTruoc");

            migrationBuilder.DropForeignKey(
                name: "FK_PhieuXuat_HoaDon_HoaDonId",
                table: "PhieuXuat");

            migrationBuilder.DropTable(
                name: "ChiTietCombo");

            migrationBuilder.DropTable(
                name: "ChiTietHoaDon");

            migrationBuilder.DropTable(
                name: "DinhMucMon");

            migrationBuilder.DropTable(
                name: "MonAnSize");

            migrationBuilder.DropIndex(
                name: "IX_NguyenLieu_TenNguyenLieu",
                table: "NguyenLieu");

            migrationBuilder.DropIndex(
                name: "IX_MonDatTruoc_MonAnSizeId",
                table: "MonDatTruoc");

            migrationBuilder.DropIndex(
                name: "IX_HoaDon_DatBanId",
                table: "HoaDon");

            migrationBuilder.DropIndex(
                name: "IX_HoaDon_KhachHangId",
                table: "HoaDon");

            migrationBuilder.DropIndex(
                name: "IX_HoaDon_VoucherId",
                table: "HoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HoaDon_PhuongThucThanhToan_Enum",
                table: "HoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HoaDon_SoTien",
                table: "HoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DatBan_Coc",
                table: "DatBan");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DatBan_TrangThaiCoc_Enum",
                table: "DatBan");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChiTietPhieuXuat_SoLuong",
                table: "ChiTietPhieuXuat");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChiTietPhieuNhap_LuongGia",
                table: "ChiTietPhieuNhap");

            migrationBuilder.DropColumn(
                name: "TongThanhToan",
                table: "HoaDon");

            migrationBuilder.DropColumn(
                name: "DonViTinh",
                table: "NguyenLieu");

            migrationBuilder.DropColumn(
                name: "MonAnSizeId",
                table: "MonDatTruoc");

            migrationBuilder.DropColumn(
                name: "DatBanId",
                table: "HoaDon");

            migrationBuilder.DropColumn(
                name: "KhachHangId",
                table: "HoaDon");

            migrationBuilder.DropColumn(
                name: "PhuongThucThanhToan",
                table: "HoaDon");

            migrationBuilder.DropColumn(
                name: "ThoiDiemThanhToan",
                table: "HoaDon");

            migrationBuilder.DropColumn(
                name: "VoucherId",
                table: "HoaDon");

            migrationBuilder.DropColumn(
                name: "ThoiDiemCoc",
                table: "DatBan");

            migrationBuilder.DropColumn(
                name: "TienCocDaNop",
                table: "DatBan");

            migrationBuilder.DropColumn(
                name: "TrangThaiCoc",
                table: "DatBan");

            migrationBuilder.RenameColumn(
                name: "HoaDonId",
                table: "PhieuXuat",
                newName: "DonHangId");

            migrationBuilder.RenameIndex(
                name: "IX_PhieuXuat_HoaDonId",
                table: "PhieuXuat",
                newName: "IX_PhieuXuat_DonHangId");

            migrationBuilder.RenameColumn(
                name: "ChiTietComboSnapshot",
                table: "MonDatTruoc",
                newName: "ThanhPhanSetSnapshot");

            migrationBuilder.RenameColumn(
                name: "TongTienHang",
                table: "HoaDon",
                newName: "TienThue");

            migrationBuilder.RenameColumn(
                name: "TienCocDaTru",
                table: "HoaDon",
                newName: "TienMon");

            migrationBuilder.RenameColumn(
                name: "NhanVienId",
                table: "HoaDon",
                newName: "ThuNganId");

            migrationBuilder.RenameColumn(
                name: "MaHoaDon",
                table: "HoaDon",
                newName: "SoHoaDon");

            migrationBuilder.RenameIndex(
                name: "IX_HoaDon_NhanVienId",
                table: "HoaDon",
                newName: "IX_HoaDon_ThuNganId");

            migrationBuilder.RenameIndex(
                name: "IX_HoaDon_MaHoaDon",
                table: "HoaDon",
                newName: "IX_HoaDon_SoHoaDon");

            migrationBuilder.RenameColumn(
                name: "HoaDonId",
                table: "DanhGia",
                newName: "DonHangId");

            migrationBuilder.RenameColumn(
                name: "ChiTietHoaDonId",
                table: "DanhGia",
                newName: "ChiTietDonHangId");

            migrationBuilder.RenameIndex(
                name: "IX_DanhGia_HoaDonId_ChiTietHoaDonId",
                table: "DanhGia",
                newName: "IX_DanhGia_DonHangId_ChiTietDonHangId");

            migrationBuilder.RenameIndex(
                name: "IX_DanhGia_ChiTietHoaDonId_HoaDonId",
                table: "DanhGia",
                newName: "IX_DanhGia_ChiTietDonHangId_DonHangId");

            migrationBuilder.AddColumn<int>(
                name: "DonViCoSoId",
                table: "NguyenLieu",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "GiaBan",
                table: "MonAn",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DonHangId",
                table: "HoaDon",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PhiDichVu",
                table: "HoaDon",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PhiGiaoHang",
                table: "HoaDon",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DonViTinhId",
                table: "ChiTietPhieuXuat",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "HeSoQuyDoi",
                table: "ChiTietPhieuXuat",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DonViTinhId",
                table: "ChiTietPhieuNhap",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "HeSoQuyDoi",
                table: "ChiTietPhieuNhap",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TongThanhToan",
                table: "HoaDon",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                computedColumnSql: "[TienMon]-[TienGiam]+[TienThue]+[PhiDichVu]+[PhiGiaoHang]",
                stored: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SoLuongCoSo",
                table: "ChiTietPhieuXuat",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                computedColumnSql: "CONVERT(decimal(18,6),[SoLuong]*[HeSoQuyDoi])",
                stored: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SoLuongCoSo",
                table: "ChiTietPhieuNhap",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                computedColumnSql: "CONVERT(decimal(18,6),[SoLuong]*[HeSoQuyDoi])",
                stored: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_HoaDon_Id_DonHangId",
                table: "HoaDon",
                columns: new[] { "Id", "DonHangId" });

            migrationBuilder.CreateTable(
                name: "DinhLuongMon",
                columns: table => new
                {
                    MonAnId = table.Column<int>(type: "int", nullable: false),
                    NguyenLieuId = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    SoLuongCoSo = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false)
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
                name: "DonHang",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DatBanId = table.Column<int>(type: "int", nullable: true),
                    KhachHangId = table.Column<int>(type: "int", nullable: true),
                    NhanVienLapId = table.Column<int>(type: "int", nullable: true),
                    DiaChiGiaoHang = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DienThoaiGiaoHang = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Loai = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    MaDonHang = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    TenNguoiNhan = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ThoiDiemTao = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
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
                name: "DonViTinh",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    TenDonVi = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonViTinh", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GiaoDichThanhToan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DatBanId = table.Column<int>(type: "int", nullable: true),
                    GiaoDichGocId = table.Column<int>(type: "int", nullable: true),
                    HoaDonId = table.Column<int>(type: "int", nullable: true),
                    NhanVienId = table.Column<int>(type: "int", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    KhoaChongLap = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Loai = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    MaThamChieu = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PhuongThuc = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ThoiDiem = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
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
                    HoaDonId = table.Column<int>(type: "int", nullable: false),
                    VoucherId = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    ThoiDiem = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
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
                name: "ThanhPhanSet",
                columns: table => new
                {
                    SetId = table.Column<int>(type: "int", nullable: false),
                    MonAnId = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    SoLuong = table.Column<int>(type: "int", nullable: false)
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
                name: "ChiTietDonHang",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonHangId = table.Column<int>(type: "int", nullable: false),
                    MonAnId = table.Column<int>(type: "int", nullable: false),
                    MonDatTruocId = table.Column<int>(type: "int", nullable: true),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    TenMonLucBan = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ThanhPhanSetSnapshot = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    YeuCauCheBien = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
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
                name: "DonHangBan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BanAnId = table.Column<int>(type: "int", nullable: false),
                    DonHangId = table.Column<int>(type: "int", nullable: false),
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
                name: "DoiTruCoc",
                columns: table => new
                {
                    GiaoDichCocId = table.Column<int>(type: "int", nullable: false),
                    HoaDonId = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ThoiDiem = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
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
                    ChiTietDonHangId = table.Column<int>(type: "int", nullable: true),
                    HoaDonId = table.Column<int>(type: "int", nullable: false),
                    KhuyenMaiId = table.Column<int>(type: "int", nullable: false),
                    SuDungVoucherId = table.Column<int>(type: "int", nullable: true),
                    DonHangId = table.Column<int>(type: "int", nullable: false),
                    CoSoTinhGiam = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GiaTriLucApDung = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KieuGiamLucApDung = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    SoTienGiam = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TenChuongTrinhLucApDung = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ThuTu = table.Column<int>(type: "int", nullable: false)
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
                name: "IX_NguyenLieu_DonViCoSoId",
                table: "NguyenLieu",
                column: "DonViCoSoId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MonAn_Gia",
                table: "MonAn",
                sql: "[GiaBan]>=0");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_DonHangId",
                table: "HoaDon",
                column: "DonHangId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_HoaDon_SoTien",
                table: "HoaDon",
                sql: "[TienMon]>=0 AND [TienGiam]>=0 AND [TienGiam]<=[TienMon] AND [TienThue]>=0 AND [PhiDichVu]>=0 AND [PhiGiaoHang]>=0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DatBan_Coc",
                table: "DatBan",
                sql: "[TienCocYeuCau]>=0");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuXuat_DonViTinhId",
                table: "ChiTietPhieuXuat",
                column: "DonViTinhId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChiTietPhieuXuat_SoLuong",
                table: "ChiTietPhieuXuat",
                sql: "[SoLuong]>0 AND [HeSoQuyDoi]>0");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuNhap_DonViTinhId",
                table: "ChiTietPhieuNhap",
                column: "DonViTinhId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChiTietPhieuNhap_LuongGia",
                table: "ChiTietPhieuNhap",
                sql: "[SoLuong]>0 AND [HeSoQuyDoi]>0 AND [DonGia]>=0");

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
                name: "IX_ThanhPhanSet_MonAnId",
                table: "ThanhPhanSet",
                column: "MonAnId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietPhieuNhap_DonViTinh_DonViTinhId",
                table: "ChiTietPhieuNhap",
                column: "DonViTinhId",
                principalTable: "DonViTinh",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietPhieuXuat_DonViTinh_DonViTinhId",
                table: "ChiTietPhieuXuat",
                column: "DonViTinhId",
                principalTable: "DonViTinh",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DanhGia_ChiTietDonHang_ChiTietDonHangId_DonHangId",
                table: "DanhGia",
                columns: new[] { "ChiTietDonHangId", "DonHangId" },
                principalTable: "ChiTietDonHang",
                principalColumns: new[] { "Id", "DonHangId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DanhGia_DonHang_DonHangId",
                table: "DanhGia",
                column: "DonHangId",
                principalTable: "DonHang",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HoaDon_DonHang_DonHangId",
                table: "HoaDon",
                column: "DonHangId",
                principalTable: "DonHang",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HoaDon_NhanVien_ThuNganId",
                table: "HoaDon",
                column: "ThuNganId",
                principalTable: "NhanVien",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NguyenLieu_DonViTinh_DonViCoSoId",
                table: "NguyenLieu",
                column: "DonViCoSoId",
                principalTable: "DonViTinh",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PhieuXuat_DonHang_DonHangId",
                table: "PhieuXuat",
                column: "DonHangId",
                principalTable: "DonHang",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
