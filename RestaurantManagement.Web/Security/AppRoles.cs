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
        BoiBan => "Phục vụ", ThuNgan => "Thu ngân", Bep => "Quản lý bếp", Kho => "Kho",
        "ThucDon" or "DanhMucMon" => "Vai trò cũ — cần phân công lại",
        _ => role ?? "Chưa cấp tài khoản"
    };
    public static string? RoleForJob(string? job) => job?.Trim() switch
    {
        Admin or "Quản lý" or "Quản trị" => Admin,
        TiepTan or "Tiếp tân" or "Lễ tân" => TiepTan,
        BoiBan or "Bồi bàn" or "Phục vụ" => BoiBan,
        ThuNgan or "Thu ngân" => ThuNgan,
        Bep or "Bếp" or "Đầu bếp" or "Quản lý bếp" => Bep,
        Kho or "Nhân viên kho" => Kho,
        _ => null
    };
    public static string? StaffDestination(string role) => role switch
    {
        TiepTan => "QuanLyDatBan",
        BoiBan => "SoDoBan", ThuNgan => "HoaDon", Kho => "NguyenLieu", Bep => "Bep", _ => null
    };
    public static string StaffLabel(string role) => role switch
    {
        TiepTan => "Tiếp tân · Đặt bàn", BoiBan => "Bồi bàn · Bàn ăn",
        ThuNgan => "Thu ngân · Hóa đơn", Bep => "Quản lý bếp · Điều phối & chuẩn bị món",
        Kho => "Kho · Nguyên liệu", _ => Label(role)
    };
}
