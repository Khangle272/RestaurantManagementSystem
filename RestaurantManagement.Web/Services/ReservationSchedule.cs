namespace RestaurantManagement.Web.Services;

public sealed record DiningSlot(string Id, string Name, TimeOnly Start, TimeOnly End)
{
    public string Label => $"{Name} · {Start:HH:mm}–{End:HH:mm}";
    public DateTime Arrival(DateOnly day) => day.ToDateTime(Start);
}

public sealed record DiningSlotAvailability(string Id, string Label, string Value, bool Available, string Reason);

public static class ReservationSchedule
{
    public static readonly IReadOnlyList<DiningSlot> Slots = Array.AsReadOnly<DiningSlot>([
        new("trua", "Ca trưa", new(11, 0), new(13, 0)),
        new("chieu", "Ca chiều", new(17, 0), new(19, 0)),
        new("toi", "Ca tối", new(19, 30), new(21, 30))
    ]);

    public static DiningSlot? Match(DateTime arrival) => Slots.FirstOrDefault(x => x.Start == TimeOnly.FromDateTime(arrival));
    public static DateTime NextArrival(DateTime localNow) => Enumerable.Range(0, 2)
        .SelectMany(offset => Slots.Select(slot => slot.Arrival(DateOnly.FromDateTime(localNow.AddDays(offset)))))
        .First(value => value >= localNow.AddMinutes(30));

    public static string? Validate(DateTime arrival, DateTime localNow)
    {
        if (Match(arrival) is null) return "Chọn một ca cố định: 11:00–13:00, 17:00–19:00 hoặc 19:30–21:30.";
        if (arrival < localNow.AddMinutes(30) || arrival > localNow.AddDays(180))
            return "Vui lòng chọn ca bắt đầu sau ít nhất 30 phút và trong 180 ngày tới.";
        return null;
    }

    public static DateTimeOffset EndOf(DateTime arrival) => TableService.VietnamTime(
        DateOnly.FromDateTime(arrival).ToDateTime(Match(arrival)?.End
            ?? throw new ArgumentException("Giờ bắt đầu không thuộc ca đặt bàn.", nameof(arrival))));
}
