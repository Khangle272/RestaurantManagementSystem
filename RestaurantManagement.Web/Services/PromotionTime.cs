namespace RestaurantManagement.Web.Services;

public static class PromotionTime
{
    public static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
    public static DateTime Today => DateTimeOffset.UtcNow.ToOffset(VietnamOffset).Date;
    // datetime-local không mang múi giờ. Luôn diễn giải theo giờ nhà hàng, bất kể giờ máy chủ.
    public static DateTimeOffset FromLocal(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), VietnamOffset);
    public static DateTime Local(DateTimeOffset value) => value.ToOffset(VietnamOffset).DateTime;
    public static bool IsActive(DateTimeOffset start, DateTimeOffset end, DateTimeOffset now) => start <= now && now <= end;
}
