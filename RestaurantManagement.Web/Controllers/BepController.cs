using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Security;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin + "," + AppRoles.Bep + "," + AppRoles.BoiBan)]
public class BepController(RestaurantDbContext db, RestaurantManagement.Web.Services.OrderService orders) : Controller
{
    // Tên bàn đầy đủ cho bếp: "Bàn VIP 1" / "Bàn Sân Vườn 2" thay vì mã viết tắt.
    public static string TenBanHienThi(BanAn ban)
    {
        var khuVuc = ban.KhuVuc.LaPhongVip ? "VIP" : ban.KhuVuc.TenKhuVuc;
        var soBan = new string(ban.MaBan.Where(char.IsDigit).ToArray()).TrimStart('0');
        return string.IsNullOrEmpty(soBan) ? $"Bàn {ban.MaBan}" : $"Bàn {khuVuc} {soBan}";
    }

    // Bếp chỉ quản lý món gọi trong ngày hiện tại, xếp theo giờ gọi (gọi trước làm trước).
    private IQueryable<ChiTietHoaDon> TodayQuery(string? filter)
    {
        var today = RestaurantManagement.Web.Services.KitchenDayClosing.StartOfToday;
        var query = db.ChiTietHoaDon.AsNoTracking()
            .Where(x => x.ThoiDiemGoi >= today && x.HoaDon.TrangThai != TrangThaiHoaDon.DaHuy
                && (x.HoaDon.DatBan == null || x.HoaDon.DatBan.TrangThai != TrangThaiDatBan.DaHuy));
        return filter switch
        {
            "ready" => query.Where(x => x.TrangThai == TrangThaiCheBien.SanSang),
            "all" => query.Where(x => x.TrangThai == TrangThaiCheBien.ChoCheBien
                || x.TrangThai == TrangThaiCheBien.DangCheBien || x.TrangThai == TrangThaiCheBien.SanSang),
            _ => query.Where(x => x.TrangThai == TrangThaiCheBien.ChoCheBien || x.TrangThai == TrangThaiCheBien.DangCheBien)
        };
    }

    // Món khách vừa hủy (trong 60 phút): hiện riêng để bếp dừng chế biến ngay.
    private IQueryable<ChiTietHoaDon> RecentlyCancelled()
    {
        var since = DateTimeOffset.UtcNow.AddMinutes(-60);
        var today = RestaurantManagement.Web.Services.KitchenDayClosing.StartOfToday;
        return db.ChiTietHoaDon.AsNoTracking().Where(x => x.TrangThai == TrangThaiCheBien.DaHuy
            && x.ThoiDiemHuy >= since && x.ThoiDiemGoi >= today);
    }

    private static string TableLabel(HoaDon bill) => bill.LoaiDonHang switch
    {
        LoaiDonHang.TaiBan => bill.DatBan == null ? "Tại bàn" : string.Join(", ", bill.DatBan.Ban.Select(b => TenBanHienThi(b.BanAn))),
        LoaiDonHang.MangDi => "Mang đi",
        LoaiDonHang.GiaoHang => string.IsNullOrWhiteSpace(bill.TenNguoiNhan) ? "Giao hàng" : $"Giao hàng · {bill.TenNguoiNhan}",
        _ => "—"
    };

