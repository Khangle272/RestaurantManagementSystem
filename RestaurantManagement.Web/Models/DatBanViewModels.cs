using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Models;

public class DatBanCreateVM
{
    [Display(Name = "Họ và tên"), Required(ErrorMessage = "Vui lòng nhập họ tên."), StringLength(120)]
    public string HoTen { get; set; } = "";

    [Display(Name = "Số điện thoại"), Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải gồm 10 chữ số, bắt đầu bằng 0.")]
    public string SoDienThoai { get; set; } = "";

    [Display(Name = "Email"), EmailAddress(ErrorMessage = "Email không hợp lệ."), StringLength(256)]
    public string? Email { get; set; }

    [Display(Name = "Thời gian đến"), Required(ErrorMessage = "Vui lòng chọn thời gian đến.")]
    public DateTimeOffset? ThoiGianDen { get; set; }

    [Display(Name = "Số người"), Range(1, 50, ErrorMessage = "Số người phải từ 1 đến 50.")]
    public int SoNguoi { get; set; } = 2;

    [Display(Name = "Ghi chú"), StringLength(1000)]
    public string? GhiChu { get; set; }

    [Display(Name = "Khu vực mong muốn")]
    public int? MaKhuVuc { get; set; }

    [ValidateNever]
    public List<SelectListItem> KhuVucOptions { get; set; } = new();
}

public class DatBanLookupVM
{
    [Display(Name = "Số điện thoại")]
    public string? SoDienThoai { get; set; }

    [Display(Name = "Mã đặt bàn")]
    public string? BookingCode { get; set; }

    public List<DatBanItemVM> DanhSachDatBan { get; set; } = new();
}

public class DatBanItemVM
{
    public int MaDatBan { get; set; }
    public string BookingCode { get; set; } = "";
    public DateTimeOffset ThoiGianDen { get; set; }
    public int SoNguoi { get; set; }
    public string? TenBan { get; set; }
    public TrangThaiDatBan TrangThai { get; set; }
    public decimal? TongTienHoaDon { get; set; }
    public List<ChiTietMonVM> ChiTietMonAn { get; set; } = new();
    public bool DaDanhGia { get; set; }
    public string? TrangThaiHoaDon { get; set; }
}

public class ChiTietMonVM
{
    public string TenMon { get; set; } = "";
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal ThanhTien => SoLuong * DonGia;
}

public class DatBanAdminVM
{
    public List<DatBanAdminItemVM> DanhSachDatBan { get; set; } = new();
    public string? FilterTrangThai { get; set; }
    public DateOnly? FilterNgay { get; set; }
    public List<BanAnOption> DanhSachBanTrong { get; set; } = new();
    public int ChoXacNhanCount { get; set; }
    public int DaXacNhanCount { get; set; }
    public int DaNhanBanCount { get; set; }
    public int HoanTatHuyCount { get; set; }
}

public class DatBanAdminItemVM
{
    public int Id { get; set; }
    public string MaDatBan { get; set; } = "";
    public string HoTen { get; set; } = "";
    public string? SoDienThoai { get; set; }
    public DateTimeOffset GioDen { get; set; }
    public int SoNguoi { get; set; }
    public string? TenKhuVuc { get; set; }
    public string? TenBan { get; set; }
    public int? MaBanId { get; set; }
    public TrangThaiDatBan TrangThai { get; set; }
    public string? YeuCau { get; set; }
}

public class BanAnOption
{
    public int Id { get; set; }
    public string MaBan { get; set; } = "";
    public string TenKhuVuc { get; set; } = "";
    public int SoChoNgoi { get; set; }
}

public static class DatBanLabels
{
    public static string TrangThai(TrangThaiDatBan value) => value switch
    {
        TrangThaiDatBan.ChoXacNhan => "Chờ xác nhận",
        TrangThaiDatBan.ChoCoc => "Chờ cọc",
        TrangThaiDatBan.DaXacNhan => "Đã xác nhận",
        TrangThaiDatBan.DaNhanBan => "Đang phục vụ",
        TrangThaiDatBan.HoanTat => "Hoàn tất",
        TrangThaiDatBan.DaHuy => "Đã hủy",
        TrangThaiDatBan.KhongDen => "Không đến",
        _ => "Không xác định"
    };

    public static string BadgeClass(TrangThaiDatBan value) => value switch
    {
        TrangThaiDatBan.ChoXacNhan or TrangThaiDatBan.ChoCoc => "badge-status-warning",
        TrangThaiDatBan.DaXacNhan => "badge bg-info text-white",
        TrangThaiDatBan.DaNhanBan => "badge-status-success",
        TrangThaiDatBan.HoanTat => "badge bg-secondary text-white",
        TrangThaiDatBan.DaHuy or TrangThaiDatBan.KhongDen => "badge-status-danger",
        _ => "badge bg-secondary text-white"
    };
}
