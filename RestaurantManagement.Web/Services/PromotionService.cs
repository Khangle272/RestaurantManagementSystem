using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Services;

public class PromotionEvaluationResult
{
    public decimal TongTienHang { get; set; }
    public decimal TongTienGiam { get; set; }
    public decimal TongThanhToanSauGiam { get; set; }
    public int? AppliedVoucherId { get; set; }
    public string? AppliedVoucherCode { get; set; }
    public List<AppliedPromotionDetail> AppliedPromotions { get; set; } = [];
    public List<IneligiblePromotionDetail> IneligiblePromotions { get; set; } = [];
    public string? ErrorMessage { get; set; }
    public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
}

public class AppliedPromotionDetail
{
    public int KhuyenMaiId { get; set; }
    public string TenChuongTrinh { get; set; } = "";
    public PhamViUuDai PhamVi { get; set; }
    public KieuGiam KieuGiam { get; set; }
    public decimal GiaTri { get; set; }
    public decimal SoTienGiam { get; set; }
    public string MoTaCachTinh { get; set; } = "";
    public bool TuVoucher { get; set; }
    public string? MaVoucher { get; set; }
}

public class IneligiblePromotionDetail
{
    public string TenChuongTrinh { get; set; } = "";
    public string LyDoKhongDuDieuKien { get; set; } = "";
}

public interface IPromotionService
{
    Task<PromotionEvaluationResult> EvaluatePromotionsAsync(int hoaDonId, string? voucherCode = null);
    Task<(bool Success, string? Error, PromotionEvaluationResult? Result)> ApplyVoucherToInvoiceAsync(int hoaDonId, string voucherCode);
    Task<(bool Success, string? Error)> RemoveVoucherFromInvoiceAsync(int hoaDonId);
    Task<int> GetVoucherUsageCountAsync(int voucherId);
}

public class PromotionService(RestaurantDbContext db) : IPromotionService
{
    public async Task<int> GetVoucherUsageCountAsync(int voucherId)
    {
        return await db.HoaDon.AsNoTracking()
            .CountAsync(x => x.VoucherId == voucherId && x.TrangThai != TrangThaiHoaDon.DaHuy);
    }

