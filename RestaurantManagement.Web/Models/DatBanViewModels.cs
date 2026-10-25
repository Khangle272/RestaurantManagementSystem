using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RestaurantManagement.Web.Models;

public class DatBanCreateVM : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(120, ErrorMessage = "Họ và tên không quá 120 ký tự.")]
    [Display(Name = "Họ và tên")]
    public string HoTen { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ (gồm 10 số, bắt đầu bằng 0).")]
    [Display(Name = "Số điện thoại")]
    public string SoDienThoai { get; set; } = "";

    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(256)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thời gian đến.")]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired]
    [Display(Name = "Thời gian đến")]
    public DateTime ThoiGianDen { get; set; } = Services.ReservationSchedule.NextArrival(Services.TableService.VietnamNow);
    public DateOnly? NgayDat { get; set; }

    [Range(1, 50, ErrorMessage = "Số lượng khách từ 1 đến 50 người.")]
    [Display(Name = "Số lượng khách")]
    public int SoNguoi { get; set; } = 2;

    [StringLength(1000, ErrorMessage = "Ghi chú không quá 1000 ký tự.")]
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    [Display(Name = "Khu vực ưu tiên")]
    public int? MaKhuVuc { get; set; }

    public List<SelectListItem> KhuVucOptions { get; set; } = [];
    public Guid? RequestId { get; set; }
    public bool ChuanBiTruoc { get; set; }
    public bool YeuCauTrangTri { get; set; }
    [Range(0, 49)] public int SoTreEm { get; set; }
    public List<OrderItemInputModel> Items { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (NgayDat.HasValue && NgayDat.Value != DateOnly.FromDateTime(ThoiGianDen))
            yield return new ValidationResult("Ngày đặt và giờ đến chưa khớp. Vui lòng chọn lại ngày/giờ.", [nameof(ThoiGianDen)]);
        if (Services.ReservationSchedule.Validate(ThoiGianDen, Services.TableService.VietnamNow) is { } error)
            yield return new ValidationResult(error, [nameof(ThoiGianDen)]);
    }
}

public class DatBanLookupVM
{
    [Display(Name = "Số điện thoại")]
    public string? SoDienThoai { get; set; }

    [Display(Name = "Mã đặt bàn")]
    public string? BookingCode { get; set; }

    public List<DatBanItemVM> DanhSachDatBan { get; set; } = [];
}

public class DatBanItemVM
{
    public bool CanEditPreorder { get; set; }
    public string RowVersion { get; set; } = "";
    public int MaDatBan { get; set; }
    public string BookingCode { get; set; } = "";
    public string HoTen { get; set; } = "";
    public string SoDienThoai { get; set; } = "";
    public DateTimeOffset ThoiGianDen { get; set; }
    public int SoNguoi { get; set; }
    public string TenBan { get; set; } = "Chưa gán";
    public int? BanAnId { get; set; }
    public string TrangThai { get; set; } = "";
    public string TrangThaiBadgeClass { get; set; } = "badge-status-warning";
    public decimal TongTienHoaDon { get; set; }
    public int? HoaDonId { get; set; }
    public bool DaThanhToan { get; set; }
    public bool DaDanhGia { get; set; }
    public string? GhiChu { get; set; }
    public List<ChiTietMonAnItemVM> ChiTietMonAn { get; set; } = [];
    public DateTimeOffset? GioKetThuc { get; set; }
    public string TinhTrangCoc { get; set; } = "";
}

public class ChiTietMonAnItemVM
{
    public string TenMon { get; set; } = "";
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal ThanhTien => SoLuong * DonGia;
}

public class DatBanAdminVM
{
    public List<DatBanItemVM> DanhSachDatBan { get; set; } = [];
    public string? FilterTrangThai { get; set; }
    public DateOnly? FilterNgay { get; set; }
    public List<SelectListItem> DanhSachBanTrong { get; set; } = [];

    // Thống kê nhanh
    public int ChoXacNhanCount { get; set; }
    public int DaXacNhanCount { get; set; }
    public int DangPhucVuCount { get; set; }
    public int DaHuyCount { get; set; }
    public int HomNayCount { get; set; }
}

