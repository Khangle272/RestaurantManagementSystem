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
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên chương trình khuyến mãi.")]
    [MaxLength(150, ErrorMessage = "Tên chương trình không được vượt quá 150 ký tự.")]
    [Display(Name = "Tên chương trình ưu đãi")]
    public string TenChuongTrinh { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn thời gian bắt đầu.")]
    [Display(Name = "Bắt đầu")]
    public DateTime BatDau { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Vui lòng chọn thời gian kết thúc.")]
    [Display(Name = "Kết thúc")]
    public DateTime KetThuc { get; set; } = DateTime.Today.AddMonths(1);

    [Display(Name = "Phạm vi áp dụng")]
    public PhamViUuDai PhamVi { get; set; } = PhamViUuDai.HoaDon;

    [Display(Name = "Kiểu giảm giá")]
    public KieuGiam KieuGiam { get; set; } = KieuGiam.PhanTram;

    [Range(0.01, 100000000, ErrorMessage = "Giá trị giảm phải lớn hơn 0.")]
    [Display(Name = "Giá trị giảm")]
    public decimal GiaTri { get; set; } = 10;

    [Display(Name = "Mức giảm tối đa (VNĐ)")]
    public decimal? MucGiamToiDa { get; set; }

    [Display(Name = "Giá trị đơn tối thiểu (VNĐ)")]
    public decimal GiaTriToiThieu { get; set; } = 0;

    [Display(Name = "Cho phép kết hợp với ưu đãi khác")]
    public bool ChoPhepKetHop { get; set; } = true;

    [Display(Name = "Thứ tự ưu tiên áp dụng")]
    public int ThuTuApDung { get; set; } = 1;

    [Display(Name = "Yêu cầu mã Voucher")]
    public bool CanVoucher { get; set; } = false;

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

public class VoucherListViewModel
{
    public string? Search { get; set; }
    public int? KhuyenMaiId { get; set; }
    public string? StatusFilter { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalItems { get; set; }
    public List<VoucherItemViewModel> Items { get; set; } = [];
    public List<KhuyenMaiOptionItem> PromotionOptions { get; set; } = [];
}

public class KhuyenMaiOptionItem
{
    public int Id { get; set; }
    public string TenChuongTrinh { get; set; } = "";
}

public class VoucherItemViewModel
{
    public int Id { get; set; }
    public string Ma { get; set; } = "";
    public int KhuyenMaiId { get; set; }
    public string TenKhuyenMai { get; set; } = "";
    public string UuDaiMota { get; set; } = "";
    public int? KhachHangId { get; set; }
    public string? TenKhachHang { get; set; }
    public int GioiHanTongLuot { get; set; }
    public int SoLuotDaDung { get; set; }
    public bool DangSuDung { get; set; }
    public DateTimeOffset BatDau { get; set; }
    public DateTimeOffset KetThuc { get; set; }
    public bool HetHan => DateTimeOffset.UtcNow > KetThuc;
    public bool HetLuot => SoLuotDaDung >= GioiHanTongLuot;
}

public class VoucherFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã Voucher.")]
    [MaxLength(40, ErrorMessage = "Mã Voucher không vượt quá 40 ký tự.")]
    [RegularExpression(@"^[A-Za-z0-9_-]+$", ErrorMessage = "Mã voucher chỉ bao gồm chữ cái, số và dấu gạch nối/dưới.")]
    [Display(Name = "Mã Voucher")]
    public string Ma { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn chương trình khuyến mãi.")]
    [Display(Name = "Chương trình khuyến mãi")]
    public int KhuyenMaiId { get; set; }

    [Display(Name = "Khách hàng áp dụng (tùy chọn)")]
    public int? KhachHangId { get; set; }

    [Range(1, 1000000, ErrorMessage = "Giới hạn tổng lượt phải từ 1 trở lên.")]
    [Display(Name = "Giới hạn tổng lượt sử dụng")]
    public int GioiHanTongLuot { get; set; } = 100;

    [Display(Name = "Đang kích hoạt")]
    public bool DangSuDung { get; set; } = true;

    public List<KhuyenMaiOptionItem> PromotionOptions { get; set; } = [];
}
