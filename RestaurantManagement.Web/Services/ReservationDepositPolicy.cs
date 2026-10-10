using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Services;

public static class ReservationDepositPolicy
{
    // Project policy adapted from CoCo/merchant pre-orders and NBKH per-booking deposits.
    public const decimal PreorderRate = 0.5m;
    public const decimal TableDeposit = 299000m;
    public const int PaymentMinutes = 60;
    public const string Terms = "Có món đặt trước: cọc 50% tổng tiền món; chưa chọn món: cọc giữ bàn 299.000đ/lịch. "
        + "Cọc được trừ vào hóa đơn cuối. Giữ chỗ 1 tiếng để báo thanh toán; báo trong hạn thì giữ tạm chờ đối chiếu, chưa chốt lịch. Thu ngân phải đối chiếu tiền và mã lịch. "
        + "Yêu cầu hủy được nhà hàng đối chiếu với món đã chuẩn bị và khoản tiền đã nhận trước khi xử lý hoàn/giữ cọc.";

    public static decimal Calculate(decimal foodTotal) => foodTotal > 0 ? decimal.Ceiling(foodTotal * PreorderRate) : TableDeposit;
    public static decimal Received(DatBan booking) => booking.TienCocDaNop - booking.TienCocDaHoan - booking.TienCocDaGiu;
    public static decimal Remaining(DatBan booking) => Math.Max(0, booking.TienCocYeuCau - Received(booking));
    public static bool Expired(DatBan booking) => booking.CocTuDong && Received(booking) == 0 && booking.ThoiDiemBaoChuyenKhoan is null
        && booking.HanThanhToanCoc <= DateTimeOffset.UtcNow;

    public static void Apply(DatBan booking, decimal foodTotal)
    {
        booking.CocTuDong = booking.YeuCauCoc = true;
        booking.TienCocYeuCau = Calculate(foodTotal);
        booking.DieuKienCocDaThoaThuan = Terms;
        booking.HanThanhToanCoc ??= DateTimeOffset.UtcNow.AddMinutes(PaymentMinutes);
        if (booking.TrangThai != TrangThaiDatBan.DaNhanBan)
            booking.TrangThai = Remaining(booking) > 0 ? TrangThaiDatBan.ChoCoc
                : booking.Ban.Any() ? TrangThaiDatBan.DaXacNhan : TrangThaiDatBan.ChoXacNhan;
    }
}
