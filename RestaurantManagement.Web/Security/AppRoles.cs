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

    public const string NhanVien = Admin + "," + TiepTan + "," + BoiBan + "," + ThuNgan + "," + Bep + "," + Kho;
    public static readonly string[] All = [Admin, KhachHang, TiepTan, BoiBan, ThuNgan, Bep, Kho];
    public static readonly string[] AssignableStaff = [TiepTan, BoiBan, ThuNgan, Bep, Kho];
}
