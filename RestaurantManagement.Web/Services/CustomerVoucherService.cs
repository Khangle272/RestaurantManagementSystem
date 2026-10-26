using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Services;

public record CustomerVoucherItem(int Id, string Ma, string ChuongTrinh, string UuDai, DateTimeOffset BatDau,
    DateTimeOffset KetThuc, bool DaLuu, bool CoTheDung, string TrangThai, int DiemCanDoi = 0);

public class CustomerVoucherService(RestaurantDbContext db, IPromotionService promotions)
{
    public async Task<List<CustomerVoucherItem>> ListAsync(int customerId, bool wallet = false)
    {
        var now = DateTimeOffset.UtcNow;
        var query = db.Voucher.AsNoTracking().Include(x => x.KhuyenMai).AsQueryable();
        var saved = await db.VoucherKhachHang.Where(x => x.KhachHangId == customerId).Select(x => x.VoucherId).ToHashSetAsync();
        if (wallet) query = query.Where(x => saved.Contains(x.Id));
        else query = query.Where(x => x.DangSuDung && x.KhuyenMai.DangSuDung && x.KhuyenMai.CanVoucher
            && x.KhuyenMai.BatDau <= now && now <= x.KhuyenMai.KetThuc
            && (x.BatDau == null || x.BatDau <= now) && (x.KetThuc == null || now <= x.KetThuc)
            && (x.KhachHangId == null || x.KhachHangId == customerId));
        var vouchers = await query.OrderByDescending(x => x.Id).ToListAsync();
        var ids = vouchers.Select(x => x.Id).ToList();
        var usage = await db.HoaDon.Where(x => x.VoucherId != null && ids.Contains(x.VoucherId.Value) && x.TrangThai != TrangThaiHoaDon.DaHuy)
            .GroupBy(x => x.VoucherId!.Value).Select(g => new { Id = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count);
        var mine = await db.HoaDon.Where(x => x.KhachHangId == customerId && x.VoucherId != null && x.TrangThai != TrangThaiHoaDon.DaHuy)
            .Select(x => x.VoucherId!.Value).ToHashSetAsync();
        var issued = await db.VoucherKhachHang.Where(x => ids.Contains(x.VoucherId)).GroupBy(x => x.VoucherId)
            .Select(g => new { Id = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count);
        var parents = vouchers.Select(x => x.KhuyenMaiId).Distinct().ToList();
        var programIssued = await db.VoucherKhachHang.Where(x => parents.Contains(x.Voucher.KhuyenMaiId)).GroupBy(x => x.Voucher.KhuyenMaiId)
            .Select(g => new { Id = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count);
        var direct = await VoucherInventory.DirectTickets(db).Where(x => ids.Contains(x.VoucherId!.Value))
            .GroupBy(x => x.VoucherId!.Value).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync();
        foreach (var group in direct) issued[group.Id] = issued.GetValueOrDefault(group.Id) + group.Count;
        var directParents = await VoucherInventory.DirectTickets(db).Where(x => parents.Contains(x.Voucher!.KhuyenMaiId))
            .GroupBy(x => x.Voucher!.KhuyenMaiId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync();
        foreach (var group in directParents) programIssued[group.Id] = programIssued.GetValueOrDefault(group.Id) + group.Count;
        return vouchers.Select(v => {
            var p = v.KhuyenMai;
            var status = !v.DangSuDung || !p.DangSuDung || !p.CanVoucher ? "Ngừng sử dụng"
                : v.KhachHangId.HasValue && v.KhachHangId != customerId ? "Không dành cho tài khoản này"
                : !PromotionTime.IsActive(p.BatDau, p.KetThuc, now) || !PromotionTime.IsActive(v.BatDau ?? p.BatDau, v.KetThuc ?? p.KetThuc, now) ? "Ngoài thời hạn"
                : mine.Contains(v.Id) ? "Đã dùng / đang áp dụng"
                : v.GioiHanTongLuot.HasValue && usage.GetValueOrDefault(v.Id) >= v.GioiHanTongLuot ? "Hết lượt dùng"
                : !saved.Contains(v.Id) && ((v.GioiHanPhatHanh.HasValue && issued.GetValueOrDefault(v.Id) >= v.GioiHanPhatHanh)
                    || (p.GioiHanPhatHanh.HasValue && programIssued.GetValueOrDefault(p.Id) >= p.GioiHanPhatHanh)) ? "Hết vé thu thập" : "Có thể dùng";
            var value = v.GiaTri ?? p.GiaTri; var type = v.KieuGiam ?? p.KieuGiam; var scope = v.PhamVi ?? p.PhamVi;
            var cap = v.GiaTri.HasValue ? v.MucGiamToiDa : p.MucGiamToiDa;
            var minimum = v.GiaTri.HasValue ? v.GiaTriToiThieu : p.GiaTriToiThieu;
            var description = $"Giảm {value:N0}{(type == KieuGiam.PhanTram ? "%" : "đ")} {(scope == PhamViUuDai.MonAn ? "món áp dụng" : "hóa đơn")}"
                + (cap.HasValue ? $" · Tối đa {cap:N0}đ" : "") + (minimum > 0 ? $" · Đơn từ {minimum:N0}đ" : "");
            var available = status == "Có thể dùng";
            if (available && v.DiemCanDoi > 0 && !saved.Contains(v.Id)) status = "Có thể đổi điểm";
            return new CustomerVoucherItem(v.Id, v.Ma, p.TenChuongTrinh, description,
                v.BatDau ?? p.BatDau, v.KetThuc ?? p.KetThuc, saved.Contains(v.Id), available, status, v.DiemCanDoi);
        }).ToList();
    }

    public Task<string?> CollectAsync(int customerId, int voucherId) => AcquireAsync(customerId, voucherId, null);

    public Task<string?> RedeemAsync(int customerId, int voucherId, int expectedPoints) => AcquireAsync(customerId, voucherId, expectedPoints);

    private async Task<string?> AcquireAsync(int customerId, int voucherId, int? expectedPoints)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try {
            await promotions.LockVoucherAsync(voucherId);
            var v = await db.Voucher.AsNoTracking().Include(x => x.KhuyenMai).SingleOrDefaultAsync(x => x.Id == voucherId);
            if (v is null) return "Voucher không tồn tại.";
            // Serialize all redemptions for this customer, including different campaigns.
            var customer = await db.KhachHang.FromSqlInterpolated($"SELECT * FROM [KhachHang] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={customerId}")
                .AsNoTracking().SingleOrDefaultAsync();
            if (customer is null || !customer.DangSuDung) return "Khách hàng không hoạt động.";
            if (await db.VoucherKhachHang.AnyAsync(x => x.KhachHangId == customerId && x.VoucherId == voucherId)) return null;
            if (expectedPoints.HasValue) {
                if (v.DiemCanDoi <= 0 || expectedPoints != v.DiemCanDoi) return "Mức điểm đổi đã thay đổi. Vui lòng tải lại danh sách voucher.";
            }
            else if (v.DiemCanDoi > 0) return "Voucher này cần đổi điểm để nhận vào ví.";
            var now = DateTimeOffset.UtcNow; var p = v.KhuyenMai;
            if (!v.DangSuDung || !p.DangSuDung || !p.CanVoucher || !PromotionTime.IsActive(p.BatDau, p.KetThuc, now)
                || !PromotionTime.IsActive(v.BatDau ?? p.BatDau, v.KetThuc ?? p.KetThuc, now)) return "Voucher chưa bắt đầu, đã hết hạn hoặc ngừng sử dụng.";
            if (v.KhachHangId.HasValue && v.KhachHangId != customerId) return "Voucher dành riêng cho khách hàng khác.";
            if (await db.HoaDon.AnyAsync(x => x.KhachHangId == customerId && x.VoucherId == v.Id && x.TrangThai != TrangThaiHoaDon.DaHuy)) return "Bạn đã dùng hoặc đang áp dụng mã này.";
            if (v.GioiHanTongLuot.HasValue && await promotions.GetVoucherUsageCountAsync(v.Id) >= v.GioiHanTongLuot) return "Voucher đã hết lượt sử dụng.";
            if (v.GioiHanPhatHanh.HasValue && await VoucherInventory.IssuedAsync(db, v.Id) >= v.GioiHanPhatHanh) return "Mã đã hết vé thu thập.";
            if (p.GioiHanPhatHanh.HasValue && await VoucherInventory.ProgramIssuedAsync(db, p.Id) >= p.GioiHanPhatHanh) return "Chương trình đã hết vé thu thập.";
            if (v.DiemCanDoi > 0) {
                var balance = await new CustomerHistoryService(db).BalanceAsync(customerId);
                if (balance < v.DiemCanDoi) return $"Bạn chưa đủ điểm. Cần {v.DiemCanDoi:N0} điểm, hiện có {balance:N0} điểm.";
                db.LichSuTichDiem.Add(new LichSuTichDiem {
                    KhachHangId = customerId, VoucherId = v.Id, SoDiem = -v.DiemCanDoi,
                    ThoiDiem = now, NoiDung = $"Đổi điểm lấy voucher {v.Ma}"
                });
            }
            db.VoucherKhachHang.Add(new VoucherKhachHang { KhachHangId = customerId, VoucherId = v.Id, ThoiDiemThuThap = now });
            await db.SaveChangesAsync(); await tx.CommitAsync(); return null;
        }
        catch (Exception e) when (e is SqlException { Number: 1205 or 1222 } || e is DbUpdateException && e.GetBaseException() is SqlException { Number: 1205 or 1222 or 2601 or 2627 }) {
            return "Có thao tác thu thập / đổi điểm khác đang xử lý. Vui lòng tải lại ví và thử lại.";
        }
    }
}