    public async Task<IActionResult> Index(string? filter = "active")
    {
        ViewData["ActiveMenu"] = "Bep";
        ViewData["AutoCollapseSidebar"] = true;
        ViewBag.Filter = filter;

        var rows = await TodayQuery(filter)
            .Include(x => x.MonAnSize)
            .Include(x => x.HoaDon).ThenInclude(x => x.DatBan).ThenInclude(x => x!.Ban).ThenInclude(x => x.BanAn).ThenInclude(x => x.KhuVuc)
            .OrderBy(x => x.ThoiDiemGoi).ThenBy(x => x.Id).Take(150)
            .ToListAsync();
        var billIds = rows.Select(x => x.HoaDonId).Distinct().ToList();
        // Lượt gọi đầu tiên của mỗi hóa đơn; món gọi sau mốc này là món gọi thêm.
        var firstOrder = await db.ChiTietHoaDon.Where(x => billIds.Contains(x.HoaDonId))
            .GroupBy(x => x.HoaDonId).Select(g => new { g.Key, First = g.Min(x => x.ThoiDiemGoi) })
            .ToDictionaryAsync(x => x.Key, x => x.First);

        var items = rows.Select(x => new BepRow
        {
            Id = x.Id,
            Mon = x.TenMonLucBan,
            Size = !string.IsNullOrEmpty(x.TenSizeLucBan) ? x.TenSizeLucBan : (x.MonAnSize?.TenSize ?? "Mặc định"),
            SoLuong = x.SoLuong,
            GhiChu = string.Join(" · ", new[] { x.YeuCauCheBien, x.MonAnSize?.GhiChuThanhPhanKhacBiet }
                .Where(s => !string.IsNullOrWhiteSpace(s))),
            TrangThai = x.TrangThai,
            Ban = TableLabel(x.HoaDon),
            ThoiDiemGoi = x.ThoiDiemGoi,
            GoiThem = firstOrder.TryGetValue(x.HoaDonId, out var first) && x.ThoiDiemGoi > first.AddSeconds(30)
        }).ToList();

        var cancelled = await RecentlyCancelled()
            .Include(x => x.HoaDon).ThenInclude(x => x.DatBan).ThenInclude(x => x!.Ban).ThenInclude(x => x.BanAn).ThenInclude(x => x.KhuVuc)
            .OrderByDescending(x => x.ThoiDiemHuy).Take(20).ToListAsync();
        ViewBag.Cancelled = cancelled.Select(x => new BepRow { Id = x.Id, Mon = x.TenMonLucBan, Size = x.TenSizeLucBan ?? "Mặc định",
            SoLuong = x.SoLuong, TrangThai = x.TrangThai, Ban = TableLabel(x.HoaDon), ThoiDiemGoi = x.ThoiDiemHuy ?? x.ThoiDiemGoi }).ToList();

        return View(items);
    }

    // Kết thúc ngày: đóng toàn bộ món còn treo để không trôi sang ngày hôm sau.
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = AppRoles.Admin + "," + AppRoles.Bep)]
    public async Task<IActionResult> KetThucNgay()
    {
        var count = await RestaurantManagement.Web.Services.KitchenDayClosing.CloseBeforeAsync(db, DateTimeOffset.UtcNow);
        TempData["Success"] = count == 0 ? "Không còn món nào treo trên màn hình bếp." : $"Đã kết thúc ngày: đóng {count} món còn treo (món chưa nấu của bill chưa thu tiền được hủy).";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CapNhat(int id, TrangThaiCheBien trangThai, string? filter = null)
    {
        var serving = trangThai == TrangThaiCheBien.DaPhucVu;
        if (serving && !User.IsInRole(AppRoles.Admin) && !User.IsInRole(AppRoles.BoiBan)) return Forbid();
        if (!serving && !User.IsInRole(AppRoles.Admin) && !User.IsInRole(AppRoles.Bep)) return Forbid();
        if (trangThai is not (TrangThaiCheBien.DangCheBien or TrangThaiCheBien.SanSang or TrangThaiCheBien.DaPhucVu))
            return BadRequest();
        var error = await orders.UpdateDishStatusAsync(id, trangThai, isWaiterOrAdmin: serving);
        if (error is not null) return BadRequest(error);
        return RedirectToAction(nameof(Index), new { filter });
    }

    [HttpGet]
    public async Task<IActionResult> Snapshot(string? filter = "active")
    {
        // TrangThai lưu dạng nvarchar: cast sau khi đã tải về, không để EF dịch thành CAST(... AS int) trong SQL.
        var rows = await TodayQuery(filter).OrderBy(x => x.ThoiDiemGoi).ThenBy(x => x.Id).Take(150)
            .Select(x => new { x.Id, x.TrangThai, x.ThoiDiemGoi })
            .ToListAsync();
        var cancelled = await RecentlyCancelled().OrderBy(x => x.Id).Select(x => x.Id).ToListAsync();
        var snapshot = rows.Select(x => new { x.Id, Status = (int)x.TrangThai, x.ThoiDiemGoi });
        return Json(new { items = snapshot, cancelled });
    }
}

public class BepRow
{
    public int Id { get; set; }
    public string Mon { get; set; } = "";
    public string Size { get; set; } = "Mặc định";
    public int SoLuong { get; set; }
    public string? GhiChu { get; set; }
    public TrangThaiCheBien TrangThai { get; set; }
    public string Ban { get; set; } = "";
    public DateTimeOffset ThoiDiemGoi { get; set; }
    public bool GoiThem { get; set; }
}