    public async Task<PromotionEvaluationResult> EvaluatePromotionsAsync(int hoaDonId, string? voucherCode = null)
    {
        var result = new PromotionEvaluationResult();

        var invoice = await db.HoaDon.AsNoTracking()
            .Include(x => x.ChiTiet.Where(c => c.TrangThai != TrangThaiCheBien.DaHuy))
            .SingleOrDefaultAsync(x => x.Id == hoaDonId);

        if (invoice == null)
        {
            result.ErrorMessage = "Không tìm thấy hóa đơn.";
            return result;
        }

        var activeItems = invoice.ChiTiet.Where(x => x.TrangThai != TrangThaiCheBien.DaHuy).ToList();
        var totalAmount = activeItems.Sum(x => x.SoLuong * x.DonGia);
        result.TongTienHang = totalAmount;

        if (totalAmount <= 0)
        {
            result.TongTienGiam = 0;
            result.TongThanhToanSauGiam = 0;
            return result;
        }

        var now = DateTimeOffset.UtcNow;

        // 1. Tải các chương trình khuyến mãi tự động đang hiệu lực (CanVoucher = false)
        var autoPromotions = await db.KhuyenMai.AsNoTracking()
            .Include(x => x.Id != 0 ? db.KhuyenMaiMon.Where(km => km.KhuyenMaiId == x.Id) : null)
            .Where(x => x.DangSuDung && !x.CanVoucher && x.BatDau <= now && x.KetThuc >= now)
            .OrderBy(x => x.ThuTuApDung)
            .ToListAsync();

        // 2. Tải Voucher nếu có mã được cung cấp hoặc đã lưu trên hóa đơn
        Voucher? voucher = null;
        KhuyenMai? voucherPromotion = null;
        var codeToUse = !string.IsNullOrWhiteSpace(voucherCode) ? voucherCode.Trim() : null;

        if (codeToUse != null)
        {
            voucher = await db.Voucher.AsNoTracking()
                .Include(x => x.KhuyenMai)
                .SingleOrDefaultAsync(x => x.Ma.ToUpper() == codeToUse.ToUpper());

            if (voucher == null)
            {
                result.IneligiblePromotions.Add(new IneligiblePromotionDetail
                {
                    TenChuongTrinh = $"Mã '{codeToUse}'",
                    LyDoKhongDuDieuKien = "Mã voucher không tồn tại."
                });
            }
            else if (!voucher.DangSuDung)
            {
                result.IneligiblePromotions.Add(new IneligiblePromotionDetail
                {
                    TenChuongTrinh = $"Mã '{voucher.Ma}'",
                    LyDoKhongDuDieuKien = "Voucher đã bị khóa hoặc ngừng sử dụng."
                });
            }
            else
            {
                var usageCount = await GetVoucherUsageCountAsync(voucher.Id);
                // Nếu hóa đơn hiện tại đã gắn voucher này rồi thì không tính trùng lượt của chính nó
                if (invoice.VoucherId == voucher.Id) usageCount = Math.Max(0, usageCount - 1);

                if (usageCount >= voucher.GioiHanTongLuot)
                {
                    result.IneligiblePromotions.Add(new IneligiblePromotionDetail
                    {
                        TenChuongTrinh = $"Voucher {voucher.Ma}",
                        LyDoKhongDuDieuKien = $"Voucher đã hết lượt sử dụng ({usageCount}/{voucher.GioiHanTongLuot})."
                    });
                }
                else if (now < voucher.KhuyenMai.BatDau || now > voucher.KhuyenMai.KetThuc)
                {
                    result.IneligiblePromotions.Add(new IneligiblePromotionDetail
                    {
                        TenChuongTrinh = $"Voucher {voucher.Ma} ({voucher.KhuyenMai.TenChuongTrinh})",
                        LyDoKhongDuDieuKien = "Chương trình ưu đãi của voucher đã hết hạn hoặc chưa bắt đầu."
                    });
                }
                else if (voucher.KhachHangId.HasValue && invoice.KhachHangId != voucher.KhachHangId)
                {
                    result.IneligiblePromotions.Add(new IneligiblePromotionDetail
                    {
                        TenChuongTrinh = $"Voucher {voucher.Ma}",
                        LyDoKhongDuDieuKien = "Voucher chỉ dành riêng cho khách hàng được chỉ định."
                    });
                }
                else
                {
                    voucherPromotion = voucher.KhuyenMai;
                    result.AppliedVoucherId = voucher.Id;
                    result.AppliedVoucherCode = voucher.Ma;
                }
            }
        }

        // Tập hợp tất cả ứng viên khuyến mãi
        var allCandidatePromotions = new List<(KhuyenMai Promotion, bool IsVoucher, string? VoucherCode)>();
        foreach (var auto in autoPromotions)
        {
            allCandidatePromotions.Add((auto, false, null));
        }
        if (voucherPromotion != null && voucher != null)
        {
            allCandidatePromotions.Add((voucherPromotion, true, voucher.Ma));
        }

        // Tải danh sách món ăn liên kết cho các khuyến mãi theo món
        var dishPromoIds = allCandidatePromotions
            .Where(x => x.Promotion.PhamVi == PhamViUuDai.MonAn)
            .Select(x => x.Promotion.Id)
            .Distinct().ToList();

        var dishLinks = await db.KhuyenMaiMon.AsNoTracking()
            .Where(x => dishPromoIds.Contains(x.KhuyenMaiId))
            .ToListAsync();

        var promoDishMap = dishLinks.GroupBy(x => x.KhuyenMaiId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.MonAnId).ToHashSet());

        // ĐÁNH GIÁ TỪNG KHUYẾN MÃI
        var calculatedDetails = new List<AppliedPromotionDetail>();

