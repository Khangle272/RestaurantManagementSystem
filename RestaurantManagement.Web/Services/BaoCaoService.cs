using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Services;

public interface IBaoCaoService
{
    Task<BaoCaoDashboardViewModel> GetDashboardReportAsync(DateTime? tuNgay = null, DateTime? denNgay = null);
    Task<BaoCaoDoanhThuViewModel> GetRevenueReportAsync(string cheDo, DateTime? tuNgay, DateTime? denNgay, int? nam);
    Task<BaoCaoMonBanChayViewModel> GetTopDishesReportAsync(DateTime? tuNgay, DateTime? denNgay, int? danhMucId, int topN = 10);
    Task<BaoCaoTonKhoTongHopViewModel> GetInventorySummaryReportAsync(DateTime? tuNgay, DateTime? denNgay);
    Task<BaoCaoHieuQuaKhuyenMaiViewModel> GetPromotionEffectivenessReportAsync(DateTime? tuNgay, DateTime? denNgay);
}

public class BaoCaoService(RestaurantDbContext db) : IBaoCaoService
{
    public async Task<BaoCaoDashboardViewModel> GetDashboardReportAsync(DateTime? tuNgay = null, DateTime? denNgay = null)
    {
        var start = tuNgay.HasValue
            ? new DateTimeOffset(tuNgay.Value.Date.ToUniversalTime(), TimeSpan.Zero)
            : DateTimeOffset.UtcNow.AddDays(-30);
        var end = denNgay.HasValue
            ? new DateTimeOffset(denNgay.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime(), TimeSpan.Zero)
            : DateTimeOffset.UtcNow;

        var invoices = await db.HoaDon.AsNoTracking()
            .Where(x => x.ThoiDiemLap >= start && x.ThoiDiemLap <= end)
            .ToListAsync();

        var paidInvoices = invoices.Where(x => x.TrangThai == TrangThaiHoaDon.DaThanhToan).ToList();
        var cancelledInvoices = invoices.Where(x => x.TrangThai == TrangThaiHoaDon.DaHuy).ToList();

        var model = new BaoCaoDashboardViewModel
        {
            TuNgay = start,
            DenNgay = end,
            TongDoanhThu = paidInvoices.Sum(x => x.TongThanhToan),
            TongSoDonHang = invoices.Count,
            SoDonThanhCong = paidInvoices.Count,
            SoDonDaHuy = cancelledInvoices.Count,
            TongChiPhiKhuyenMai = paidInvoices.Sum(x => x.TienGiam),
            DoanhThuTienMat = paidInvoices.Where(x => x.PhuongThucThanhToan == PhuongThucThanhToan.TienMat).Sum(x => x.TongThanhToan),
            DoanhThuChuyenKhoan = paidInvoices.Where(x => x.PhuongThucThanhToan == PhuongThucThanhToan.ChuyenKhoan).Sum(x => x.TongThanhToan),
            DoanhThuThe = paidInvoices.Where(x => x.PhuongThucThanhToan == PhuongThucThanhToan.The).Sum(x => x.TongThanhToan)
        };

        // Doanh thu theo 14 ngày gần nhất
        var daysBack = 14;
        var recentStart = DateTime.Today.AddDays(-daysBack + 1);
        for (int i = 0; i < daysBack; i++)
        {
            var day = recentStart.AddDays(i);
            var nextDay = day.AddDays(1);
            var dayInvoices = paidInvoices.Where(x => x.ThoiDiemLap.LocalDateTime.Date == day).ToList();

            model.DoanhThuTheoNgay.Add(new DoanhThuTimelineRow
            {
                NhanThoiGian = day.ToString("dd/MM"),
                SoDonThanhCong = dayInvoices.Count,
                DoanhThuThuc = dayInvoices.Sum(x => x.TongThanhToan)
            });
        }

        // Top 5 món bán chạy
        var topDishes = await GetTopDishesReportAsync(start.LocalDateTime.Date, end.LocalDateTime.Date, null, 5);
        model.TopMonBanChay = topDishes.DanhSachMon;

        // Tồn kho cảnh báo
        var ingredients = await db.NguyenLieu.AsNoTracking().Where(x => x.DangSuDung).ToListAsync();
        model.TongGiaTriTonKho = ingredients.Sum(x => x.SoLuongTon * x.DonGia);
        var warningItems = ingredients.Where(x => x.SoLuongTon <= x.NguongCanhBao).ToList();
        model.SoNguyenLieuCanhBao = warningItems.Count;
        model.NguyenLieuCanhBao = warningItems.Take(5).Select(x => new NguyenLieuCanhBaoItem
        {
            Id = x.Id,
            TenNguyenLieu = x.TenNguyenLieu,
            DonViTinh = x.DonViTinh,
            SoLuongTon = x.SoLuongTon,
            NguongCanhBao = x.NguongCanhBao,
            DonGia = x.DonGia
        }).ToList();

        // Hiệu quả khuyến mãi tóm tắt
        var promoReport = await GetPromotionEffectivenessReportAsync(start.LocalDateTime.Date, end.LocalDateTime.Date);
        model.TopKhuyenMaiHieuQua = promoReport.DanhSachChuongTrinh.Take(5).ToList();

        return model;
    }

