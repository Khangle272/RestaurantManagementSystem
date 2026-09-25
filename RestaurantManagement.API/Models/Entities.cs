using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace RestaurantManagement.API.Models;

public enum TrangThaiBan { SanSang, DangPhucVu, CanDon, NgungSuDung }
public enum TrangThaiDatBan { ChoXacNhan, ChoCoc, DaXacNhan, DaNhanBan, HoanTat, DaHuy, KhongDen }
public enum TrangThaiCoc { ChuaCoc, DaCoc, DaHoan, DaDoiTru }
public enum LoaiMon { MonLe, ThucUong, Set }
public enum TrangThaiMon { DangPhucVu, TamHet, NgungKinhDoanh }
public enum TrangThaiCheBien { ChoCheBien, DangCheBien, SanSang, DaPhucVu, DaHuy }
public enum TrangThaiHoaDon { ChuaThanhToan, ThanhToanMotPhan, DaThanhToan, DaHuy }
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
    public TrangThaiMon TrangThai { get; set; }
    public bool LaMonMoi { get; set; }
    public bool LaMonNoiBat { get; set; }
    public ICollection<MonAnSize> Sizes { get; set; } = new List<MonAnSize>();
    public ICollection<ChiTietCombo> ThanhPhanCombo { get; set; } = new List<ChiTietCombo>();
}

public class MonAnSize
{
    public int Id { get; set; }
    public int MonAnId { get; set; }
    public MonAn MonAn { get; set; } = null!;
    [MaxLength(50)] public string TenSize { get; set; } = "Mặc định";
    public decimal GiaBan { get; set; }
    public bool DangSuDung { get; set; } = true;
}

public class ChiTietCombo
{
    public int ComboId { get; set; }
    public MonAn Combo { get; set; } = null!;
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
    [MaxLength(256)] public string? EmailLienHe { get; set; }
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
    // Tiền cọc gốc lưu tại DatBan (không dùng bảng giao dịch riêng).
    public decimal TienCocDaNop { get; set; }
    public DateTimeOffset? ThoiDiemCoc { get; set; }
    public TrangThaiCoc TrangThaiCoc { get; set; } = TrangThaiCoc.ChuaCoc;
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
    public ICollection<HoaDon> HoaDon { get; set; } = new List<HoaDon>();
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
    public int? MonAnSizeId { get; set; }
    public MonAnSize? MonAnSize { get; set; }
    [MaxLength(150)] public string TenMonLucDat { get; set; } = "";
    public int SoLuong { get; set; }
    public decimal DonGiaThoaThuan { get; set; }
    [MaxLength(500)] public string? YeuCauCheBien { get; set; }
    // Snapshot combo tại thời điểm đặt trước; không phải danh mục thứ hai.
    [MaxLength(4000)] public string? ChiTietComboSnapshot { get; set; }
}

public class HoaDon
{
    public int Id { get; set; }
    [MaxLength(30)] public string MaHoaDon { get; set; } = "";
    public DateTimeOffset ThoiDiemLap { get; set; }
    public int? DatBanId { get; set; }
    public DatBan? DatBan { get; set; }
    public int? KhachHangId { get; set; }
    public KhachHang? KhachHang { get; set; }
    public int NhanVienId { get; set; }
    public NhanVien NhanVien { get; set; } = null!;
    public TrangThaiHoaDon TrangThai { get; set; }
    public decimal TongTienHang { get; set; }
    public decimal TienGiam { get; set; }
    // Số cọc thực tế được trừ vào bill (tiền gốc lưu ở DatBan).
    public decimal TienCocDaTru { get; set; }
    public decimal TongThanhToan { get; private set; }
    public int? VoucherId { get; set; }
    public Voucher? Voucher { get; set; }
    public PhuongThucThanhToan PhuongThucThanhToan { get; set; }
    public DateTimeOffset? ThoiDiemThanhToan { get; set; }
    public ICollection<ChiTietHoaDon> ChiTiet { get; set; } = new List<ChiTietHoaDon>();
}

public class ChiTietHoaDon
{
    public int Id { get; set; }
    public int HoaDonId { get; set; }
    public HoaDon HoaDon { get; set; } = null!;
    public int MonAnId { get; set; }
    public MonAn MonAn { get; set; } = null!;
    public int? MonAnSizeId { get; set; }
    public MonAnSize? MonAnSize { get; set; }
    [MaxLength(150)] public string TenMonLucBan { get; set; } = "";
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
    [MaxLength(500)] public string? YeuCauCheBien { get; set; }
    [MaxLength(4000)] public string? ChiTietComboSnapshot { get; set; }
    public TrangThaiCheBien TrangThai { get; set; }
    public int? MonDatTruocId { get; set; }
    public MonDatTruoc? MonDatTruoc { get; set; }
}

public class NguyenLieu
{
    public int Id { get; set; }
    [MaxLength(120)] public string TenNguyenLieu { get; set; } = "";
    // Đơn vị tính lưu trực tiếp (g/kg/ml/lít/cái...); không làm quy đổi phức tạp.
    [MaxLength(30)] public string DonViTinh { get; set; } = "";
    public decimal NguongCanhBao { get; set; }
    public bool DangSuDung { get; set; } = true;
}

public class DinhMucMon
{
    public int MonAnId { get; set; }
    public MonAn MonAn { get; set; } = null!;
    public int NguyenLieuId { get; set; }
    public NguyenLieu NguyenLieu { get; set; } = null!;
    public decimal SoLuong { get; set; }
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
    public decimal SoLuong { get; set; }
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
    public int? HoaDonId { get; set; }
    public HoaDon? HoaDon { get; set; }
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
    public decimal SoLuong { get; set; }
    // Liên kết lô nhập (cùng nguyên liệu); NULL khi xuất không theo lô cụ thể.
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

public class DanhGia
{
    public int Id { get; set; }
    public int HoaDonId { get; set; }
    public HoaDon HoaDon { get; set; } = null!;
    public int? ChiTietHoaDonId { get; set; }
    public ChiTietHoaDon? ChiTietHoaDon { get; set; }
    public int? KhachHangId { get; set; }
    public KhachHang? KhachHang { get; set; }
    public int Diem { get; set; }
    public int DiemDichVu { get; set; }
    [MaxLength(2000)] public string? NoiDung { get; set; }
    [MaxLength(500)] public string? HinhAnh { get; set; }
    [MaxLength(2000)] public string? PhanHoi { get; set; }
    public DateTimeOffset? ThoiDiemPhanHoi { get; set; }
    public DateTimeOffset ThoiDiem { get; set; }
}
