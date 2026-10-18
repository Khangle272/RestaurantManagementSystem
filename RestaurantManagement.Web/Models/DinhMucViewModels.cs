using System.ComponentModel.DataAnnotations;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Models;

public class DinhMucTheoSizeVM
{
    public int MonAnId { get; set; }
    public string TenMon { get; set; } = "";
    public string TenDanhMuc { get; set; } = "";
    public LoaiMon Loai { get; set; }
    public bool LaThucUong { get; set; }

    // Định lượng gốc (size đầu tiên); các size khác = định lượng gốc × Hệ số tỷ lệ.
    public List<DinhMucItemViewModel> DinhMucItems { get; set; } = [];
    public List<SizeDinhMucGroupVM> Sizes { get; set; } = [];
    public List<NguyenLieuOptionVM> AllIngredients { get; set; } = [];
}

public class SizeDinhMucGroupVM
{
    public int MonAnSizeId { get; set; }
    public string TenSize { get; set; } = "";
    public decimal GiaBan { get; set; }
    public bool DangSuDung { get; set; } = true;
    public decimal HeSoTyLe { get; set; } = 1.0m;
    public string? GhiChuThanhPhanKhacBiet { get; set; }

    // Nguyên liệu phụ / thành phần khác biệt chỉ có ở size này, không nhân theo hệ số.
    public List<SizeDinhMucItemVM> Items { get; set; } = [];
}

public class SizeDinhMucItemVM
{
    [Range(1, int.MaxValue)]
    public int NguyenLieuId { get; set; }
    public string TenNguyenLieu { get; set; } = "";
    public string DonViTinh { get; set; } = "";
    public decimal DonGia { get; set; }

    [Range(typeof(decimal), "0.000001", "999999999999.999999", ErrorMessage = "Số lượng định mức phải lớn hơn 0.")]
    public decimal SoLuong { get; set; }

    public decimal ThanhTien => SoLuong * DonGia;
}

public class NguyenLieuOptionVM
{
    public int Id { get; set; }
    public string TenNguyenLieu { get; set; } = "";
    public string DonViTinh { get; set; } = "";
    public decimal DonGia { get; set; }
}