    public async Task<BaoCaoDoanhThuViewModel> GetRevenueReportAsync(string cheDo, DateTime? tuNgay, DateTime? denNgay, int? nam)
    {
        cheDo = string.IsNullOrWhiteSpace(cheDo) ? "TheoNgay" : cheDo;
        var currentYear = nam ?? DateTime.Today.Year;
        var start = tuNgay ?? DateTime.Today.AddDays(-30);
        var end = denNgay ?? DateTime.Today;

        DateTimeOffset startOffset;
        DateTimeOffset endOffset;

        if (cheDo == "TheoThang")
        {
            startOffset = new DateTimeOffset(new DateTime(currentYear, 1, 1), TimeSpan.Zero);
            endOffset = new DateTimeOffset(new DateTime(currentYear, 12, 31, 23, 59, 59), TimeSpan.Zero);
        }
        else if (cheDo == "TheoNam")
        {
            startOffset = new DateTimeOffset(new DateTime(currentYear - 4, 1, 1), TimeSpan.Zero);
            endOffset = new DateTimeOffset(new DateTime(currentYear, 12, 31, 23, 59, 59), TimeSpan.Zero);
        }
        else
        {
            startOffset = new DateTimeOffset(start.Date.ToUniversalTime(), TimeSpan.Zero);
            endOffset = new DateTimeOffset(end.Date.AddDays(1).AddTicks(-1).ToUniversalTime(), TimeSpan.Zero);
        }

        var invoices = await db.HoaDon.AsNoTracking()
            .Where(x => x.ThoiDiemLap >= startOffset && x.ThoiDiemLap <= endOffset)
            .ToListAsync();

        var paidInvoices = invoices.Where(x => x.TrangThai == TrangThaiHoaDon.DaThanhToan).ToList();
        var cancelledInvoices = invoices.Where(x => x.TrangThai == TrangThaiHoaDon.DaHuy).ToList();

        var model = new BaoCaoDoanhThuViewModel
        {
            CheDo = cheDo,
            TuNgay = start,
            DenNgay = end,
            Nam = currentYear,
            TongDoanhThu = paidInvoices.Sum(x => x.TongThanhToan),
            TongTienHang = paidInvoices.Sum(x => x.TongTienHang),
            TongTienGiam = paidInvoices.Sum(x => x.TienGiam),
            TongTienCocDaTru = paidInvoices.Sum(x => x.TienCocDaTru),
            TongSoDonHang = invoices.Count,
            SoDonThanhCong = paidInvoices.Count,
            SoDonDaHuy = cancelledInvoices.Count,
            DoanhThuTienMat = paidInvoices.Where(x => x.PhuongThucThanhToan == PhuongThucThanhToan.TienMat).Sum(x => x.TongThanhToan),
            DoanhThuChuyenKhoan = paidInvoices.Where(x => x.PhuongThucThanhToan == PhuongThucThanhToan.ChuyenKhoan).Sum(x => x.TongThanhToan),
            DoanhThuThe = paidInvoices.Where(x => x.PhuongThucThanhToan == PhuongThucThanhToan.The).Sum(x => x.TongThanhToan),
            DoanhThuTaiBan = paidInvoices.Where(x => x.LoaiDonHang == LoaiDonHang.TaiBan).Sum(x => x.TongThanhToan),
            DoanhThuMangDi = paidInvoices.Where(x => x.LoaiDonHang == LoaiDonHang.MangDi).Sum(x => x.TongThanhToan),
            DoanhThuGiaoHang = paidInvoices.Where(x => x.LoaiDonHang == LoaiDonHang.GiaoHang).Sum(x => x.TongThanhToan)
        };

        // Nhóm timeline theo chế độ
        if (cheDo == "TheoThang")
        {
            for (int m = 1; m <= 12; m++)
            {
                var monthPaid = paidInvoices.Where(x => x.ThoiDiemLap.LocalDateTime.Year == currentYear && x.ThoiDiemLap.LocalDateTime.Month == m).ToList();
                var monthCancelled = cancelledInvoices.Where(x => x.ThoiDiemLap.LocalDateTime.Year == currentYear && x.ThoiDiemLap.LocalDateTime.Month == m).Count();

                model.ChiTietTimeline.Add(new DoanhThuTimelineRow
                {
                    NhanThoiGian = $"Tháng {m:D2}/{currentYear}",
                    SoDonThanhCong = monthPaid.Count,
                    SoDonHuy = monthCancelled,
                    TongTienHang = monthPaid.Sum(x => x.TongTienHang),
                    TienGiam = monthPaid.Sum(x => x.TienGiam),
                    DoanhThuThuc = monthPaid.Sum(x => x.TongThanhToan),
                    TienMat = monthPaid.Where(x => x.PhuongThucThanhToan == PhuongThucThanhToan.TienMat).Sum(x => x.TongThanhToan),
                    ChuyenKhoan = monthPaid.Where(x => x.PhuongThucThanhToan == PhuongThucThanhToan.ChuyenKhoan).Sum(x => x.TongThanhToan),
                    The = monthPaid.Where(x => x.PhuongThucThanhToan == PhuongThucThanhToan.The).Sum(x => x.TongThanhToan)
                });
            }
        }
        else if (cheDo == "TheoNam")
        {
            for (int y = currentYear - 4; y <= currentYear; y++)
            {
                var yearPaid = paidInvoices.Where(x => x.ThoiDiemLap.LocalDateTime.Year == y).ToList();
                var yearCancelled = cancelledInvoices.Where(x => x.ThoiDiemLap.LocalDateTime.Year == y).Count();

                model.ChiTietTimeline.Add(new DoanhThuTimelineRow
                {
                    NhanThoiGian = $"Năm {y}",
                    SoDonThanhCong = yearPaid.Count,
                    SoDonHuy = yearCancelled,
                    TongTienHang = yearPaid.Sum(x => x.TongTienHang),
                    TienGiam = yearPaid.Sum(x => x.TienGiam),
                    DoanhThuThuc = yearPaid.Sum(x => x.TongThanhToan),
                    TienMat = yearPaid.Where(x => x.PhuongThucThanhToan == PhuongThucThanhToan.TienMat).Sum(x => x.TongThanhToan),
                    ChuyenKhoan = yearPaid.Where(x => x.PhuongThucThanhToan == PhuongThucThanhToan.ChuyenKhoan).Sum(x => x.TongThanhToan),
                    The = yearPaid.Where(x => x.PhuongThucThanhToan == PhuongThucThanhToan.The).Sum(x => x.TongThanhToan)
                });
            }
        }
        else
        {
            // Theo ngày
            var currentDay = start.Date;
            var maxDay = end.Date;
            while (currentDay <= maxDay)
            {
                var dayPaid = paidInvoices.Where(x => x.ThoiDiemLap.LocalDateTime.Date == currentDay).ToList();
                var dayCancelled = cancelledInvoices.Where(x => x.ThoiDiemLap.LocalDateTime.Date == currentDay).Count();

                model.ChiTietTimeline.Add(new DoanhThuTimelineRow
                {
                    NhanThoiGian = currentDay.ToString("dd/MM/yyyy"),
                    SoDonThanhCong = dayPaid.Count,
                    SoDonHuy = dayCancelled,
                    TongTienHang = dayPaid.Sum(x => x.TongTienHang),
                    TienGiam = dayPaid.Sum(x => x.TienGiam),
                    DoanhThuThuc = dayPaid.Sum(x => x.TongThanhToan),
                    TienMat = dayPaid.Where(x => x.PhuongThucThanhToan == PhuongThucThanhToan.TienMat).Sum(x => x.TongThanhToan),
                    ChuyenKhoan = dayPaid.Where(x => x.PhuongThucThanhToan == PhuongThucThanhToan.ChuyenKhoan).Sum(x => x.TongThanhToan),
                    The = dayPaid.Where(x => x.PhuongThucThanhToan == PhuongThucThanhToan.The).Sum(x => x.TongThanhToan)
                });

                currentDay = currentDay.AddDays(1);
            }
        }

        return model;
    }

