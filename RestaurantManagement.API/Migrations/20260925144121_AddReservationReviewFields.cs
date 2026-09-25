using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationReviewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_DatBan_TrangThai_Enum",
                table: "DatBan");

            migrationBuilder.AddColumn<string>(
                name: "EmailLienHe",
                table: "DatBan",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiemDichVu",
                table: "DanhGia",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "HinhAnh",
                table: "DanhGia",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhanHoi",
                table: "DanhGia",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ThoiDiemPhanHoi",
                table: "DanhGia",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_DatBan_TrangThai_Enum",
                table: "DatBan",
                sql: "[TrangThai] IN ('ChoXacNhan','ChoCoc','DaXacNhan','DaNhanBan','HoanTat','DaHuy','KhongDen')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DanhGia_DiemDichVu",
                table: "DanhGia",
                sql: "[DiemDichVu] BETWEEN 1 AND 5");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_DatBan_TrangThai_Enum",
                table: "DatBan");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DanhGia_DiemDichVu",
                table: "DanhGia");

            migrationBuilder.DropColumn(
                name: "EmailLienHe",
                table: "DatBan");

            migrationBuilder.DropColumn(
                name: "DiemDichVu",
                table: "DanhGia");

            migrationBuilder.DropColumn(
                name: "HinhAnh",
                table: "DanhGia");

            migrationBuilder.DropColumn(
                name: "PhanHoi",
                table: "DanhGia");

            migrationBuilder.DropColumn(
                name: "ThoiDiemPhanHoi",
                table: "DanhGia");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DatBan_TrangThai_Enum",
                table: "DatBan",
                sql: "[TrangThai] IN ('ChoXacNhan','ChoCoc','DaXacNhan','DaNhanBan','DaHuy','KhongDen')");
        }
    }
}
