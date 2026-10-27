using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class LoyaltyVoucherRedemption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LichSuTichDiem_HoaDonId",
                table: "LichSuTichDiem");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LichSuTichDiem_Diem",
                table: "LichSuTichDiem");

            migrationBuilder.AddColumn<int>(
                name: "DiemCanDoi",
                table: "Voucher",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "HoaDonId",
                table: "LichSuTichDiem",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "VoucherId",
                table: "LichSuTichDiem",
                type: "int",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Voucher_DiemCanDoi",
                table: "Voucher",
                sql: "[DiemCanDoi]>=0");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuTichDiem_HoaDonId",
                table: "LichSuTichDiem",
                column: "HoaDonId",
                unique: true,
                filter: "[HoaDonId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuTichDiem_KhachHangId_VoucherId",
                table: "LichSuTichDiem",
                columns: new[] { "KhachHangId", "VoucherId" },
                unique: true,
                filter: "[VoucherId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuTichDiem_VoucherId",
                table: "LichSuTichDiem",
                column: "VoucherId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LichSuTichDiem_Diem",
                table: "LichSuTichDiem",
                sql: "([HoaDonId] IS NOT NULL AND [VoucherId] IS NULL AND [SoDiem]>=0 AND [SoTienTinhDiem]>=0) OR ([HoaDonId] IS NULL AND [VoucherId] IS NOT NULL AND [SoDiem]<0 AND [SoTienTinhDiem]=0)");

            migrationBuilder.AddForeignKey(
                name: "FK_LichSuTichDiem_Voucher_VoucherId",
                table: "LichSuTichDiem",
                column: "VoucherId",
                principalTable: "Voucher",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The old schema cannot represent redemption history. Never discard customer debits.
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [LichSuTichDiem] WHERE [VoucherId] IS NOT NULL) THROW 51000, N'Cannot roll back while loyalty redemptions exist.', 1;");

            migrationBuilder.DropForeignKey(
                name: "FK_LichSuTichDiem_Voucher_VoucherId",
                table: "LichSuTichDiem");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Voucher_DiemCanDoi",
                table: "Voucher");

            migrationBuilder.DropIndex(
                name: "IX_LichSuTichDiem_HoaDonId",
                table: "LichSuTichDiem");

            migrationBuilder.DropIndex(
                name: "IX_LichSuTichDiem_KhachHangId_VoucherId",
                table: "LichSuTichDiem");

            migrationBuilder.DropIndex(
                name: "IX_LichSuTichDiem_VoucherId",
                table: "LichSuTichDiem");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LichSuTichDiem_Diem",
                table: "LichSuTichDiem");

            migrationBuilder.DropColumn(
                name: "DiemCanDoi",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "VoucherId",
                table: "LichSuTichDiem");

            migrationBuilder.AlterColumn<int>(
                name: "HoaDonId",
                table: "LichSuTichDiem",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LichSuTichDiem_HoaDonId",
                table: "LichSuTichDiem",
                column: "HoaDonId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_LichSuTichDiem_Diem",
                table: "LichSuTichDiem",
                sql: "[SoDiem]>=0 AND [SoTienTinhDiem]>=0");
        }
    }
}
