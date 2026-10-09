using System.Data;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Security;

namespace RestaurantManagement.Web.Services;

public static class Week8DemoSetup
{
    public const string AreaName = "Sảnh gia đình";
    private const string PreviousAreaName = "Minh họa tuần 8";
    private const string Marker = "Khách dùng bữa tại nhà hàng, phục vụ theo bàn.";
    private const string PreviousMarker = "Dữ liệu minh họa tuần 8; khởi tạo bằng --init-week8-demo.";

    // The explicit Development-only command runs after normal demo-account initialization.
    public static async Task InitializeAsync(RestaurantDbContext db)
    {
        var staff = await db.NhanVien.SingleOrDefaultAsync(x => x.MaNhanVien == "DEMO-TT"
            && x.DangLamViec && x.ChucVu == AppRoles.TiepTan
            && x.TaiKhoan != null && x.TaiKhoan.Email == "tieptan.demo@example.test");
        var customer = await db.KhachHang.SingleOrDefaultAsync(x => x.DangSuDung
            && x.TaiKhoan != null && x.TaiKhoan.Email == "khach.demo@example.test");
        if (staff is null || customer is null)
            throw new InvalidOperationException("Cần khởi tạo tài khoản thử và hồ sơ tiếp tân/khách hàng trước khi tạo dữ liệu tuần 8.");

        var sessions = new List<(DatBan Booking, BanAn Table)>();
        await using (var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            var area = await db.KhuVuc.SingleOrDefaultAsync(x => x.TenKhuVuc == AreaName);
            if (area is null)
            {
                var previous = await db.KhuVuc.SingleOrDefaultAsync(x => x.TenKhuVuc == PreviousAreaName);
                if (previous is not null && await db.BanAn.AnyAsync(x => x.KhuVucId == previous.Id && x.MaBan == "W8-01"))
                {
                    previous.TenKhuVuc = AreaName;
                    area = previous;
                }
            }
            if (area is null)
            {
                area = new KhuVuc { TenKhuVuc = AreaName, Tang = 1, DangSuDung = true };
                db.KhuVuc.Add(area);
            }
            else if (!area.DangSuDung || area.LaPhongVip)
                throw new InvalidOperationException("Khu vực tuần 8 đã thay đổi; không tự ghi đè cấu hình hiện có.");

            var now = DateTimeOffset.UtcNow;
            for (var i = 1; i <= 3; i++)
            {
                var tableCode = $"W8-{i:00}";
                var bookingCode = $"W8-DEMO-{i:00}";
                var table = await db.BanAn.SingleOrDefaultAsync(x => x.MaBan == tableCode);
                var booking = await db.DatBan.Include(x => x.Ban).SingleOrDefaultAsync(x => x.MaDatBan == bookingCode);
                if (table is not null && (table.KhuVucId != area.Id || booking is null))
                    throw new InvalidOperationException($"{tableCode} đã tồn tại ngoài bộ dữ liệu tuần 8; không tự ghi đè.");
                if (booking is not null && (table is null || booking.KhachHangId != customer.Id
                    || (booking.YeuCau != Marker && booking.YeuCau != PreviousMarker) || booking.Ban.Any(x => x.BanAnId != table.Id)))
                    throw new InvalidOperationException($"{bookingCode} đã thay đổi; giữ nguyên dữ liệu hiện có.");
                if (table is null)
                {
                    table = new BanAn { MaBan = tableCode, KhuVuc = area, SoChoNgoi = 4, TrangThai = TrangThaiBan.SanSang };
                    db.BanAn.Add(table);
                    booking = new DatBan
                    {
                        MaDatBan = bookingCode, KhachHangId = customer.Id, HoTenLienHe = customer.HoTen,
                        SoDienThoaiLienHe = customer.SoDienThoai, LaKhachTrucTiep = true,
                        ThoiDiemTao = now, GioDen = now.AddMinutes(-5), GioKetThucDuKien = now.AddMinutes(-5).AddHours(TableService.MaxDiningHours),
                        SoNguoiLon = 2, TrangThai = TrangThaiDatBan.ChoXacNhan,
                        NhanVienTiepNhanId = staff.Id, YeuCau = Marker
                    };
                    db.DatBan.Add(booking);
                }
                if (booking!.YeuCau == PreviousMarker) booking.YeuCau = Marker;
                sessions.Add((booking, table));
            }
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        var tables = new TableService(db, new PreorderService(db, new OrderService(db)));
        foreach (var (booking, table) in sessions)
        {
            string Version() => Convert.ToBase64String(db.Entry(booking).Property<byte[]>("RowVersion").CurrentValue!);
            if (booking.TrangThai == TrangThaiDatBan.ChoXacNhan)
            {
                var error = await tables.Assign(booking.Id, [table.Id], Version(), staff.Id);
                if (error is not null) throw new InvalidOperationException($"Không thể xếp bàn tuần 8: {error}");
            }
            if (booking.TrangThai == TrangThaiDatBan.DaXacNhan)
            {
                var error = await tables.CheckIn(booking.Id, Version(), staff.Id);
                if (error is not null) throw new InvalidOperationException($"Không thể nhận bàn tuần 8: {error}");
            }
            if (booking.TrangThai != TrangThaiDatBan.DaNhanBan || booking.ThoiDiemKetThuc is not null
                || table.TrangThai != TrangThaiBan.DangPhucVu)
                throw new InvalidOperationException("Lượt khách tuần 8 đã thay đổi hoặc kết thúc; không tự mở lại hay ghi đè.");
        }
    }
}
