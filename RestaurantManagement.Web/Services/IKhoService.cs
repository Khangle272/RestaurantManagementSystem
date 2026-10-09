using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Services;

public class InventoryDeductionResult
{
    public bool Success { get; set; } = true;
    public int? PhieuXuatId { get; set; }
    public string? MaPhieuXuat { get; set; }
    public List<string> Warnings { get; set; } = [];
    public List<string> LowStockIngredients { get; set; } = [];
    public string? Message { get; set; }
}

public interface IKhoService
{
    Task<InventoryDeductionResult> DeductInventoryForOrderDishesAsync(int hoaDonId, List<OrderItemInputModel> items, int? staffId = null);
    Task<InventoryDeductionResult> DeductInventoryForBookingPreOrdersAsync(int datBanId, int hoaDonId, int? staffId = null);
}
