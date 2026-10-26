using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Services;

public static class VoucherInventory
{
    // A saved ticket is counted once. Direct entry without a wallet also occupies an issued
    // ticket while reserved/paid, so typing a code cannot bypass either issuance ceiling.
    public static IQueryable<HoaDon> DirectTickets(RestaurantDbContext db, int excludeInvoiceId = 0) =>
        db.HoaDon.Where(h => h.Id != excludeInvoiceId && h.VoucherId != null && h.TrangThai != TrangThaiHoaDon.DaHuy
            && !db.VoucherKhachHang.Any(w => w.VoucherId == h.VoucherId && w.KhachHangId == h.KhachHangId));

    public static async Task<int> IssuedAsync(RestaurantDbContext db, int voucherId, int excludeInvoiceId = 0) =>
        await db.VoucherKhachHang.CountAsync(x => x.VoucherId == voucherId)
        + await DirectTickets(db, excludeInvoiceId).CountAsync(x => x.VoucherId == voucherId);

    public static async Task<int> ProgramIssuedAsync(RestaurantDbContext db, int programId, int excludeInvoiceId = 0) =>
        await db.VoucherKhachHang.CountAsync(x => x.Voucher.KhuyenMaiId == programId)
        + await DirectTickets(db, excludeInvoiceId).CountAsync(x => x.Voucher!.KhuyenMaiId == programId);

    public static async Task<bool> NoCapacityAsync(RestaurantDbContext db, Voucher voucher, HoaDon invoice)
    {
        if (invoice.KhachHangId.HasValue && await db.VoucherKhachHang.AnyAsync(x => x.KhachHangId == invoice.KhachHangId && x.VoucherId == voucher.Id)) return false;
        return (voucher.GioiHanPhatHanh.HasValue && await IssuedAsync(db, voucher.Id, invoice.Id) >= voucher.GioiHanPhatHanh)
            || (voucher.KhuyenMai.GioiHanPhatHanh.HasValue && await ProgramIssuedAsync(db, voucher.KhuyenMaiId, invoice.Id) >= voucher.KhuyenMai.GioiHanPhatHanh);
    }
}
