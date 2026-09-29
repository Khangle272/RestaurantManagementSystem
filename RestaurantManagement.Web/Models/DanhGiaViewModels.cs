using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RestaurantManagement.Web.Models;

public class DanhGiaCreateVM
{
    [Required]
    public string MaDatBan { get; set; } = "";

    public string? TenKhachHang { get; set; }
    public DateTimeOffset? ThoiGianDen { get; set; }

    [Range(1, 5, ErrorMessage = "Số sao cho món ăn từ 1 đến 5.")]
    [Display(Name = "Chất lượng món ăn")]
    public int SoSaoMonAn { get; set; } = 5;

    [Range(1, 5, ErrorMessage = "Số sao cho dịch vụ từ 1 đến 5.")]
    [Display(Name = "Dịch vụ & Không gian")]
    public int SoSaoDichVu { get; set; } = 5;

    [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá.")]
    [StringLength(1500, ErrorMessage = "Nội dung nhận xét tối đa 1500 ký tự.")]
    [Display(Name = "Nhận xét")]
    public string NoiDung { get; set; } = "";

    [Display(Name = "Hình ảnh trải nghiệm")]
    public IFormFile? HinhAnhFile { get; set; }
}

public class DanhGiaItemVM
{
    public int Id { get; set; }
    public string BookingCode { get; set; } = "";
    public string? TenKhachHang { get; set; }
    public DateTimeOffset ThoiDiem { get; set; }
    public int Diem { get; set; }
    public int SoSaoMonAn { get; set; }
    public int SoSaoDichVu { get; set; }
    public string NoiDung { get; set; } = "";
    public string? HinhAnhUrl { get; set; }
    public string? PhanHoiAdmin { get; set; }
}

public class DanhGiaAdminVM
{
    public List<DanhGiaItemVM> DanhSachDanhGia { get; set; } = [];
    public double DiemTrungBinh => DanhSachDanhGia.Count > 0 ? DanhSachDanhGia.Average(x => x.Diem) : 0;
    public int TongSoDanhGia => DanhSachDanhGia.Count;
}

