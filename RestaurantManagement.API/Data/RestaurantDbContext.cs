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
    public DbSet<GiaoDichCoc> GiaoDichCoc => Set<GiaoDichCoc>();
    public DbSet<HoaDon> HoaDon => Set<HoaDon>();
    public DbSet<ChiTietHoaDon> ChiTietHoaDon => Set<ChiTietHoaDon>();
    public DbSet<ChiTietThanhToan> ChiTietThanhToan => Set<ChiTietThanhToan>();
    public DbSet<NguyenLieu> NguyenLieu => Set<NguyenLieu>();
    public DbSet<DinhMucMon> DinhMucMon => Set<DinhMucMon>();
    public DbSet<NhaCungCap> NhaCungCap => Set<NhaCungCap>();
    public DbSet<NhaCungCapNguyenLieu> NhaCungCapNguyenLieus => Set<NhaCungCapNguyenLieu>();
    public DbSet<PhieuNhap> PhieuNhap => Set<PhieuNhap>();
    public DbSet<ChiTietPhieuNhap> ChiTietPhieuNhap => Set<ChiTietPhieuNhap>();
    public DbSet<PhieuXuat> PhieuXuat => Set<PhieuXuat>();
    public DbSet<ChiTietPhieuXuat> ChiTietPhieuXuat => Set<ChiTietPhieuXuat>();
    public DbSet<PhieuThanhLy> PhieuThanhLy => Set<PhieuThanhLy>();
    public DbSet<ChiTietPhieuThanhLy> ChiTietPhieuThanhLy => Set<ChiTietPhieuThanhLy>();
    public DbSet<KhuyenMai> KhuyenMai => Set<KhuyenMai>();
    public DbSet<KhuyenMaiMon> KhuyenMaiMon => Set<KhuyenMaiMon>();
    public DbSet<Voucher> Voucher => Set<Voucher>();
    public DbSet<VoucherMon> VoucherMon => Set<VoucherMon>();
    public DbSet<VoucherKhachHang> VoucherKhachHang => Set<VoucherKhachHang>();
    public DbSet<LichSuTichDiem> LichSuTichDiem => Set<LichSuTichDiem>();
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
        b.Entity<MonAn>().Property(x => x.DaDuyet).HasDefaultValue(true);
        b.Entity<MonAnSize>().HasIndex(x => new { x.MonAnId, x.TenSize }).IsUnique();
        b.Entity<MonAnSize>().HasOne(x => x.MonAn).WithMany(x => x.Sizes).HasForeignKey(x => x.MonAnId);
        b.Entity<ChiTietCombo>().HasKey(x => new { x.ComboId, x.MonAnId });
        b.Entity<ChiTietCombo>().HasOne(x => x.Combo).WithMany(x => x.ThanhPhanCombo).HasForeignKey(x => x.ComboId);
        b.Entity<ChiTietCombo>().HasOne(x => x.MonAn).WithMany().HasForeignKey(x => x.MonAnId);
        b.Entity<BanAn>().HasIndex(x => x.MaBan).IsUnique();
        b.Entity<KhuVuc>().Property(x => x.Tang).HasDefaultValue(1);
        b.Entity<DatBan>().HasOne(x => x.KhuVucUuTien).WithMany().HasForeignKey(x => x.KhuVucUuTienId);
        b.Entity<DatBan>().HasIndex(x => x.MaDatBan).IsUnique();
        b.Entity<DatBan>().HasIndex(x => new { x.KhachHangId, x.YeuCauTaoId }).IsUnique()
            .HasFilter("[YeuCauTaoId] IS NOT NULL AND [KhachHangId] IS NOT NULL");
        b.Entity<GiaoDichCoc>().HasIndex(x => x.YeuCauId).IsUnique();
        b.Entity<GiaoDichCoc>().HasOne(x => x.DatBan).WithMany().HasForeignKey(x => x.DatBanId);
        b.Entity<GiaoDichCoc>().HasOne(x => x.TaiKhoanXuLy).WithMany().HasForeignKey(x => x.TaiKhoanXuLyId);
        b.Entity<DatBan>().HasIndex(x => new { x.TrangThai, x.GioDen, x.GioKetThucDuKien });
        b.Entity<ChiTietDatBan>().HasKey(x => new { x.DatBanId, x.BanAnId });
        b.Entity<ChiTietDatBan>().HasOne(x => x.DatBan).WithMany(x => x.Ban).HasForeignKey(x => x.DatBanId);
        b.Entity<ChiTietDatBan>().HasIndex(x => x.BanAnId);
        b.Entity<MonDatTruoc>().HasOne(x => x.DatBan).WithMany(x => x.MonDatTruoc).HasForeignKey(x => x.DatBanId);
        b.Entity<MonDatTruoc>().HasOne(x => x.MonAnSize).WithMany().HasForeignKey(x => x.MonAnSizeId);
        b.Entity<HoaDon>().HasIndex(x => x.MaHoaDon).IsUnique();
        b.Entity<HoaDon>().Property(x => x.LoaiDonHang).HasDefaultValue(LoaiDonHang.TaiBan);
        b.Entity<HoaDon>().HasOne(x => x.DatBan).WithMany(x => x.HoaDon).HasForeignKey(x => x.DatBanId);
        b.Entity<HoaDon>().HasOne(x => x.Voucher).WithMany().HasForeignKey(x => x.VoucherId);
        b.Entity<HoaDon>().Property(x => x.TongThanhToan)
            .HasComputedColumnSql("[TongTienHang]-[TienGiam]-[TienCocDaTru]", stored: true);
        b.Entity<ChiTietHoaDon>().Property(x => x.ThoiDiemGoi).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        b.Entity<ChiTietHoaDon>().HasOne(x => x.HoaDon).WithMany(x => x.ChiTiet).HasForeignKey(x => x.HoaDonId);
        b.Entity<ChiTietHoaDon>().HasOne(x => x.MonAnSize).WithMany().HasForeignKey(x => x.MonAnSizeId);
        b.Entity<ChiTietHoaDon>().HasOne(x => x.MonDatTruoc).WithOne().HasForeignKey<ChiTietHoaDon>(x => x.MonDatTruocId);
        b.Entity<ChiTietHoaDon>().HasAlternateKey(x => new { x.Id, x.HoaDonId });
        b.Entity<ChiTietThanhToan>().HasIndex(x => x.MaThanhToan).IsUnique();
        b.Entity<ChiTietThanhToan>().HasIndex(x => new { x.NgayThanhToan, x.GioThanhToan });
        b.Entity<ChiTietThanhToan>().HasOne(x => x.HoaDon).WithMany(x => x.ThanhToan).HasForeignKey(x => x.HoaDonId);
        b.Entity<ChiTietThanhToan>().HasOne(x => x.DatBan).WithMany().HasForeignKey(x => x.DatBanId);
        b.Entity<ChiTietThanhToan>().HasOne(x => x.NhanVien).WithMany().HasForeignKey(x => x.NhanVienId);
        b.Entity<NguyenLieu>().HasIndex(x => x.TenNguyenLieu).IsUnique();
        b.Entity<DinhMucMon>().HasKey(x => new { x.MonAnId, x.MaKichCo, x.NguyenLieuId });
        b.Entity<DinhMucMon>().HasOne(x => x.MonAn).WithMany().HasForeignKey(x => x.MonAnId);
        b.Entity<DinhMucMon>().HasOne(x => x.MonAnSize).WithMany(x => x.DinhMuc).HasForeignKey(x => x.MaKichCo);
        b.Entity<DinhMucMon>().HasOne(x => x.NguyenLieu).WithMany().HasForeignKey(x => x.NguyenLieuId);
        b.Entity<NhaCungCapNguyenLieu>().HasKey(x => new { x.MaNhaCungCap, x.MaNguyenLieu });
        b.Entity<NhaCungCapNguyenLieu>().HasOne(x => x.NhaCungCap).WithMany(x => x.NguyenLieuCungUng).HasForeignKey(x => x.MaNhaCungCap);
        b.Entity<NhaCungCapNguyenLieu>().HasOne(x => x.NguyenLieu).WithMany(x => x.NhaCungCapCungUng).HasForeignKey(x => x.MaNguyenLieu);
        b.Entity<PhieuNhap>().HasIndex(x => x.MaPhieu).IsUnique();
        b.Entity<PhieuXuat>().HasIndex(x => x.MaPhieu).IsUnique();
        b.Entity<PhieuThanhLy>().HasIndex(x => x.MaPhieu).IsUnique();
        b.Entity<PhieuThanhLy>().HasOne(x => x.NhanVien).WithMany().HasForeignKey(x => x.NhanVienId);
        b.Entity<ChiTietPhieuThanhLy>().HasOne(x => x.PhieuThanhLy).WithMany(x => x.ChiTiet).HasForeignKey(x => x.PhieuThanhLyId);
        b.Entity<ChiTietPhieuThanhLy>().HasOne(x => x.NguyenLieu).WithMany().HasForeignKey(x => x.NguyenLieuId);
        b.Entity<ChiTietPhieuXuat>().HasOne(x => x.LoNhap).WithMany().HasForeignKey(x => x.ChiTietPhieuNhapId);
        b.Entity<KhuyenMaiMon>().HasKey(x => new { x.KhuyenMaiId, x.MonAnId });
        b.Entity<Voucher>().HasIndex(x => x.Ma).IsUnique();
        b.Entity<Voucher>().HasOne(x => x.KhuyenMai).WithMany(x => x.Vouchers).HasForeignKey(x => x.KhuyenMaiId);
        b.Entity<VoucherMon>().HasKey(x => new { x.VoucherId, x.MonAnId });
        b.Entity<VoucherMon>().HasOne(x => x.Voucher).WithMany(x => x.MonApDung).HasForeignKey(x => x.VoucherId);
        b.Entity<VoucherKhachHang>().HasKey(x => new { x.KhachHangId, x.VoucherId });
        b.Entity<VoucherKhachHang>().HasIndex(x => x.VoucherId);
        b.Entity<LichSuTichDiem>().HasIndex(x => x.HoaDonId).IsUnique();
        b.Entity<LichSuTichDiem>().HasIndex(x => new { x.KhachHangId, x.VoucherId }).IsUnique().HasFilter("[VoucherId] IS NOT NULL");
        b.Entity<LichSuTichDiem>().HasIndex(x => new { x.KhachHangId, x.ThoiDiem });
        b.Entity<HoaDon>().HasIndex(x => new { x.KhachHangId, x.ThoiDiemLap });
        b.Entity<DanhGia>().HasOne(x => x.HoaDon).WithMany().HasForeignKey(x => x.HoaDonId);
        b.Entity<DanhGia>().HasOne(x => x.ChiTietHoaDon).WithMany()
            .HasForeignKey(x => new { x.ChiTietHoaDonId, x.HoaDonId }).HasPrincipalKey(x => new { x.Id, x.HoaDonId });
        b.Entity<DanhGia>().HasIndex(x => new { x.HoaDonId, x.ChiTietHoaDonId }).IsUnique().HasFilter(null);

        Check<MonAnSize>(b, "Gia", "[GiaBan]>=0");
        Check<MonAnSize>(b, "HeSoTyLe", "[HeSoTyLe]>0");
        Check<ChiTietCombo>(b, "ThanhPhan", "[ComboId]<>[MonAnId] AND [SoLuong]>0");
        Check<BanAn>(b, "SoCho", "[SoChoNgoi]>0");
        Check<DatBan>(b, "ThoiGian", "[GioKetThucDuKien]>[GioDen]");
        Check<DatBan>(b, "SoKhach", "[SoNguoiLon]>=0 AND [SoTreEm]>=0 AND [SoNguoiLon]+[SoTreEm]>0");
        Check<DatBan>(b, "Coc", "[TienCocYeuCau]>=0 AND [TienCocDaNop]>=0");
        Check<DatBan>(b, "XuLyCoc", "[TienCocDaHoan]>=0 AND [TienCocDaGiu]>=0 AND [TienCocDaHoan]+[TienCocDaGiu]<=[TienCocDaNop]");
        Check<GiaoDichCoc>(b, "SoTien", "[SoTien]>0");
        Check<DatBan>(b, "LienHe", "[LaKhachTrucTiep]=1 OR ([SoDienThoaiLienHe] IS NOT NULL AND LEN([SoDienThoaiLienHe])>0)");
        Check<DatBan>(b, "NhanHuy", "([TrangThai]<>'DaNhanBan' OR [ThoiDiemNhanBan] IS NOT NULL) AND ([TrangThai]<>'DaHuy' OR [ThoiDiemHuy] IS NOT NULL)");
        Check<MonDatTruoc>(b, "LuongGia", "[SoLuong]>0 AND [DonGiaThoaThuan]>=0");
        Check<HoaDon>(b, "SoTien", "[TongTienHang]>=0 AND [TienGiam]>=0 AND [TienGiam]<=[TongTienHang] AND [TienCocDaTru]>=0 AND [TienCocDaTru]<=[TongTienHang]-[TienGiam]");
        Check<ChiTietHoaDon>(b, "LuongGia", "[SoLuong]>0 AND [DonGia]>=0");
        Check<ChiTietThanhToan>(b, "SoTien", "[SoTien]>0");
        Check<ChiTietThanhToan>(b, "Nguon", "[HoaDonId] IS NOT NULL OR [DatBanId] IS NOT NULL");
        Check<KhuVuc>(b, "TienCoc", "[TienCocGiuCho]>=0");
        Check<NguyenLieu>(b, "Nguong", "[NguongCanhBao]>=0");
        Check<DinhMucMon>(b, "SoLuong", "[SoLuong]>0");
        Check<NhaCungCapNguyenLieu>(b, "DonGia", "[DonGiaCungUng]>=0");
        Check<PhieuNhap>(b, "NhaCungCap", "[LyDo]<>'MuaHang' OR [NhaCungCapId] IS NOT NULL");
        Check<ChiTietPhieuNhap>(b, "LuongGia", "[SoLuong]>0 AND [DonGia]>=0");
        Check<ChiTietPhieuXuat>(b, "SoLuong", "[SoLuong]>0");
        Check<ChiTietPhieuThanhLy>(b, "LuongGia", "[SoLuong]>0 AND [DonGiaVon]>=0");
        Check<KhuyenMai>(b, "DieuKien", "[KetThuc]>[BatDau] AND [GiaTri]>0 AND ([KieuGiam]<>'PhanTram' OR [GiaTri]<=100) AND [GiaTriToiThieu]>=0 AND ([MucGiamToiDa] IS NULL OR [MucGiamToiDa]>0) AND [ThuTuApDung]>=0");
        Check<KhuyenMai>(b, "PhatHanh", "[GioiHanPhatHanh] IS NULL OR [GioiHanPhatHanh]>0");
        Check<Voucher>(b, "Luot", "[GioiHanTongLuot] IS NULL OR [GioiHanTongLuot]>0");
        Check<Voucher>(b, "DieuKien", "([BatDau] IS NULL OR [KetThuc] IS NULL OR [KetThuc]>=[BatDau]) AND ([GiaTri] IS NULL OR ([GiaTri]>0 AND ([KieuGiam]<>'PhanTram' OR [GiaTri]<=100))) AND [GiaTriToiThieu]>=0 AND ([MucGiamToiDa] IS NULL OR [MucGiamToiDa]>0) AND ([GioiHanPhatHanh] IS NULL OR [GioiHanPhatHanh]>0)");
        Check<Voucher>(b, "DiemCanDoi", "[DiemCanDoi]>=0");
        Check<LichSuTichDiem>(b, "Diem", "([HoaDonId] IS NOT NULL AND [VoucherId] IS NULL AND [SoDiem]>=0 AND [SoTienTinhDiem]>=0) OR ([HoaDonId] IS NULL AND [VoucherId] IS NOT NULL AND [SoDiem]<0 AND [SoTienTinhDiem]=0)");
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
                    bool quantity = p.Name is "SoLuong" or "NguongCanhBao" or "SoLuongTon" or "HeSoTyLe";
                    b.Entity(entity.ClrType).Property(p.Name).HasPrecision(18, quantity ? 6 : 2);
                }
                var enumType = Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType;
                if (enumType.IsEnum)
                {
                    b.Entity(entity.ClrType).Property(p.Name).HasConversion<string>().HasMaxLength(40);
                    var values = string.Join(",", Enum.GetNames(enumType).Select(x => $"'{x}'"));
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
