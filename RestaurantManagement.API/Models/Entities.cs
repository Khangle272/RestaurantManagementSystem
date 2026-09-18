using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace RestaurantManagement.API.Models;

public enum TrangThaiBan { SanSang, DangPhucVu, CanDon, NgungSuDung }
public enum TrangThaiDatBan { ChoXacNhan, ChoCoc, DaXacNhan, DaNhanBan, DaHuy, KhongDen }
public enum LoaiDon { TaiCho, GiaoHang }
public enum TrangThaiDon { Moi, DaXacNhan, DangPhucVu, DangGiao, HoanTat, DaHuy }
public enum LoaiMon { MonLe, ThucUong, Set }
public enum TrangThaiMon { DangPhucVu, TamHet, NgungKinhDoanh }
public enum TrangThaiCheBien { ChoCheBien, DangCheBien, SanSang, DaPhucVu, DaHuy }
public enum TrangThaiHoaDon { ChuaThanhToan, ThanhToanMotPhan, DaThanhToan, DaHuy }
public enum LoaiGiaoDich { ThuCoc, ThuHoaDon, HoanCoc, HoanThanhToan }
public enum TrangThaiGiaoDich { ChoXuLy, ThanhCong, ThatBai }
public enum PhuongThucThanhToan { TienMat, ChuyenKhoan, The, ViDienTu }
public enum TrangThaiPhieu { Nhap, DaGhiSo, DaHuy }
public enum LyDoNhap { MuaHang, TonDauKy, DieuChinhTang }
public enum LyDoXuat { CheBien, ThanhLy, HuyHong, TraNhaCungCap, DieuChinhGiam }
public enum PhamViUuDai { MonAn, HoaDon }
public enum KieuGiam { PhanTram, SoTien }

public class TaiKhoan : IdentityUser<int> { }

public class NhanVien
{
    public int Id { get; set; }
    [MaxLength(20)] public string MaNhanVien { get; set; } = "";
    [MaxLength(120)] public string HoTen { get; set; } = "";
    [MaxLength(20)] public string SoDienThoai { get; set; } = "";
    [MaxLength(256)] public string? Email { get; set; }
    [MaxLength(300)] public string? DiaChi { get; set; }
    [MaxLength(80)] public string ChucVu { get; set; } = "";
    public DateOnly NgayVaoLam { get; set; }
    public bool DangLamViec { get; set; } = true;
    public int? TaiKhoanId { get; set; }
    public TaiKhoan? TaiKhoan { get; set; }
}

public class KhachHang
{
    public int Id { get; set; }
    [MaxLength(120)] public string HoTen { get; set; } = "";
    [MaxLength(20)] public string SoDienThoai { get; set; } = "";
    [MaxLength(256)] public string? Email { get; set; }
    public DateTimeOffset NgayDangKy { get; set; }
    public bool DongYNhanUuDai { get; set; }
    public DateTimeOffset? ThoiDiemDongYNhanUuDai { get; set; }
    public bool DangSuDung { get; set; } = true;
    public int? TaiKhoanId { get; set; }
    public TaiKhoan? TaiKhoan { get; set; }
}

public class DanhMuc
{
    public int Id { get; set; }
    [MaxLength(100)] public string TenDanhMuc { get; set; } = "";
    [MaxLength(500)] public string? MoTa { get; set; }
    public bool DangSuDung { get; set; } = true;
    public ICollection<MonAn> MonAn { get; set; } = new List<MonAn>();
}

public class MonAn
{
    public int Id { get; set; }
    public int DanhMucId { get; set; }
    public DanhMuc DanhMuc { get; set; } = null!;
    [MaxLength(150)] public string TenMon { get; set; } = "";
    [MaxLength(1000)] public string? MoTa { get; set; }
    [MaxLength(500)] public string? HinhAnh { get; set; }
    public LoaiMon Loai { get; set; }
    public decimal GiaBan { get; set; }
    public TrangThaiMon TrangThai { get; set; }
    public bool LaMonMoi { get; set; }
    public bool LaMonNoiBat { get; set; }
    public ICollection<ThanhPhanSet> ThanhPhan { get; set; } = new List<ThanhPhanSet>();
}

public class ThanhPhanSet
{
    public int SetId { get; set; }
    public MonAn Set { get; set; } = null!;
    public int MonAnId { get; set; }
    public MonAn MonAn { get; set; } = null!;
    public int SoLuong { get; set; }
}

public class KhuVuc
{
    public int Id { get; set; }
    [MaxLength(100)] public string TenKhuVuc { get; set; } = "";
    public bool LaPhongVip { get; set; }
    public bool DangSuDung { get; set; } = true;
}

public class BanAn
{
    public int Id { get; set; }
    [MaxLength(20)] public string MaBan { get; set; } = "";
    public int KhuVucId { get; set; }
    public KhuVuc KhuVuc { get; set; } = null!;
    public int SoChoNgoi { get; set; }
    public TrangThaiBan TrangThai { get; set; }
}