    public async Task<BaoCaoMonBanChayViewModel> GetTopDishesReportAsync(DateTime? tuNgay, DateTime? denNgay, int? danhMucId, int topN = 10)
    {
        var start = tuNgay ?? DateTime.Today.AddDays(-30);
        var end = denNgay ?? DateTime.Today;

        var startOffset = new DateTimeOffset(start.Date.ToUniversalTime(), TimeSpan.Zero);
        var endOffset = new DateTimeOffset(end.Date.AddDays(1).AddTicks(-1).ToUniversalTime(), TimeSpan.Zero);

        var query = db.ChiTietHoaDon.AsNoTracking()
            .Include(x => x.HoaDon)
            .Include(x => x.MonAn).ThenInclude(m => m.DanhMuc)
            .Include(x => x.MonAnSize)
            .Where(x => x.HoaDon.TrangThai == TrangThaiHoaDon.DaThanhToan
                && x.TrangThai != TrangThaiCheBien.DaHuy
                && x.HoaDon.ThoiDiemLap >= startOffset
                && x.HoaDon.ThoiDiemLap <= endOffset);

        if (danhMucId.HasValue && danhMucId.Value > 0)
        {
            query = query.Where(x => x.MonAn.DanhMucId == danhMucId.Value);
        }

        var lines = await query.ToListAsync();

        var totalDishRevenue = lines.Sum(x => x.SoLuong * x.DonGia);
        var totalDishQuantity = lines.Sum(x => x.SoLuong);

        var grouped = lines.GroupBy(x => x.MonAnId)
            .Select(g =>
            {
                var first = g.First();
                var qty = g.Sum(x => x.SoLuong);
                var rev = g.Sum(x => x.SoLuong * x.DonGia);
                var mostCommonSize = g.GroupBy(x => x.TenSizeLucBan ?? x.MonAnSize?.TenSize ?? "Mặc định")
                    .OrderByDescending(sg => sg.Sum(x => x.SoLuong))
                    .Select(sg => sg.Key).FirstOrDefault() ?? "Mặc định";

                return new TopMonBanChayItem
                {
                    MonAnId = g.Key,
                    TenMon = first.TenMonLucBan.Length > 0 ? first.TenMonLucBan : first.MonAn.TenMon,
                    TenDanhMuc = first.MonAn?.DanhMuc?.TenDanhMuc ?? "Chung",
                    HinhAnh = first.MonAn?.HinhAnh,
                    SizePhoBien = mostCommonSize,
                    SoLuongDaBan = qty,
                    DoanhThuMangLai = rev,
                    TyTrongDoanhThu = totalDishRevenue > 0 ? Math.Round((rev / totalDishRevenue) * 100m, 2) : 0
                };
            })
            .OrderByDescending(x => x.SoLuongDaBan)
            .ThenByDescending(x => x.DoanhThuMangLai)
            .Take(topN)
            .ToList();

        // Cơ cấu danh mục
        var categoryGroups = lines.GroupBy(x => x.MonAn?.DanhMuc?.TenDanhMuc ?? "Chung")
            .Select(g =>
            {
                var rev = g.Sum(x => x.SoLuong * x.DonGia);
                return new CoCauDanhMucItem
                {
                    TenDanhMuc = g.Key,
                    SoLuongBan = g.Sum(x => x.SoLuong),
                    DoanhThu = rev,
                    TyTrong = totalDishRevenue > 0 ? Math.Round((rev / totalDishRevenue) * 100m, 2) : 0
                };
            })
            .OrderByDescending(x => x.DoanhThu)
            .ToList();

        var categories = await db.DanhMuc.AsNoTracking()
            .Where(x => x.DangSuDung)
            .OrderBy(x => x.TenDanhMuc)
            .Select(x => new DanhMucOptionItem { Id = x.Id, TenDanhMuc = x.TenDanhMuc })
            .ToListAsync();

        return new BaoCaoMonBanChayViewModel
        {
            TuNgay = start,
            DenNgay = end,
            DanhMucId = danhMucId,
            TopN = topN,
            TongDoanhThuMon = totalDishRevenue,
            TongSoLuongMonDaBan = totalDishQuantity,
            DanhMucOptions = categories,
            DanhSachMon = grouped,
            CoCauDanhMuc = categoryGroups
        };
    }

