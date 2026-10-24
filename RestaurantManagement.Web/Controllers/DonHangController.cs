using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Security;
using RestaurantManagement.Web.Services;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin + "," + AppRoles.BoiBan + "," + AppRoles.ThuNgan + "," + AppRoles.TiepTan)]
public class DonHangController(
    RestaurantDbContext db,
    OrderService orderService,
    UserManager<TaiKhoan> userManager) : Controller
{
    private async Task<int?> GetCurrentStaffIdAsync()
    {
        if (!int.TryParse(userManager.GetUserId(User), out var accountId))
            return null;

        var staffId = await db.NhanVien.AsNoTracking()
            .Where(x => x.TaiKhoanId == accountId && x.DangLamViec)
            .Select(x => (int?)x.Id)
            .SingleOrDefaultAsync();

        if (staffId == null && User.IsInRole(AppRoles.Admin))
        {
            // If admin has no staff profile, fallback to first active admin staff or first active staff
            staffId = await db.NhanVien.AsNoTracking()
                .Where(x => x.DangLamViec)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();
        }

        return staffId;
    }

    public async Task<IActionResult> Index(LoaiDonHang? loaiDonHang, TrangThaiHoaDon? trangThai, string? search, int page = 1)
    {
        ViewData["ActiveMenu"] = "DonHang";
        ViewData["AutoCollapseSidebar"] = true;
        page = Math.Max(1, page);
        const int pageSize = 15;

        var query = db.HoaDon.AsNoTracking()
            .Include(x => x.NhanVien)
            .Include(x => x.DatBan).ThenInclude(x => x!.Ban).ThenInclude(x => x.BanAn)
            .Include(x => x.ChiTiet)
            .AsQueryable();

        if (loaiDonHang.HasValue)
            query = query.Where(x => x.LoaiDonHang == loaiDonHang.Value);

        if (trangThai.HasValue)
            query = query.Where(x => x.TrangThai == trangThai.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(x => x.MaHoaDon.Contains(search)
                || (x.TenNguoiNhan != null && x.TenNguoiNhan.Contains(search))
                || (x.SoDienThoaiNhan != null && x.SoDienThoaiNhan.Contains(search))
                || (x.DatBan != null && x.DatBan.HoTenLienHe.Contains(search))
                || (x.DatBan != null && x.DatBan.Ban.Any(b => b.BanAn.MaBan.Contains(search))));
        }

        var totalItems = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        page = Math.Min(page, totalPages);

        var list = await query
            .OrderByDescending(x => x.ThoiDiemLap)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var cards = list.Select(x =>
        {
            var activeItems = x.ChiTiet.Where(c => c.TrangThai != TrangThaiCheBien.DaHuy).ToList();
            var banName = x.LoaiDonHang switch
            {
                LoaiDonHang.TaiBan => x.DatBan != null && x.DatBan.Ban.Count > 0
                    ? string.Join(", ", x.DatBan.Ban.Select(b => b.BanAn.MaBan))
                    : "Chưa gắn bàn",
                LoaiDonHang.MangDi => "Mang đi",
                LoaiDonHang.GiaoHang => "Giao hàng",
                _ => "—"
            };

            var customerName = x.LoaiDonHang == LoaiDonHang.TaiBan
                ? x.DatBan?.HoTenLienHe
                : x.TenNguoiNhan;

            var phone = x.LoaiDonHang == LoaiDonHang.TaiBan
                ? x.DatBan?.SoDienThoaiLienHe
                : x.SoDienThoaiNhan;

            return new OrderCardItem
            {
                Id = x.Id,
                MaHoaDon = x.MaHoaDon,
                LoaiDonHang = x.LoaiDonHang,
                Ban = banName,
                TenKhachHang = customerName,
                SoDienThoai = phone,
                ThoiDiemLap = x.ThoiDiemLap,
                TongSoMon = activeItems.Sum(c => c.SoLuong),
                TongTien = x.TongTienHang,
                TrangThai = x.TrangThai,
                PhuongThucThanhToan = x.PhuongThucThanhToan,
                NhanVienLap = x.NhanVien.HoTen,
                MonChoCheBien = activeItems.Count(c => c.TrangThai == TrangThaiCheBien.ChoCheBien),
                MonDangCheBien = activeItems.Count(c => c.TrangThai == TrangThaiCheBien.DangCheBien),
                MonSanSang = activeItems.Count(c => c.TrangThai == TrangThaiCheBien.SanSang),
                MonDaPhucVu = activeItems.Count(c => c.TrangThai == TrangThaiCheBien.DaPhucVu)
            };
        }).ToList();

        var model = new OrderListViewModel
        {
            LoaiDonHang = loaiDonHang,
            TrangThai = trangThai,
            Search = search,
            Page = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            Orders = cards
        };

        return View(model);
    }

    public async Task<IActionResult> Create(int? banAnId = null)
    {
        ViewData["ActiveMenu"] = "DonHang";
        ViewData["AutoCollapseSidebar"] = true;
        var model = await orderService.PrepareCreateViewModelAsync(banAnId);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrderCreateViewModel model)
    {
        ViewData["ActiveMenu"] = "DonHang";
        ViewData["AutoCollapseSidebar"] = true;
        var staffId = await GetCurrentStaffIdAsync();
        if (staffId == null)
        {
            TempData["Error"] = "Tài khoản hiện tại chưa được liên kết với hồ sơ nhân viên đang làm việc.";
            return RedirectToAction(nameof(Create));
        }

        var (error, hoaDonId) = await orderService.CreateOrderAsync(model, staffId.Value);
        if (error != null)
        {
            TempData["Error"] = error;
            var prep = await orderService.PrepareCreateViewModelAsync(model.BanAnId);
            prep.LoaiDonHang = model.LoaiDonHang;
            prep.TenNguoiNhan = model.TenNguoiNhan;
            prep.SoDienThoaiNhan = model.SoDienThoaiNhan;
            prep.DiaChiGiaoHang = model.DiaChiGiaoHang;
            prep.GhiChuDonHang = model.GhiChuDonHang;
            return View(prep);
        }

        TempData["Success"] = "Đã ghi nhận gọi món thành công và gửi thông tin chế biến vào bếp.";
        return IsAjaxRequest
            ? Json(new { ok = true, message = "Đã ghi nhận gọi món và gửi vào bếp.", redirectUrl = Url.Action(nameof(AddDishes), new { id = hoaDonId!.Value }) })
            : RedirectToAction(nameof(AddDishes), new { id = hoaDonId!.Value });
    }

    public async Task<IActionResult> Details(int id)
    {
        ViewData["ActiveMenu"] = "DonHang";
        ViewData["AutoCollapseSidebar"] = true;
        var invoice = await db.HoaDon.AsSplitQuery()
            .Include(x => x.NhanVien)
            .Include(x => x.DatBan).ThenInclude(x => x!.Ban).ThenInclude(x => x.BanAn)
            .Include(x => x.ChiTiet).ThenInclude(x => x.MonAnSize)
            .SingleOrDefaultAsync(x => x.Id == id);

        if (invoice == null) return NotFound();

        ViewBag.RowVersion = Convert.ToBase64String(db.Entry(invoice).Property<byte[]>("RowVersion").CurrentValue ?? []);
        return View(invoice);
    }

    public async Task<IActionResult> AddDishes(int id)
    {
        ViewData["ActiveMenu"] = "DonHang";
        ViewData["AutoCollapseSidebar"] = true;
        var invoice = await db.HoaDon.AsNoTracking()
            .Include(x => x.DatBan).ThenInclude(x => x!.Ban).ThenInclude(x => x.BanAn)
            .Include(x => x.ChiTiet)
            .SingleOrDefaultAsync(x => x.Id == id);
        if (invoice == null) return NotFound();
        if (invoice.TrangThai != TrangThaiHoaDon.ChuaThanhToan)
        {
            TempData["Error"] = "Đơn hàng đã thanh toán hoặc đã hủy, không thể gọi thêm món.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var prep = await orderService.PrepareCreateViewModelAsync();
        prep.LoaiDonHang = invoice.LoaiDonHang;
        prep.DatBanId = invoice.DatBanId;
        prep.ExistingInvoiceCode = invoice.MaHoaDon;
        prep.ExistingTableLabel = invoice.DatBan is { Ban.Count: > 0 } booking
            ? string.Join(", ", booking.Ban.Select(x => x.BanAn.MaBan))
            : null;
        prep.ExistingCustomerLabel = invoice.DatBan?.HoTenLienHe ?? invoice.TenNguoiNhan;
        prep.ExistingItems = invoice.ChiTiet
            .Where(x => x.TrangThai != TrangThaiCheBien.DaHuy)
            .OrderBy(x => x.ThoiDiemGoi).ThenBy(x => x.Id)
            .Select(x => new TableOrderLineItem
            {
                TenMon = x.TenMonLucBan,
                TenSize = string.IsNullOrWhiteSpace(x.TenSizeLucBan) ? "Mặc định" : x.TenSizeLucBan,
                SoLuong = x.SoLuong,
                DonGia = x.DonGia,
                YeuCauCheBien = x.YeuCauCheBien,
                TrangThai = x.TrangThai,
                ThoiDiemGoi = x.ThoiDiemGoi
            })
            .ToList();
        ViewBag.HoaDonId = invoice.Id;
        ViewBag.MaHoaDon = invoice.MaHoaDon;
        return View(prep);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDishes(int id, List<OrderItemInputModel> items)
    {
        var (error, _) = await orderService.AddDishesAsync(id, items);
        if (error != null)
        {
            if (IsAjaxRequest) return Conflict(new { ok = false, message = error });
            TempData["Error"] = error;
        }
        else
        {
            if (IsAjaxRequest) return Json(new { ok = true, message = "Đã gửi món gọi thêm xuống bếp.", redirectUrl = Url.Action(nameof(AddDishes), new { id }) });
            TempData["Success"] = "Đã gọi thêm món thành công và gửi thông tin vào bếp.";
        }

        return RedirectToAction(nameof(AddDishes), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> LiveOrderStatus(int? id = null)
    {
        if (id is int invoiceId)
        {
            var invoice = await db.HoaDon.AsNoTracking().Include(x => x.ChiTiet)
                .SingleOrDefaultAsync(x => x.Id == invoiceId);
            if (invoice is null) return NotFound();
            if (invoice.TrangThai != TrangThaiHoaDon.ChuaThanhToan)
                return Json(new { active = false, message = "Hóa đơn đã thay đổi trạng thái. Tải lại để tiếp tục." });
            var lines = invoice.ChiTiet.Where(x => x.TrangThai != TrangThaiCheBien.DaHuy).Select(x => new TableOrderLineItem
            {
                TenMon = x.TenMonLucBan,
                TenSize = string.IsNullOrWhiteSpace(x.TenSizeLucBan) ? "Mặc định" : x.TenSizeLucBan,
                SoLuong = x.SoLuong,
                DonGia = x.DonGia,
                TrangThai = x.TrangThai,
                ThoiDiemGoi = x.ThoiDiemGoi
            });
            return Json(new { active = true, orders = new[] { Snapshot(invoice.Id, invoice.MaHoaDon, null, lines) } });
        }

        var tables = await orderService.GetOccupiedTablesAsync();
        var orders = tables.Select(table => Snapshot(table.HoaDonIdDangMo, table.MaHoaDonDangMo, table.Id,
            table.OrderedItems.Select(line => line)));
        return Json(new { active = true, orders });
    }

    private static object Snapshot(int? invoiceId, string? invoiceCode, int? tableId, IEnumerable<TableOrderLineItem> lines) => new
    {
        invoiceId,
        invoiceCode,
        tableId,
        lines = lines.Select(line => new
        {
            name = line.TenMon,
            size = line.TenSize,
            quantity = line.SoLuong,
            price = line.DonGia,
            total = line.ThanhTien,
            note = line.YeuCauCheBien,
            status = (int)line.TrangThai,
            time = line.ThoiDiemGoi.ToOffset(TimeSpan.FromHours(7)).ToString("HH:mm")
        })
    };

    private bool IsAjaxRequest => string.Equals(Request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.BoiBan)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CapNhatMon(int id, int hoaDonId, TrangThaiCheBien trangThai)
    {
        var error = await orderService.UpdateDishStatusAsync(id, trangThai, isWaiterOrAdmin: true);
        if (error != null)
        {
            TempData["Error"] = error;
        }
        else
        {
            TempData["Success"] = trangThai switch
            {
                TrangThaiCheBien.DaPhucVu => "Đã xác nhận phục vụ món ăn đến khách.",
                TrangThaiCheBien.DaHuy => "Đã hủy món ăn thành công.",
                _ => "Đã cập nhật trạng thái món ăn."
            };
        }

        return RedirectToAction(nameof(Details), new { id = hoaDonId });
    }
}
