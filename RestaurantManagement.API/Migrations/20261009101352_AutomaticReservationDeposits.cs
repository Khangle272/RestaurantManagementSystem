using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class AutomaticReservationDeposits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CocTuDong",
                table: "DatBan",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "HanThanhToanCoc",
                table: "DatBan",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaGiaoDichKhachBao",
                table: "DatBan",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SoTienKhachBao",
                table: "DatBan",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ThoiDiemBaoChuyenKhoan",
                table: "DatBan",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CocTuDong",
                table: "DatBan");

            migrationBuilder.DropColumn(
                name: "HanThanhToanCoc",
                table: "DatBan");

            migrationBuilder.DropColumn(
                name: "MaGiaoDichKhachBao",
                table: "DatBan");

            migrationBuilder.DropColumn(
                name: "SoTienKhachBao",
                table: "DatBan");

            migrationBuilder.DropColumn(
                name: "ThoiDiemBaoChuyenKhoan",
                table: "DatBan");
        }
    }
}
