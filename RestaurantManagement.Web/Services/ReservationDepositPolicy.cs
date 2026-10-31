using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Services;

public static class ReservationDepositPolicy
{
    // Có đặt món trước: tiền đặt cọc = % tổng giá trị món. Chỉ giữ chỗ: tiền đặt cọc theo khu vực/loại bàn (KhuVuc.TienCocGiuCho).
    public const decimal PreorderRate = 0.5m;
    public const decimal DefaultAreaDeposit = 200000m;
    public const int PaymentMinutes = 60;
    public static string PreorderRateText => $"{PreorderRate * 100:0}%";

    public static string Terms(decimal areaDeposit, bool preorder) => (preorder
            ? $"Tiền đặt cọc: {PreorderRateText} tổng giá trị món đặt trước. "
            : $"Tiền đặt cọc giữ chỗ theo khu vực: {areaDeposit:N0}đ/lịch. ")
        + "Tiền đặt cọc được trừ vào hóa đơn khi thanh toán. Giữ chỗ 1 tiếng để báo thanh toán; thu ngân đối chiếu tiền thực nhận và mã đặt bàn rồi mới xác nhận. "
        + "Yêu cầu hủy được nhà hàng đối chiếu với món đã chuẩn bị và tiền đặt cọc đã nhận trước khi hoàn/giữ cọc.";

    public static decimal Calculate(decimal foodTotal, decimal areaDeposit) =>
        foodTotal > 0 ? decimal.Ceiling(foodTotal * PreorderRate) : areaDeposit;
    public static decimal Received(DatBan booking) => booking.TienCocDaNop - booking.TienCocDaHoan - booking.TienCocDaGiu;
    public static decimal Remaining(DatBan booking) => Math.Max(0, booking.TienCocYeuCau - Received(booking));
    public static bool Expired(DatBan booking) => booking.CocTuDong && Received(booking) == 0 && booking.ThoiDiemBaoChuyenKhoan is null
        && booking.HanThanhToanCoc <= DateTimeOffset.UtcNow;

    // Trạng thái cọc hiển thị thống nhất ở mọi màn hình: "Chưa đặt cọc" hoặc "Đã đặt cọc <số tiền>".
    public static string StatusText(DatBan booking)
    {
        var received = booking.TienCocDaNop;
        if (booking.TrangThaiCoc == TrangThaiCoc.DaHoan) return $"Đã hoàn cọc {booking.TienCocDaHoan:N0} đ";
        if (received <= 0)
            return booking.TienCocYeuCau <= 0 ? "Chưa đặt cọc (không yêu cầu)"
                : booking.ThoiDiemBaoChuyenKhoan is not null ? $"Chưa đặt cọc · khách báo đã chuyển {booking.SoTienKhachBao:N0} đ, chờ đối chiếu"
                : $"Chưa đặt cọc · cần cọc {booking.TienCocYeuCau:N0} đ";
        var remaining = Remaining(booking);
        return remaining > 0 ? $"Đã đặt cọc {received:N0} đ · còn thiếu {remaining:N0} đ" : $"Đã đặt cọc {received:N0} đ";
    }

    public static bool HasDeposit(DatBan booking) => booking.TienCocDaNop > 0 && booking.TrangThaiCoc != TrangThaiCoc.DaHoan;

    // Khu vực của bàn đã chọn quyết định mức cọc; chưa có bàn thì dùng khu vực khách chọn.
    public static async Task<decimal> AreaDepositAsync(RestaurantDbContext db, DatBan booking)
    {
        var tableIds = booking.Ban.Select(x => x.BanAnId).ToList();
        if (tableIds.Count == 0 && booking.Id > 0)
            tableIds = await db.ChiTietDatBan.Where(x => x.DatBanId == booking.Id).Select(x => x.BanAnId).ToListAsync();
        if (tableIds.Count > 0)
            return await db.BanAn.Where(x => tableIds.Contains(x.Id)).MaxAsync(x => (decimal?)x.KhuVuc.TienCocGiuCho) ?? DefaultAreaDeposit;
        if (booking.KhuVucUuTienId is int areaId)
            return await db.KhuVuc.Where(x => x.Id == areaId).Select(x => (decimal?)x.TienCocGiuCho).SingleOrDefaultAsync() ?? DefaultAreaDeposit;
        return await db.KhuVuc.Where(x => x.DangSuDung && x.LaPhongVip == booking.YeuCauVip)
            .MinAsync(x => (decimal?)x.TienCocGiuCho) ?? DefaultAreaDeposit;
    }

    public static void Apply(DatBan booking, decimal foodTotal, decimal areaDeposit)
    {
        booking.CocTuDong = booking.YeuCauCoc = true;
        booking.TienCocYeuCau = Calculate(foodTotal, areaDeposit);
        booking.DieuKienCocDaThoaThuan = Terms(areaDeposit, foodTotal > 0);
        booking.HanThanhToanCoc ??= DateTimeOffset.UtcNow.AddMinutes(PaymentMinutes);
        if (booking.TrangThai != TrangThaiDatBan.DaNhanBan)
            booking.TrangThai = Remaining(booking) > 0 ? TrangThaiDatBan.ChoCoc
                : booking.Ban.Any() ? TrangThaiDatBan.DaXacNhan : TrangThaiDatBan.ChoXacNhan;
    }
}
