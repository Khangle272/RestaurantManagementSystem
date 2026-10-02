using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Security;
using RestaurantManagement.Web.Services;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin + "," + AppRoles.TiepTan)]
public class QuanLyDatBanController(RestaurantDbContext context, TableService tables) : ManagementControllerBase(context)
{
    // GET /QuanLyDatBan
    [HttpGet]
    public async Task<IActionResult> Index(string? tab = "ChoXacNhan", string? filterNgay = null)
    {
        ViewData["ActiveMenu"] = "QuanLyDatBan";
        ViewBag.CurrentTab = tab ?? "ChoXacNhan";

        var allQuery = Db.DatBan
            .AsSplitQuery()
            .Include(x => x.Ban).ThenInclude(b => b.BanAn).ThenInclude(b => b.KhuVuc)
            .Include(x => x.HoaDon).ThenInclude(h => h.ChiTiet)
            .AsTracking();

        DateOnly? parsedDate = null;
        if (!string.IsNullOrWhiteSpace(filterNgay) && DateOnly.TryParse(filterNgay, out var d))
        {
            parsedDate = d;
            var startUtc = TableService.VietnamTime(d.ToDateTime(TimeOnly.MinValue));
            var endUtc = TableService.VietnamTime(d.ToDateTime(TimeOnly.MaxValue));
            allQuery = allQuery.Where(x => x.GioDen >= startUtc && x.GioDen <= endUtc);
        }

        var todayStart = TableService.VietnamTime(TableService.VietnamNow.Date);
        var todayEnd = todayStart.AddDays(1);

        // Thống kê nhanh theo tab
        var choXacNhanCount = await Db.DatBan.CountAsync(x => x.TrangThai == TrangThaiDatBan.ChoXacNhan || x.TrangThai == TrangThaiDatBan.ChoCoc);
        var daXacNhanCount = await Db.DatBan.CountAsync(x => x.TrangThai == TrangThaiDatBan.DaXacNhan);
        var dangPhucVuCount = await Db.DatBan.CountAsync(x => x.TrangThai == TrangThaiDatBan.DaNhanBan && x.ThoiDiemKetThuc == null);
        var homNayCount = await Db.DatBan.CountAsync(x => x.GioDen >= todayStart && x.GioDen < todayEnd);

        // Lọc dữ liệu theo tab đã chọn
        var tabQuery = tab switch
        {
            "DaXacNhan" => allQuery.Where(x => x.TrangThai == TrangThaiDatBan.DaXacNhan),
            "DangPhucVu" => allQuery.Where(x => x.TrangThai == TrangThaiDatBan.DaNhanBan && x.ThoiDiemKetThuc == null),
            "HoanTatHuy" => allQuery.Where(x => x.TrangThai == TrangThaiDatBan.DaHuy
                                                || x.TrangThai == TrangThaiDatBan.KhongDen
                                                || (x.TrangThai == TrangThaiDatBan.DaNhanBan && x.ThoiDiemKetThuc != null)),
            _ => allQuery.Where(x => x.TrangThai == TrangThaiDatBan.ChoXacNhan || x.TrangThai == TrangThaiDatBan.ChoCoc)
        };

        var rawList = await tabQuery.OrderBy(x => x.GioDen).ToListAsync();

        var model = new DatBanAdminVM
        {
            FilterTrangThai = tab,
            FilterNgay = parsedDate,
            ChoXacNhanCount = choXacNhanCount,
            DaXacNhanCount = daXacNhanCount,
            DangPhucVuCount = dangPhucVuCount,
            HomNayCount = homNayCount,
            DanhSachBanTrong = await GetBanAnSelectOptionsAsync(),
            DanhSachDatBan = rawList.Select(x =>
            {
                var invoice = x.HoaDon.OrderByDescending(h => h.Id).FirstOrDefault();
                var daThanhToan = invoice?.TrangThai == TrangThaiHoaDon.DaThanhToan;
                var isCompleted = x.TrangThai == TrangThaiDatBan.DaNhanBan && x.ThoiDiemKetThuc.HasValue;

                string badgeClass = x.TrangThai switch
                {
                    TrangThaiDatBan.ChoXacNhan => "badge-status-warning",
                    TrangThaiDatBan.DaXacNhan => "badge-status-info text-white bg-primary",
                    TrangThaiDatBan.DaNhanBan when !isCompleted => "badge-status-success",
                    TrangThaiDatBan.DaHuy => "badge-status-danger",
                    _ => isCompleted ? "badge-status-success" : "badge-secondary"
                };

                string trangThaiStr = x.TrangThai switch
                {
                    TrangThaiDatBan.ChoXacNhan => "Chờ xác nhận",
                    TrangThaiDatBan.DaXacNhan => "Đã xác nhận",
                    TrangThaiDatBan.DaNhanBan when !isCompleted => "Đang phục vụ",
                    TrangThaiDatBan.DaHuy => "Đã hủy",
                    TrangThaiDatBan.KhongDen => "Không đến",
                    _ => isCompleted ? "Hoàn tất" : x.TrangThai.ToString()
                };

                return new DatBanItemVM
                {
                    MaDatBan = x.Id,
                    RowVersion = VersionOf(x),
                    BookingCode = x.MaDatBan,
                    HoTen = x.HoTenLienHe,
                    SoDienThoai = x.SoDienThoaiLienHe ?? "",
                    ThoiGianDen = x.GioDen,
                    SoNguoi = x.SoNguoiLon + x.SoTreEm,
                    TenBan = x.Ban.Any() ? string.Join(", ", x.Ban.Select(b => $"{b.BanAn.MaBan} ({b.BanAn.KhuVuc.TenKhuVuc})")) : "Chưa xếp bàn",
                    BanAnId = x.Ban.Select(b => b.BanAnId).FirstOrDefault(),
                    TrangThai = trangThaiStr,
                    TrangThaiBadgeClass = badgeClass,
                    GhiChu = x.YeuCau,
                    HoaDonId = invoice?.Id,
                    TongTienHoaDon = invoice?.ChiTiet.Sum(c => c.SoLuong * c.DonGia) ?? 0,
                    DaThanhToan = daThanhToan
                };
            }).ToList()
        };

        return View(model);
    }