public class DatBan
{
    public int Id { get; set; }
    [MaxLength(30)] public string MaDatBan { get; set; } = "";
    public int? KhachHangId { get; set; }
    public KhachHang? KhachHang { get; set; }
    [MaxLength(120)] public string HoTenLienHe { get; set; } = "";
    [MaxLength(20)] public string? SoDienThoaiLienHe { get; set; }
    public bool LaKhachTrucTiep { get; set; }
    public DateTimeOffset ThoiDiemTao { get; set; }
    public DateTimeOffset GioDen { get; set; }
    public DateTimeOffset GioKetThucDuKien { get; set; }
    public int SoNguoiLon { get; set; }
    public int SoTreEm { get; set; }
    [MaxLength(1000)] public string? YeuCau { get; set; }
    public bool YeuCauTrangTri { get; set; }
    public bool YeuCauVip { get; set; }
    public decimal TienCocYeuCau { get; set; }
    [MaxLength(1000)] public string? DieuKienCocDaThoaThuan { get; set; }
    public TrangThaiDatBan TrangThai { get; set; }
    public DateTimeOffset? ThoiDiemNhanBan { get; set; }
    public DateTimeOffset? ThoiDiemHuy { get; set; }
    [MaxLength(500)] public string? LyDoHuy { get; set; }
    public int? NhanVienTiepNhanId { get; set; }
    public NhanVien? NhanVienTiepNhan { get; set; }
    public int? NhanVienHuyId { get; set; }
    public NhanVien? NhanVienHuy { get; set; }
    public ICollection<ChiTietDatBan> Ban { get; set; } = new List<ChiTietDatBan>();
    public ICollection<MonDatTruoc> MonDatTruoc { get; set; } = new List<MonDatTruoc>();
}

public class ChiTietDatBan
{
    public int DatBanId { get; set; }
    public DatBan DatBan { get; set; } = null!;
    public int BanAnId { get; set; }
    public BanAn BanAn { get; set; } = null!;
}

public class MonDatTruoc
{
    public int Id { get; set; }
    public int DatBanId { get; set; }
    public DatBan DatBan { get; set; } = null!;
    public int MonAnId { get; set; }
    public MonAn MonAn { get; set; } = null!;
    [MaxLength(150)] public string TenMonLucDat { get; set; } = "";
    public int SoLuong { get; set; }
    public decimal DonGiaThoaThuan { get; set; }
    [MaxLength(500)] public string? YeuCauCheBien { get; set; }
    // Snapshot set accepted at preorder; not a second editable catalogue.
    [MaxLength(4000)] public string? ThanhPhanSetSnapshot { get; set; }
}

public class DonHang
{
    public int Id { get; set; }
    [MaxLength(30)] public string MaDonHang { get; set; } = "";
    public int? DatBanId { get; set; }
    public DatBan? DatBan { get; set; }
    public int? KhachHangId { get; set; }
    public KhachHang? KhachHang { get; set; }
    public int? NhanVienLapId { get; set; }
    public NhanVien? NhanVienLap { get; set; }
    public LoaiDon Loai { get; set; }
    public TrangThaiDon TrangThai { get; set; }
    public DateTimeOffset ThoiDiemTao { get; set; }
    [MaxLength(120)] public string? TenNguoiNhan { get; set; }
    [MaxLength(20)] public string? DienThoaiGiaoHang { get; set; }
    [MaxLength(500)] public string? DiaChiGiaoHang { get; set; }
    public ICollection<ChiTietDonHang> ChiTiet { get; set; } = new List<ChiTietDonHang>();
    public HoaDon? HoaDon { get; set; }
}

public class DonHangBan
{
    public int Id { get; set; }
    public int DonHangId { get; set; }
    public DonHang DonHang { get; set; } = null!;
    public int BanAnId { get; set; }
    public BanAn BanAn { get; set; } = null!;
    public DateTimeOffset BatDau { get; set; }
    public DateTimeOffset? KetThuc { get; set; }
}

public class ChiTietDonHang
{
    public int Id { get; set; }
    public int DonHangId { get; set; }
    public DonHang DonHang { get; set; } = null!;
    public int MonAnId { get; set; }
    public MonAn MonAn { get; set; } = null!;
    public int? MonDatTruocId { get; set; }
    public MonDatTruoc? MonDatTruoc { get; set; }
    [MaxLength(150)] public string TenMonLucBan { get; set; } = "";
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
    [MaxLength(500)] public string? YeuCauCheBien { get; set; }
    [MaxLength(4000)] public string? ThanhPhanSetSnapshot { get; set; }
    public TrangThaiCheBien TrangThai { get; set; }
}

