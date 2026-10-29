using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class PaymentLedgerAndAreaDeposit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TienCocGiuCho",
                table: "KhuVuc",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ThoiDiemHuy",
                table: "ChiTietHoaDon",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChiTietThanhToan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaThanhToan = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    HoaDonId = table.Column<int>(type: "int", nullable: true),
                    DatBanId = table.Column<int>(type: "int", nullable: true),
                    NgayThanhToan = table.Column<DateOnly>(type: "date", nullable: false),
                    GioThanhToan = table.Column<TimeOnly>(type: "time", nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LoaiGiaoDich = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    HinhThuc = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    MaThamChieu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NhanVienId = table.Column<int>(type: "int", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietThanhToan", x => x.Id);
                    table.CheckConstraint("CK_ChiTietThanhToan_HinhThuc_Enum", "[HinhThuc] IN ('TienMat','ChuyenKhoan','The','ViDienTu')");
                    table.CheckConstraint("CK_ChiTietThanhToan_LoaiGiaoDich_Enum", "[LoaiGiaoDich] IN ('DatCoc','ThanhToan','HoanCoc')");
                    table.CheckConstraint("CK_ChiTietThanhToan_Nguon", "[HoaDonId] IS NOT NULL OR [DatBanId] IS NOT NULL");
                    table.CheckConstraint("CK_ChiTietThanhToan_SoTien", "[SoTien]>0");
                    table.ForeignKey(
                        name: "FK_ChiTietThanhToan_DatBan_DatBanId",
                        column: x => x.DatBanId,
                        principalTable: "DatBan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietThanhToan_HoaDon_HoaDonId",
                        column: x => x.HoaDonId,
                        principalTable: "HoaDon",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietThanhToan_NhanVien_NhanVienId",
                        column: x => x.NhanVienId,
                        principalTable: "NhanVien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_KhuVuc_TienCoc",
                table: "KhuVuc",
                sql: "[TienCocGiuCho]>=0");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietThanhToan_DatBanId",
                table: "ChiTietThanhToan",
                column: "DatBanId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietThanhToan_HoaDonId",
                table: "ChiTietThanhToan",
                column: "HoaDonId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietThanhToan_MaThanhToan",
                table: "ChiTietThanhToan",
                column: "MaThanhToan",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietThanhToan_NgayThanhToan_GioThanhToan",
                table: "ChiTietThanhToan",
                columns: new[] { "NgayThanhToan", "GioThanhToan" });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietThanhToan_NhanVienId",
                table: "ChiTietThanhToan",
                column: "NhanVienId");

            // Cọc giữ chỗ mặc định theo loại khu vực; quản lý chỉnh lại trong màn hình Khu vực.
            migrationBuilder.Sql("UPDATE [KhuVuc] SET [TienCocGiuCho] = CASE WHEN [LaPhongVip] = 1 THEN 500000 ELSE 200000 END;");

            // Chuyển lịch sử tiền đã thu sang sổ chi tiết thanh toán (ngày/giờ theo giờ Việt Nam).
            migrationBuilder.Sql(@"
INSERT INTO [ChiTietThanhToan] ([MaThanhToan],[HoaDonId],[DatBanId],[NgayThanhToan],[GioThanhToan],[SoTien],[LoaiGiaoDich],[HinhThuc],[MaThamChieu],[GhiChu])
SELECT CONCAT('TT-CO-', g.[Id]),
       (SELECT TOP 1 h.[Id] FROM [HoaDon] h WHERE h.[DatBanId] = g.[DatBanId] AND h.[TrangThai] = 'DaThanhToan' ORDER BY h.[ThoiDiemThanhToan]),
       g.[DatBanId],
       CAST(SWITCHOFFSET(g.[ThoiDiem], '+07:00') AS date), CAST(SWITCHOFFSET(g.[ThoiDiem], '+07:00') AS time(0)),
       g.[SoTien], CASE g.[Loai] WHEN 'Thu' THEN 'DatCoc' ELSE 'HoanCoc' END, 'ChuyenKhoan', g.[MaThamChieu], LEFT(g.[LyDo], 300)
FROM [GiaoDichCoc] g WHERE g.[Loai] IN ('Thu','Hoan');

INSERT INTO [ChiTietThanhToan] ([MaThanhToan],[HoaDonId],[DatBanId],[NgayThanhToan],[GioThanhToan],[SoTien],[LoaiGiaoDich],[HinhThuc],[MaThamChieu],[NhanVienId])
SELECT CONCAT('TT-HD-', h.[Id]), h.[Id], h.[DatBanId],
       CAST(SWITCHOFFSET(COALESCE(h.[ThoiDiemThanhToan], h.[ThoiDiemLap]), '+07:00') AS date),
       CAST(SWITCHOFFSET(COALESCE(h.[ThoiDiemThanhToan], h.[ThoiDiemLap]), '+07:00') AS time(0)),
       h.[TongThanhToan], 'ThanhToan', h.[PhuongThucThanhToan], h.[MaGiaoDich], h.[NhanVienId]
FROM [HoaDon] h WHERE h.[TrangThai] = 'DaThanhToan' AND h.[TongThanhToan] > 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietThanhToan");

            migrationBuilder.DropCheckConstraint(
                name: "CK_KhuVuc_TienCoc",
                table: "KhuVuc");

            migrationBuilder.DropColumn(
                name: "TienCocGiuCho",
                table: "KhuVuc");

            migrationBuilder.DropColumn(
                name: "ThoiDiemHuy",
                table: "ChiTietHoaDon");
        }
    }
}
