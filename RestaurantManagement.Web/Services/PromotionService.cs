using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Services;

public class PromotionEvaluationResult
{
    public decimal TongTienHang { get; set; }
    public decimal TongTienGiam { get; set; }
    public decimal TienGiamThuCong { get; set; }
    public bool ThieuLichSu { get; set; }
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

public class InvoiceDiscountSnapshot
{
    public int Version { get; set; } = 1;
    public DateTimeOffset ThoiDiem { get; set; }
    public decimal TienGiamThuCong { get; set; }
    public List<AppliedPromotionDetail> UuDai { get; set; } = [];
}

public class IneligiblePromotionDetail
{
    public string TenChuongTrinh { get; set; } = "";
    public string LyDoKhongDuDieuKien { get; set; } = "";
}

public interface IPromotionService
{
    Task<PromotionEvaluationResult> EvaluatePromotionsAsync(int hoaDonId, string? voucherCode = null, decimal? manualDiscount = null);
    Task<(bool Success, string? Error, PromotionEvaluationResult? Result)> ApplyVoucherToInvoiceAsync(int hoaDonId, string voucherCode, string? rowVersion = null);
    Task<(bool Success, string? Error)> RemoveVoucherFromInvoiceAsync(int hoaDonId, string? rowVersion = null);
    Task<int> GetVoucherUsageCountAsync(int voucherId);
    Task LockVoucherAsync(int? voucherId);
}

public class PromotionService(RestaurantDbContext db, TimeProvider? timeProvider = null) : IPromotionService
{
    public static InvoiceDiscountSnapshot? ReadSnapshot(HoaDon invoice)
    {
        if (string.IsNullOrWhiteSpace(invoice.UuDaiSnapshot)) return null;
        try
        {
            var snapshot = JsonSerializer.Deserialize<InvoiceDiscountSnapshot>(invoice.UuDaiSnapshot);
            if (snapshot is null || snapshot.Version != 1 || snapshot.UuDai is null
                || snapshot.TienGiamThuCong < 0 || snapshot.UuDai.Any(x => x.SoTienGiam <= 0)
                || (invoice.TrangThai != TrangThaiHoaDon.ChuaThanhToan
                    && snapshot.TienGiamThuCong + snapshot.UuDai.Sum(x => x.SoTienGiam) != invoice.TienGiam)) return null;
            return snapshot;
        }
        catch (JsonException) { return null; }
    }

    public static decimal ManualDiscount(HoaDon invoice) => ReadSnapshot(invoice)?.TienGiamThuCong
        ?? (invoice.VoucherId is null ? invoice.TienGiam : 0);

    public static string CaptureSnapshot(PromotionEvaluationResult result) => JsonSerializer.Serialize(new InvoiceDiscountSnapshot
    {
        ThoiDiem = DateTimeOffset.UtcNow, TienGiamThuCong = result.TienGiamThuCong, UuDai = result.AppliedPromotions
    });

    public async Task LockVoucherAsync(int? voucherId)
    {
        if (voucherId is null) return;
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Voucher phải được kiểm tra trong transaction.");
        // Always lock parent before child: collection, edits, reservation and payment share this order.
        var parentId = await db.Voucher.FromSqlInterpolated($"SELECT * FROM [Voucher] WITH (READUNCOMMITTED) WHERE [Id]={voucherId.Value}")
            .Select(x => x.KhuyenMaiId).SingleOrDefaultAsync();
        await db.KhuyenMai.FromSqlInterpolated($"SELECT * FROM [KhuyenMai] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {parentId}")
            .AsNoTracking().SingleOrDefaultAsync();
        await db.Voucher.FromSqlInterpolated($"SELECT * FROM [Voucher] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {voucherId.Value}")
            .AsNoTracking().SingleOrDefaultAsync();
    }

    public async Task<int> GetVoucherUsageCountAsync(int voucherId)
    {
        return await db.HoaDon.AsNoTracking()
            .CountAsync(x => x.VoucherId == voucherId && x.TrangThai != TrangThaiHoaDon.DaHuy);
    }

    public async Task<PromotionEvaluationResult> EvaluatePromotionsAsync(int hoaDonId, string? voucherCode = null, decimal? manualDiscount = null)
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

        if (invoice.TrangThai != TrangThaiHoaDon.ChuaThanhToan)
        {
            var history = ReadSnapshot(invoice);
            result.TongTienHang = invoice.TongTienHang;
            result.TongTienGiam = invoice.TienGiam;
            result.TongThanhToanSauGiam = invoice.TongTienHang - invoice.TienGiam;
            result.TienGiamThuCong = history?.TienGiamThuCong ?? 0;
            result.AppliedPromotions = history?.UuDai ?? [];
            result.ThieuLichSu = history is null && invoice.TienGiam > 0;
            return result;
        }
        result.TienGiamThuCong = manualDiscount ?? ManualDiscount(invoice);

