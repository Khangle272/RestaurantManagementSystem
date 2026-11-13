using System.Globalization;

namespace RestaurantManagement.Shared;

// Định dạng ngày giờ hiển thị thống nhất cho website, API và app mobile:
// ngày "dd/MM/yyyy", ngày giờ "dd/MM/yyyy HH:mm", luôn theo giờ Việt Nam (UTC+7).
// DateTimeOffset được đổi sang UTC+7 trước khi in; DateTime/DateOnly coi như đã là giờ Việt Nam.
public static class VnTime
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(7);
    public const string DateFormat = "dd/MM/yyyy";
    public const string DateTimeFormat = "dd/MM/yyyy HH:mm";
    public const string TimeFormat = "HH:mm";

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static DateTimeOffset Local(DateTimeOffset value) => value.ToOffset(Offset);

    public static string Ngay(DateTimeOffset value) => Local(value).ToString(DateFormat, Invariant);
    public static string Ngay(DateTimeOffset? value) => value is { } v ? Ngay(v) : "";
    public static string Ngay(DateTime value) => value.ToString(DateFormat, Invariant);
    public static string Ngay(DateTime? value) => value is { } v ? Ngay(v) : "";
    public static string Ngay(DateOnly value) => value.ToString(DateFormat, Invariant);
    public static string Ngay(DateOnly? value) => value is { } v ? Ngay(v) : "";

    public static string NgayGio(DateTimeOffset value) => Local(value).ToString(DateTimeFormat, Invariant);
    public static string NgayGio(DateTimeOffset? value) => value is { } v ? NgayGio(v) : "";
    public static string NgayGio(DateTime value) => value.ToString(DateTimeFormat, Invariant);
    public static string NgayGio(DateTime? value) => value is { } v ? NgayGio(v) : "";
    public static string NgayGio(DateOnly day, TimeOnly time) => NgayGio(day.ToDateTime(time));

    // Chỉ giờ phút, dùng cho mốc trong cùng một ngày đã hiển thị (giờ gọi món, giờ kết thúc dự kiến...).
    public static string Gio(DateTimeOffset value) => Local(value).ToString(TimeFormat, Invariant);
    public static string Gio(DateTimeOffset? value) => value is { } v ? Gio(v) : "";
    public static string Gio(DateTime value) => value.ToString(TimeFormat, Invariant);
    public static string Gio(TimeOnly value) => value.ToString(TimeFormat, Invariant);
}
