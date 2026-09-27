using System.ComponentModel.DataAnnotations;

namespace RestaurantManagement.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu."), DataType(DataType.Password)]
    public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required, StringLength(120)] public string HoTen { get; set; } = "";
    [Required, Phone, StringLength(20)] public string SoDienThoai { get; set; } = "";
    [Required, EmailAddress, StringLength(256)] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp."), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
    public bool DongYNhanUuDai { get; set; }
}

public class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password)] public string CurrentPassword { get; set; } = "";
    [Required, DataType(DataType.Password)] public string NewPassword { get; set; } = "";
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp."), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}

public class CreateStaffAccountViewModel
{
    [Range(1, int.MaxValue)] public int NhanVienId { get; set; }
    [Required, EmailAddress, StringLength(256)] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required] public string Role { get; set; } = "";
}

public class StaffAccountRow
{
    public int NhanVienId { get; set; }
    public string MaNhanVien { get; set; } = "";
    public string HoTen { get; set; } = "";
    public bool DangLamViec { get; set; }
    public int? TaiKhoanId { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public bool IsLocked { get; set; }
}
