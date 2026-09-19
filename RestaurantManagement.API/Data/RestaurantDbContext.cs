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
    public DbSet<MonAnSize> MonAnSize => Set<MonAnSize>();
    public DbSet<ChiTietCombo> ChiTietCombo => Set<ChiTietCombo>();
    public DbSet<KhuVuc> KhuVuc => Set<KhuVuc>();
    public DbSet<BanAn> BanAn => Set<BanAn>();
    public DbSet<DatBan> DatBan => Set<DatBan>();
    public DbSet<ChiTietDatBan> ChiTietDatBan => Set<ChiTietDatBan>();
    public DbSet<MonDatTruoc> MonDatTruoc => Set<MonDatTruoc>();
    public DbSet<HoaDon> HoaDon => Set<HoaDon>();
    public DbSet<ChiTietHoaDon> ChiTietHoaDon => Set<ChiTietHoaDon>();
    public DbSet<NguyenLieu> NguyenLieu => Set<NguyenLieu>();
    public DbSet<DinhMucMon> DinhMucMon => Set<DinhMucMon>();
    public DbSet<NhaCungCap> NhaCungCap => Set<NhaCungCap>();
    public DbSet<PhieuNhap> PhieuNhap => Set<PhieuNhap>();
    public DbSet<ChiTietPhieuNhap> ChiTietPhieuNhap => Set<ChiTietPhieuNhap>();
    public DbSet<PhieuXuat> PhieuXuat => Set<PhieuXuat>();
    public DbSet<ChiTietPhieuXuat> ChiTietPhieuXuat => Set<ChiTietPhieuXuat>();
    public DbSet<KhuyenMai> KhuyenMai => Set<KhuyenMai>();
    public DbSet<KhuyenMaiMon> KhuyenMaiMon => Set<KhuyenMaiMon>();
    public DbSet<Voucher> Voucher => Set<Voucher>();
    public DbSet<DanhGia> DanhGia => Set<DanhGia>();

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
        b.Entity<MonAnSize>().HasIndex(x => new { x.MonAnId, x.TenSize }).IsUnique();
        b.Entity<MonAnSize>().HasOne(x => x.MonAn).WithMany(x => x.Sizes).HasForeignKey(x => x.MonAnId);
        b.Entity<ChiTietCombo>().HasKey(x => new { x.ComboId, x.MonAnId });
        b.Entity<ChiTietCombo>().HasOne(x => x.Combo).WithMany(x => x.ThanhPhanCombo).HasForeignKey(x => x.ComboId);
        b.Entity<ChiTietCombo>().HasOne(x => x.MonAn).WithMany().HasForeignKey(x => x.MonAnId);
        b.Entity<BanAn>().HasIndex(x => x.MaBan).IsUnique();
        b.Entity<DatBan>().HasIndex(x => x.MaDatBan).IsUnique();
        b.Entity<DatBan>().HasIndex(x => new { x.TrangThai, x.GioDen, x.GioKetThucDuKien });
        b.Entity<ChiTietDatBan>().HasKey(x => new { x.DatBanId, x.BanAnId });
        b.Entity<ChiTietDatBan>().HasOne(x => x.DatBan).WithMany(x => x.Ban).HasForeignKey(x => x.DatBanId);
        b.Entity<ChiTietDatBan>().HasIndex(x => x.BanAnId);
        b.Entity<MonDatTruoc>().HasOne(x => x.DatBan).WithMany(x => x.MonDatTruoc).HasForeignKey(x => x.DatBanId);
        b.Entity<MonDatTruoc>().HasOne(x => x.MonAnSize).WithMany().HasForeignKey(x => x.MonAnSizeId);
        b.Entity<HoaDon>().HasIndex(x => x.MaHoaDon).IsUnique();
        b.Entity<HoaDon>().HasOne(x => x.DatBan).WithMany(x => x.HoaDon).HasForeignKey(x => x.DatBanId);
        b.Entity<HoaDon>().HasOne(x => x.Voucher).WithMany().HasForeignKey(x => x.VoucherId);
        b.Entity<HoaDon>().Property(x => x.TongThanhToan)
            .HasComputedColumnSql("[TongTienHang]-[TienGiam]-[TienCocDaTru]", stored: true);
        b.Entity<ChiTietHoaDon>().HasOne(x => x.HoaDon).WithMany(x => x.ChiTiet).HasForeignKey(x => x.HoaDonId);
        b.Entity<ChiTietHoaDon>().HasOne(x => x.MonAnSize).WithMany().HasForeignKey(x => x.MonAnSizeId);
        b.Entity<ChiTietHoaDon>().HasOne(x => x.MonDatTruoc).WithOne().HasForeignKey<ChiTietHoaDon>(x => x.MonDatTruocId);
        b.Entity<ChiTietHoaDon>().HasAlternateKey(x => new { x.Id, x.HoaDonId });
        b.Entity<NguyenLieu>().HasIndex(x => x.TenNguyenLieu).IsUnique();
        b.Entity<DinhMucMon>().HasKey(x => new { x.MonAnId, x.NguyenLieuId });
        b.Entity<PhieuNhap>().HasIndex(x => x.MaPhieu).IsUnique();
        b.Entity<PhieuXuat>().HasIndex(x => x.MaPhieu).IsUnique();
        b.Entity<ChiTietPhieuXuat>().HasOne(x => x.LoNhap).WithMany().HasForeignKey(x => x.ChiTietPhieuNhapId);
        b.Entity<KhuyenMaiMon>().HasKey(x => new { x.KhuyenMaiId, x.MonAnId });
        b.Entity<Voucher>().HasIndex(x => x.Ma).IsUnique();
        b.Entity<DanhGia>().HasOne(x => x.HoaDon).WithMany().HasForeignKey(x => x.HoaDonId);
        b.Entity<DanhGia>().HasOne(x => x.ChiTietHoaDon).WithMany()
            .HasForeignKey(x => new { x.ChiTietHoaDonId, x.HoaDonId }).HasPrincipalKey(x => new { x.Id, x.HoaDonId });
        b.Entity<DanhGia>().HasIndex(x => new { x.HoaDonId, x.ChiTietHoaDonId }).IsUnique().HasFilter(null);

        Check<MonAnSize>(b, "Gia", "[GiaBan]>=0");
        Check<ChiTietCombo>(b, "ThanhPhan", "[ComboId]<>[MonAnId] AND [SoLuong]>0");
        Check<BanAn>(b, "SoCho", "[SoChoNgoi]>0");
        Check<DatBan>(b, "ThoiGian", "[GioKetThucDuKien]>[GioDen]");
        Check<DatBan>(b, "SoKhach", "[SoNguoiLon]>=0 AND [SoTreEm]>=0 AND [SoNguoiLon]+[SoTreEm]>0");
        Check<DatBan>(b, "Coc", "[TienCocYeuCau]>=0 AND [TienCocDaNop]>=0");
        Check<DatBan>(b, "LienHe", "[LaKhachTrucTiep]=1 OR ([SoDienThoaiLienHe] IS NOT NULL AND LEN([SoDienThoaiLienHe])>0)");
        Check<DatBan>(b, "NhanHuy", "([TrangThai]<>'DaNhanBan' OR [ThoiDiemNhanBan] IS NOT NULL) AND ([TrangThai]<>'DaHuy' OR [ThoiDiemHuy] IS NOT NULL)");
        Check<MonDatTruoc>(b, "LuongGia", "[SoLuong]>0 AND [DonGiaThoaThuan]>=0");
        Check<HoaDon>(b, "SoTien", "[TongTienHang]>=0 AND [TienGiam]>=0 AND [TienGiam]<=[TongTienHang] AND [TienCocDaTru]>=0 AND [TienCocDaTru]<=[TongTienHang]-[TienGiam]");
        Check<ChiTietHoaDon>(b, "LuongGia", "[SoLuong]>0 AND [DonGia]>=0");
        Check<NguyenLieu>(b, "Nguong", "[NguongCanhBao]>=0");
        Check<DinhMucMon>(b, "SoLuong", "[SoLuong]>0");
        Check<PhieuNhap>(b, "NhaCungCap", "[LyDo]<>'MuaHang' OR [NhaCungCapId] IS NOT NULL");
        Check<ChiTietPhieuNhap>(b, "LuongGia", "[SoLuong]>0 AND [DonGia]>=0");
        Check<ChiTietPhieuXuat>(b, "SoLuong", "[SoLuong]>0");
        Check<KhuyenMai>(b, "DieuKien", "[KetThuc]>[BatDau] AND [GiaTri]>0 AND ([KieuGiam]<>'PhanTram' OR [GiaTri]<=100) AND [GiaTriToiThieu]>=0 AND ([MucGiamToiDa] IS NULL OR [MucGiamToiDa]>0) AND [ThuTuApDung]>=0");
        Check<Voucher>(b, "Luot", "[GioiHanTongLuot]>0");
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
                    bool quantity = p.Name is "SoLuong" or "NguongCanhBao";
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
