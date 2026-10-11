using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Security;
using RestaurantManagement.Web.Services;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class BaoCaoController(IBaoCaoService baoCaoService) : Controller
{
    public async Task<IActionResult> Index(DateTime? tuNgay, DateTime? denNgay)
    {
        var model = await baoCaoService.GetDashboardReportAsync(tuNgay, denNgay);
        return View(model);
    }

    public async Task<IActionResult> DoanhThu(string? cheDo, DateTime? tuNgay, DateTime? denNgay, int? nam)
    {
        var model = await baoCaoService.GetRevenueReportAsync(cheDo ?? "TheoNgay", tuNgay, denNgay, nam);
        return View(model);
    }

    public async Task<IActionResult> XuatDoanhThuCsv(string? cheDo, DateTime? tuNgay, DateTime? denNgay, int? nam)
    {
        var model = await baoCaoService.GetRevenueReportAsync(cheDo ?? "TheoNgay", tuNgay, denNgay, nam);
        var sb = new StringBuilder();
        sb.AppendLine("Thời gian,Số đơn thành công,Số đơn hủy,Tổng tiền hàng (VNĐ),Tiền giảm giá (VNĐ),Doanh thu thực thu (VNĐ),Tiền mặt (VNĐ),Chuyển khoản QR (VNĐ),Thẻ POS (VNĐ)");

        foreach (var r in model.ChiTietTimeline)
        {
            sb.AppendLine($"\"{r.NhanThoiGian}\",{r.SoDonThanhCong},{r.SoDonHuy},{r.TongTienHang:F0},{r.TienGiam:F0},{r.DoanhThuThuc:F0},{r.TienMat:F0},{r.ChuyenKhoan:F0},{r.The:F0}");
        }

        var fileName = $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    public async Task<IActionResult> MonBanChay(DateTime? tuNgay, DateTime? denNgay, int? danhMucId, int topN = 10)
    {
        var model = await baoCaoService.GetTopDishesReportAsync(tuNgay, denNgay, danhMucId, topN);
        return View(model);
    }

    public async Task<IActionResult> XuatMonBanChayCsv(DateTime? tuNgay, DateTime? denNgay, int? danhMucId, int topN = 50)
    {
        var model = await baoCaoService.GetTopDishesReportAsync(tuNgay, denNgay, danhMucId, topN);
        var sb = new StringBuilder();
        sb.AppendLine("Thứ hạng,Tên món ăn,Danh mục,Size phổ biến,Số lượng đã bán,Doanh thu (VNĐ),Tỷ trọng (%)");

        int rank = 1;
        foreach (var m in model.DanhSachMon)
        {
            sb.AppendLine($"{rank++},\"{m.TenMon}\",\"{m.TenDanhMuc}\",\"{m.SizePhoBien}\",{m.SoLuongDaBan},{m.DoanhThuMangLai:F0},{m.TyTrongDoanhThu:F2}%");
        }

        var fileName = $"TopMonBanChay_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    public async Task<IActionResult> TonKho(DateTime? tuNgay, DateTime? denNgay)
    {
        var model = await baoCaoService.GetInventorySummaryReportAsync(tuNgay, denNgay);
        return View(model);
    }

    public async Task<IActionResult> HieuQuaKhuyenMai(DateTime? tuNgay, DateTime? denNgay)
    {
        var model = await baoCaoService.GetPromotionEffectivenessReportAsync(tuNgay, denNgay);
        return View(model);
    }

    public async Task<IActionResult> XuatHieuQuaKhuyenMaiCsv(DateTime? tuNgay, DateTime? denNgay)
    {
        var model = await baoCaoService.GetPromotionEffectivenessReportAsync(tuNgay, denNgay);
        var sb = new StringBuilder();
        sb.AppendLine("Chương trình / Voucher,Phạm vi,Hình thức ưu đãi,Yêu cầu Voucher,Số lượt áp dụng,Tổng tiền chiết khấu (VNĐ),Doanh thu mang lại (VNĐ),Tỷ lệ giảm trung bình (%),Chỉ số ROI,Đánh giá hiệu quả");

        foreach (var item in model.DanhSachChuongTrinh)
        {
            var uuDai = item.KieuGiam == RestaurantManagement.API.Models.KieuGiam.PhanTram ? $"{item.GiaTri:0.##}%" : $"{item.GiaTri:N0} đ";
            var phamVi = item.PhamVi == RestaurantManagement.API.Models.PhamViUuDai.MonAn ? "Theo món" : "Toàn bill";
            sb.AppendLine($"\"{item.TenChuongTrinh}\",\"{phamVi}\",\"{uuDai}\",{(item.CanVoucher ? "Có" : "Không")},{item.SoLuotApDung},{item.TongTienGiam:F0},{item.DoanhThuMangLai:F0},{item.TyLeGiamTrungBinh:F2}%,{item.ChiSoROI:F2},\"{item.DanhGiaHieuQua}\"");
        }

        var fileName = $"BaoCaoHieuQuaKhuyenMai_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
}
