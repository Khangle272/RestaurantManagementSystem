using System.ComponentModel.DataAnnotations;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Models;

public class KhuyenMaiListViewModel
{
    public string? Search { get; set; }
    public string? StatusFilter { get; set; } // TatCa, DangDienRa, SapDienRa, DaKetThuc, NgungSuDung
    public PhamViUuDai? PhamViFilter { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalItems { get; set; }
    public List<KhuyenMaiItemViewModel> Items { get; set; } = [];
}

public class KhuyenMaiItemViewModel
{
    public int Id { get; set; }
    public string TenChuongTrinh { get; set; } = "";
    public DateTimeOffset BatDau { get; set; }
    public DateTimeOffset KetThuc { get; set; }
    public PhamViUuDai PhamVi { get; set; }
    public KieuGiam KieuGiam { get; set; }
    public decimal GiaTri { get; set; }
    public decimal? MucGiamToiDa { get; set; }
    public decimal GiaTriToiThieu { get; set; }
    public bool ChoPhepKetHop { get; set; }
    public int ThuTuApDung { get; set; }
    public bool CanVoucher { get; set; }
    public bool DangSuDung { get; set; }
    public int SoMonApDung { get; set; }
    public int SoVoucherDaPhatHanh { get; set; }
    public int SoLuotDaSuDung { get; set; }

    public string TrangThaiHienThi
    {
        get
        {
            if (!DangSuDung) return "Đã tắt";
            var now = DateTimeOffset.UtcNow;
            if (now < BatDau) return "Sắp diễn ra";
            if (now > KetThuc) return "Đã kết thúc";
            return "Đang diễn ra";
        }
    }
}

public class KhuyenMaiFormViewModel
{
    [Range(1, 1000000)] public int? GioiHanPhatHanh { get; set; }
    public int SoVeDaPhatHanh { get; set; }
    public List<CustomerOption> AvailableCustomers { get; set; } = [];
    public List<CampaignVoucherForm> Vouchers { get; set; } = [];
    public int Id { get; set; }
    public string? RowVersion { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên chương trình khuyến mãi.")]
    [MaxLength(150, ErrorMessage = "Tên chương trình không được vượt quá 150 ký tự.")]
    [Display(Name = "Tên chương trình ưu đãi")]
    public string TenChuongTrinh { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn thời gian bắt đầu.")]
    [Display(Name = "Bắt đầu")]
    public DateTime BatDau { get; set; } = Services.PromotionTime.Today;

    [Required(ErrorMessage = "Vui lòng chọn thời gian kết thúc.")]
    [Display(Name = "Kết thúc")]
    public DateTime KetThuc { get; set; } = Services.PromotionTime.Today.AddMonths(1);

    [Display(Name = "Phạm vi áp dụng")]
    public PhamViUuDai PhamVi { get; set; } = PhamViUuDai.HoaDon;

    [Display(Name = "Kiểu giảm giá")]
    public KieuGiam KieuGiam { get; set; } = KieuGiam.PhanTram;

    [Range(0.01, 100000000, ErrorMessage = "Giá trị giảm phải lớn hơn 0.")]
    [Display(Name = "Giá trị giảm")]
    public decimal GiaTri { get; set; } = 10;

    [Display(Name = "Mức giảm tối đa (VNĐ)")]
    [Range(typeof(decimal), "0", "100000000", ErrorMessage = "Mức giảm tối đa phải không âm.")]
    public decimal? MucGiamToiDa { get; set; }

    [Display(Name = "Giá trị đơn tối thiểu (VNĐ)")]
    [Range(typeof(decimal), "0", "100000000", ErrorMessage = "Giá trị đơn tối thiểu phải không âm.")]
    public decimal GiaTriToiThieu { get; set; } = 0;

    [Display(Name = "Cho phép kết hợp với ưu đãi khác")]
    public bool ChoPhepKetHop { get; set; } = true;

    [Display(Name = "Thứ tự ưu tiên áp dụng")]
    public int ThuTuApDung { get; set; } = 1;

    [Display(Name = "Yêu cầu mã Voucher")]
    public bool CanVoucher { get; set; } = true;

    [Display(Name = "Đang kích hoạt")]
    public bool DangSuDung { get; set; } = true;

    // Danh sách món ăn áp dụng khi Phạm vi là Món ăn
    public List<int> SelectedMonAnIds { get; set; } = [];

    // Helper danh sách món để hiển thị checkbox trên View
    public List<MonAnSelectionItem> AvailableDishes { get; set; } = [];
}

public class MonAnSelectionItem
{
    public int Id { get; set; }
    public string TenMon { get; set; } = "";
    public string TenDanhMuc { get; set; } = "";
    public decimal GiaThamKhao { get; set; }
    public bool IsSelected { get; set; }
}

public class CampaignVoucherForm
{
    [Range(0, int.MaxValue, ErrorMessage = "Điểm đổi voucher phải là số nguyên không âm.")]
    public int DiemCanDoi { get; set; }
    public int SoVeDaPhatHanh { get; set; }
    public int SoLuotDaDung { get; set; }
    public int Id { get; set; }
    public string? RowVersion { get; set; }
    public bool Xoa { get; set; }
    [Required, MaxLength(40), RegularExpression(@"^[A-Za-z0-9_-]+$")]
    public string Ma { get; set; } = "";
    public DateTime BatDau { get; set; } = Services.PromotionTime.Today;
    public DateTime KetThuc { get; set; } = Services.PromotionTime.Today.AddMonths(1);
    public PhamViUuDai PhamVi { get; set; } = PhamViUuDai.HoaDon;
    public KieuGiam KieuGiam { get; set; } = KieuGiam.PhanTram;
    [Range(typeof(decimal), "0.01", "100000000")]
    public decimal GiaTri { get; set; } = 10;
    [Range(typeof(decimal), "0.01", "100000000")]
    public decimal? MucGiamToiDa { get; set; }
    [Range(typeof(decimal), "0", "100000000")]
    public decimal GiaTriToiThieu { get; set; }
    [Range(1, 1000000)] public int? GioiHanPhatHanh { get; set; }
    [Range(1, 1000000)] public int? GioiHanTongLuot { get; set; }
    public int? KhachHangId { get; set; }
    public bool DangSuDung { get; set; } = true;
    public List<int> SelectedMonAnIds { get; set; } = [];
}

public record CustomerOption(int Id, string HoTen, string SoDienThoai);
