using System.ComponentModel.DataAnnotations;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Models;

public class SavePreorderModel
{
    [Required] public string RowVersion { get; set; } = "";
    [Required] public Guid? RequestId { get; set; }
    public bool ChuanBiTruoc { get; set; }
    public List<OrderItemInputModel> Items { get; set; } = [];
}

public class BookingDetailViewModel
{
    public DatBan Booking { get; set; } = null!;
    public string Version { get; set; } = "";
    public List<PreorderLineViewModel> Items { get; set; } = [];
    public List<GiaoDichCoc> Transactions { get; set; } = [];
    public decimal AvailableDeposit { get; set; }
    public bool CanEdit { get; set; }
    public bool Internal { get; set; }
    public List<NhanVien> Staff { get; set; } = [];
    public int? ActorStaffId { get; set; }
}

public class PreorderLineViewModel
{
    public int Id { get; set; }
    public int MonAnId { get; set; }
    public int? SizeId { get; set; }
    public string Name { get; set; } = "";
    public string SizeName { get; set; } = "";
    public string? Image { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
    public bool Sent { get; set; }
    public string Status { get; set; } = "Đặt trước";
}

public class DepositQuoteModel
{
    [Required] public string RowVersion { get; set; } = "";
    [Range(typeof(decimal), "0", "1000000000")] public decimal SoTien { get; set; }
    [Required, StringLength(1000)] public string DieuKien { get; set; } = "";
}

public class DepositTransactionModel
{
    public bool DaDoiChieuNganHang { get; set; }
    [Required] public string RowVersion { get; set; } = "";
    public Guid RequestId { get; set; }
    public LoaiGiaoDichCoc Loai { get; set; }
    [Range(typeof(decimal), "1", "1000000000")] public decimal SoTien { get; set; }
    [StringLength(100)] public string? MaThamChieu { get; set; }
    [Required, StringLength(500)] public string LyDo { get; set; } = "";
}

public class PaymentNoticeModel
{
    [Required] public string RowVersion { get; set; } = "";
    [Range(typeof(decimal), "1", "1000000000")] public decimal SoTien { get; set; }
    [StringLength(100)] public string? MaGiaoDich { get; set; }
}

public class ReservationPaymentViewModel
{
    public string QrVersion { get; set; } = "";
    public DatBan Booking { get; set; } = null!;
    public string Version { get; set; } = "";
    public decimal Remaining { get; set; }
    public decimal FoodTotal { get; set; }
    public bool IsDemo { get; set; }
}
