using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Models;

public class HomeViewModel
{
    public List<MenuItemViewModel> Featured { get; set; } = [];
    public List<MenuItemViewModel> NewItems { get; set; } = [];
    public List<MenuItemViewModel> Menu { get; set; } = [];
    public List<DanhMuc> Categories { get; set; } = [];
    public List<PromotionViewModel> Promotions { get; set; } = [];
    public List<PromotionViewModel> Vouchers { get; set; } = [];
    public int? CategoryId { get; set; }
}

public class MenuItemViewModel
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Image { get; set; }
    public string Category { get; set; } = "";
    public LoaiMon Type { get; set; }
    public TrangThaiMon Status { get; set; }
    public bool IsNew { get; set; }
    public bool IsFeatured { get; set; }
    public bool CanOrder => Status == TrangThaiMon.DangPhucVu;
    public decimal? FromPrice { get; set; }
    public List<MenuSizeViewModel> Sizes { get; set; } = [];
    public List<string> ComboItems { get; set; } = [];
    public List<PromotionViewModel> Promotions { get; set; } = [];
    public string TypeLabel => Type switch { LoaiMon.Set => "Set / Combo", LoaiMon.ThucUong => "Thức uống", _ => "Món lẻ" };
    // Không lặp chữ khi tên danh mục trùng loại món (ví dụ "Thức uống · Thức uống").
    public string CategoryLine => string.Equals(Category.Trim(), TypeLabel, StringComparison.OrdinalIgnoreCase)
        || (Type == LoaiMon.Set && Category.Contains("set", StringComparison.OrdinalIgnoreCase))
        ? Category : $"{Category} · {TypeLabel}";
    // Ảnh thay thế khi món chưa có hình: biểu tượng theo loại món thay vì luôn là tách cà phê.
    public string PlaceholderIcon => Type switch { LoaiMon.ThucUong => "bi-cup-straw", LoaiMon.Set => "bi-basket2", _ => "bi-egg-fried" };
}

public class MenuSizeViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

public class PromotionViewModel
{
    public string Name { get; set; } = "";
    public string Summary { get; set; } = "";
    public DateTimeOffset End { get; set; }
    // Mã voucher công khai (chỉ có với chương trình dạng voucher); khách đăng nhập vào ví để lưu mã.
    public string? Code { get; set; }
}
