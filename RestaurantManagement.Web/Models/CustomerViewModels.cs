using System.ComponentModel.DataAnnotations;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Models;

public class CustomerHistoryViewModel
{
    public KhachHang Customer { get; set; } = null!;
    public bool IsAdmin { get; set; }
    public List<HoaDon> Orders { get; set; } = [];
    public List<LichSuTichDiem> Points { get; set; } = [];
    public int Balance { get; set; }
    public decimal TotalSpent { get; set; }
    public int OrderPage { get; set; }
    public int OrderPages { get; set; }
    public int PointPage { get; set; }
    public int PointPages { get; set; }
    public TrangThaiHoaDon? Status { get; set; }
}

public class CustomerProfileForm
{
    public int Id { get; set; }
    public string? RowVersion { get; set; }
    [Required, MaxLength(120)] public string HoTen { get; set; } = "";
    [Required, RegularExpression(@"^\+?[0-9]{9,15}$")] public string SoDienThoai { get; set; } = "";
    [EmailAddress, MaxLength(256)] public string? Email { get; set; }
    public bool DangSuDung { get; set; } = true;
}

public class CustomerAccountForm
{
    public int Id { get; set; }
    public string? RowVersion { get; set; }
    public string? HoTen { get; set; }
    [Required, EmailAddress, MaxLength(256)] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required, Compare(nameof(Password)), DataType(DataType.Password)] public string ConfirmPassword { get; set; } = "";
}