    // POST /QuanLyDatBan/XacNhan
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> XacNhan(int datBanId, int[] banAnIds, string? rowVersion, int banAnId = 0)
    {
        if (banAnIds.Length == 0 && banAnId > 0) banAnIds = [banAnId];
        var error = await tables.Assign(datBanId, banAnIds, rowVersion, await StaffId());
        TempData[error is null ? "SuccessMessage" : "ErrorMessage"] = error ?? "Đã xác nhận và xếp bàn cho khách.";
        return RedirectToAction("Index", "SoDoBan", error is null ? null : new { datBanId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NhanBan(int datBanId, string? rowVersion)
    {
        var error = await tables.CheckIn(datBanId, rowVersion, await StaffId());
        TempData[error is null ? "SuccessMessage" : "ErrorMessage"] = error ?? "Khách đã nhận bàn. Bàn chuyển sang đang phục vụ.";
        return RedirectToAction(nameof(Index), new { tab = error is null ? "DangPhucVu" : "DaXacNhan" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> HuyBan(int datBanId, string? lyDoHuy, string? rowVersion)
    {
        var error = await tables.Cancel(datBanId, rowVersion, lyDoHuy, await StaffId());
        TempData[error is null ? "SuccessMessage" : "ErrorMessage"] = error ?? "Đã hủy yêu cầu đặt bàn.";
        return RedirectToAction(nameof(Index));
    }

    private Task<int?> StaffId()
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var accountId);
        return Db.NhanVien.Where(x => x.TaiKhoanId == accountId && x.DangLamViec).Select(x => (int?)x.Id).SingleOrDefaultAsync();
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? banAnId)
    {
        var model = new ReceptionBookingViewModel { Areas = await Db.KhuVuc.Where(x => x.DangSuDung).ToListAsync() };
        if (banAnId.HasValue)
        {
            var table = await RequestedTable(banAnId.Value);
            if (table is null) return NotFound();
            model.BanAnId = table.Id;
            model.KhuVucId = table.KhuVucId;
            model.RequestedTableName = $"{table.MaBan} · {table.KhuVuc.TenKhuVuc} · {table.SoChoNgoi} chỗ";
        }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReceptionBookingViewModel model)
    {
        model.Areas = await Db.KhuVuc.Where(x => x.DangSuDung).ToListAsync();
        var arrival = TableService.VietnamTime(model.GioDen);
        if (arrival < DateTimeOffset.UtcNow.AddMinutes(-15) || arrival > DateTimeOffset.UtcNow.AddDays(180))
            ModelState.AddModelError(nameof(model.GioDen), "Chọn giờ đến từ hiện tại đến tối đa 180 ngày.");
        if (model.KhuVucId.HasValue && !model.Areas.Any(x => x.Id == model.KhuVucId))
            ModelState.AddModelError(nameof(model.KhuVucId), "Khu vực không còn sử dụng.");
        if (string.IsNullOrWhiteSpace(model.HoTen)) ModelState.AddModelError(nameof(model.HoTen), "Nhập họ tên khách.");
        if (model.BanAnId is int requestedId)
        {
            var table = await RequestedTable(requestedId);
            if (table is null) ModelState.AddModelError(nameof(model.BanAnId), "Bàn đã ngừng sử dụng hoặc không tồn tại. Chọn bàn khác trên sơ đồ.");
            else
            {
                model.RequestedTableName = $"{table.MaBan} · {table.KhuVuc.TenKhuVuc} · {table.SoChoNgoi} chỗ";
                if (model.KhuVucId != table.KhuVucId)
                    ModelState.AddModelError(nameof(model.KhuVucId), "Khu vực cần khớp với bàn đã chọn. Quay lại sơ đồ nếu muốn đổi bàn.");
            }
        }
        if (!ModelState.IsValid) return View(model);
        var customerId = await Db.KhachHang.Where(x => x.SoDienThoai == model.SoDienThoai).Select(x => (int?)x.Id).SingleOrDefaultAsync();
        var booking = new DatBan {
            MaDatBan = TableService.NewCode("BK"), HoTenLienHe = model.HoTen.Trim(), SoDienThoaiLienHe = model.SoDienThoai,
            KhachHangId = customerId, LaKhachTrucTiep = true, ThoiDiemTao = DateTimeOffset.UtcNow,
            GioDen = arrival, GioKetThucDuKien = arrival.AddMinutes(model.SoPhut), SoNguoiLon = model.SoNguoi,
            KhuVucUuTienId = model.KhuVucId, YeuCau = model.GhiChu?.Trim(), TrangThai = TrangThaiDatBan.ChoXacNhan,
            NhanVienTiepNhanId = await StaffId()
        };
        Db.DatBan.Add(booking);
        if (!await SaveAsync("", "Không tạo được yêu cầu. Vui lòng thử lại.")) return View(model);
        TempData["SuccessMessage"] = "Đã ghi nhận khách. Chọn bàn phù hợp để xác nhận.";
        return RedirectToAction("Index", "SoDoBan", new { datBanId = booking.Id, khuVucId = model.KhuVucId, preferredTableId = model.BanAnId });
    }

    private Task<BanAn?> RequestedTable(int id) => Db.BanAn.AsNoTracking().Include(x => x.KhuVuc)
        .SingleOrDefaultAsync(x => x.Id == id && x.KhuVuc.DangSuDung && x.TrangThai != TrangThaiBan.NgungSuDung);

    // GET /QuanLyDatBan/DanhGia
    [HttpGet]
    public async Task<IActionResult> DanhGia()
    {
        ViewData["ActiveMenu"] = "QuanLyDanhGia";

        var list = await Db.DanhGia
            .Include(d => d.HoaDon).ThenInclude(h => h.DatBan)
            .Include(d => d.KhachHang)
            .OrderByDescending(d => d.ThoiDiem)
            .ToListAsync();

        var model = new DanhGiaAdminVM
        {
            DanhSachDanhGia = list.Select(d =>
            {
                var parsed = ParseDanhGiaContent(d.NoiDung, d.Diem);
                return new DanhGiaItemVM
                {
                    Id = d.Id,
                    BookingCode = d.HoaDon?.DatBan?.MaDatBan ?? $"HD-{d.HoaDonId}",
                    TenKhachHang = d.KhachHang?.HoTen ?? d.HoaDon?.DatBan?.HoTenLienHe ?? "Khách hàng",
                    ThoiDiem = d.ThoiDiem,
                    Diem = d.Diem,
                    SoSaoMonAn = parsed.SoSaoMonAn,
                    SoSaoDichVu = parsed.SoSaoDichVu,
                    NoiDung = parsed.CleanNoiDung,
                    HinhAnhUrl = parsed.HinhAnhUrl,
                    PhanHoiAdmin = parsed.PhanHoiAdmin
                };
            }).ToList()
        };

        return View(model);
    }

    // POST /QuanLyDatBan/PhanHoiDanhGia
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PhanHoiDanhGia(int danhGiaId, string noiDungPhanHoi)
    {
        if (danhGiaId <= 0 || string.IsNullOrWhiteSpace(noiDungPhanHoi))
        {
            TempData["ErrorMessage"] = "Nội dung phản hồi không hợp lệ.";
            return RedirectToAction(nameof(DanhGia));
        }

        var danhGia = await Db.DanhGia.FindAsync(danhGiaId);
        if (danhGia == null) return NotFound();

        var replyTag = "\n[Phản hồi từ Nhà hàng]: ";
        var currentNoiDung = danhGia.NoiDung ?? "";
        int markerIdx = currentNoiDung.IndexOf("\n[Phản hồi từ Nhà hàng]:", StringComparison.Ordinal);
        var baseContent = markerIdx >= 0 ? currentNoiDung[..markerIdx] : currentNoiDung;

        var fullContent = $"{baseContent}{replyTag}{noiDungPhanHoi.Trim()} ({DateTime.Now:dd/MM/yyyy HH:mm})";
        danhGia.NoiDung = fullContent.Length > 2000 ? fullContent[..2000] : fullContent;

        await Db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã gửi câu trả lời phản hồi cho khách hàng thành công.";
        return RedirectToAction(nameof(DanhGia));
    }

    private async Task<List<SelectListItem>> GetBanAnSelectOptionsAsync() =>
        await Db.BanAn.AsNoTracking()
            .Include(x => x.KhuVuc)
            .Where(x => x.TrangThai != TrangThaiBan.NgungSuDung)
            .OrderBy(x => x.KhuVuc.TenKhuVuc).ThenBy(x => x.MaBan)
            .Select(x => new SelectListItem(
                $"{x.MaBan} ({x.KhuVuc.TenKhuVuc} - {x.SoChoNgoi} chỗ) - [{x.TrangThai}]",
                x.Id.ToString()))
            .ToListAsync();

    private static (int SoSaoMonAn, int SoSaoDichVu, string CleanNoiDung, string? HinhAnhUrl, string? PhanHoiAdmin)
        ParseDanhGiaContent(string? raw, int defaultScore)
    {
        if (string.IsNullOrWhiteSpace(raw)) return (defaultScore, defaultScore, "", null, null);

        int monAnScore = defaultScore;
        int dichVuScore = defaultScore;
        string? hinhAnh = null;
        string? phanHoi = null;
        string clean = raw;

        var scoreMatch = Regex.Match(clean, @"\[Món ăn:\s*(\d+)★\s*\|\s*Dịch vụ:\s*(\d+)★\]");
        if (scoreMatch.Success)
        {
            int.TryParse(scoreMatch.Groups[1].Value, out monAnScore);
            int.TryParse(scoreMatch.Groups[2].Value, out dichVuScore);
            clean = clean.Replace(scoreMatch.Value, "").Trim();
        }

        var imgMatch = Regex.Match(clean, @"\[Ảnh:\s*([^\]]+)\]");
        if (imgMatch.Success)
        {
            hinhAnh = imgMatch.Groups[1].Value.Trim();
            clean = clean.Replace(imgMatch.Value, "").Trim();
        }

        var replyMatch = Regex.Match(clean, @"\[Phản hồi từ Nhà hàng\]:\s*([\s\S]+)$");
        if (replyMatch.Success)
        {
            phanHoi = replyMatch.Groups[1].Value.Trim();
            clean = clean.Replace(replyMatch.Value, "").Trim();
        }

        return (monAnScore, dichVuScore, clean, hinhAnh, phanHoi);
    }
}
