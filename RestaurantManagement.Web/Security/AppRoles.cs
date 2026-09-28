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

    public static string Label(string? role) => role switch
    {
        Admin => "Quản lý", KhachHang => "Khách hàng", TiepTan => "Tiếp tân",
        BoiBan => "Phục vụ", ThuNgan => "Thu ngân", Bep => "Bếp", Kho => "Kho",
        _ => role ?? "Chưa cấp tài khoản"
    };

    public static string? RoleForJob(string? job) => job?.Trim() switch
    {
        Admin or "Quản lý" => Admin,
        TiepTan or "Tiếp tân" or "Lễ tân" => TiepTan,
        BoiBan or "Bồi bàn" or "Phục vụ" => BoiBan,
        ThuNgan or "Thu ngân" => ThuNgan,
        Bep or "Bếp" or "Đầu bếp" => Bep,
        Kho or "Nhân viên kho" => Kho,
        _ => null
    };
}
