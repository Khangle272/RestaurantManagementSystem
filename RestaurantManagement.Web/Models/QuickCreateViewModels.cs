using System.ComponentModel.DataAnnotations;

namespace RestaurantManagement.Web.Models;

public class QuickCreateDanhMucVM
{
    [Required(ErrorMessage = "Vui lòng nhập tên danh mục."), StringLength(100)]
    public string TenDanhMuc { get; set; } = "";

    [StringLength(500)]
    public string? MoTa { get; set; }
}

public class QuickCreateNguyenLieuVM
{
    [Required(ErrorMessage = "Vui lòng nhập tên nguyên liệu."), StringLength(120)]
    public string TenNguyenLieu { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập đơn vị tính."), StringLength(30)]
    public string DonViTinh { get; set; } = "";

    [StringLength(50)]
    public string? DanhMuc { get; set; }

    [Range(typeof(decimal), "0", "999999999999.99", ErrorMessage = "Đơn giá nhập không hợp lệ.")]
    public decimal DonGia { get; set; }

    [Range(typeof(decimal), "0", "999999999999.999999", ErrorMessage = "Định mức tối thiểu không hợp lệ.")]
    public decimal DinhMucToiThieu { get; set; }

    [Range(typeof(decimal), "0", "999999999999.999999", ErrorMessage = "Số lượng tồn ban đầu không hợp lệ.")]
    public decimal SoLuongTon { get; set; }
}

public class QuickCreateNhaCungCapVM
{
    [Required(ErrorMessage = "Vui lòng nhập tên nhà cung cấp."), StringLength(150)]
    public string TenNhaCungCap { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại."), StringLength(20)]
    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
    public string SoDienThoai { get; set; } = "";

    [EmailAddress(ErrorMessage = "Email không hợp lệ."), StringLength(100)]
    public string? Email { get; set; }

    [StringLength(300)]
    public string? DiaChi { get; set; }

    [StringLength(30)]
    public string? MaSoThue { get; set; }

    [StringLength(100)]
    public string? NguoiLienHe { get; set; }

    [StringLength(500)]
    public string? GhiChu { get; set; }
}

public class SupplierLinkDto
{
    public int MaNhaCungCap { get; set; }
    public decimal DonGiaCungUng { get; set; }
    public string? MaHangNCC { get; set; }
    public string? GhiChu { get; set; }
}

public class SupplierItemDto
{
    public int Id { get; set; }
    public string TenNhaCungCap { get; set; } = "";
    public string? SoDienThoai { get; set; }
    public decimal DonGiaCungUng { get; set; }
    public string? MaHangNCC { get; set; }
    public bool DaLienKet { get; set; }
}