public class HoaDon
{
    public int Id { get; set; }
    [MaxLength(30)] public string SoHoaDon { get; set; } = "";
    public int DonHangId { get; set; }
    public DonHang DonHang { get; set; } = null!;
    public int ThuNganId { get; set; }
    public NhanVien ThuNgan { get; set; } = null!;
    public DateTimeOffset ThoiDiemLap { get; set; }
    public decimal TienMon { get; set; }
    public decimal TienGiam { get; set; }
    public decimal TienThue { get; set; }
    public decimal PhiDichVu { get; set; }
    public decimal PhiGiaoHang { get; set; }
    public decimal TongThanhToan { get; private set; }
    public TrangThaiHoaDon TrangThai { get; set; }
}

public class GiaoDichThanhToan
{
    public int Id { get; set; }
    public Guid KhoaChongLap { get; set; } = Guid.NewGuid();
    public int? DatBanId { get; set; }
    public DatBan? DatBan { get; set; }
    public int? HoaDonId { get; set; }
    public HoaDon? HoaDon { get; set; }
    public int? GiaoDichGocId { get; set; }
    public GiaoDichThanhToan? GiaoDichGoc { get; set; }
    public LoaiGiaoDich Loai { get; set; }
    public TrangThaiGiaoDich TrangThai { get; set; }
    public PhuongThucThanhToan PhuongThuc { get; set; }
    public decimal SoTien { get; set; }
    public DateTimeOffset ThoiDiem { get; set; }
    public int? NhanVienId { get; set; }
    public NhanVien? NhanVien { get; set; }
    [MaxLength(150)] public string? MaThamChieu { get; set; }
    [MaxLength(500)] public string? GhiChu { get; set; }
}

public class DoiTruCoc
{
    // One successful deposit receipt can be applied to only one invoice, once.
    public int GiaoDichCocId { get; set; }
    public GiaoDichThanhToan GiaoDichCoc { get; set; } = null!;
    public int HoaDonId { get; set; }
    public HoaDon HoaDon { get; set; } = null!;
    public decimal SoTien { get; set; }
    public DateTimeOffset ThoiDiem { get; set; }
}

public class DonViTinh
{
    public int Id { get; set; }
    [MaxLength(30)] public string TenDonVi { get; set; } = "";
}

public class NguyenLieu
{
    public int Id { get; set; }
    [MaxLength(120)] public string TenNguyenLieu { get; set; } = "";
    public int DonViCoSoId { get; set; }
    public DonViTinh DonViCoSo { get; set; } = null!;
    public decimal NguongCanhBao { get; set; }
    public bool DangSuDung { get; set; } = true;
}

public class QuyDoiNguyenLieu
{
    public int NguyenLieuId { get; set; }
    public NguyenLieu NguyenLieu { get; set; } = null!;
    public int DonViTinhId { get; set; }
    public DonViTinh DonViTinh { get; set; } = null!;
    public decimal HeSoVeDonViCoSo { get; set; }
}

public class DinhLuongMon
{
    public int MonAnId { get; set; }
    public MonAn MonAn { get; set; } = null!;
    public int NguyenLieuId { get; set; }
    public NguyenLieu NguyenLieu { get; set; } = null!;
    public decimal SoLuongCoSo { get; set; }
}

public class NhaCungCap
{
    public int Id { get; set; }
    [MaxLength(150)] public string TenNhaCungCap { get; set; } = "";
    [MaxLength(20)] public string? SoDienThoai { get; set; }
    [MaxLength(300)] public string? DiaChi { get; set; }
    [MaxLength(30)] public string? MaSoThue { get; set; }
    public bool DangSuDung { get; set; } = true;
}

public class PhieuNhap
{
    public int Id { get; set; }
    [MaxLength(30)] public string MaPhieu { get; set; } = "";
    public int? NhaCungCapId { get; set; }
    public NhaCungCap? NhaCungCap { get; set; }
    public int NhanVienId { get; set; }
    public NhanVien NhanVien { get; set; } = null!;
    public DateTimeOffset ThoiDiem { get; set; }
    public TrangThaiPhieu TrangThai { get; set; }
    public LyDoNhap LyDo { get; set; }
    [MaxLength(500)] public string? GhiChu { get; set; }
    public ICollection<ChiTietPhieuNhap> ChiTiet { get; set; } = new List<ChiTietPhieuNhap>();
}

public class ChiTietPhieuNhap
{
    public int Id { get; set; }
    public int PhieuNhapId { get; set; }
    public PhieuNhap PhieuNhap { get; set; } = null!;
    public int NguyenLieuId { get; set; }
    public NguyenLieu NguyenLieu { get; set; } = null!;
    public int DonViTinhId { get; set; }
    public DonViTinh DonViTinh { get; set; } = null!;
    public decimal SoLuong { get; set; }
    public decimal HeSoQuyDoi { get; set; }
    public decimal SoLuongCoSo { get; private set; }
    public decimal DonGia { get; set; }
    public DateOnly? HanSuDung { get; set; }
    [MaxLength(50)] public string? MaLo { get; set; }
}

