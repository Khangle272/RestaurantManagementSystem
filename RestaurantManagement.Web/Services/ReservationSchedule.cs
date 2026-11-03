namespace RestaurantManagement.Web.Services;

public sealed record DiningSlot(string Id, string Name, TimeOnly Start, TimeOnly End)
{
    public string Label => $"{Name} · {Start:HH:mm}–{End:HH:mm}";
    public DateTime Arrival(DateOnly day) => day.ToDateTime(Start);
}

public sealed record DiningSlotAvailability(string Id, string Label, string Value, bool Available, string Reason);

public static class ReservationSchedule
{
    public static readonly TimeOnly OpeningTime = new(11, 0);
    public static readonly TimeOnly ClosingTime = new(22, 0);
    public const string ServiceHours = "11:00–22:00";
    // Một lượt khách giữ bàn 2 tiếng kể từ giờ đến; bàn nhận được lượt khác ngay sau đó.
    public const int TurnMinutes = 120;
    public static readonly TimeOnly LastBookingArrival = new(21, 0);
    public static readonly TimeOnly LastWalkInArrival = new(21, 30);
    public static TimeOnly LastArrival(int leadMinutes) => leadMinutes > 0 ? LastBookingArrival : LastWalkInArrival;
    // Management shifts partition the service day; they do not create an afternoon closure.
    public static readonly IReadOnlyList<DiningSlot> Slots = Array.AsReadOnly<DiningSlot>([
        new("trua", "Ca trưa", new(11, 0), new(17, 0)),
        new("toi", "Ca tối", new(17, 0), new(22, 0))
    ]);

    public static bool IsOpen(DateTime arrival) => TimeOnly.FromDateTime(arrival) >= OpeningTime && TimeOnly.FromDateTime(arrival) < ClosingTime;
    public static DiningSlot? Match(DateTime arrival) => Slots.FirstOrDefault(x => x.Start <= TimeOnly.FromDateTime(arrival) && TimeOnly.FromDateTime(arrival) < x.End);
    public static DateTime NextArrival(DateTime localNow)
    {
        var earliest = localNow.AddMinutes(30);
        var rounded = earliest.Date.AddMinutes(Math.Ceiling(earliest.TimeOfDay.TotalMinutes / 15) * 15);
        if (Match(rounded) is not null) return rounded;
        return Enumerable.Range(0, 2).SelectMany(offset => Slots.Select(slot => slot.Arrival(DateOnly.FromDateTime(localNow.AddDays(offset)))))
            .First(value => value >= earliest);
    }

    public static string? Validate(DateTime arrival, DateTime localNow, int leadMinutes = 30)
    {
        if (!IsOpen(arrival)) return $"Chọn giờ đến trong giờ phục vụ {ServiceHours}, trước giờ đóng cửa.";
        if (TimeOnly.FromDateTime(arrival) > LastArrival(leadMinutes))
            return $"Nhà hàng nhận khách muộn nhất lúc {LastArrival(leadMinutes):HH:mm} để kịp phục vụ trước giờ đóng cửa {ClosingTime:HH:mm}.";
        if (arrival < localNow.AddMinutes(leadMinutes) || arrival > localNow.AddDays(180))
            return leadMinutes > 0 ? "Vui lòng đặt trước ít nhất 30 phút và trong 180 ngày tới." : "Giờ đến không hợp lệ hoặc đã quá 15 phút; chọn lại giờ.";
        return null;
    }

    public static IReadOnlyList<DateTime> QuickArrivals(DateOnly day, DateTime localNow, int leadMinutes = 30)
    {
        var earliest = localNow.AddMinutes(Math.Max(0, leadMinutes));
        var result = new List<DateTime>();
        foreach (var shift in Slots)
        {
            var start = day.ToDateTime(shift.Start);
            if (start < earliest) start = earliest;
            var rounded = start.Date.AddMinutes(Math.Ceiling(start.TimeOfDay.TotalMinutes / 15) * 15);
            if (rounded < day.ToDateTime(shift.End) && TimeOnly.FromDateTime(rounded) <= LastArrival(leadMinutes)) result.Add(rounded);
        }
        return result;
    }

    public static DateTimeOffset EndOf(DateTime arrival)
    {
        if (!IsOpen(arrival)) throw new ArgumentException("Giờ đến ngoài giờ hoạt động.", nameof(arrival));
        var closing = DateOnly.FromDateTime(arrival).ToDateTime(ClosingTime);
        var end = arrival.AddMinutes(TurnMinutes);
        return TableService.VietnamTime(end < closing ? end : closing);
    }

    public static (DateTimeOffset From, DateTimeOffset Until) Window(DateTimeOffset arrival)
    {
        var local = arrival.ToOffset(TimeSpan.FromHours(7)).DateTime;
        return !IsOpen(local) ? (TableService.VietnamTime(local.Date), TableService.VietnamTime(local.Date.AddDays(1)))
            : (arrival, EndOf(local));
    }
}
