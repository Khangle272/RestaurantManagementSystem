using System.ComponentModel.DataAnnotations;
namespace RestaurantManagement.Web.Models;
public class BookingRescheduleViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Customer { get; set; } = "";
    [Required] public string RowVersion { get; set; } = "";
    [Required] public DateTime GioDen { get; set; }
    [Required, StringLength(200)] public string LyDo { get; set; } = "";
}