        foreach (var candidate in allCandidatePromotions)
        {
            var p = candidate.Promotion;
            decimal discountForThis = 0;
            string calcDescription = "";

            if (p.PhamVi == PhamViUuDai.MonAn)
            {
                // Khuyến mãi theo món ăn
                if (!promoDishMap.TryGetValue(p.Id, out var eligibleDishIds) || eligibleDishIds.Count == 0)
                {
                    result.IneligiblePromotions.Add(new IneligiblePromotionDetail
                    {
                        TenChuongTrinh = p.TenChuongTrinh,
                        LyDoKhongDuDieuKien = "Chương trình chưa được gán món ăn áp dụng."
                    });
                    continue;
                }

                var matchedLines = activeItems.Where(x => eligibleDishIds.Contains(x.MonAnId)).ToList();
                if (matchedLines.Count == 0)
                {
                    result.IneligiblePromotions.Add(new IneligiblePromotionDetail
                    {
                        TenChuongTrinh = p.TenChuongTrinh,
                        LyDoKhongDuDieuKien = "Đơn hàng không có món ăn thuộc danh mục ưu đãi này."
                    });
                    continue;
                }

                var matchedTotal = matchedLines.Sum(x => x.SoLuong * x.DonGia);
                if (p.GiaTriToiThieu > 0 && matchedTotal < p.GiaTriToiThieu)
                {
                    result.IneligiblePromotions.Add(new IneligiblePromotionDetail
                    {
                        TenChuongTrinh = p.TenChuongTrinh,
                        LyDoKhongDuDieuKien = $"Tổng tiền các món ưu đãi ({matchedTotal:N0} đ) chưa đạt mức tối thiểu ({p.GiaTriToiThieu:N0} đ)."
                    });
                    continue;
                }

                if (p.KieuGiam == KieuGiam.PhanTram)
                {
                    discountForThis = matchedTotal * (p.GiaTri / 100m);
                    if (p.MucGiamToiDa.HasValue && discountForThis > p.MucGiamToiDa.Value)
                    {
                        discountForThis = p.MucGiamToiDa.Value;
                        calcDescription = $"Giảm {p.GiaTri:0.##}% các món áp dụng (chạm trần tối đa {p.MucGiamToiDa.Value:N0} đ)";
                    }
                    else
                    {
                        calcDescription = $"Giảm {p.GiaTri:0.##}% trên tổng {matchedTotal:N0} đ món áp dụng";
                    }
                }
                else
                {
                    // Giảm số tiền cố định
                    discountForThis = Math.Min(p.GiaTri, matchedTotal);
                    calcDescription = $"Giảm cố định {p.GiaTri:N0} đ cho món áp dụng";
                }
            }
            else
            {
                // Khuyến mãi theo toàn bộ Hóa đơn
                if (p.GiaTriToiThieu > 0 && totalAmount < p.GiaTriToiThieu)
                {
                    result.IneligiblePromotions.Add(new IneligiblePromotionDetail
                    {
                        TenChuongTrinh = p.TenChuongTrinh,
                        LyDoKhongDuDieuKien = $"Tổng tiền hóa đơn ({totalAmount:N0} đ) chưa đạt giá trị tối thiểu ({p.GiaTriToiThieu:N0} đ)."
                    });
                    continue;
                }

                if (p.KieuGiam == KieuGiam.PhanTram)
                {
                    discountForThis = totalAmount * (p.GiaTri / 100m);
                    if (p.MucGiamToiDa.HasValue && discountForThis > p.MucGiamToiDa.Value)
                    {
                        discountForThis = p.MucGiamToiDa.Value;
                        calcDescription = $"Giảm {p.GiaTri:0.##}% hóa đơn (tối đa {p.MucGiamToiDa.Value:N0} đ)";
                    }
                    else
                    {
                        calcDescription = $"Giảm {p.GiaTri:0.##}% trên tổng hóa đơn {totalAmount:N0} đ";
                    }
                }
                else
                {
                    discountForThis = Math.Min(p.GiaTri, totalAmount);
                    calcDescription = $"Giảm trực tiếp {p.GiaTri:N0} đ vào hóa đơn";
                }
            }

            discountForThis = Math.Round(discountForThis, 0);
            if (discountForThis > 0)
            {
                calculatedDetails.Add(new AppliedPromotionDetail
                {
                    KhuyenMaiId = p.Id,
                    TenChuongTrinh = p.TenChuongTrinh,
                    PhamVi = p.PhamVi,
                    KieuGiam = p.KieuGiam,
                    GiaTri = p.GiaTri,
                    SoTienGiam = discountForThis,
                    MoTaCachTinh = calcDescription,
                    TuVoucher = candidate.IsVoucher,
                    MaVoucher = candidate.VoucherCode
                });
            }
        }

