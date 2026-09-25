using System.ComponentModel.DataAnnotations;

namespace RestaurantManagement.Web.Models;

public class DanhGiaCreateVM
{
    public int MaDatBan { get; set; }

    public string? TenKhachHang { get; set; }
    public DateTimeOffset? ThoiGianDen { get; set; }
    public string? TenBan { get; set; }

    [Display(Name = "Chất lượng món ăn"), Required(ErrorMessage = "Vui lòng chọn số sao cho món ăn.")]
    [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5.")]
    public int SoSaoMonAn { get; set; }

    [Display(Name = "Chất lượng phục vụ"), Required(ErrorMessage = "Vui lòng chọn số sao cho dịch vụ.")]
    [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5.")]
    public int SoSaoDichVu { get; set; }

    [Display(Name = "Nhận xét"), Required(ErrorMessage = "Vui lòng nhập nhận xét."), StringLength(2000)]
    public string NoiDung { get; set; } = "";

    [Display(Name = "Ảnh thực tế")]
    public IFormFile? HinhAnhFile { get; set; }
}

public class DanhGiaAdminVM
{
    public List<DanhGiaAdminItemVM> DanhSachDanhGia { get; set; } = new();
    public string? FilterSoSao { get; set; }
    public string? FilterPhanHoi { get; set; }
}

public class DanhGiaAdminItemVM
{
    public int Id { get; set; }
    public string TenKhachHang { get; set; } = "";
    public string? SoDienThoai { get; set; }
    public DateTimeOffset ThoiDiem { get; set; }
    public int DiemMonAn { get; set; }
    public int DiemDichVu { get; set; }
    public string? NoiDung { get; set; }
    public string? HinhAnh { get; set; }
    public string? PhanHoi { get; set; }
    public DateTimeOffset? ThoiDiemPhanHoi { get; set; }
    public string? MaDatBan { get; set; }
}
