using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class InventoryManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BoPhanNhan",
                table: "PhieuXuat",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguoiNhan",
                table: "PhieuXuat",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TongTien",
                table: "PhieuXuat",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SoHoaDon",
                table: "PhieuNhap",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TongTien",
                table: "PhieuNhap",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "NhaCungCap",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GhiChu",
                table: "NhaCungCap",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguoiLienHe",
                table: "NhaCungCap",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DanhMuc",
                table: "NguyenLieu",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DonGia",
                table: "NguyenLieu",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SoLuongTon",
                table: "NguyenLieu",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DonGiaXuat",
                table: "ChiTietPhieuXuat",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ThanhTien",
                table: "ChiTietPhieuXuat",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ThanhTien",
                table: "ChiTietPhieuNhap",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "PhieuThanhLy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaPhieu = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NhanVienId = table.Column<int>(type: "int", nullable: false),
                    ThoiDiem = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LyDoThanhLy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TongTienThietHai = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuThanhLy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhieuThanhLy_NhanVien_NhanVienId",
                        column: x => x.NhanVienId,
                        principalTable: "NhanVien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietPhieuThanhLy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhieuThanhLyId = table.Column<int>(type: "int", nullable: false),
                    NguyenLieuId = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    DonGiaVon = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ThanhTien = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietPhieuThanhLy", x => x.Id);
                    table.CheckConstraint("CK_ChiTietPhieuThanhLy_LuongGia", "[SoLuong]>0 AND [DonGiaVon]>=0");
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuThanhLy_NguyenLieu_NguyenLieuId",
                        column: x => x.NguyenLieuId,
                        principalTable: "NguyenLieu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuThanhLy_PhieuThanhLy_PhieuThanhLyId",
                        column: x => x.PhieuThanhLyId,
                        principalTable: "PhieuThanhLy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuThanhLy_NguyenLieuId",
                table: "ChiTietPhieuThanhLy",
                column: "NguyenLieuId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuThanhLy_PhieuThanhLyId",
                table: "ChiTietPhieuThanhLy",
                column: "PhieuThanhLyId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuThanhLy_MaPhieu",
                table: "PhieuThanhLy",
                column: "MaPhieu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhieuThanhLy_NhanVienId",
                table: "PhieuThanhLy",
                column: "NhanVienId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietPhieuThanhLy");

            migrationBuilder.DropTable(
                name: "PhieuThanhLy");

            migrationBuilder.DropColumn(
                name: "BoPhanNhan",
                table: "PhieuXuat");

            migrationBuilder.DropColumn(
                name: "NguoiNhan",
                table: "PhieuXuat");

            migrationBuilder.DropColumn(
                name: "TongTien",
                table: "PhieuXuat");

            migrationBuilder.DropColumn(
                name: "SoHoaDon",
                table: "PhieuNhap");

            migrationBuilder.DropColumn(
                name: "TongTien",
                table: "PhieuNhap");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "NhaCungCap");

            migrationBuilder.DropColumn(
                name: "GhiChu",
                table: "NhaCungCap");

            migrationBuilder.DropColumn(
                name: "NguoiLienHe",
                table: "NhaCungCap");

            migrationBuilder.DropColumn(
                name: "DanhMuc",
                table: "NguyenLieu");

            migrationBuilder.DropColumn(
                name: "DonGia",
                table: "NguyenLieu");

            migrationBuilder.DropColumn(
                name: "SoLuongTon",
                table: "NguyenLieu");

            migrationBuilder.DropColumn(
                name: "DonGiaXuat",
                table: "ChiTietPhieuXuat");

            migrationBuilder.DropColumn(
                name: "ThanhTien",
                table: "ChiTietPhieuXuat");

            migrationBuilder.DropColumn(
                name: "ThanhTien",
                table: "ChiTietPhieuNhap");
        }
    }
}
