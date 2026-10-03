using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class OrderFulfillmentAndMultiPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiaChiGiaoHang",
                table: "HoaDon",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GhiChuDonHang",
                table: "HoaDon",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoaiDonHang",
                table: "HoaDon",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "TaiBan");

            migrationBuilder.AddColumn<string>(
                name: "MaGiaoDich",
                table: "HoaDon",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoDienThoaiNhan",
                table: "HoaDon",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenNguoiNhan",
                table: "HoaDon",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TienKhachDua",
                table: "HoaDon",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TienThoiLai",
                table: "HoaDon",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TenSizeLucBan",
                table: "ChiTietHoaDon",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ThoiDiemGoi",
                table: "ChiTietHoaDon",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "SYSDATETIMEOFFSET()");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HoaDon_LoaiDonHang_Enum",
                table: "HoaDon",
                sql: "[LoaiDonHang] IN ('TaiBan','MangDi','GiaoHang')");

            migrationBuilder.Sql("UPDATE c SET c.TenSizeLucBan = s.TenSize FROM ChiTietHoaDon c JOIN MonAnSize s ON c.MonAnSizeId = s.Id WHERE c.TenSizeLucBan IS NULL;");
            migrationBuilder.Sql("UPDATE ChiTietHoaDon SET TenSizeLucBan = N'Mặc định' WHERE TenSizeLucBan IS NULL;");
            migrationBuilder.Sql("UPDATE HoaDon SET LoaiDonHang = 'MangDi' WHERE DatBanId IS NULL;");
            migrationBuilder.Sql("UPDATE c SET c.ThoiDiemGoi = h.ThoiDiemLap FROM ChiTietHoaDon c JOIN HoaDon h ON c.HoaDonId = h.Id;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_HoaDon_LoaiDonHang_Enum",
                table: "HoaDon");

            migrationBuilder.DropColumn(
                name: "DiaChiGiaoHang",
                table: "HoaDon");

            migrationBuilder.DropColumn(
                name: "GhiChuDonHang",
                table: "HoaDon");

            migrationBuilder.DropColumn(
                name: "LoaiDonHang",
                table: "HoaDon");

            migrationBuilder.DropColumn(
                name: "MaGiaoDich",
                table: "HoaDon");

            migrationBuilder.DropColumn(
                name: "SoDienThoaiNhan",
                table: "HoaDon");

            migrationBuilder.DropColumn(
                name: "TenNguoiNhan",
                table: "HoaDon");

            migrationBuilder.DropColumn(
                name: "TienKhachDua",
                table: "HoaDon");

            migrationBuilder.DropColumn(
                name: "TienThoiLai",
                table: "HoaDon");

            migrationBuilder.DropColumn(
                name: "TenSizeLucBan",
                table: "ChiTietHoaDon");

            migrationBuilder.DropColumn(
                name: "ThoiDiemGoi",
                table: "ChiTietHoaDon");
        }
    }
}
