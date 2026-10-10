using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Models;

public class HomeViewModel
{
    public List<MenuItemViewModel> Featured { get; set; } = [];
    public List<MenuItemViewModel> NewItems { get; set; } = [];
    public List<MenuItemViewModel> Menu { get; set; } = [];
    public List<DanhMuc> Categories { get; set; } = [];
    public List<PromotionViewModel> Promotions { get; set; } = [];
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
}