        // XỬ LÝ KẾT HỢP ĐỒNG THỜI (ChoPhepKetHop & ThuTuApDung)
        if (calculatedDetails.Count == 0)
        {
            result.TongTienGiam = 0;
            result.TongThanhToanSauGiam = totalAmount;
            return result;
        }

        // Lấy danh sách gốc để kiểm tra cờ ChoPhepKetHop của từng khuyến mãi
        var promoLookup = allCandidatePromotions.ToDictionary(x => x.Promotion.Id, x => x.Promotion);

        // Kiểm tra xem tất cả các ưu đãi có cho phép kết hợp hay không
        var allAllowCombine = calculatedDetails.All(x => promoLookup.TryGetValue(x.KhuyenMaiId, out var km) && km.ChoPhepKetHop);

        if (allAllowCombine)
        {
            // Trường hợp 1: Tất cả đều cho phép kết hợp -> Áp dụng đồng thời theo thứ tự ưu tiên
            var sortedDetails = calculatedDetails
                .OrderBy(x => promoLookup[x.KhuyenMaiId].ThuTuApDung)
                .ToList();

            decimal runningDiscount = 0;
            foreach (var item in sortedDetails)
            {
                // Đảm bảo không giảm quá tổng tiền hàng
                if (runningDiscount + item.SoTienGiam > totalAmount)
                {
                    item.SoTienGiam = totalAmount - runningDiscount;
                }
                runningDiscount += item.SoTienGiam;
                result.AppliedPromotions.Add(item);

                if (runningDiscount >= totalAmount) break;
            }

            result.TongTienGiam = runningDiscount;
        }
        else
        {
            // Trường hợp 2: Có ưu đãi không cho phép kết hợp
            // Ta so sánh:
            // Phương án A: Kết hợp tất cả các ưu đãi cho phép kết hợp
            var stackableItems = calculatedDetails
                .Where(x => promoLookup[x.KhuyenMaiId].ChoPhepKetHop)
                .OrderBy(x => promoLookup[x.KhuyenMaiId].ThuTuApDung)
                .ToList();

            var stackableTotal = Math.Min(totalAmount, stackableItems.Sum(x => x.SoTienGiam));

            // Phương án B: Lấy ưu đãi độc lập (không kết hợp) có mức giảm cao nhất
            var nonStackableItems = calculatedDetails
                .Where(x => !promoLookup[x.KhuyenMaiId].ChoPhepKetHop)
                .OrderByDescending(x => x.SoTienGiam)
                .ToList();

            var bestNonStackable = nonStackableItems.FirstOrDefault();

            if (bestNonStackable != null && bestNonStackable.SoTienGiam > stackableTotal)
            {
                // Chọn ưu đãi không kết hợp vì mang lại lợi ích lớn nhất cho khách
                result.AppliedPromotions.Add(bestNonStackable);
                result.TongTienGiam = Math.Min(totalAmount, bestNonStackable.SoTienGiam);

                // Ghi chú những khuyến mãi bị bỏ qua do không thể kết hợp
                foreach (var other in calculatedDetails.Where(x => x.KhuyenMaiId != bestNonStackable.KhuyenMaiId))
                {
                    result.IneligiblePromotions.Add(new IneligiblePromotionDetail
                    {
                        TenChuongTrinh = other.TenChuongTrinh,
                        LyDoKhongDuDieuKien = "Không thể kết hợp cùng chương trình ưu đãi độc lập khác."
                    });
                }
            }
            else
            {
                // Chọn phương án kết hợp
                decimal runningDiscount = 0;
                foreach (var item in stackableItems)
                {
                    if (runningDiscount + item.SoTienGiam > totalAmount)
                    {
                        item.SoTienGiam = totalAmount - runningDiscount;
                    }
                    runningDiscount += item.SoTienGiam;
                    result.AppliedPromotions.Add(item);
                    if (runningDiscount >= totalAmount) break;
                }

                result.TongTienGiam = runningDiscount;

                foreach (var nonStack in nonStackableItems)
                {
                    result.IneligiblePromotions.Add(new IneligiblePromotionDetail
                    {
                        TenChuongTrinh = nonStack.TenChuongTrinh,
                        LyDoKhongDuDieuKien = "Chương trình không cho phép áp dụng kết hợp với các ưu đãi khác."
                    });
                }
            }
        }

