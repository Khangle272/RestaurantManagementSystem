using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class Week9SizeRatioAndExtraIngredients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GhiChuThanhPhanKhacBiet",
                table: "MonAnSize",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HeSoTyLe",
                table: "MonAnSize",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 1.0m);

            migrationBuilder.AddColumn<bool>(
                name: "LaNguyenLieuPhu",
                table: "DinhMucMon",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddCheckConstraint(
                name: "CK_MonAnSize_HeSoTyLe",
                table: "MonAnSize",
                sql: "[HeSoTyLe]>0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_MonAnSize_HeSoTyLe",
                table: "MonAnSize");

            migrationBuilder.DropColumn(
                name: "GhiChuThanhPhanKhacBiet",
                table: "MonAnSize");

            migrationBuilder.DropColumn(
                name: "HeSoTyLe",
                table: "MonAnSize");

            migrationBuilder.DropColumn(
                name: "LaNguyenLieuPhu",
                table: "DinhMucMon");
        }
    }
}
