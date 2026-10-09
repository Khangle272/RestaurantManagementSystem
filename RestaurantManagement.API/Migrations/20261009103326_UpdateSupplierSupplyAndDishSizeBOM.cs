using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSupplierSupplyAndDishSizeBOM : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DinhMucMon",
                table: "DinhMucMon");

            migrationBuilder.AddColumn<int>(
                name: "MaKichCo",
                table: "DinhMucMon",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE dm
                SET dm.MaKichCo = s.Id
                FROM DinhMucMon dm
                CROSS APPLY (
                    SELECT TOP 1 s.Id
                    FROM MonAnSize s
                    WHERE s.MonAnId = dm.MonAnId
                    ORDER BY s.Id
                ) s
                WHERE dm.MaKichCo = 0;

                DELETE FROM DinhMucMon WHERE MaKichCo = 0;
            ");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DinhMucMon",
                table: "DinhMucMon",
                columns: new[] { "MonAnId", "MaKichCo", "NguyenLieuId" });

            migrationBuilder.CreateTable(
                name: "NhaCungCapNguyenLieu",
                columns: table => new
                {
                    MaNhaCungCap = table.Column<int>(type: "int", nullable: false),
                    MaNguyenLieu = table.Column<int>(type: "int", nullable: false),
                    DonGiaCungUng = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaHangNCC = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NgayLienKet = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhaCungCapNguyenLieu", x => new { x.MaNhaCungCap, x.MaNguyenLieu });
                    table.CheckConstraint("CK_NhaCungCapNguyenLieu_DonGia", "[DonGiaCungUng]>=0");
                    table.ForeignKey(
                        name: "FK_NhaCungCapNguyenLieu_NguyenLieu_MaNguyenLieu",
                        column: x => x.MaNguyenLieu,
                        principalTable: "NguyenLieu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NhaCungCapNguyenLieu_NhaCungCap_MaNhaCungCap",
                        column: x => x.MaNhaCungCap,
                        principalTable: "NhaCungCap",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DinhMucMon_MaKichCo",
                table: "DinhMucMon",
                column: "MaKichCo");

            migrationBuilder.CreateIndex(
                name: "IX_NhaCungCapNguyenLieu_MaNguyenLieu",
                table: "NhaCungCapNguyenLieu",
                column: "MaNguyenLieu");

            migrationBuilder.AddForeignKey(
                name: "FK_DinhMucMon_MonAnSize_MaKichCo",
                table: "DinhMucMon",
                column: "MaKichCo",
                principalTable: "MonAnSize",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DinhMucMon_MonAnSize_MaKichCo",
                table: "DinhMucMon");

            migrationBuilder.DropTable(
                name: "NhaCungCapNguyenLieu");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DinhMucMon",
                table: "DinhMucMon");

            migrationBuilder.DropIndex(
                name: "IX_DinhMucMon_MaKichCo",
                table: "DinhMucMon");

            migrationBuilder.DropColumn(
                name: "MaKichCo",
                table: "DinhMucMon");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DinhMucMon",
                table: "DinhMucMon",
                columns: new[] { "MonAnId", "NguyenLieuId" });
        }
    }
}