        // Chặn âm
        result.TongTienGiam = Math.Min(result.TongTienGiam, totalAmount);
        result.TongThanhToanSauGiam = Math.Max(0, totalAmount - result.TongTienGiam);

        return result;
    }

    public async Task<(bool Success, string? Error, PromotionEvaluationResult? Result)> ApplyVoucherToInvoiceAsync(int hoaDonId, string voucherCode)
    {
        if (string.IsNullOrWhiteSpace(voucherCode))
            return (false, "Vui lòng nhập mã Voucher.", null);

        var invoice = await db.HoaDon.SingleOrDefaultAsync(x => x.Id == hoaDonId);
        if (invoice == null)
            return (false, "Không tìm thấy hóa đơn.", null);

        if (invoice.TrangThai != TrangThaiHoaDon.ChuaThanhToan)
            return (false, "Chỉ có thể áp dụng Voucher cho hóa đơn chưa thanh toán.", null);

        var eval = await EvaluatePromotionsAsync(hoaDonId, voucherCode.Trim());
        if (!eval.IsSuccess)
            return (false, eval.ErrorMessage, eval);

        var appliedVoucher = eval.AppliedPromotions.FirstOrDefault(x => x.TuVoucher);
        if (appliedVoucher == null)
        {
            var reasons = string.Join("; ", eval.IneligiblePromotions.Select(x => $"{x.TenChuongTrinh}: {x.LyDoKhongDuDieuKien}"));
            return (false, string.IsNullOrEmpty(reasons) ? "Mã Voucher không đủ điều kiện áp dụng cho hóa đơn này." : reasons, eval);
        }

        invoice.VoucherId = eval.AppliedVoucherId;
        invoice.TienGiam = eval.TongTienGiam;

        try
        {
            await db.SaveChangesAsync();
            return (true, null, eval);
        }
        catch (DbUpdateConcurrencyException)
        {
            return (false, "Hóa đơn đã bị thay đổi bởi thao tác khác. Vui lòng tải lại trang.", null);
        }
    }

    public async Task<(bool Success, string? Error)> RemoveVoucherFromInvoiceAsync(int hoaDonId)
    {
        var invoice = await db.HoaDon.SingleOrDefaultAsync(x => x.Id == hoaDonId);
        if (invoice == null)
            return (false, "Không tìm thấy hóa đơn.");

        if (invoice.TrangThai != TrangThaiHoaDon.ChuaThanhToan)
            return (false, "Hóa đơn đã thanh toán hoặc đã hủy, không thể thay đổi.");

        invoice.VoucherId = null;

        // Tính toán lại tiền giảm từ các khuyến mãi tự động còn lại
        var eval = await EvaluatePromotionsAsync(hoaDonId, null);
        invoice.TienGiam = eval.TongTienGiam;

        try
        {
            await db.SaveChangesAsync();
            return (true, null);
        }
        catch (DbUpdateConcurrencyException)
        {
            return (false, "Hóa đơn đã bị thay đổi bởi thao tác khác. Vui lòng tải lại.");
        }
    }
}