public class PhieuXuat
{
    public int Id { get; set; }
    [MaxLength(30)] public string MaPhieu { get; set; } = "";
    public int NhanVienId { get; set; }
    public NhanVien NhanVien { get; set; } = null!;
    public int? DonHangId { get; set; }
    public DonHang? DonHang { get; set; }
    public DateTimeOffset ThoiDiem { get; set; }
    public TrangThaiPhieu TrangThai { get; set; }
    public LyDoXuat LyDo { get; set; }
    [MaxLength(500)] public string? GhiChu { get; set; }
    public ICollection<ChiTietPhieuXuat> ChiTiet { get; set; } = new List<ChiTietPhieuXuat>();
}

public class ChiTietPhieuXuat
{
    public int Id { get; set; }
    public int PhieuXuatId { get; set; }
    public PhieuXuat PhieuXuat { get; set; } = null!;
    public int NguyenLieuId { get; set; }
    public NguyenLieu NguyenLieu { get; set; } = null!;
    public int DonViTinhId { get; set; }
    public DonViTinh DonViTinh { get; set; } = null!;
    public decimal SoLuong { get; set; }
    public decimal HeSoQuyDoi { get; set; }
    public decimal SoLuongCoSo { get; private set; }
    // Optional lot allocation; must reference the same ingredient in the service.
    public int? ChiTietPhieuNhapId { get; set; }
    public ChiTietPhieuNhap? LoNhap { get; set; }
}

public class KhuyenMai
{
    public int Id { get; set; }
    [MaxLength(150)] public string TenChuongTrinh { get; set; } = "";
    public DateTimeOffset BatDau { get; set; }
    public DateTimeOffset KetThuc { get; set; }
    public PhamViUuDai PhamVi { get; set; }
    public KieuGiam KieuGiam { get; set; }
    public decimal GiaTri { get; set; }
    public decimal? MucGiamToiDa { get; set; }
    public decimal GiaTriToiThieu { get; set; }
    public bool ChoPhepKetHop { get; set; }
    public int ThuTuApDung { get; set; }
    public bool CanVoucher { get; set; }
    public bool DangSuDung { get; set; } = true;
}

public class KhuyenMaiMon
{
    public int KhuyenMaiId { get; set; }
    public KhuyenMai KhuyenMai { get; set; } = null!;
    public int MonAnId { get; set; }
    public MonAn MonAn { get; set; } = null!;
}

public class Voucher
{
    public int Id { get; set; }
    [MaxLength(40)] public string Ma { get; set; } = "";
    public int KhuyenMaiId { get; set; }
    public KhuyenMai KhuyenMai { get; set; } = null!;
    public int? KhachHangId { get; set; }
    public KhachHang? KhachHang { get; set; }
    public int GioiHanTongLuot { get; set; }
    public bool DangSuDung { get; set; } = true;
}

public class SuDungVoucher
{
    public int Id { get; set; }
    public int VoucherId { get; set; }
    public Voucher Voucher { get; set; } = null!;
    public int HoaDonId { get; set; }
    public HoaDon HoaDon { get; set; } = null!;
    public DateTimeOffset ThoiDiem { get; set; }
}

public class ApDungKhuyenMai
{
    public int Id { get; set; }
    public int HoaDonId { get; set; }
    public HoaDon HoaDon { get; set; } = null!;
    public int DonHangId { get; set; }
    public int? ChiTietDonHangId { get; set; }
    public ChiTietDonHang? ChiTietDonHang { get; set; }
    public int KhuyenMaiId { get; set; }
    public KhuyenMai KhuyenMai { get; set; } = null!;
    public int? SuDungVoucherId { get; set; }
    public SuDungVoucher? SuDungVoucher { get; set; }
    [MaxLength(150)] public string TenChuongTrinhLucApDung { get; set; } = "";
    public KieuGiam KieuGiamLucApDung { get; set; }
    public decimal GiaTriLucApDung { get; set; }
    public int ThuTu { get; set; }
    public decimal CoSoTinhGiam { get; set; }
    public decimal SoTienGiam { get; set; }
}

public class DanhGia
{
    public int Id { get; set; }
    public int DonHangId { get; set; }
    public DonHang DonHang { get; set; } = null!;
    public int? ChiTietDonHangId { get; set; }
    public ChiTietDonHang? ChiTietDonHang { get; set; }
    public int? KhachHangId { get; set; }
    public KhachHang? KhachHang { get; set; }
    public int Diem { get; set; }
    [MaxLength(2000)] public string? NoiDung { get; set; }
    public DateTimeOffset ThoiDiem { get; set; }
}