    public async Task<BaoCaoTonKhoTongHopViewModel> GetInventorySummaryReportAsync(DateTime? tuNgay, DateTime? denNgay)
    {
        var start = tuNgay ?? DateTime.Today.AddDays(-30);
        var end = denNgay ?? DateTime.Today;

        var startOffset = new DateTimeOffset(start.Date.ToUniversalTime(), TimeSpan.Zero);
        var endOffset = new DateTimeOffset(end.Date.AddDays(1).AddTicks(-1).ToUniversalTime(), TimeSpan.Zero);

        var ingredients = await db.NguyenLieu.AsNoTracking()
            .Where(x => x.DangSuDung)
            .OrderBy(x => x.DanhMuc).ThenBy(x => x.TenNguyenLieu)
            .ToListAsync();

        var importTotal = await db.PhieuNhap.AsNoTracking()
            .Where(x => x.TrangThai == TrangThaiPhieu.DaGhiSo && x.ThoiDiem >= startOffset && x.ThoiDiem <= endOffset)
            .SumAsync(x => x.TongTien);

        var exportTotal = await db.PhieuXuat.AsNoTracking()
            .Where(x => x.TrangThai == TrangThaiPhieu.DaGhiSo && x.ThoiDiem >= startOffset && x.ThoiDiem <= endOffset)
            .SumAsync(x => x.TongTien);

        var disposalTotal = await db.PhieuThanhLy.AsNoTracking()
            .Where(x => x.ThoiDiem >= startOffset && x.ThoiDiem <= endOffset)
            .SumAsync(x => x.TongTienThietHai);

        var currentTotalStockValue = ingredients.Sum(x => x.SoLuongTon * x.DonGia);
        var warnings = ingredients.Where(x => x.SoLuongTon <= x.NguongCanhBao).Select(x => new NguyenLieuCanhBaoItem
        {
            Id = x.Id,
            TenNguyenLieu = x.TenNguyenLieu,
            DonViTinh = x.DonViTinh,
            SoLuongTon = x.SoLuongTon,
            NguongCanhBao = x.NguongCanhBao,
            DonGia = x.DonGia
        }).ToList();

        var rows = ingredients.Select(x => new TonKhoChiTietRow
        {
            Id = x.Id,
            TenNguyenLieu = x.TenNguyenLieu,
            DonViTinh = x.DonViTinh,
            DanhMuc = x.DanhMuc,
            SoLuongTon = x.SoLuongTon,
            NguongCanhBao = x.NguongCanhBao,
            DonGia = x.DonGia
        }).ToList();

        return new BaoCaoTonKhoTongHopViewModel
        {
            TuNgay = start,
            DenNgay = end,
            TongGiaTriTonHienTai = currentTotalStockValue,
            TongGiaTriNhapTrongKy = importTotal,
            TongGiaTriXuatTrongKy = exportTotal,
            TongGiaTriThanhLyTrongKy = disposalTotal,
            SoNguyenLieuCanhBao = warnings.Count,
            DanhSachCanhBao = warnings,
            ChiTietTonKho = rows
        };
    }

