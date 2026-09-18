using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.API.Data;

public class RestaurantDbContext(DbContextOptions<RestaurantDbContext> options)
    : IdentityDbContext<TaiKhoan, IdentityRole<int>, int>(options)
{
    public DbSet<NhanVien> NhanVien => Set<NhanVien>();
    public DbSet<KhachHang> KhachHang => Set<KhachHang>();
    public DbSet<DanhMuc> DanhMuc => Set<DanhMuc>();
    public DbSet<MonAn> MonAn => Set<MonAn>();
    public DbSet<DatBan> DatBan => Set<DatBan>();
    public DbSet<DonHang> DonHang => Set<DonHang>();
    public DbSet<HoaDon> HoaDon => Set<HoaDon>();
    public DbSet<NguyenLieu> NguyenLieu => Set<NguyenLieu>();
    public DbSet<PhieuNhap> PhieuNhap => Set<PhieuNhap>();
    public DbSet<PhieuXuat> PhieuXuat => Set<PhieuXuat>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<TaiKhoan>().ToTable("TaiKhoan");
        b.Entity<IdentityRole<int>>().ToTable("VaiTro");
        b.Entity<IdentityUserRole<int>>().ToTable("TaiKhoanVaiTro");
        b.Entity<IdentityUserClaim<int>>().ToTable("TaiKhoanClaim");
        b.Entity<IdentityRoleClaim<int>>().ToTable("VaiTroClaim");
        b.Entity<IdentityUserLogin<int>>().ToTable("DangNhapNgoai");
        b.Entity<IdentityUserToken<int>>().ToTable("TokenTaiKhoan");

        b.Entity<NhanVien>().HasIndex(x => x.MaNhanVien).IsUnique();
        b.Entity<NhanVien>().HasOne(x => x.TaiKhoan).WithOne().HasForeignKey<NhanVien>(x => x.TaiKhoanId);
        b.Entity<KhachHang>().HasIndex(x => x.SoDienThoai).IsUnique();
        b.Entity<KhachHang>().HasOne(x => x.TaiKhoan).WithOne().HasForeignKey<KhachHang>(x => x.TaiKhoanId);
        b.Entity<DanhMuc>().HasIndex(x => x.TenDanhMuc).IsUnique();
        b.Entity<ThanhPhanSet>().HasKey(x => new { x.SetId, x.MonAnId });
        b.Entity<ThanhPhanSet>().HasOne(x => x.Set).WithMany(x => x.ThanhPhan).HasForeignKey(x => x.SetId);
        b.Entity<ThanhPhanSet>().HasOne(x => x.MonAn).WithMany().HasForeignKey(x => x.MonAnId);
        b.Entity<BanAn>().HasIndex(x => x.MaBan).IsUnique();
        b.Entity<DatBan>().HasIndex(x => x.MaDatBan).IsUnique();
        b.Entity<DatBan>().HasIndex(x => new { x.TrangThai, x.GioDen, x.GioKetThucDuKien });
        b.Entity<ChiTietDatBan>().HasKey(x => new { x.DatBanId, x.BanAnId });
        b.Entity<ChiTietDatBan>().HasOne(x => x.DatBan).WithMany(x => x.Ban).HasForeignKey(x => x.DatBanId);
        b.Entity<ChiTietDatBan>().HasIndex(x => x.BanAnId);
        b.Entity<MonDatTruoc>().HasOne(x => x.DatBan).WithMany(x => x.MonDatTruoc).HasForeignKey(x => x.DatBanId);
        b.Entity<DonHang>().HasIndex(x => x.MaDonHang).IsUnique();
        b.Entity<DonHang>().HasOne(x => x.DatBan).WithOne().HasForeignKey<DonHang>(x => x.DatBanId);
        b.Entity<DonHangBan>().HasIndex(x => x.BanAnId).IsUnique().HasFilter("[KetThuc] IS NULL");
        b.Entity<DonHangBan>().HasIndex(x => new { x.DonHangId, x.BanAnId, x.BatDau }).IsUnique();
        b.Entity<ChiTietDonHang>().HasAlternateKey(x => new { x.Id, x.DonHangId });
        b.Entity<ChiTietDonHang>().HasOne(x => x.MonDatTruoc).WithOne().HasForeignKey<ChiTietDonHang>(x => x.MonDatTruocId);
        b.Entity<HoaDon>().HasIndex(x => x.SoHoaDon).IsUnique();
        b.Entity<HoaDon>().HasOne(x => x.DonHang).WithOne(x => x.HoaDon).HasForeignKey<HoaDon>(x => x.DonHangId);
        b.Entity<HoaDon>().HasAlternateKey(x => new { x.Id, x.DonHangId });
        b.Entity<HoaDon>().Property(x => x.TongThanhToan)
            .HasComputedColumnSql("[TienMon]-[TienGiam]+[TienThue]+[PhiDichVu]+[PhiGiaoHang]", stored: true);
        b.Entity<GiaoDichThanhToan>().HasIndex(x => x.KhoaChongLap).IsUnique();
        b.Entity<DoiTruCoc>().HasKey(x => x.GiaoDichCocId);
        b.Entity<DoiTruCoc>().HasOne(x => x.GiaoDichCoc).WithOne().HasForeignKey<DoiTruCoc>(x => x.GiaoDichCocId);
        b.Entity<DonViTinh>().HasIndex(x => x.TenDonVi).IsUnique();
        b.Entity<QuyDoiNguyenLieu>().HasKey(x => new { x.NguyenLieuId, x.DonViTinhId });
        b.Entity<DinhLuongMon>().HasKey(x => new { x.MonAnId, x.NguyenLieuId });
        b.Entity<PhieuNhap>().HasIndex(x => x.MaPhieu).IsUnique();
        b.Entity<PhieuXuat>().HasIndex(x => x.MaPhieu).IsUnique();
        b.Entity<ChiTietPhieuNhap>().Property(x => x.SoLuongCoSo)
            .HasComputedColumnSql("CONVERT(decimal(18,6),[SoLuong]*[HeSoQuyDoi])", stored: true);
        b.Entity<ChiTietPhieuXuat>().Property(x => x.SoLuongCoSo)
            .HasComputedColumnSql("CONVERT(decimal(18,6),[SoLuong]*[HeSoQuyDoi])", stored: true);
        b.Entity<ChiTietPhieuXuat>().HasOne(x => x.LoNhap).WithMany().HasForeignKey(x => x.ChiTietPhieuNhapId);
        b.Entity<KhuyenMaiMon>().HasKey(x => new { x.KhuyenMaiId, x.MonAnId });
        b.Entity<Voucher>().HasIndex(x => x.Ma).IsUnique();
        b.Entity<SuDungVoucher>().HasIndex(x => new { x.VoucherId, x.HoaDonId }).IsUnique();
        b.Entity<SuDungVoucher>().HasAlternateKey(x => new { x.Id, x.HoaDonId });
        b.Entity<ApDungKhuyenMai>().HasOne(x => x.HoaDon).WithMany()
            .HasForeignKey(x => new { x.HoaDonId, x.DonHangId }).HasPrincipalKey(x => new { x.Id, x.DonHangId });
        b.Entity<ApDungKhuyenMai>().HasOne(x => x.ChiTietDonHang).WithMany()
            .HasForeignKey(x => new { x.ChiTietDonHangId, x.DonHangId }).HasPrincipalKey(x => new { x.Id, x.DonHangId });
        b.Entity<ApDungKhuyenMai>().HasOne(x => x.SuDungVoucher).WithMany()
            .HasForeignKey(x => new { x.SuDungVoucherId, x.HoaDonId }).HasPrincipalKey(x => new { x.Id, x.HoaDonId });
        b.Entity<ApDungKhuyenMai>().HasIndex(x => new { x.HoaDonId, x.KhuyenMaiId, x.ChiTietDonHangId })
            .IsUnique().HasFilter(null);
        b.Entity<DanhGia>().HasOne(x => x.ChiTietDonHang).WithMany()
            .HasForeignKey(x => new { x.ChiTietDonHangId, x.DonHangId }).HasPrincipalKey(x => new { x.Id, x.DonHangId });
        b.Entity<DanhGia>().HasIndex(x => new { x.DonHangId, x.ChiTietDonHangId }).IsUnique().HasFilter(null);

        Check<MonAn>(b, "Gia", "[GiaBan]>=0");
        Check<ThanhPhanSet>(b, "ThanhPhan", "[SetId]<>[MonAnId] AND [SoLuong]>0");
        Check<BanAn>(b, "SoCho", "[SoChoNgoi]>0");
        Check<DatBan>(b, "ThoiGian", "[GioKetThucDuKien]>[GioDen]");
        Check<DatBan>(b, "SoKhach", "[SoNguoiLon]>=0 AND [SoTreEm]>=0 AND [SoNguoiLon]+[SoTreEm]>0");
        Check<DatBan>(b, "Coc", "[TienCocYeuCau]>=0");
        Check<DatBan>(b, "LienHe", "[LaKhachTrucTiep]=1 OR ([SoDienThoaiLienHe] IS NOT NULL AND LEN([SoDienThoaiLienHe])>0)");
        Check<DatBan>(b, "NhanHuy", "([TrangThai]<>'DaNhanBan' OR [ThoiDiemNhanBan] IS NOT NULL) AND ([TrangThai]<>'DaHuy' OR [ThoiDiemHuy] IS NOT NULL)");
        Check<MonDatTruoc>(b, "LuongGia", "[SoLuong]>0 AND [DonGiaThoaThuan]>=0");
        Check<DonHang>(b, "LoaiDon", "([Loai]='TaiCho' AND [DatBanId] IS NOT NULL) OR ([Loai]='GiaoHang' AND [DatBanId] IS NULL AND [TenNguoiNhan] IS NOT NULL AND [DienThoaiGiaoHang] IS NOT NULL AND [DiaChiGiaoHang] IS NOT NULL)");
        Check<DonHangBan>(b, "ThoiGian", "[KetThuc] IS NULL OR [KetThuc]>=[BatDau]");
        Check<ChiTietDonHang>(b, "LuongGia", "[SoLuong]>0 AND [DonGia]>=0");
        Check<HoaDon>(b, "SoTien", "[TienMon]>=0 AND [TienGiam]>=0 AND [TienGiam]<=[TienMon] AND [TienThue]>=0 AND [PhiDichVu]>=0 AND [PhiGiaoHang]>=0");
        Check<GiaoDichThanhToan>(b, "SoTien", "[SoTien]>0");
        Check<GiaoDichThanhToan>(b, "Dich", "([Loai] IN ('ThuCoc','HoanCoc') AND [DatBanId] IS NOT NULL AND [HoaDonId] IS NULL) OR ([Loai] IN ('ThuHoaDon','HoanThanhToan') AND [HoaDonId] IS NOT NULL AND [DatBanId] IS NULL)");
        Check<GiaoDichThanhToan>(b, "Hoan", "([Loai] IN ('ThuCoc','ThuHoaDon') AND [GiaoDichGocId] IS NULL) OR ([Loai] IN ('HoanCoc','HoanThanhToan') AND [GiaoDichGocId] IS NOT NULL AND [GiaoDichGocId]<>[Id])");
        Check<DoiTruCoc>(b, "SoTien", "[SoTien]>0");
        Check<NguyenLieu>(b, "Nguong", "[NguongCanhBao]>=0");
        Check<QuyDoiNguyenLieu>(b, "HeSo", "[HeSoVeDonViCoSo]>0");
        Check<DinhLuongMon>(b, "SoLuong", "[SoLuongCoSo]>0");
        Check<PhieuNhap>(b, "NhaCungCap", "[LyDo]<>'MuaHang' OR [NhaCungCapId] IS NOT NULL");
        Check<ChiTietPhieuNhap>(b, "LuongGia", "[SoLuong]>0 AND [HeSoQuyDoi]>0 AND [DonGia]>=0");
        Check<ChiTietPhieuXuat>(b, "SoLuong", "[SoLuong]>0 AND [HeSoQuyDoi]>0");
        Check<KhuyenMai>(b, "DieuKien", "[KetThuc]>[BatDau] AND [GiaTri]>0 AND ([KieuGiam]<>'PhanTram' OR [GiaTri]<=100) AND [GiaTriToiThieu]>=0 AND ([MucGiamToiDa] IS NULL OR [MucGiamToiDa]>0) AND [ThuTuApDung]>=0");
        Check<Voucher>(b, "Luot", "[GioiHanTongLuot]>0");
        Check<ApDungKhuyenMai>(b, "SoTien", "[SoTienGiam]>=0 AND [CoSoTinhGiam]>=[SoTienGiam] AND [GiaTriLucApDung]>0 AND [ThuTu]>=0");
        Check<DanhGia>(b, "Diem", "[Diem] BETWEEN 1 AND 5");

        // Common SQL representation, enum domains and optimistic concurrency.
        foreach (var entity in b.Model.GetEntityTypes().ToList())
        {
            var businessEntity = entity.ClrType.Namespace == typeof(MonAn).Namespace && entity.ClrType != typeof(TaiKhoan);
            if (businessEntity)
            {
                b.Entity(entity.ClrType).ToTable(entity.ClrType.Name);
                b.Entity(entity.ClrType).Property<byte[]>("RowVersion").IsRowVersion();
            }
            foreach (var p in entity.GetProperties().ToList())
            {
                if (p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?))
                {
                    bool quantity = p.Name is "SoLuong" or "SoLuongCoSo" or "HeSoQuyDoi" or "HeSoVeDonViCoSo" or "NguongCanhBao";
                    b.Entity(entity.ClrType).Property(p.Name).HasPrecision(18, quantity ? 6 : 2);
                }
                if (p.ClrType.IsEnum)
                {
                    b.Entity(entity.ClrType).Property(p.Name).HasConversion<string>().HasMaxLength(40);
                    var values = string.Join(",", Enum.GetNames(p.ClrType).Select(x => $"'{x}'"));
                    b.Entity(entity.ClrType).ToTable(t => t.HasCheckConstraint($"CK_{entity.ClrType.Name}_{p.Name}_Enum", $"[{p.Name}] IN ({values})"));
                }
            }
        }
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }

    private static void Check<T>(ModelBuilder b, string name, string sql) where T : class =>
        b.Entity<T>().ToTable(t => t.HasCheckConstraint($"CK_{typeof(T).Name}_{name}", sql));
}
