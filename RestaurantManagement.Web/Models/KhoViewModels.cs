using System.ComponentModel.DataAnnotations;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Models;

public class NhaCungCapVM
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên nhà cung cấp.")]
    [StringLength(150, ErrorMessage = "Tên nhà cung cấp tối đa 150 ký tự.")]
    [Display(Name = "Tên nhà cung cấp")]
    public string TenNhaCungCap { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
    [StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
    [Display(Name = "Số điện thoại")]
    public string SoDienThoai { get; set; } = "";

    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [StringLength(300, ErrorMessage = "Địa chỉ tối đa 300 ký tự.")]
    [Display(Name = "Địa chỉ kho/văn phòng")]
    public string? DiaChi { get; set; }

    [StringLength(30, ErrorMessage = "Mã số thuế tối đa 30 ký tự.")]
    [Display(Name = "Mã số thuế")]
    public string? MaSoThue { get; set; }

    [StringLength(100, ErrorMessage = "Người liên hệ tối đa 100 ký tự.")]
    [Display(Name = "Người đại diện/liên hệ")]
    public string? NguoiLienHe { get; set; }

    [StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    [Display(Name = "Đang hợp tác")]
    public bool DangSuDung { get; set; } = true;
}

public class NhaCungCapIndexVM
{
    public List<NhaCungCap> Items { get; set; } = new();
    public PaginationViewModel Pagination { get; set; } = new();
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public string? Search { get; set; }
    public bool? Active { get; set; }
}

public class ChiTietNhapItemVM
{
    [Required(ErrorMessage = "Vui lòng chọn nguyên liệu.")]
    public int MaNguyenLieu { get; set; }
    public string? TenNguyenLieu { get; set; }
    public string? DonViTinh { get; set; }

    [Range(typeof(decimal), "0.000001", "999999999", ErrorMessage = "Số lượng nhập phải lớn hơn 0.")]
    public decimal SoLuong { get; set; }

    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Đơn giá nhập không được âm.")]
    public decimal DonGiaNhap { get; set; }

    public decimal ThanhTien => SoLuong * DonGiaNhap;
    public DateOnly? HanSuDung { get; set; }
    public string? SoLo { get; set; }
}

public class PhieuNhapCreateVM
{
    [Required(ErrorMessage = "Vui lòng chọn nhà cung cấp.")]
    [Display(Name = "Nhà cung cấp")]
    public int MaNhaCungCap { get; set; }

    [Display(Name = "Ngày nhập kho")]
    public DateTime NgayNhap { get; set; } = DateTime.Now;

    [Display(Name = "Số hóa đơn đỏ / chứng từ")]
    public string? SoHoaDon { get; set; }

    [Display(Name = "Ghi chú nhập hàng")]
    public string? GhiChu { get; set; }

    public List<ChiTietNhapItemVM> ChiTiets { get; set; } = new();
}

public class PhieuNhapIndexVM
{
    public List<PhieuNhap> Items { get; set; } = new();
    public PaginationViewModel Pagination { get; set; } = new();
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? NhaCungCapId { get; set; }
    public List<NhaCungCap> NhaCungCapList { get; set; } = new();
    public decimal TongGiaTriNhap { get; set; }
    public int TongSoPhieu { get; set; }
}

public class PhieuNhapDetailsVM
{
    public PhieuNhap PhieuNhap { get; set; } = null!;
}

public class ChiTietXuatItemVM
{
    [Required(ErrorMessage = "Vui lòng chọn nguyên liệu.")]
    public int MaNguyenLieu { get; set; }
    public string? TenNguyenLieu { get; set; }
    public string? DonViTinh { get; set; }
    public decimal TonHienTai { get; set; }

    [Range(typeof(decimal), "0.000001", "999999999", ErrorMessage = "Số lượng xuất phải lớn hơn 0.")]
    public decimal SoLuong { get; set; }
}

public class PhieuXuatCreateVM
{
    [Required(ErrorMessage = "Vui lòng chọn bộ phận nhận.")]
    [Display(Name = "Bộ phận nhận")]
    public string BoPhanNhan { get; set; } = "Bếp nóng";

    [Display(Name = "Người nhận")]
    public string? NguoiNhan { get; set; }

    [Display(Name = "Ngày xuất")]
    public DateTime NgayXuat { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "Vui lòng chọn hoặc nhập lý do xuất.")]
    [Display(Name = "Lý do xuất kho")]
    public string LyDoXuat { get; set; } = "Phục vụ chế biến";

    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    public List<ChiTietXuatItemVM> ChiTiets { get; set; } = new();
}

public class PhieuXuatIndexVM
{
    public List<PhieuXuat> Items { get; set; } = new();
    public PaginationViewModel Pagination { get; set; } = new();
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? BoPhan { get; set; }
    public decimal TongGiaTriXuat { get; set; }
    public int TongSoPhieu { get; set; }
}

public class PhieuXuatDetailsVM
{
    public PhieuXuat PhieuXuat { get; set; } = null!;
}

public class ChiTietThanhLyItemVM
{
    [Required(ErrorMessage = "Vui lòng chọn nguyên liệu.")]
    public int MaNguyenLieu { get; set; }
    public string? TenNguyenLieu { get; set; }
    public string? DonViTinh { get; set; }
    public decimal TonHienTai { get; set; }

    [Range(typeof(decimal), "0.000001", "999999999", ErrorMessage = "Số lượng thanh lý phải lớn hơn 0.")]
    public decimal SoLuong { get; set; }

    public decimal DonGiaVon { get; set; }
    public decimal ThanhTien => SoLuong * DonGiaVon;
    public string? GhiChu { get; set; }
}

public class PhieuThanhLyCreateVM
{
    [Display(Name = "Ngày thanh lý")]
    public DateTime NgayThanhLy { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "Vui lòng chọn lý do thanh lý.")]
    [Display(Name = "Lý do chính")]
    public string LyDoThanhLy { get; set; } = "Hết hạn sử dụng";

    [Display(Name = "Ghi chú biên bản")]
    public string? GhiChu { get; set; }

    public List<ChiTietThanhLyItemVM> ChiTiets { get; set; } = new();
}