        var activeItems = invoice.ChiTiet.Where(x => x.TrangThai != TrangThaiCheBien.DaHuy).ToList();
        var totalAmount = activeItems.Sum(x => x.SoLuong * x.DonGia);
        result.TongTienHang = totalAmount;

        if (totalAmount <= 0)
        {
            result.TongTienGiam = 0;
            result.TongThanhToanSauGiam = 0;
            return result;
        }

        var now = (timeProvider ?? TimeProvider.System).GetUtcNow();

        // 1. Tải các chương trình khuyến mãi tự động đang hiệu lực (CanVoucher = false)
        var autoPromotions = await db.KhuyenMai.AsNoTracking()
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
            else if (!voucher.DangSuDung || !voucher.KhuyenMai.DangSuDung || !voucher.KhuyenMai.CanVoucher)
            {
                result.IneligiblePromotions.Add(new IneligiblePromotionDetail
                {
                    TenChuongTrinh = $"Mã '{voucher.Ma}'",
                    LyDoKhongDuDieuKien = "Voucher hoặc chương trình đã ngừng sử dụng, hoặc chương trình không dành cho voucher."
                });
            }
            else
            {
                var usageCount = await GetVoucherUsageCountAsync(voucher.Id);
                // Nếu hóa đơn hiện tại đã gắn voucher này rồi thì không tính trùng lượt của chính nó
                if (invoice.VoucherId == voucher.Id) usageCount = Math.Max(0, usageCount - 1);

                if (voucher.GioiHanTongLuot.HasValue && usageCount >= voucher.GioiHanTongLuot.Value)
                {
                    result.IneligiblePromotions.Add(new IneligiblePromotionDetail
                    {
                        TenChuongTrinh = $"Voucher {voucher.Ma}",
                        LyDoKhongDuDieuKien = $"Voucher đã hết lượt sử dụng ({usageCount}/{voucher.GioiHanTongLuot})."
                    });
                }
                else if (!PromotionTime.IsActive(voucher.KhuyenMai.BatDau, voucher.KhuyenMai.KetThuc, now)
                    || !PromotionTime.IsActive(voucher.BatDau ?? voucher.KhuyenMai.BatDau, voucher.KetThuc ?? voucher.KhuyenMai.KetThuc, now))
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
                else if (voucher.DiemCanDoi > 0 && (!invoice.KhachHangId.HasValue
                    || !await db.VoucherKhachHang.AnyAsync(x => x.VoucherId == voucher.Id && x.KhachHangId == invoice.KhachHangId)))
                {
                    result.IneligiblePromotions.Add(new IneligiblePromotionDetail {
                        TenChuongTrinh = $"Voucher {voucher.Ma}", LyDoKhongDuDieuKien = "Khách hàng cần đổi điểm để nhận voucher này vào ví trước khi sử dụng." });
                }
                else if (await VoucherInventory.NoCapacityAsync(db, voucher, invoice))
                {
                    result.IneligiblePromotions.Add(new IneligiblePromotionDetail {
                        TenChuongTrinh = $"Voucher {voucher.Ma}", LyDoKhongDuDieuKien = "Voucher hoặc chương trình đã hết số vé phát hành." });
                }
                else if (invoice.KhachHangId.HasValue
                    && await db.VoucherKhachHang.AnyAsync(x => x.VoucherId == voucher.Id && x.KhachHangId == invoice.KhachHangId)
                    && await db.HoaDon.AnyAsync(x => x.Id != invoice.Id && x.KhachHangId == invoice.KhachHangId
                        && x.VoucherId == voucher.Id && x.TrangThai != TrangThaiHoaDon.DaHuy))
                {
                    result.IneligiblePromotions.Add(new IneligiblePromotionDetail {
                        TenChuongTrinh = $"Voucher {voucher.Ma}", LyDoKhongDuDieuKien = "Vé trong ví đã sử dụng hoặc đang giữ trên hóa đơn khác." });
                }
                else
                {
                    var parent = voucher.KhuyenMai;
                    voucherPromotion = new KhuyenMai {
                        Id = parent.Id, TenChuongTrinh = parent.TenChuongTrinh,
                        PhamVi = voucher.PhamVi ?? parent.PhamVi, KieuGiam = voucher.KieuGiam ?? parent.KieuGiam,
                        GiaTri = voucher.GiaTri ?? parent.GiaTri,
                        MucGiamToiDa = voucher.GiaTri.HasValue ? voucher.MucGiamToiDa : parent.MucGiamToiDa,
                        GiaTriToiThieu = voucher.GiaTri.HasValue ? voucher.GiaTriToiThieu : parent.GiaTriToiThieu,
                        ChoPhepKetHop = parent.ChoPhepKetHop, ThuTuApDung = parent.ThuTuApDung
                    };
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

        if (voucher is not null && voucherPromotion?.PhamVi == PhamViUuDai.MonAn && voucher.GiaTri.HasValue)
            promoDishMap[voucherPromotion.Id] = await db.VoucherMon.Where(x => x.VoucherId == voucher.Id)
                .Select(x => x.MonAnId).ToHashSetAsync();

        // ĐÁNH GIÁ TỪNG KHUYẾN MÃI
        var calculatedDetails = new List<AppliedPromotionDetail>();

        foreach (var candidate in allCandidatePromotions)
        {
            var p = candidate.Promotion;
            decimal discountForThis = 0;
            string calcDescription = "";

            if (p.GiaTriToiThieu > 0 && totalAmount < p.GiaTriToiThieu)
            {
                result.IneligiblePromotions.Add(new IneligiblePromotionDetail
                {
                    TenChuongTrinh = p.TenChuongTrinh,
                    LyDoKhongDuDieuKien = $"Tiền món toàn hóa đơn ({totalAmount:N0} đ) chưa đạt tối thiểu ({p.GiaTriToiThieu:N0} đ)."
                });
                continue;
            }

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
            result.AppliedVoucherId = null;
            result.AppliedVoucherCode = null;
            result.TongTienGiam = 0;
            result.TongThanhToanSauGiam = totalAmount;
            FinishDiscount(result, totalAmount);
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

        if (!result.AppliedPromotions.Any(x => x.TuVoucher))
        {
            result.AppliedVoucherId = null;
            result.AppliedVoucherCode = null;
        }
        FinishDiscount(result, totalAmount);

        return result;
    }

    private static void FinishDiscount(PromotionEvaluationResult result, decimal total)
    {
        if (result.TienGiamThuCong < 0 || result.TienGiamThuCong > total - result.TongTienGiam)
        {
            result.ErrorMessage = "Giảm thủ công phải từ 0 đến phần tiền còn lại sau ưu đãi. Vui lòng điều chỉnh giảm thủ công.";
            return;
        }
        result.TongTienGiam += result.TienGiamThuCong;
        result.TongThanhToanSauGiam = total - result.TongTienGiam;
    }

    public Task<(bool Success, string? Error, PromotionEvaluationResult? Result)> ApplyVoucherToInvoiceAsync(int hoaDonId, string voucherCode, string? rowVersion = null)
    {
        if (string.IsNullOrWhiteSpace(voucherCode))
            return Task.FromResult<(bool, string?, PromotionEvaluationResult?)>((false, "Vui lòng nhập mã Voucher.", null));
        return ChangeVoucherAsync(hoaDonId, voucherCode.Trim(), rowVersion);
    }

    public async Task<(bool Success, string? Error)> RemoveVoucherFromInvoiceAsync(int hoaDonId, string? rowVersion = null)
    {
        var result = await ChangeVoucherAsync(hoaDonId, null, rowVersion);
        return (result.Success, result.Error);
    }

    private async Task<(bool Success, string? Error, PromotionEvaluationResult? Result)> ChangeVoucherAsync(int id, string? code, string? rowVersion)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var invoice = await db.HoaDon.FromSqlInterpolated($"SELECT * FROM [HoaDon] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}").SingleOrDefaultAsync();
            if (invoice is null) return (false, "Không tìm thấy hóa đơn.", null);
            if (invoice.TrangThai != TrangThaiHoaDon.ChuaThanhToan) return (false, "Chỉ thay đổi ưu đãi trên hóa đơn chưa thanh toán.", null);
            var expected = new byte[8];
            if (rowVersion is null || !Convert.TryFromBase64String(rowVersion, expected, out var length) || length != 8
                || !expected.SequenceEqual(db.Entry(invoice).Property<byte[]>("RowVersion").CurrentValue ?? []))
                return (false, "Hóa đơn đã thay đổi. Vui lòng tải lại và kiểm tra.", null);
            if (code is not null)
            {
                var voucherId = await db.Voucher.Where(x => x.Ma == code.ToUpper()).Select(x => (int?)x.Id).SingleOrDefaultAsync();
                await LockVoucherAsync(voucherId);
            }
            var result = await EvaluatePromotionsAsync(id, code);
            if (!result.IsSuccess) return (false, result.ErrorMessage, result);
            if (code is not null && result.AppliedVoucherId is null)
                return (false, string.Join("; ", result.IneligiblePromotions.Select(x => x.LyDoKhongDuDieuKien)) is { Length: > 0 } reason
                    ? reason : "Voucher không được áp dụng trong phương án ưu đãi này.", result);
            invoice.VoucherId = result.AppliedVoucherId;
            invoice.TienGiam = result.TongTienGiam;
            invoice.UuDaiSnapshot = CaptureSnapshot(result);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return (true, null, result);
        }
        catch (DbUpdateConcurrencyException)
        {
            return (false, "Hóa đơn đã thay đổi. Vui lòng tải lại.", null);
        }
        catch (SqlException e) when (e.Number is 1205 or 1222)
        { return (false, "Ưu đãi đang được xử lý bởi thao tác khác. Vui lòng tải lại và thử lại.", null); }
    }
}
