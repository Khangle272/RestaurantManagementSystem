using System.ComponentModel.DataAnnotations;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Models;

public class TableBoardViewModel
{
    public List<KhuVuc> Areas { get; set; } = [];
    public List<TableCard> Tables { get; set; } = [];
    public DatBan? Booking { get; set; }
    public string BookingVersion { get; set; } = "";
    public int? AreaId { get; set; }
    public int? Floor { get; set; }
    public int? PreferredTableId { get; set; }
    public DateTimeOffset From { get; set; }
    public DateTimeOffset Until { get; set; }
    public DateOnly ScheduleDay { get; set; }
    public TimeOnly ScheduleTime { get; set; }
    public List<DatBan> Requests { get; set; } = [];
    public bool CanAssign => Booking is not null && Booking.GioKetThucDuKien > DateTimeOffset.UtcNow
        && Booking.TrangThai is TrangThaiDatBan.ChoXacNhan or TrangThaiDatBan.ChoCoc or TrangThaiDatBan.DaXacNhan;
}
public class TableCard
{
    public BanAn Table { get; set; } = null!;
    public string Version { get; set; } = "";
    public DatBan? Occupant { get; set; }
    public string OccupantVersion { get; set; } = "";
    public List<DatBan> Conflicts { get; set; } = [];
    public List<DatBan> ScheduledBookings { get; set; } = [];
    public bool CanChoose { get; set; }
    public bool IsSuggested { get; set; }
    public bool IsSelected { get; set; }
}
public class ReceptionBookingViewModel
{
    [Required, StringLength(120)] public string HoTen { get; set; } = "";
    [Required, RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 chữ số, bắt đầu bằng 0.")]
    public string SoDienThoai { get; set; } = "";
    public DateTime GioDen { get; set; } = Services.TableService.VietnamNow.AddMinutes(5);
    [Range(1, 50)] public int SoNguoi { get; set; } = 2;
    [Range(30, Services.TableService.MaxDiningHours * 60, ErrorMessage = "Thời lượng từ 30 đến 180 phút (tối đa 3 tiếng).")]
    public int SoPhut { get; set; } = Services.TableService.MaxDiningHours * 60;
    public int? KhuVucId { get; set; }
    [StringLength(1000)] public string? GhiChu { get; set; }
    public List<KhuVuc> Areas { get; set; } = [];
    public int? BanAnId { get; set; }
    public string? RequestedTableName { get; set; }
}
public class AreaFormViewModel
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string TenKhuVuc { get; set; } = "";
    [Range(0, 99)] public int Tang { get; set; } = 1;
    public bool LaPhongVip { get; set; }
    public bool DangSuDung { get; set; } = true;
    public string? RowVersion { get; set; }
}
