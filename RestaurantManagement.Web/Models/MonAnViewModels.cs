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
    public bool LaMonMoi { get; set; }
    public bool LaMonNoiBat { get; set; }
    public List<MonAnSizeFormViewModel> Sizes { get; set; } = new();
    public List<DinhMucItemViewModel> DinhMucItems { get; set; } = new();
    public List<ComboItemViewModel> ComboItems { get; set; } = new();
    [ValidateNever] public List<DanhMuc> DanhMucList { get; set; } = new();
    [ValidateNever] public List<NguyenLieu> NguyenLieuList { get; set; } = new();
    [ValidateNever] public List<MonAn> MonLeList { get; set; } = new();
}

public class MonAnSizeFormViewModel
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập tên size."), StringLength(50)]
    public string TenSize { get; set; } = "Mặc định";
    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Giá bán phải không âm.")]
    public decimal GiaBan { get; set; }
    public bool DangSuDung { get; set; } = true;
}

public class DinhMucItemViewModel
{
    [Range(1, int.MaxValue)] public int NguyenLieuId { get; set; }
    [Range(typeof(decimal), "0.000001", "999999999999.999999", ErrorMessage = "Định mức phải lớn hơn 0.")]
    public decimal SoLuong { get; set; }
}

public class ComboItemViewModel
{
    [Range(1, int.MaxValue)] public int MonAnId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
    public int SoLuong { get; set; } = 1;
}
