namespace RestaurantManagement.Web.Models;
public class ReservationPlanningViewModel
{
    public DateOnly From { get; set; }
    public DateOnly Until { get; set; }
    public string? Shift { get; set; }
    public bool? Vip { get; set; }
    public int Waiting { get; set; }
    public int WithoutFood { get; set; }
    public List<ReservationPlanningRow> Rows { get; set; } = [];
    public List<ArrivalTimeRow> ArrivalTimes { get; set; } = [];
    public List<IngredientPlanningRow> Ingredients { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
// Số lượt đặt theo đúng mốc giờ hẹn (DatBan.GioDen theo giờ Việt Nam); không gồm lịch đã hủy/không đến.
public record ArrivalTimeRow(DateOnly Day, TimeOnly Time, int Confirmed, int Waiting, int Tables, int Guests)
{
    public int Total => Confirmed + Waiting;
}
public record ReservationPlanningRow(DateOnly Day, string Shift, bool Vip, int Reservations, int Tables, int Guests);
public record IngredientPlanningRow(string Name, string Unit, decimal Required, decimal Stock)
{
    public decimal Shortage => Math.Max(0, Required - Stock);
}
