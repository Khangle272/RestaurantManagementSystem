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

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin + "," + AppRoles.TiepTan)]
public class QuanLyDatBanController(RestaurantDbContext context) : ManagementControllerBase(context)
{
    // GET /QuanLyDatBan
    [HttpGet]
    public async Task<IActionResult> Index(string? tab = "ChoXacNhan", string? filterNgay = null)
    {
        ViewData["ActiveMenu"] = "QuanLyDatBan";
        ViewBag.CurrentTab = tab ?? "ChoXacNhan";

        var allQuery = Db.DatBan
            .Include(x => x.Ban).ThenInclude(b => b.BanAn).ThenInclude(b => b.KhuVuc)
            .Include(x => x.HoaDon).ThenInclude(h => h.ChiTiet)
            .AsNoTracking();

        DateOnly? parsedDate = null;
        if (!string.IsNullOrWhiteSpace(filterNgay) && DateOnly.TryParse(filterNgay, out var d))
        {
            parsedDate = d;
            var startUtc = new DateTimeOffset(d.ToDateTime(TimeOnly.MinValue));
            var endUtc = new DateTimeOffset(d.ToDateTime(TimeOnly.MaxValue));
            allQuery = allQuery.Where(x => x.GioDen >= startUtc && x.GioDen <= endUtc);
        }

        var todayStart = new DateTimeOffset(DateTime.Today);
        var todayEnd = new DateTimeOffset(DateTime.Today.AddDays(1).AddTicks(-1));

        // Thống kê nhanh theo tab
        var choXacNhanCount = await Db.DatBan.CountAsync(x => x.TrangThai == TrangThaiDatBan.ChoXacNhan || x.TrangThai == TrangThaiDatBan.ChoCoc);
        var daXacNhanCount = await Db.DatBan.CountAsync(x => x.TrangThai == TrangThaiDatBan.DaXacNhan);
        var dangPhucVuCount = await Db.DatBan.CountAsync(x => x.TrangThai == TrangThaiDatBan.DaNhanBan && !x.HoaDon.Any(h => h.TrangThai == TrangThaiHoaDon.DaThanhToan));
        var homNayCount = await Db.DatBan.CountAsync(x => x.GioDen >= todayStart && x.GioDen <= todayEnd);

        // Lọc dữ liệu theo tab đã chọn
        var tabQuery = tab switch
        {
            "DaXacNhan" => allQuery.Where(x => x.TrangThai == TrangThaiDatBan.DaXacNhan),
            "DangPhucVu" => allQuery.Where(x => x.TrangThai == TrangThaiDatBan.DaNhanBan && !x.HoaDon.Any(h => h.TrangThai == TrangThaiHoaDon.DaThanhToan)),
            "HoanTatHuy" => allQuery.Where(x => x.TrangThai == TrangThaiDatBan.DaHuy
                                                || x.TrangThai == TrangThaiDatBan.KhongDen
                                                || (x.TrangThai == TrangThaiDatBan.DaNhanBan && x.HoaDon.Any(h => h.TrangThai == TrangThaiHoaDon.DaThanhToan))),
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
                var isCompleted = x.TrangThai == TrangThaiDatBan.DaNhanBan && daThanhToan;

                string badgeClass = x.TrangThai switch
                {
                    TrangThaiDatBan.ChoXacNhan => "badge-status-warning",
                    TrangThaiDatBan.DaXacNhan => "badge-status-info text-white bg-primary",
                    TrangThaiDatBan.DaNhanBan when !daThanhToan => "badge-status-success",
                    TrangThaiDatBan.DaHuy => "badge-status-danger",
                    _ => isCompleted ? "badge-status-success" : "badge-secondary"
                };

                string trangThaiStr = x.TrangThai switch
                {
                    TrangThaiDatBan.ChoXacNhan => "Chờ xác nhận",
                    TrangThaiDatBan.DaXacNhan => "Đã xác nhận",
                    TrangThaiDatBan.DaNhanBan when !daThanhToan => "Đang phục vụ",
                    TrangThaiDatBan.DaHuy => "Đã hủy",
                    TrangThaiDatBan.KhongDen => "Không đến",
                    _ => isCompleted ? "Hoàn tất" : x.TrangThai.ToString()
                };

                return new DatBanItemVM
                {
                    MaDatBan = x.Id,
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
    public async Task<IActionResult> XacNhan(int datBanId, int banAnId)
    {
        if (datBanId <= 0 || banAnId <= 0)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn bàn ăn hợp lệ.";
            return RedirectToAction(nameof(Index), new { tab = "ChoXacNhan" });
        }

        var datBan = await Db.DatBan.Include(x => x.Ban).FirstOrDefaultAsync(x => x.Id == datBanId);
        if (datBan == null) return NotFound();

        var banAn = await Db.BanAn.Include(b => b.KhuVuc).FirstOrDefaultAsync(b => b.Id == banAnId);
        if (banAn == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy thông tin bàn ăn được chọn.";
            return RedirectToAction(nameof(Index), new { tab = "ChoXacNhan" });
        }

        // Kiểm tra xung đột thời gian trong khoảng +/- 2 tiếng
        var windowStart = datBan.GioDen.AddHours(-2);
        var windowEnd = datBan.GioDen.AddHours(2);

        var conflict = await Db.ChiTietDatBan
            .Include(c => c.DatBan)
            .AnyAsync(c => c.BanAnId == banAnId
                && c.DatBanId != datBan.Id
                && (c.DatBan.TrangThai == TrangThaiDatBan.DaXacNhan || c.DatBan.TrangThai == TrangThaiDatBan.DaNhanBan)
                && c.DatBan.GioDen > windowStart
                && c.DatBan.GioDen < windowEnd);

        if (conflict)
        {
            TempData["ErrorMessage"] = $"Bàn '{banAn.MaBan}' đã có khách đặt trong khoảng +/- 2 tiếng xung quanh thời điểm {datBan.GioDen.ToLocalTime():HH:mm dd/MM/yyyy}. Vui lòng chọn bàn khác!";
            return RedirectToAction(nameof(Index), new { tab = "ChoXacNhan" });
        }

        // Gán bàn
        Db.ChiTietDatBan.RemoveRange(datBan.Ban);
        Db.ChiTietDatBan.Add(new ChiTietDatBan { DatBanId = datBan.Id, BanAnId = banAnId });
        datBan.TrangThai = TrangThaiDatBan.DaXacNhan;

        await Db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Xác nhận thành công đơn '{datBan.MaDatBan}' và đã xếp bàn '{banAn.MaBan}'.";
        return RedirectToAction(nameof(Index), new { tab = "DaXacNhan" });
    }

    // POST /QuanLyDatBan/NhanBan
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NhanBan(int datBanId)
    {
        var datBan = await Db.DatBan
            .Include(x => x.Ban).ThenInclude(b => b.BanAn)
            .FirstOrDefaultAsync(x => x.Id == datBanId);

        if (datBan == null) return NotFound();

        if (!datBan.Ban.Any())
        {
            TempData["ErrorMessage"] = "Đơn đặt bàn chưa được xếp bàn ăn. Vui lòng bấm 'Xếp bàn' trước khi nhận bàn!";
            return RedirectToAction(nameof(Index), new { tab = "ChoXacNhan" });
        }

        // Tìm nhân viên hiện hành
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var accId);
        var staffId = await Db.NhanVien
            .Where(x => (x.TaiKhoanId == accId || accId == 0) && x.DangLamViec)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();

        // Giao dịch nguyên tử (Atomic Database Transaction)
        await using var tx = await Db.Database.BeginTransactionAsync();
        try
        {
            // 1. Chuyển trạng thái DatBan sang DaNhanBan
            datBan.TrangThai = TrangThaiDatBan.DaNhanBan;
            datBan.ThoiDiemNhanBan = DateTimeOffset.UtcNow;

            // 2. Chuyển trạng thái BanAn sang DangPhucVu
            foreach (var item in datBan.Ban)
            {
                item.BanAn.TrangThai = TrangThaiBan.DangPhucVu;
            }

            // 3. Tự động khởi tạo HoaDon nếu chưa có
            var invoiceExists = await Db.HoaDon.AnyAsync(h => h.DatBanId == datBan.Id && h.TrangThai != TrangThaiHoaDon.DaHuy);
            if (!invoiceExists)
            {
                string maHoaDon;
                do
                {
                    maHoaDon = $"HD-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
                } while (await Db.HoaDon.AnyAsync(h => h.MaHoaDon == maHoaDon));

                var hoaDon = new HoaDon
                {
                    MaHoaDon = maHoaDon,
                    DatBanId = datBan.Id,
                    KhachHangId = datBan.KhachHangId,
                    NhanVienId = staffId,
                    ThoiDiemLap = DateTimeOffset.UtcNow,
                    TrangThai = TrangThaiHoaDon.ChuaThanhToan,
                    TongTienHang = 0,
                    TienGiam = 0,
                    TienCocDaTru = 0,
                    PhuongThucThanhToan = PhuongThucThanhToan.TienMat
                };
                Db.HoaDon.Add(hoaDon);
            }

            await Db.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["SuccessMessage"] = $"Nhận bàn thành công cho đơn '{datBan.MaDatBan}'. Bàn đã chuyển sang trạng thái 'Đang phục vụ' và mở Hóa đơn mới.";
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            TempData["ErrorMessage"] = $"Lỗi khi thực hiện giao dịch nhận bàn: {ex.Message}";
        }

        return RedirectToAction(nameof(Index), new { tab = "DangPhucVu" });
    }

    // POST /QuanLyDatBan/HuyBan
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> HuyBan(int datBanId, string lyDoHuy)
    {
        var datBan = await Db.DatBan.Include(x => x.Ban).ThenInclude(b => b.BanAn).FirstOrDefaultAsync(x => x.Id == datBanId);
        if (datBan == null) return NotFound();

        datBan.TrangThai = TrangThaiDatBan.DaHuy;
        datBan.ThoiDiemHuy = DateTimeOffset.UtcNow;
        datBan.LyDoHuy = string.IsNullOrWhiteSpace(lyDoHuy) ? "Quản lý/Lễ tân hủy" : lyDoHuy.Trim();

        // Nếu bàn đang ở trạng thái DangPhucVu do đơn này, trả về Sẵn sàng
        foreach (var item in datBan.Ban)
        {
            if (item.BanAn.TrangThai == TrangThaiBan.DangPhucVu)
            {
                item.BanAn.TrangThai = TrangThaiBan.SanSang;
            }
        }

        await Db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Đã hủy đơn đặt bàn '{datBan.MaDatBan}'.";
        return RedirectToAction(nameof(Index), new { tab = "HoanTatHuy" });
    }

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
