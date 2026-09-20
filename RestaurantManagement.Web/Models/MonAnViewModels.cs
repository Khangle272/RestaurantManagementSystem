namespace RestaurantManagement.Web.Models;

using RestaurantManagement.API.Models;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

// For Index page
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
    public decimal GiaBan { get; set; } // From first MonAnSize
}

// For Create/Edit page
public class MonAnCreateViewModel
{
    [Required(ErrorMessage = "Tên món ăn là bắt buộc")]
    [MaxLength(150)]
    public string TenMon { get; set; } = "";
    
    [Required(ErrorMessage = "Vui lòng chọn danh mục")]
    public int DanhMucId { get; set; }
    
    [MaxLength(1000)]
    public string? MoTa { get; set; }
    
    public IFormFile? HinhAnhFile { get; set; }
    
    public LoaiMon Loai { get; set; } = LoaiMon.MonLe;
    
    public TrangThaiMon TrangThai { get; set; } = TrangThaiMon.DangPhucVu;
    
    [Required(ErrorMessage = "Vui lòng nhập giá bán")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải >= 0")]
    public decimal GiaBan { get; set; }
    
    // Dynamic ingredient list from JS
    public List<DinhMucItemViewModel> DinhMucItems { get; set; } = new();
    
    // For populating dropdowns
    public List<DanhMuc>? DanhMucList { get; set; }
    public List<NguyenLieu>? NguyenLieuList { get; set; }
}

public class DinhMucItemViewModel
{
    public int NguyenLieuId { get; set; }
    public decimal SoLuong { get; set; }
}
