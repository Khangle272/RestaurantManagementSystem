using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Services;

// Sổ chi tiết thanh toán: mỗi lần thu/chi tiền thật tạo một dòng ChiTietThanhToan, ghi ngày/giờ theo giờ Việt Nam.
public static class PaymentLedger
{
    public static ChiTietThanhToan Create(LoaiGiaoDichThanhToan loai, PhuongThucThanhToan hinhThuc, decimal soTien,
        int? hoaDonId = null, int? datBanId = null, int? nhanVienId = null, string? maThamChieu = null, string? ghiChu = null)
    {
        var now = TableService.VietnamNow;
        return new ChiTietThanhToan
        {
            MaThanhToan = $"TT-{now:yyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            HoaDonId = hoaDonId, DatBanId = datBanId, NhanVienId = nhanVienId,
            NgayThanhToan = DateOnly.FromDateTime(now), GioThanhToan = new TimeOnly(now.Hour, now.Minute, now.Second),
            SoTien = soTien, LoaiGiaoDich = loai, HinhThuc = hinhThuc,
            MaThamChieu = string.IsNullOrWhiteSpace(maThamChieu) ? null : maThamChieu.Trim(),
            GhiChu = ghiChu is null ? null : ghiChu.Length > 300 ? ghiChu[..300] : ghiChu
        };
    }

    public static string LoaiText(LoaiGiaoDichThanhToan loai) => loai switch
    {
        LoaiGiaoDichThanhToan.DatCoc => "Tiền đặt cọc",
        LoaiGiaoDichThanhToan.ThanhToan => "Thanh toán dứt điểm",
        _ => "Hoàn cọc"
    };

    public static string HinhThucText(PhuongThucThanhToan hinhThuc) => hinhThuc switch
    {
        PhuongThucThanhToan.TienMat => "Tiền mặt",
        PhuongThucThanhToan.ChuyenKhoan => "Chuyển khoản",
        PhuongThucThanhToan.The => "Thẻ (dữ liệu cũ)",
        _ => "Ví điện tử (dữ liệu cũ)"
    };

    // Hoàn cọc là tiền chi ra nên mang dấu âm khi cộng dòng tiền.
    public static decimal SignedAmount(ChiTietThanhToan payment) =>
        payment.LoaiGiaoDich == LoaiGiaoDichThanhToan.HoanCoc ? -payment.SoTien : payment.SoTien;

    public static DiningSlot ShiftOf(TimeOnly time) =>
        ReservationSchedule.Slots.LastOrDefault(x => x.Start <= time) ?? ReservationSchedule.Slots[0];
}
