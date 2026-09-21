using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Models;

public class EditRecordViewModel
{
    public int Id { get; set; }
    public string? RowVersion { get; set; }
}

public class NhanVienFormViewModel : EditRecordViewModel
{
    [Display(Name = "Mã nhân viên"), Required(ErrorMessage = "Vui lòng nhập mã nhân viên."), StringLength(20)]
    public string MaNhanVien { get; set; } = "";

    [Display(Name = "Họ tên"), Required(ErrorMessage = "Vui lòng nhập họ tên."), StringLength(120)]
    public string HoTen { get; set; } = "";

    [Display(Name = "Số điện thoại"), Required(ErrorMessage = "Vui lòng nhập số điện thoại."), StringLength(20)]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string SoDienThoai { get; set; } = "";

    [EmailAddress(ErrorMessage = "Email không hợp lệ."), StringLength(256)]
    public string? Email { get; set; }

    [Display(Name = "Địa chỉ"), StringLength(300)]
    public string? DiaChi { get; set; }

    [Display(Name = "Chức vụ"), Required(ErrorMessage = "Vui lòng nhập chức vụ."), StringLength(80)]
    public string ChucVu { get; set; } = "";

    [Display(Name = "Ngày vào làm"), Required(ErrorMessage = "Vui lòng chọn ngày vào làm."), DataType(DataType.Date)]
    public DateOnly? NgayVaoLam { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Đang làm việc")]
    public bool DangLamViec { get; set; } = true;
}

public class BanAnFormViewModel : EditRecordViewModel
{
    [Display(Name = "Mã bàn"), Required(ErrorMessage = "Vui lòng nhập mã bàn."), StringLength(20)]
    public string MaBan { get; set; } = "";

    [Display(Name = "Khu vực"), Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn khu vực.")]
    public int KhuVucId { get; set; }

    [Display(Name = "Số chỗ ngồi"), Range(1, int.MaxValue, ErrorMessage = "Số chỗ ngồi phải lớn hơn 0.")]
    public int SoChoNgoi { get; set; } = 4;

    [Display(Name = "Trạng thái"), EnumDataType(typeof(TrangThaiBan), ErrorMessage = "Trạng thái bàn không hợp lệ.")]
    public TrangThaiBan TrangThai { get; set; } = TrangThaiBan.SanSang;

    [ValidateNever]
    public List<SelectListItem> KhuVucOptions { get; set; } = new();
}

public class DanhMucFormViewModel : EditRecordViewModel
{
    [Display(Name = "Tên danh mục"), Required(ErrorMessage = "Vui lòng nhập tên danh mục."), StringLength(100)]
    public string TenDanhMuc { get; set; } = "";

    [Display(Name = "Mô tả"), StringLength(500)]
    public string? MoTa { get; set; }

    [Display(Name = "Đang sử dụng")]
    public bool DangSuDung { get; set; } = true;
}

public class PagedListViewModel<T>
{
    public List<T> Items { get; set; } = new();
    public PaginationViewModel Pagination { get; set; } = new();
}

public class PaginationViewModel
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public string? Search { get; set; }
    public string? TrangThai { get; set; }
    public int? KhuVucId { get; set; }
}

public class DeleteRecordViewModel : EditRecordViewModel
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}

public static class BanAnLabels
{
    public static string TrangThai(TrangThaiBan value) => value switch
    {
        TrangThaiBan.SanSang => "Sẵn sàng",
        TrangThaiBan.DangPhucVu => "Đang phục vụ",
        TrangThaiBan.CanDon => "Cần dọn",
        TrangThaiBan.NgungSuDung => "Ngừng sử dụng",
        _ => "Không xác định"
    };
}
