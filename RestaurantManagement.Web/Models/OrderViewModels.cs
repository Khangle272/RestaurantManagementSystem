using System.ComponentModel.DataAnnotations;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Models;

public class OrderItemInputModel
{
    public int MonAnId { get; set; }
    public int MonAnSizeId { get; set; }
    public int SoLuong { get; set; }
    [MaxLength(500)]
    public string? YeuCauCheBien { get; set; }
}

public class OrderCreateViewModel
{
    public LoaiDonHang LoaiDonHang { get; set; } = LoaiDonHang.TaiBan;
    public int? BanAnId { get; set; }
    public int? DatBanId { get; set; }

    [MaxLength(120)]
    [Display(Name = "Tên khách hàng / Người nhận")]
    public string? TenNguoiNhan { get; set; }

    [MaxLength(20)]
    [Phone]
    [Display(Name = "Số điện thoại")]
    public string? SoDienThoaiNhan { get; set; }

    [MaxLength(300)]
    [Display(Name = "Địa chỉ giao hàng")]
    public string? DiaChiGiaoHang { get; set; }

    [MaxLength(500)]
    [Display(Name = "Ghi chú đơn hàng")]
    public string? GhiChuDonHang { get; set; }

    public List<OrderItemInputModel> Items { get; set; } = [];

    // Helper data for rendering the UI
    public List<BanAnCardItem> OccupiedTables { get; set; } = [];
    public List<DanhMuc> Categories { get; set; } = [];
    public List<MenuDishItem> MenuDishes { get; set; } = [];
}

public class BanAnCardItem
{
    public int Id { get; set; }
    public string MaBan { get; set; } = "";
    public string TenKhuVuc { get; set; } = "";
    public int Tang { get; set; }
    public int SoChoNgoi { get; set; }
    public int? DatBanId { get; set; }
    public string? KhachHang { get; set; }
    public int SoKhach { get; set; }
    public DateTimeOffset? GioNhan { get; set; }
}

public class MenuDishItem
{
    public int Id { get; set; }
    public int DanhMucId { get; set; }
    public string TenMon { get; set; } = "";
    public string? HinhAnh { get; set; }
    public string? MoTa { get; set; }
    public LoaiMon Loai { get; set; }
    public List<MenuSizeItem> Sizes { get; set; } = [];
}

public class MenuSizeItem
{
    public int Id { get; set; }
    public string TenSize { get; set; } = "";
    public decimal GiaBan { get; set; }
}

public class OrderListViewModel
{
    public LoaiDonHang? LoaiDonHang { get; set; }
    public TrangThaiHoaDon? TrangThai { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalItems { get; set; }
    public List<OrderCardItem> Orders { get; set; } = [];
}

public class OrderCardItem
{
    public int Id { get; set; }
    public string MaHoaDon { get; set; } = "";
    public LoaiDonHang LoaiDonHang { get; set; }
    public string TenLoaiDon => LoaiDonHang switch
    {
        LoaiDonHang.TaiBan => "Tại bàn",
        LoaiDonHang.MangDi => "Mang đi",
        LoaiDonHang.GiaoHang => "Giao hàng",
        _ => "Không xác định"
    };

    public string Ban { get; set; } = "—";
    public string? TenKhachHang { get; set; }
    public string? SoDienThoai { get; set; }
    public DateTimeOffset ThoiDiemLap { get; set; }
    public int TongSoMon { get; set; }
    public decimal TongTien { get; set; }
    public TrangThaiHoaDon TrangThai { get; set; }
    public PhuongThucThanhToan PhuongThucThanhToan { get; set; }
    public string NhanVienLap { get; set; } = "";
    public int MonChoCheBien { get; set; }
    public int MonDangCheBien { get; set; }
    public int MonSanSang { get; set; }
    public int MonDaPhucVu { get; set; }
}

public class PaymentFormModel
{
    public int Id { get; set; }
    public string? RowVersion { get; set; }
    public PhuongThucThanhToan PhuongThuc { get; set; } = PhuongThucThanhToan.TienMat;
    public decimal TienKhachDua { get; set; }
    public decimal TienGiam { get; set; }
    public string? MaGiaoDich { get; set; }
    public string? LoaiThe { get; set; }
    public string? SoThe4SoCuoi { get; set; }
}
