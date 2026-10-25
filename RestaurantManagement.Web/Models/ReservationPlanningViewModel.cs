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
    public List<IngredientPlanningRow> Ingredients { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
public record ReservationPlanningRow(DateOnly Day, string Shift, bool Vip, int Reservations, int Tables, int Guests);
public record IngredientPlanningRow(string Name, string Unit, decimal Required, decimal Stock)
{
    public decimal Shortage => Math.Max(0, Required - Stock);
}
