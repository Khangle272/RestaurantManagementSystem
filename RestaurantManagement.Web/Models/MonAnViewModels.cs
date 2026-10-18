using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Models;

public class MonAnIndexViewModel
{
    public List<MonAnCardViewModel> Items { get; set; } = new();
    public List<DanhMuc> DanhMucList { get; set; } = new();
    public string? Search { get; set; }
    public int? DanhMucId { get; set; }
    public string? TrangThai { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public int TotalCount { get; set; }
    public int ChoDuyetCount { get; set; }
    public int DangPhucVuCount { get; set; }
    public int TamHetCount { get; set; }
    public int NgungKinhDoanhCount { get; set; }
}

public class MonAnCardViewModel
{
    public int Id { get; set; }
    public string TenMon { get; set; } = "";
    public string? MoTa { get; set; }
    public string? HinhAnh { get; set; }
    public string TenDanhMuc { get; set; } = "";
    public TrangThaiMon TrangThai { get; set; }
    public bool DaDuyet { get; set; }
    public decimal? GiaBan { get; set; }
    public int SoSize { get; set; }
}

public class MonAnFormViewModel : EditRecordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên món."), StringLength(150)]
    public string TenMon { get; set; } = "";
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn danh mục.")]
    public int DanhMucId { get; set; }
    [StringLength(1000)] public string? MoTa { get; set; }
    public IFormFile? HinhAnhFile { get; set; }
    [ValidateNever] public string? HinhAnh { get; set; }
    public bool XoaHinhAnh { get; set; }
    [EnumDataType(typeof(LoaiMon))] public LoaiMon Loai { get; set; }
    [EnumDataType(typeof(TrangThaiMon))] public TrangThaiMon TrangThai { get; set; }
    public bool DaDuyet { get; set; } = true;
    [StringLength(500)] public string? LyDoNgung { get; set; }
    public bool LaMonMoi { get; set; }
    public bool LaMonNoiBat { get; set; }
    public List<MonAnSizeFormViewModel> Sizes { get; set; } = new();
    public List<DinhMucItemViewModel> DinhMucItems { get; set; } = new();
    public List<ComboItemViewModel> ComboItems { get; set; } = new();
    [ValidateNever] public List<DanhMuc> DanhMucList { get; set; } = new();
    [ValidateNever] public List<NguyenLieu> NguyenLieuList { get; set; } = new();
    [ValidateNever] public List<MonAn> MonLeList { get; set; } = new();
    [ValidateNever] public List<string> TenSizeOptions { get; set; } = new();
}

public class MonAnSizeFormViewModel
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập tên size."), StringLength(50)]
    public string TenSize { get; set; } = "Mặc định";
    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Giá bán phải không âm.")]
    public decimal GiaBan { get; set; }
    public bool DangSuDung { get; set; } = true;
    [Range(typeof(decimal), "0.000001", "9999", ErrorMessage = "Hệ số tỷ lệ phải lớn hơn 0.")]
    public decimal HeSoTyLe { get; set; } = 1.0m;
    [StringLength(1000)] public string? GhiChuThanhPhanKhacBiet { get; set; }
    public List<DinhMucItemViewModel> DinhMucItems { get; set; } = new();
}

public class DinhMucItemViewModel
{
    [Range(1, int.MaxValue)] public int NguyenLieuId { get; set; }
    [Range(typeof(decimal), "0.000001", "999999999999.999999", ErrorMessage = "Định mức phải lớn hơn 0.")]
    public decimal SoLuong { get; set; }
}

// View model riêng cho partial chia đôi Định mức & Kích cỡ dùng chung Form món ăn và trang BOM.
public class BomSplitViewModel
{
    public string BaseName { get; set; } = "DinhMucItems";
    public string SizeName { get; set; } = "Sizes";
    public string SizeItemsName { get; set; } = "DinhMucItems";
    public string SizeIdName { get; set; } = "Id";
    public bool Admin { get; set; }
    public bool ChoPhepSuaSize { get; set; } = true;
    public bool HienTrangThaiSize { get; set; }
    public bool LaThucUong { get; set; }
    public bool LaCombo { get; set; }
    public List<BomSplitItemViewModel> BaseItems { get; set; } = new();
    public List<BomSplitSizeViewModel> Sizes { get; set; } = new();
    public List<NguyenLieuOptionVM> NguyenLieuList { get; set; } = new();
    public List<string> TenSizeOptions { get; set; } = new();
}

public class BomSplitItemViewModel
{
    public int NguyenLieuId { get; set; }
    public decimal SoLuong { get; set; }
}

public class BomSplitSizeViewModel
{
    public int Id { get; set; }
    public string TenSize { get; set; } = "";
    public decimal GiaBan { get; set; }
    public bool DangSuDung { get; set; } = true;
    public decimal HeSoTyLe { get; set; } = 1.0m;
    public string? GhiChuThanhPhanKhacBiet { get; set; }
    public List<BomSplitItemViewModel> Items { get; set; } = new();
}

public class ComboItemViewModel
{
    [Range(1, int.MaxValue)] public int MonAnId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
    public int SoLuong { get; set; } = 1;
}
