namespace RestaurantManagement.Web.Security;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string KhachHang = "KhachHang";
    public const string TiepTan = "TiepTan";
    public const string BoiBan = "BoiBan";
    public const string ThuNgan = "ThuNgan";
    public const string Bep = "Bep";
    public const string Kho = "Kho";
    public const string ThucDon = "ThucDon";
    public const string DanhMucMon = "DanhMucMon";

    public const string NhanVien = Admin + "," + TiepTan + "," + BoiBan + "," + ThuNgan + "," + Bep + "," + Kho + "," + ThucDon + "," + DanhMucMon;
    public static readonly string[] All = [Admin, KhachHang, TiepTan, BoiBan, ThuNgan, Bep, Kho, ThucDon, DanhMucMon];
    public static readonly string[] AssignableStaff = [TiepTan, BoiBan, ThuNgan, Bep, Kho, ThucDon, DanhMucMon];
    public static string? StaffDestination(string role) => role switch
    {
        ThucDon => "MonAn", DanhMucMon => "DanhMuc", TiepTan => "QuanLyDatBan",
        BoiBan => "BanAn", ThuNgan => "HoaDon", Kho => "NguyenLieu", Bep => "Bep", _ => null
    };
    public static string StaffLabel(string role) => role switch
    {
        TiepTan => "Tiếp tân · Đặt bàn", BoiBan => "Bồi bàn · Bàn ăn",
        ThuNgan => "Thu ngân · Hóa đơn", Bep => "Bếp · Chế biến",
        Kho => "Kho · Nguyên liệu", ThucDon => "Thực đơn · Món ăn, size & combo",
        DanhMucMon => "Thực đơn · Danh mục món", _ => role
    };
}
