using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class Week8PreordersAndDepositLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DatBan_KhachHangId",
                table: "DatBan");

            migrationBuilder.AddColumn<string>(
                name: "TenSizeLucDat",
                table: "MonDatTruoc",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "ChuanBiTruoc",
                table: "DatBan",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LanLuuMonHash",
                table: "DatBan",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LanLuuMonId",
                table: "DatBan",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TienCocDaGiu",
                table: "DatBan",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TienCocDaHoan",
                table: "DatBan",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "YeuCauCoc",
                table: "DatBan",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "YeuCauHuy",
                table: "DatBan",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "YeuCauTaoId",
                table: "DatBan",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GiaoDichCoc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DatBanId = table.Column<int>(type: "int", nullable: false),
                    Loai = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ThoiDiem = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TaiKhoanXuLyId = table.Column<int>(type: "int", nullable: false),
                    MaThamChieu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LyDo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    YeuCauId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiaoDichCoc", x => x.Id);
                    table.CheckConstraint("CK_GiaoDichCoc_Loai_Enum", "[Loai] IN ('Thu','Hoan','Giu')");
                    table.CheckConstraint("CK_GiaoDichCoc_SoTien", "[SoTien]>0");
                    table.ForeignKey(
                        name: "FK_GiaoDichCoc_DatBan_DatBanId",
                        column: x => x.DatBanId,
                        principalTable: "DatBan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiaoDichCoc_TaiKhoan_TaiKhoanXuLyId",
                        column: x => x.TaiKhoanXuLyId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DatBan_KhachHangId_YeuCauTaoId",
                table: "DatBan",
                columns: new[] { "KhachHangId", "YeuCauTaoId" },
                unique: true,
                filter: "[YeuCauTaoId] IS NOT NULL AND [KhachHangId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DatBan_XuLyCoc",
                table: "DatBan",
                sql: "[TienCocDaHoan]>=0 AND [TienCocDaGiu]>=0 AND [TienCocDaHoan]+[TienCocDaGiu]<=[TienCocDaNop]");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichCoc_DatBanId",
                table: "GiaoDichCoc",
                column: "DatBanId");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichCoc_TaiKhoanXuLyId",
                table: "GiaoDichCoc",
                column: "TaiKhoanXuLyId");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichCoc_YeuCauId",
                table: "GiaoDichCoc",
                column: "YeuCauId",
                unique: true);

            // Keep legacy bookings and agreed sizes meaningful without replacing their history.
            migrationBuilder.Sql("""
                UPDATE p SET p.TenSizeLucDat = COALESCE(s.TenSize, N'Mặc định')
                FROM MonDatTruoc p LEFT JOIN MonAnSize s ON s.Id = p.MonAnSizeId;
                UPDATE DatBan SET TienCocDaHoan = TienCocDaNop WHERE TrangThaiCoc = N'DaHoan';
                UPDATE b SET YeuCauCoc = 1 FROM DatBan b WHERE b.TienCocYeuCau > 0
                    OR b.TienCocDaNop > 0 OR b.SoNguoiLon + b.SoTreEm >= 8
                    OR b.YeuCauVip = 1 OR b.YeuCauTrangTri = 1
                    OR EXISTS (SELECT 1 FROM MonDatTruoc p WHERE p.DatBanId = b.Id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GiaoDichCoc");

            migrationBuilder.DropIndex(
                name: "IX_DatBan_KhachHangId_YeuCauTaoId",
                table: "DatBan");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DatBan_XuLyCoc",
                table: "DatBan");

            migrationBuilder.DropColumn(
                name: "TenSizeLucDat",
                table: "MonDatTruoc");

            migrationBuilder.DropColumn(
                name: "ChuanBiTruoc",
                table: "DatBan");

            migrationBuilder.DropColumn(
                name: "LanLuuMonHash",
                table: "DatBan");

            migrationBuilder.DropColumn(
                name: "LanLuuMonId",
                table: "DatBan");

            migrationBuilder.DropColumn(
                name: "TienCocDaGiu",
                table: "DatBan");

            migrationBuilder.DropColumn(
                name: "TienCocDaHoan",
                table: "DatBan");

            migrationBuilder.DropColumn(
                name: "YeuCauCoc",
                table: "DatBan");

            migrationBuilder.DropColumn(
                name: "YeuCauHuy",
                table: "DatBan");

            migrationBuilder.DropColumn(
                name: "YeuCauTaoId",
                table: "DatBan");

            migrationBuilder.CreateIndex(
                name: "IX_DatBan_KhachHangId",
                table: "DatBan",
                column: "KhachHangId");
        }
    }
}
