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
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
    private static DateTime Today => DateTimeOffset.UtcNow.ToOffset(VietnamOffset).Date;
    private static DateTimeOffset StartDay(DateTime day) => new(day.Date, VietnamOffset);
    private static DateTime PaymentDay(HoaDon invoice) => (invoice.ThoiDiemThanhToan ?? invoice.ThoiDiemLap).ToOffset(VietnamOffset).Date;
    private static decimal Revenue(HoaDon invoice) => invoice.TongTienHang - invoice.TienGiam;
    private Task<List<HoaDon>> PaidInvoices(DateTimeOffset start, DateTimeOffset end) => db.HoaDon.AsNoTracking()
        .Where(x => x.TrangThai == TrangThaiHoaDon.DaThanhToan
            && (x.ThoiDiemThanhToan ?? x.ThoiDiemLap) >= start && (x.ThoiDiemThanhToan ?? x.ThoiDiemLap) < end).ToListAsync();
    public async Task<BaoCaoDashboardViewModel> GetDashboardReportAsync(DateTime? tuNgay = null, DateTime? denNgay = null)
    {
        var start = StartDay(tuNgay ?? Today.AddDays(-30));
        var end = StartDay((denNgay ?? Today).AddDays(1));

        var invoices = await db.HoaDon.AsNoTracking()
            .Where(x => x.ThoiDiemLap >= start && x.ThoiDiemLap < end)
            .ToListAsync();

        var paidInvoices = await PaidInvoices(start, end);
        var cancelledInvoices = invoices.Where(x => x.TrangThai == TrangThaiHoaDon.DaHuy).ToList();

        var model = new BaoCaoDashboardViewModel
        {
            TuNgay = start,
            DenNgay = end.AddTicks(-1),
            TongDoanhThu = paidInvoices.Sum(Revenue),
            TongTienCocDaTru = paidInvoices.Sum(x => x.TienCocDaTru),
            SoHoaDonThieuNgayThanhToan = paidInvoices.Count(x => x.ThoiDiemThanhToan is null),
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
        var recentStart = Today.AddDays(-daysBack + 1);
        for (int i = 0; i < daysBack; i++)
        {
            var day = recentStart.AddDays(i);
            var nextDay = day.AddDays(1);
            var dayInvoices = paidInvoices.Where(x => PaymentDay(x) == day).ToList();

            model.DoanhThuTheoNgay.Add(new DoanhThuTimelineRow
            {
                NhanThoiGian = day.ToString("dd/MM"),
                SoDonThanhCong = dayInvoices.Count,
                DoanhThuThuc = dayInvoices.Sum(Revenue)
            });
        }

        // Top 5 món bán chạy
        var topDishes = await GetTopDishesReportAsync(start.Date, end.AddDays(-1).Date, null, 5);
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
        var promoReport = await GetPromotionEffectivenessReportAsync(start.Date, end.AddDays(-1).Date);
        model.TongChiPhiKhuyenMai = promoReport.TongChiPhiKhuyenMai;
        model.TopKhuyenMaiHieuQua = promoReport.DanhSachChuongTrinh.Take(5).ToList();

        return model;
    }

    public async Task<BaoCaoDoanhThuViewModel> GetRevenueReportAsync(string cheDo, DateTime? tuNgay, DateTime? denNgay, int? nam)
    {
        cheDo = string.IsNullOrWhiteSpace(cheDo) ? "TheoNgay" : cheDo;
        var currentYear = Math.Clamp(nam ?? Today.Year, 5, 9998);
        var start = tuNgay ?? Today.AddDays(-30);
        var end = denNgay ?? Today;

        DateTimeOffset startOffset;
        DateTimeOffset endOffset;

        if (cheDo == "TheoThang")
        {
            startOffset = StartDay(new DateTime(currentYear, 1, 1));
            endOffset = StartDay(new DateTime(currentYear + 1, 1, 1));
        }
        else if (cheDo == "TheoNam")
        {
            startOffset = StartDay(new DateTime(currentYear - 4, 1, 1));
            endOffset = StartDay(new DateTime(currentYear + 1, 1, 1));
        }
        else
        {
            startOffset = StartDay(start);
            endOffset = StartDay(end.AddDays(1));
        }

        var invoices = await db.HoaDon.AsNoTracking()
            .Where(x => x.ThoiDiemLap >= startOffset && x.ThoiDiemLap < endOffset)
            .ToListAsync();

        var paidInvoices = await PaidInvoices(startOffset, endOffset);
        var cancelledInvoices = invoices.Where(x => x.TrangThai == TrangThaiHoaDon.DaHuy).ToList();

        var model = new BaoCaoDoanhThuViewModel
        {
            CheDo = cheDo,
            TuNgay = start,
            DenNgay = end,
            Nam = currentYear,
            TongDoanhThu = paidInvoices.Sum(Revenue),
            SoHoaDonThieuNgayThanhToan = paidInvoices.Count(x => x.ThoiDiemThanhToan is null),
            TongTienHang = paidInvoices.Sum(x => x.TongTienHang),
            TongTienGiam = paidInvoices.Sum(x => x.TienGiam),
            TongTienCocDaTru = paidInvoices.Sum(x => x.TienCocDaTru),
            TongSoDonHang = invoices.Count,
            SoDonThanhCong = paidInvoices.Count,
            SoDonDaHuy = cancelledInvoices.Count,
            DoanhThuTienMat = paidInvoices.Where(x => x.PhuongThucThanhToan == PhuongThucThanhToan.TienMat).Sum(x => x.TongThanhToan),
            DoanhThuChuyenKhoan = paidInvoices.Where(x => x.PhuongThucThanhToan == PhuongThucThanhToan.ChuyenKhoan).Sum(x => x.TongThanhToan),
            DoanhThuThe = paidInvoices.Where(x => x.PhuongThucThanhToan == PhuongThucThanhToan.The).Sum(x => x.TongThanhToan),
            DoanhThuTaiBan = paidInvoices.Where(x => x.LoaiDonHang == LoaiDonHang.TaiBan).Sum(Revenue),
            DoanhThuMangDi = paidInvoices.Where(x => x.LoaiDonHang == LoaiDonHang.MangDi).Sum(Revenue),
            DoanhThuGiaoHang = paidInvoices.Where(x => x.LoaiDonHang == LoaiDonHang.GiaoHang).Sum(Revenue)
        };

        // Nhóm timeline theo chế độ
        if (cheDo == "TheoThang")
        {
            for (int m = 1; m <= 12; m++)
            {
                var monthPaid = paidInvoices.Where(x => PaymentDay(x).Year == currentYear && PaymentDay(x).Month == m).ToList();
                var monthCancelled = cancelledInvoices.Count(x => x.ThoiDiemLap.ToOffset(VietnamOffset).Year == currentYear && x.ThoiDiemLap.ToOffset(VietnamOffset).Month == m);

                model.ChiTietTimeline.Add(new DoanhThuTimelineRow
                {
                    NhanThoiGian = $"Tháng {m:D2}/{currentYear}",
                    SoDonThanhCong = monthPaid.Count,
                    SoDonHuy = monthCancelled,
                    TongTienHang = monthPaid.Sum(x => x.TongTienHang),
                    TienGiam = monthPaid.Sum(x => x.TienGiam),
                    DoanhThuThuc = monthPaid.Sum(Revenue),
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
                var yearPaid = paidInvoices.Where(x => PaymentDay(x).Year == y).ToList();
                var yearCancelled = cancelledInvoices.Count(x => x.ThoiDiemLap.ToOffset(VietnamOffset).Year == y);

                model.ChiTietTimeline.Add(new DoanhThuTimelineRow
                {
                    NhanThoiGian = $"Năm {y}",
                    SoDonThanhCong = yearPaid.Count,
                    SoDonHuy = yearCancelled,
                    TongTienHang = yearPaid.Sum(x => x.TongTienHang),
                    TienGiam = yearPaid.Sum(x => x.TienGiam),
                    DoanhThuThuc = yearPaid.Sum(Revenue),
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
                var dayPaid = paidInvoices.Where(x => PaymentDay(x) == currentDay).ToList();
                var dayCancelled = cancelledInvoices.Count(x => x.ThoiDiemLap.ToOffset(VietnamOffset).Date == currentDay);

                model.ChiTietTimeline.Add(new DoanhThuTimelineRow
                {
                    NhanThoiGian = currentDay.ToString("dd/MM/yyyy"),
                    SoDonThanhCong = dayPaid.Count,
                    SoDonHuy = dayCancelled,
                    TongTienHang = dayPaid.Sum(x => x.TongTienHang),
                    TienGiam = dayPaid.Sum(x => x.TienGiam),
                    DoanhThuThuc = dayPaid.Sum(Revenue),
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
        var start = tuNgay ?? Today.AddDays(-30);
        var end = denNgay ?? Today;

        var startOffset = StartDay(start);
        var endOffset = StartDay(end.AddDays(1));

        var query = db.ChiTietHoaDon.AsNoTracking()
            .Include(x => x.HoaDon)
            .Include(x => x.MonAn).ThenInclude(m => m.DanhMuc)
            .Include(x => x.MonAnSize)
            .Where(x => x.HoaDon.TrangThai == TrangThaiHoaDon.DaThanhToan
                && x.TrangThai != TrangThaiCheBien.DaHuy
                && (x.HoaDon.ThoiDiemThanhToan ?? x.HoaDon.ThoiDiemLap) >= startOffset
                && (x.HoaDon.ThoiDiemThanhToan ?? x.HoaDon.ThoiDiemLap) < endOffset);

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
        var start = tuNgay ?? Today.AddDays(-30);
        var end = denNgay ?? Today;

        var startOffset = StartDay(start);
        var endOffset = StartDay(end.AddDays(1));

        var ingredients = await db.NguyenLieu.AsNoTracking()
            .Where(x => x.DangSuDung)
            .OrderBy(x => x.DanhMuc).ThenBy(x => x.TenNguyenLieu)
            .ToListAsync();

        var importTotal = await db.PhieuNhap.AsNoTracking()
            .Where(x => x.TrangThai == TrangThaiPhieu.DaGhiSo && x.ThoiDiem >= startOffset && x.ThoiDiem < endOffset)
            .SumAsync(x => x.TongTien);

        var exportTotal = await db.PhieuXuat.AsNoTracking()
            .Where(x => x.TrangThai == TrangThaiPhieu.DaGhiSo && x.ThoiDiem >= startOffset && x.ThoiDiem < endOffset)
            .SumAsync(x => x.TongTien);

        var disposalTotal = await db.PhieuThanhLy.AsNoTracking()
            .Where(x => x.ThoiDiem >= startOffset && x.ThoiDiem < endOffset)
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
        var start = tuNgay ?? Today.AddDays(-30);
        var end = denNgay ?? Today;
        var paidInvoices = await PaidInvoices(StartDay(start), StartDay(end.AddDays(1)));
        var histories = paidInvoices.Select(x => new { Invoice = x, History = PromotionService.ReadSnapshot(x) }).ToList();
        var usages = histories.Where(x => x.History is not null)
            .SelectMany(x => x.History!.UuDai.Select(p => new { x.Invoice, Promotion = p })).ToList();
        // Group historical versions; a later rename/rate change must not rewrite prior results.
        var promoItems = usages.GroupBy(x => new { x.Promotion.KhuyenMaiId, x.Promotion.TenChuongTrinh,
                x.Promotion.PhamVi, x.Promotion.KieuGiam, x.Promotion.GiaTri, x.Promotion.TuVoucher })
            .Select(g => new HieuQuaKhuyenMaiItem
            {
                KhuyenMaiId = g.Key.KhuyenMaiId, TenChuongTrinh = g.Key.TenChuongTrinh,
                PhamVi = g.Key.PhamVi, KieuGiam = g.Key.KieuGiam, GiaTri = g.Key.GiaTri, CanVoucher = g.Key.TuVoucher,
                SoLuotApDung = g.Select(x => x.Invoice.Id).Distinct().Count(),
                TongTienGiam = g.Sum(x => x.Promotion.SoTienGiam),
                DoanhThuMangLai = g.Select(x => x.Invoice).DistinctBy(x => x.Id).Sum(Revenue)
            }).ToList();
        var usedIds = promoItems.Select(x => x.KhuyenMaiId).ToHashSet();
        var unused = await db.KhuyenMai.AsNoTracking().Where(x => !usedIds.Contains(x.Id)).ToListAsync();
        promoItems.AddRange(unused.Select(p => new HieuQuaKhuyenMaiItem { KhuyenMaiId = p.Id,
            TenChuongTrinh = p.TenChuongTrinh, PhamVi = p.PhamVi, KieuGiam = p.KieuGiam,
            GiaTri = p.GiaTri, CanVoucher = p.CanVoucher }));
        var withPromotion = histories.Where(x => x.History?.UuDai.Count > 0).ToList();

        return new BaoCaoHieuQuaKhuyenMaiViewModel
        {
            TuNgay = start,
            DenNgay = end,
            TongChiPhiKhuyenMai = usages.Sum(x => x.Promotion.SoTienGiam),
            TongGiamThuCong = histories.Sum(x => x.History?.TienGiamThuCong ?? 0),
            SoHoaDonThieuLichSu = histories.Count(x => x.History is null && x.Invoice.TienGiam > 0),
            TongDoanhThuDonKhuyenMai = withPromotion.Sum(x => Revenue(x.Invoice)),
            SoDonApDungKhuyenMai = withPromotion.Count,
            DanhSachChuongTrinh = promoItems.OrderByDescending(x => x.SoLuotApDung).ThenByDescending(x => x.DoanhThuMangLai).ToList()
        };
    }
}
