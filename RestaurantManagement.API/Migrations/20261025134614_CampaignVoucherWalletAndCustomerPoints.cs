using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class CampaignVoucherWalletAndCustomerPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Voucher_Luot",
                table: "Voucher");

            migrationBuilder.DropIndex(
                name: "IX_HoaDon_KhachHangId",
                table: "HoaDon");

            migrationBuilder.AlterColumn<int>(
                name: "GioiHanTongLuot",
                table: "Voucher",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BatDau",
                table: "Voucher",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GiaTri",
                table: "Voucher",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GiaTriToiThieu",
                table: "Voucher",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "GioiHanPhatHanh",
                table: "Voucher",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "KetThuc",
                table: "Voucher",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KieuGiam",
                table: "Voucher",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MucGiamToiDa",
                table: "Voucher",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhamVi",
                table: "Voucher",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GioiHanPhatHanh",
                table: "KhuyenMai",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LichSuTichDiem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KhachHangId = table.Column<int>(type: "int", nullable: false),
                    HoaDonId = table.Column<int>(type: "int", nullable: false),
                    SoDiem = table.Column<int>(type: "int", nullable: false),
                    SoTienTinhDiem = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ThoiDiem = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichSuTichDiem", x => x.Id);
                    table.CheckConstraint("CK_LichSuTichDiem_Diem", "[SoDiem]>=0 AND [SoTienTinhDiem]>=0");
                    table.ForeignKey(
                        name: "FK_LichSuTichDiem_HoaDon_HoaDonId",
                        column: x => x.HoaDonId,
                        principalTable: "HoaDon",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LichSuTichDiem_KhachHang_KhachHangId",
                        column: x => x.KhachHangId,
                        principalTable: "KhachHang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VoucherKhachHang",
                columns: table => new
                {
                    KhachHangId = table.Column<int>(type: "int", nullable: false),
                    VoucherId = table.Column<int>(type: "int", nullable: false),
                    ThoiDiemThuThap = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoucherKhachHang", x => new { x.KhachHangId, x.VoucherId });
                    table.ForeignKey(
                        name: "FK_VoucherKhachHang_KhachHang_KhachHangId",
                        column: x => x.KhachHangId,
                        principalTable: "KhachHang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VoucherKhachHang_Voucher_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Voucher",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VoucherMon",
                columns: table => new
                {
                    VoucherId = table.Column<int>(type: "int", nullable: false),
                    MonAnId = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoucherMon", x => new { x.VoucherId, x.MonAnId });
                    table.ForeignKey(
                        name: "FK_VoucherMon_MonAn_MonAnId",
                        column: x => x.MonAnId,
                        principalTable: "MonAn",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VoucherMon_Voucher_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Voucher",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Voucher_DieuKien",
                table: "Voucher",
                sql: "([BatDau] IS NULL OR [KetThuc] IS NULL OR [KetThuc]>=[BatDau]) AND ([GiaTri] IS NULL OR ([GiaTri]>0 AND ([KieuGiam]<>'PhanTram' OR [GiaTri]<=100))) AND [GiaTriToiThieu]>=0 AND ([MucGiamToiDa] IS NULL OR [MucGiamToiDa]>0) AND ([GioiHanPhatHanh] IS NULL OR [GioiHanPhatHanh]>0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Voucher_KieuGiam_Enum",
                table: "Voucher",
                sql: "[KieuGiam] IN ('PhanTram','SoTien')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Voucher_Luot",
                table: "Voucher",
                sql: "[GioiHanTongLuot] IS NULL OR [GioiHanTongLuot]>0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Voucher_PhamVi_Enum",
                table: "Voucher",
                sql: "[PhamVi] IN ('MonAn','HoaDon')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_KhuyenMai_PhatHanh",
                table: "KhuyenMai",
                sql: "[GioiHanPhatHanh] IS NULL OR [GioiHanPhatHanh]>0");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_KhachHangId_ThoiDiemLap",
                table: "HoaDon",
                columns: new[] { "KhachHangId", "ThoiDiemLap" });

            migrationBuilder.CreateIndex(
                name: "IX_LichSuTichDiem_HoaDonId",
                table: "LichSuTichDiem",
                column: "HoaDonId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LichSuTichDiem_KhachHangId_ThoiDiem",
                table: "LichSuTichDiem",
                columns: new[] { "KhachHangId", "ThoiDiem" });

            migrationBuilder.CreateIndex(
                name: "IX_VoucherKhachHang_VoucherId",
                table: "VoucherKhachHang",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_VoucherMon_MonAnId",
                table: "VoucherMon",
                column: "MonAnId");

            // Preserve every legacy code's effective terms, dates and dish links.
            // No invoices are rewritten and no historical loyalty points are invented.
            migrationBuilder.Sql("""
                EXEC(N'UPDATE v SET v.BatDau=p.BatDau, v.KetThuc=p.KetThuc,
                    v.PhamVi=p.PhamVi, v.KieuGiam=p.KieuGiam, v.GiaTri=p.GiaTri,
                    v.MucGiamToiDa=p.MucGiamToiDa, v.GiaTriToiThieu=p.GiaTriToiThieu
                FROM Voucher v INNER JOIN KhuyenMai p ON p.Id=v.KhuyenMaiId;
                INSERT INTO VoucherMon (VoucherId, MonAnId)
                SELECT v.Id, m.MonAnId FROM Voucher v
                INNER JOIN KhuyenMaiMon m ON m.KhuyenMaiId=v.KhuyenMaiId;');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The old schema has no unlimited representation; retain a positive use ceiling.
            migrationBuilder.Sql("UPDATE Voucher SET GioiHanTongLuot=2147483647 WHERE GioiHanTongLuot IS NULL;");
            migrationBuilder.DropTable(
                name: "LichSuTichDiem");

            migrationBuilder.DropTable(
                name: "VoucherKhachHang");

            migrationBuilder.DropTable(
                name: "VoucherMon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Voucher_DieuKien",
                table: "Voucher");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Voucher_KieuGiam_Enum",
                table: "Voucher");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Voucher_Luot",
                table: "Voucher");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Voucher_PhamVi_Enum",
                table: "Voucher");

            migrationBuilder.DropCheckConstraint(
                name: "CK_KhuyenMai_PhatHanh",
                table: "KhuyenMai");

            migrationBuilder.DropIndex(
                name: "IX_HoaDon_KhachHangId_ThoiDiemLap",
                table: "HoaDon");

            migrationBuilder.DropColumn(
                name: "BatDau",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "GiaTri",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "GiaTriToiThieu",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "GioiHanPhatHanh",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "KetThuc",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "KieuGiam",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "MucGiamToiDa",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "PhamVi",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "GioiHanPhatHanh",
                table: "KhuyenMai");

            migrationBuilder.AlterColumn<int>(
                name: "GioiHanTongLuot",
                table: "Voucher",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Voucher_Luot",
                table: "Voucher",
                sql: "[GioiHanTongLuot]>0");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_KhachHangId",
                table: "HoaDon",
                column: "KhachHangId");
        }
    }
}
