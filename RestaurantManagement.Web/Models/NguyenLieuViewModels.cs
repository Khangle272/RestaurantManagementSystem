using System.ComponentModel.DataAnnotations;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Models;

public class NguyenLieuFormViewModel : EditRecordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên nguyên liệu."), StringLength(120)]
    public string TenNguyenLieu { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập đơn vị tính."), StringLength(30)]
    public string DonViTinh { get; set; } = "";
    [Range(typeof(decimal), "0", "999999999999.999999", ErrorMessage = "Ngưỡng cảnh báo phải không âm.")]
    public decimal NguongCanhBao { get; set; }
    public bool DangSuDung { get; set; } = true;
}

public class NguyenLieuRowViewModel
{
    public NguyenLieu Item { get; set; } = new();
    public decimal TonKho { get; set; }
}

public class NguyenLieuIndexViewModel
{
    public List<NguyenLieuRowViewModel> Items { get; set; } = new();
    public PaginationViewModel Pagination { get; set; } = new();
    public string? DonViTinh { get; set; }
    public List<string> DonViTinhList { get; set; } = new();
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
}
