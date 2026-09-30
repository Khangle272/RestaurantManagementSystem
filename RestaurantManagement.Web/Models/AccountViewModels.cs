using System.ComponentModel.DataAnnotations;

namespace RestaurantManagement.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu."), DataType(DataType.Password)]
    public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên."), StringLength(120, ErrorMessage = "Họ và tên tối đa 120 ký tự.")]
    public string HoTen { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại."), Phone(ErrorMessage = "Số điện thoại không đúng định dạng."), StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
    public string SoDienThoai { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Email không đúng định dạng."), StringLength(256, ErrorMessage = "Email tối đa 256 ký tự.")]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu."), RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$", ErrorMessage = "Mật khẩu cần ít nhất 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt."), DataType(DataType.Password)]
    public string Password { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu."), Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp."), DataType(DataType.Password)]
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
