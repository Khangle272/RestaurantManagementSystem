using System.ComponentModel.DataAnnotations;

namespace RestaurantManagement.Web.Models;

public sealed class PaymentQrViewModel
{
    public string? Version { get; set; } = "";
    [Required(ErrorMessage = "Chọn ảnh QR trước khi lưu.")]
    public IFormFile? AnhQr { get; set; }
}
