using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Security;
using RestaurantManagement.Web.Services;

namespace RestaurantManagement.Web.Controllers;

[Authorize]
public class BaoCaoController(IBaoCaoService baoCaoService) : Controller
{
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Index(DateTime? tuNgay, DateTime? denNgay)
    {
        if (InvalidPeriod(tuNgay, denNgay) is { } invalid) return invalid;
        var model = await baoCaoService.GetDashboardReportAsync(tuNgay, denNgay);
        return View(model);
    }

    // Thu ngân kết ca: kiểm tiền mặt / chuyển khoản theo ngày và ca, không cần biểu đồ.
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.ThuNgan)]
    public async Task<IActionResult> NopCa(DateOnly? ngay, string? ca)
    {
        var day = ngay ?? DateOnly.FromDateTime(TableService.VietnamNow);
        if (day.Year is < 2000 or > 2100) return BadRequest("Ngày không hợp lệ.");
        if (ca is not null && ReservationSchedule.Slots.All(x => x.Id != ca)) ca = null;
        return View(await baoCaoService.GetShiftReportAsync(day, ca));
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DoanhThu(string? cheDo, DateTime? tuNgay, DateTime? denNgay, int? nam)
    {
        if (InvalidPeriod(tuNgay, denNgay, nam, cheDo is not ("TheoThang" or "TheoNam")) is { } invalid) return invalid;
        var model = await baoCaoService.GetRevenueReportAsync(cheDo ?? "TheoNgay", tuNgay, denNgay, nam);
        return View(model);
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> XuatDoanhThuCsv(string? cheDo, DateTime? tuNgay, DateTime? denNgay, int? nam)
    {
        if (InvalidPeriod(tuNgay, denNgay, nam, cheDo is not ("TheoThang" or "TheoNam")) is { } invalid) return invalid;
        var model = await baoCaoService.GetRevenueReportAsync(cheDo ?? "TheoNgay", tuNgay, denNgay, nam);
        var sb = new StringBuilder();
        sb.AppendLine("Thời gian,Bill thanh toán,Đơn lập trong kỳ đã hủy,Tiền hàng (VNĐ),Giảm giá (VNĐ),Doanh thu sau giảm giá (VNĐ),Thu thêm tiền mặt (VNĐ),Thu thêm QR (VNĐ),Thu thêm thẻ (VNĐ)");

        foreach (var r in model.ChiTietTimeline)
        {
            sb.AppendLine($"\"{r.NhanThoiGian}\",{r.SoDonThanhCong},{r.SoDonHuy},{r.TongTienHang:F0},{r.TienGiam:F0},{r.DoanhThuThuc:F0},{r.TienMat:F0},{r.ChuyenKhoan:F0},{r.The:F0}");
        }

        var fileName = $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> MonBanChay(DateTime? tuNgay, DateTime? denNgay, int? danhMucId, int topN = 10)
    {
        if (InvalidPeriod(tuNgay, denNgay) is { } invalid) return invalid;
        var model = await baoCaoService.GetTopDishesReportAsync(tuNgay, denNgay, danhMucId, topN);
        return View(model);
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> XuatMonBanChayCsv(DateTime? tuNgay, DateTime? denNgay, int? danhMucId, int topN = 50)
    {
        if (InvalidPeriod(tuNgay, denNgay) is { } invalid) return invalid;
        var model = await baoCaoService.GetTopDishesReportAsync(tuNgay, denNgay, danhMucId, topN);
        var sb = new StringBuilder();
        sb.AppendLine("Thứ hạng,Tên món ăn,Danh mục,Size phổ biến,Số lượng đã bán,Doanh thu (VNĐ),Tỷ trọng (%)");

        int rank = 1;
        foreach (var m in model.DanhSachMon)
        {
            sb.AppendLine(FormattableString.Invariant($"{rank++},{CsvExport.Cell(m.TenMon)},{CsvExport.Cell(m.TenDanhMuc)},{CsvExport.Cell(m.SizePhoBien)},{m.SoLuongDaBan},{m.DoanhThuMangLai:F0},{m.TyTrongDoanhThu:F2}%"));
        }

        var fileName = $"TopMonBanChay_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Kho)]
    public async Task<IActionResult> TonKho(DateTime? tuNgay, DateTime? denNgay)
    {
        if (InvalidPeriod(tuNgay, denNgay) is { } invalid) return invalid;
        var model = await baoCaoService.GetInventorySummaryReportAsync(tuNgay, denNgay);
        return View(model);
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> HieuQuaKhuyenMai(DateTime? tuNgay, DateTime? denNgay)
    {
        if (InvalidPeriod(tuNgay, denNgay) is { } invalid) return invalid;
        var model = await baoCaoService.GetPromotionEffectivenessReportAsync(tuNgay, denNgay);
        return View(model);
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> XuatHieuQuaKhuyenMaiCsv(DateTime? tuNgay, DateTime? denNgay)
    {
        if (InvalidPeriod(tuNgay, denNgay) is { } invalid) return invalid;
        var model = await baoCaoService.GetPromotionEffectivenessReportAsync(tuNgay, denNgay);
        var sb = new StringBuilder();
        sb.AppendLine("Chương trình / Voucher,Phạm vi,Hình thức ưu đãi,Yêu cầu Voucher,Số lượt áp dụng,Tiền giảm thực tế (VNĐ),Doanh thu bill có áp dụng (VNĐ),Tỷ lệ giảm (%),Doanh thu bill / tiền giảm,Nhận xét tỷ lệ");

        foreach (var item in model.DanhSachChuongTrinh)
        {
            var uuDai = item.KieuGiam == RestaurantManagement.API.Models.KieuGiam.PhanTram ? $"{item.GiaTri:0.##}%" : $"{item.GiaTri:N0} đ";
            var phamVi = item.PhamVi == RestaurantManagement.API.Models.PhamViUuDai.MonAn ? "Theo món" : "Toàn bill";
            sb.AppendLine(FormattableString.Invariant($"{CsvExport.Cell(item.TenChuongTrinh)},{CsvExport.Cell(phamVi)},{CsvExport.Cell(uuDai)},{(item.CanVoucher ? "Có" : "Không")},{item.SoLuotApDung},{item.TongTienGiam:F0},{item.DoanhThuMangLai:F0},{item.TyLeGiamTrungBinh:F2}%,{item.ChiSoROI:F2},{CsvExport.Cell(item.DanhGiaHieuQua)}"));
        }

        var fileName = $"BaoCaoHieuQuaKhuyenMai_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
    private IActionResult? InvalidPeriod(DateTime? from, DateTime? to, int? year = null, bool dailyTimeline = false)
    {
        if (!ModelState.IsValid || from?.Year is < 1900 or > 2100 || to?.Year is < 1900 or > 2100
            || year is < 1900 or > 2100 || (from.HasValue && to.HasValue && from.Value.Date > to.Value.Date))
            return BadRequest("Khoảng ngày/năm không hợp lệ. Chọn ngày bắt đầu trước ngày kết thúc, trong năm 1900–2100.");
        var today = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).Date;
        if (dailyTimeline && ((to ?? today).Date - (from ?? today.AddDays(-30)).Date).TotalDays > 366)
            return BadRequest("Biểu đồ theo ngày tối đa một năm. Chọn theo tháng/năm nếu cần xem khoảng dài hơn.");
        return null;
    }
}
