using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class TableFloorAndReservationLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Tang",
                table: "KhuVuc",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "KhuVucUuTienId",
                table: "DatBan",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ThoiDiemKetThuc",
                table: "DatBan",
                type: "datetimeoffset",
                nullable: true);

            // Chỉ khép lịch sử cũ đã trả tiền, quá giờ và không còn bàn đang phục vụ.
            // Không coi việc thanh toán là khách rời bàn trong luồng mới.
            migrationBuilder.Sql("""
                UPDATE d
                SET ThoiDiemKetThuc = COALESCE(
                    (SELECT MAX(h.ThoiDiemThanhToan) FROM HoaDon h WHERE h.DatBanId = d.Id),
                    d.GioKetThucDuKien)
                FROM DatBan d
                WHERE d.TrangThai = N'DaNhanBan'
                  AND d.ThoiDiemKetThuc IS NULL
                  AND d.GioKetThucDuKien < SYSDATETIMEOFFSET()
                  AND EXISTS (SELECT 1 FROM HoaDon h WHERE h.DatBanId = d.Id AND h.TrangThai = N'DaThanhToan')
                  AND NOT EXISTS (SELECT 1 FROM HoaDon h WHERE h.DatBanId = d.Id AND h.TrangThai NOT IN (N'DaThanhToan', N'DaHuy'))
                  AND EXISTS (SELECT 1 FROM ChiTietDatBan c WHERE c.DatBanId = d.Id)
                  AND NOT EXISTS (
                      SELECT 1 FROM ChiTietDatBan c JOIN BanAn b ON b.Id = c.BanAnId
                      WHERE c.DatBanId = d.Id AND b.TrangThai = N'DangPhucVu')
                """);

            migrationBuilder.CreateIndex(
                name: "IX_DatBan_KhuVucUuTienId",
                table: "DatBan",
                column: "KhuVucUuTienId");

            migrationBuilder.AddForeignKey(
                name: "FK_DatBan_KhuVuc_KhuVucUuTienId",
                table: "DatBan",
                column: "KhuVucUuTienId",
                principalTable: "KhuVuc",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DatBan_KhuVuc_KhuVucUuTienId",
                table: "DatBan");

            migrationBuilder.DropIndex(
                name: "IX_DatBan_KhuVucUuTienId",
                table: "DatBan");

            migrationBuilder.DropColumn(
                name: "Tang",
                table: "KhuVuc");

            migrationBuilder.DropColumn(
                name: "KhuVucUuTienId",
                table: "DatBan");

            migrationBuilder.DropColumn(
                name: "ThoiDiemKetThuc",
                table: "DatBan");
        }
    }
}
