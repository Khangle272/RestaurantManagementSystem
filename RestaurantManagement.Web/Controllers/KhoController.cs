using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Security;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin + "," + AppRoles.Kho)]
public class KhoController(RestaurantDbContext context) : ManagementControllerBase(context)
{
    public async Task<IActionResult> TonKho(string? keyword, bool? lowStock, string? danhMuc, int page = 1)
    {
        keyword = keyword?.Trim();
        var query = Db.NguyenLieu.Where(x => x.DangSuDung).AsNoTracking();

        if (!string.IsNullOrEmpty(keyword))
        {
            query = query.Where(x => x.TenNguyenLieu.Contains(keyword));
        }

        if (!string.IsNullOrEmpty(danhMuc))
        {
            query = query.Where(x => x.DanhMuc == danhMuc);
        }

        if (lowStock == true)
        {
            query = query.Where(x => x.SoLuongTon <= x.NguongCanhBao);
        }

        const int pageSize = 15;
        var total = await query.CountAsync();
        var pages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, pages);

        var list = await query.OrderBy(x => x.TenNguyenLieu)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new TonKhoItemVM
            {
                MaNguyenLieu = x.Id,
                TenNguyenLieu = x.TenNguyenLieu,
                DonViTinh = x.DonViTinh,
                DanhMuc = x.DanhMuc,
                SoLuongTon = x.SoLuongTon,
                DinhMucToiThieu = x.NguongCanhBao,
                DonGia = x.DonGia
            })
            .ToListAsync();

        var allActive = await Db.NguyenLieu.Where(x => x.DangSuDung).AsNoTracking().ToListAsync();
        var danhMucList = allActive
            .Where(x => !string.IsNullOrEmpty(x.DanhMuc))
            .Select(x => x.DanhMuc!)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        var totalIngredients = allActive.Count;
        var lowStockCount = allActive.Count(x => x.SoLuongTon > 0 && x.SoLuongTon <= x.NguongCanhBao);
        var outOfStockCount = allActive.Count(x => x.SoLuongTon <= 0);
        var totalStockValue = allActive.Sum(x => x.SoLuongTon * x.DonGia);

        return View(new TonKhoFilterVM
        {
            Keyword = keyword,
            OnlyLowStock = lowStock ?? false,
            DanhMuc = danhMuc,
            DanhMucList = danhMucList,
            DanhSachTon = list,
            Pagination = new PaginationViewModel
            {
                CurrentPage = page,
                TotalPages = pages,
                TotalItems = total,
                Search = keyword
            },
            TongSoNguyenLieu = totalIngredients,
            SoNguyenLieuSapHet = lowStockCount,
            SoNguyenLieuCanKho = outOfStockCount,
            TongGiaTriTonKho = totalStockValue
        });
    }

    [HttpGet]
    public async Task<IActionResult> BaoCao(DateTime? tuNgay, DateTime? denNgay, string? preset = "ThangNay")
    {
        var model = await GenerateBaoCaoModelAsync(tuNgay, denNgay, preset);
        return View("BaoCaoTonKho", model);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(DateTime? tuNgay, DateTime? denNgay, string? preset = "ThangNay")
    {
        var model = await GenerateBaoCaoModelAsync(tuNgay, denNgay, preset);

        var sb = new StringBuilder();
        sb.AppendLine("BÁO CÁO TỔNG HỢP NHẬP - XUẤT - TỒN KHO");
        sb.AppendLine($"Khoảng thời gian: {model.TuNgay:dd/MM/yyyy} - {model.DenNgay:dd/MM/yyyy}");
        sb.AppendLine($"Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine();
        sb.AppendLine("Mã NL,Tên nguyên liệu,ĐVT,Nhóm,Đơn giá (VNĐ),Tồn đầu (SL),Tồn đầu (Tiền),Nhập trong kỳ (SL),Nhập trong kỳ (Tiền),Xuất trong kỳ (SL),Xuất trong kỳ (Tiền),Thanh lý trong kỳ (SL),Thanh lý trong kỳ (Tiền),Tồn cuối (SL),Tồn cuối (Tiền)");

        foreach (var r in model.ChiTietBaoCao)
        {
            var tenEscape = $"\"{r.TenNguyenLieu.Replace("\"", "\"\"")}\"";
            var dmEscape = $"\"{(r.DanhMuc ?? "Khác").Replace("\"", "\"\"")}\"";
            sb.AppendLine($"NL{r.MaNguyenLieu:D3},{tenEscape},{r.DonViTinh},{dmEscape},{r.DonGia:0.##},{r.TonDauKySL:0.##},{r.TonDauKyTien:0.##},{r.NhapTrongKySL:0.##},{r.NhapTrongKyTien:0.##},{r.XuatTrongKySL:0.##},{r.XuatTrongKyTien:0.##},{r.ThanhLyTrongKySL:0.##},{r.ThanhLyTrongKyTien:0.##},{r.TonCuoiKySL:0.##},{r.TonCuoiKyTien:0.##}");
        }

        sb.AppendLine();
        sb.AppendLine($",,,,TỔNG CỘNG,,{model.TongGiaTriDauKy:0.##},,{model.TongGiaTriNhap:0.##},,{model.TongGiaTriXuat:0.##},,{model.TongGiaTriThanhLy:0.##},,{model.TongGiaTriCuoiKy:0.##}");

        // Prepend UTF-8 BOM so Excel opens Vietnamese characters cleanly
        var preamble = Encoding.UTF8.GetPreamble();
        var contentBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileBytes = new byte[preamble.Length + contentBytes.Length];
        Buffer.BlockCopy(preamble, 0, fileBytes, 0, preamble.Length);
        Buffer.BlockCopy(contentBytes, 0, fileBytes, preamble.Length, contentBytes.Length);

        var fileName = $"BaoCaoNhapXuatTon_{model.TuNgay:yyyyMMdd}_{model.DenNgay:yyyyMMdd}.csv";
        return File(fileBytes, "text/csv; charset=utf-8", fileName);
    }

    private async Task<BaoCaoTonKhoVM> GenerateBaoCaoModelAsync(DateTime? tuNgay, DateTime? denNgay, string? preset)
    {
        DateTime start;
        DateTime end;
        var now = DateTime.Now;

        switch (preset?.ToLower())
        {
            case "homnay":
                start = now.Date;
                end = now.Date.AddDays(1).AddTicks(-1);
                break;
            case "tuannay":
                var diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                start = now.Date.AddDays(-1 * diff);
                end = now.Date.AddDays(1).AddTicks(-1);
                break;
            case "thangtruoc":
                var firstDayPrevMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
                var lastDayPrevMonth = new DateTime(now.Year, now.Month, 1).AddTicks(-1);
                start = firstDayPrevMonth;
                end = lastDayPrevMonth;
                break;
            case "tuychon":
                start = tuNgay?.Date ?? new DateTime(now.Year, now.Month, 1);
                end = denNgay?.Date.AddDays(1).AddTicks(-1) ?? now.Date.AddDays(1).AddTicks(-1);
                break;
            case "thangnay":
            default:
                start = new DateTime(now.Year, now.Month, 1);
                end = now.Date.AddDays(1).AddTicks(-1);
                preset = "thangnay";
                break;
        }

        var startOffset = new DateTimeOffset(start);
        var endOffset = new DateTimeOffset(end);

        var ingredients = await Db.NguyenLieu.Where(x => x.DangSuDung).OrderBy(x => x.TenNguyenLieu).ToListAsync();

        // Historical aggregates before start
        var importBefore = await Db.ChiTietPhieuNhap
            .Where(c => c.PhieuNhap.TrangThai == TrangThaiPhieu.DaGhiSo && c.PhieuNhap.ThoiDiem < startOffset)
            .GroupBy(c => c.NguyenLieuId)
            .Select(g => new { Id = g.Key, Qty = g.Sum(c => c.SoLuong) })
            .ToDictionaryAsync(x => x.Id, x => x.Qty);

        var exportBefore = await Db.ChiTietPhieuXuat
            .Where(c => c.PhieuXuat.TrangThai == TrangThaiPhieu.DaGhiSo && c.PhieuXuat.ThoiDiem < startOffset)
            .GroupBy(c => c.NguyenLieuId)
            .Select(g => new { Id = g.Key, Qty = g.Sum(c => c.SoLuong) })
            .ToDictionaryAsync(x => x.Id, x => x.Qty);

        var disposalBefore = await Db.ChiTietPhieuThanhLy
            .Where(c => c.PhieuThanhLy.TrangThai == "DaThanhLy" && c.PhieuThanhLy.ThoiDiem < startOffset)
            .GroupBy(c => c.NguyenLieuId)
            .Select(g => new { Id = g.Key, Qty = g.Sum(c => c.SoLuong) })
            .ToDictionaryAsync(x => x.Id, x => x.Qty);

        // Period aggregates
        var importPeriod = await Db.ChiTietPhieuNhap
            .Where(c => c.PhieuNhap.TrangThai == TrangThaiPhieu.DaGhiSo && c.PhieuNhap.ThoiDiem >= startOffset && c.PhieuNhap.ThoiDiem <= endOffset)
            .GroupBy(c => c.NguyenLieuId)
            .Select(g => new { Id = g.Key, Qty = g.Sum(c => c.SoLuong), Value = g.Sum(c => c.ThanhTien) })
            .ToDictionaryAsync(x => x.Id, x => new { x.Qty, x.Value });

        var exportPeriod = await Db.ChiTietPhieuXuat
            .Where(c => c.PhieuXuat.TrangThai == TrangThaiPhieu.DaGhiSo && c.PhieuXuat.ThoiDiem >= startOffset && c.PhieuXuat.ThoiDiem <= endOffset)
            .GroupBy(c => c.NguyenLieuId)
            .Select(g => new { Id = g.Key, Qty = g.Sum(c => c.SoLuong), Value = g.Sum(c => c.ThanhTien) })
            .ToDictionaryAsync(x => x.Id, x => new { x.Qty, x.Value });

        var disposalPeriod = await Db.ChiTietPhieuThanhLy
            .Where(c => c.PhieuThanhLy.TrangThai == "DaThanhLy" && c.PhieuThanhLy.ThoiDiem >= startOffset && c.PhieuThanhLy.ThoiDiem <= endOffset)
            .GroupBy(c => c.NguyenLieuId)
            .Select(g => new { Id = g.Key, Qty = g.Sum(c => c.SoLuong), Value = g.Sum(c => c.ThanhTien) })
            .ToDictionaryAsync(x => x.Id, x => new { x.Qty, x.Value });

        var rows = new List<BaoCaoTonKhoRowVM>();

        foreach (var ing in ingredients)
        {
            var inBefore = importBefore.TryGetValue(ing.Id, out var ib) ? ib : 0;
            var outBefore = exportBefore.TryGetValue(ing.Id, out var ob) ? ob : 0;
            var dispBefore = disposalBefore.TryGetValue(ing.Id, out var db) ? db : 0;

            var tonDau = inBefore - outBefore - dispBefore;
            if (tonDau < 0) tonDau = 0; // prevent visual anomaly if opening stock data was manually initialized

            var inQty = importPeriod.TryGetValue(ing.Id, out var ip) ? ip.Qty : 0;
            var inVal = importPeriod.TryGetValue(ing.Id, out var ipVal) ? ipVal.Value : 0;

            var outQty = exportPeriod.TryGetValue(ing.Id, out var ep) ? ep.Qty : 0;
            var dispQty = disposalPeriod.TryGetValue(ing.Id, out var dp) ? dp.Qty : 0;

            rows.Add(new BaoCaoTonKhoRowVM
            {
                MaNguyenLieu = ing.Id,
                TenNguyenLieu = ing.TenNguyenLieu,
                DonViTinh = ing.DonViTinh,
                DanhMuc = ing.DanhMuc,
                DonGia = ing.DonGia,
                TonDauKySL = tonDau,
                NhapTrongKySL = inQty,
                NhapTrongKyTien = inVal > 0 ? inVal : inQty * ing.DonGia,
                XuatTrongKySL = outQty,
                ThanhLyTrongKySL = dispQty
            });
        }

        return new BaoCaoTonKhoVM
        {
            TuNgay = start,
            DenNgay = end,
            Preset = preset ?? "thangnay",
            ChiTietBaoCao = rows
        };
    }
}