    public async Task<BaoCaoHieuQuaKhuyenMaiViewModel> GetPromotionEffectivenessReportAsync(DateTime? tuNgay, DateTime? denNgay)
    {
        var start = tuNgay ?? DateTime.Today.AddDays(-30);
        var end = denNgay ?? DateTime.Today;

        var startOffset = new DateTimeOffset(start.Date.ToUniversalTime(), TimeSpan.Zero);
        var endOffset = new DateTimeOffset(end.Date.AddDays(1).AddTicks(-1).ToUniversalTime(), TimeSpan.Zero);

        // Lấy tất cả hóa đơn đã thanh toán trong kỳ
        var paidInvoices = await db.HoaDon.AsNoTracking()
            .Include(x => x.Voucher).ThenInclude(v => v!.KhuyenMai)
            .Where(x => x.TrangThai == TrangThaiHoaDon.DaThanhToan && x.ThoiDiemLap >= startOffset && x.ThoiDiemLap <= endOffset)
            .ToListAsync();

        var invoicesWithDiscount = paidInvoices.Where(x => x.TienGiam > 0).ToList();

        // Danh sách tất cả chương trình khuyến mãi
        var allPromos = await db.KhuyenMai.AsNoTracking().ToListAsync();

        var promoItems = new List<HieuQuaKhuyenMaiItem>();

        foreach (var p in allPromos)
        {
            // Hóa đơn được áp dụng qua Voucher của chương trình này
            var matchedInvoices = paidInvoices.Where(x => x.Voucher != null && x.Voucher.KhuyenMaiId == p.Id).ToList();

            // Nếu không dùng voucher (khuyến mãi tự động), tính các đơn được hưởng chiết khấu trong thời gian chương trình hiệu lực
            if (!p.CanVoucher && matchedInvoices.Count == 0)
            {
                matchedInvoices = paidInvoices.Where(x => x.VoucherId == null && x.TienGiam > 0
                    && x.ThoiDiemLap >= p.BatDau && x.ThoiDiemLap <= p.KetThuc).ToList();
            }

            var usageCount = matchedInvoices.Count;
            var discountGiven = matchedInvoices.Sum(x => x.TienGiam);
            var revenueGenerated = matchedInvoices.Sum(x => x.TongThanhToan);

            promoItems.Add(new HieuQuaKhuyenMaiItem
            {
                KhuyenMaiId = p.Id,
                TenChuongTrinh = p.TenChuongTrinh,
                PhamVi = p.PhamVi,
                KieuGiam = p.KieuGiam,
                GiaTri = p.GiaTri,
                CanVoucher = p.CanVoucher,
                SoLuotApDung = usageCount,
                TongTienGiam = discountGiven,
                DoanhThuMangLai = revenueGenerated
            });
        }

        var sortedItems = promoItems.OrderByDescending(x => x.SoLuotApDung).ThenByDescending(x => x.DoanhThuMangLai).ToList();

        return new BaoCaoHieuQuaKhuyenMaiViewModel
        {
            TuNgay = start,
            DenNgay = end,
            TongChiPhiKhuyenMai = invoicesWithDiscount.Sum(x => x.TienGiam),
            TongDoanhThuDonKhuyenMai = invoicesWithDiscount.Sum(x => x.TongThanhToan),
            SoDonApDungKhuyenMai = invoicesWithDiscount.Count,
            DanhSachChuongTrinh = sortedItems
        };
    }
}