public class PhieuThanhLyIndexVM
{
    public List<PhieuThanhLy> Items { get; set; } = new();
    public PaginationViewModel Pagination { get; set; } = new();
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public decimal TongThietHai { get; set; }
    public int TongSoPhieu { get; set; }
}

public class PhieuThanhLyDetailsVM
{
    public PhieuThanhLy PhieuThanhLy { get; set; } = null!;
}

public class TonKhoItemVM
{
    public int MaNguyenLieu { get; set; }
    public string TenNguyenLieu { get; set; } = "";
    public string DonViTinh { get; set; } = "";
    public string? DanhMuc { get; set; }
    public decimal SoLuongTon { get; set; }
    public decimal DinhMucToiThieu { get; set; }
    public decimal DonGia { get; set; }
    public decimal GiaTriTonKho => SoLuongTon * DonGia;
    public string TrangThaiTon =>
        SoLuongTon <= 0 ? "Cạn kho" :
        (SoLuongTon <= DinhMucToiThieu ? "Sắp hết" : "An toàn");
}

public class TonKhoFilterVM
{
    public string? Keyword { get; set; }
    public bool OnlyLowStock { get; set; }
    public string? DanhMuc { get; set; }
    public List<string> DanhMucList { get; set; } = new();
    public List<TonKhoItemVM> DanhSachTon { get; set; } = new();
    public PaginationViewModel Pagination { get; set; } = new();
    public int TongSoNguyenLieu { get; set; }
    public int SoNguyenLieuSapHet { get; set; }
    public int SoNguyenLieuCanKho { get; set; }
    public decimal TongGiaTriTonKho { get; set; }
}

public class BaoCaoTonKhoRowVM
{
    public int MaNguyenLieu { get; set; }
    public string TenNguyenLieu { get; set; } = "";
    public string DonViTinh { get; set; } = "";
    public string? DanhMuc { get; set; }
    public decimal DonGia { get; set; }
    public decimal TonDauKySL { get; set; }
    public decimal TonDauKyTien => TonDauKySL * DonGia;
    public decimal NhapTrongKySL { get; set; }
    public decimal NhapTrongKyTien { get; set; }
    public decimal XuatTrongKySL { get; set; }
    public decimal XuatTrongKyTien => XuatTrongKySL * DonGia;
    public decimal ThanhLyTrongKySL { get; set; }
    public decimal ThanhLyTrongKyTien => ThanhLyTrongKySL * DonGia;
    public decimal TonCuoiKySL => TonDauKySL + NhapTrongKySL - XuatTrongKySL - ThanhLyTrongKySL;
    public decimal TonCuoiKyTien => TonCuoiKySL * DonGia;
}

public class BaoCaoTonKhoVM
{
    public DateTime TuNgay { get; set; }
    public DateTime DenNgay { get; set; }
    public string Preset { get; set; } = "ThangNay";
    public List<BaoCaoTonKhoRowVM> ChiTietBaoCao { get; set; } = new();
    public decimal TongGiaTriDauKy => ChiTietBaoCao.Sum(x => x.TonDauKyTien);
    public decimal TongGiaTriNhap => ChiTietBaoCao.Sum(x => x.NhapTrongKyTien);
    public decimal TongGiaTriXuat => ChiTietBaoCao.Sum(x => x.XuatTrongKyTien);
    public decimal TongGiaTriThanhLy => ChiTietBaoCao.Sum(x => x.ThanhLyTrongKyTien);
    public decimal TongGiaTriCuoiKy => ChiTietBaoCao.Sum(x => x.TonCuoiKyTien);
}
