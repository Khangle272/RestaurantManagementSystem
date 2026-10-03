using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Security;
using RestaurantManagement.Web.Services;
using System.Security.Claims;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.KhachHang)]
public class DatBanController(RestaurantDbContext db, IWebHostEnvironment env) : Controller
{
    private int AccountId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    private IQueryable<DatBan> OwnedBookings() => db.DatBan.Where(x => x.KhachHang != null && x.KhachHang.TaiKhoanId == AccountId);
    // GET /DatBan
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new DatBanCreateVM
        {
            ThoiGianDen = TableService.VietnamNow.AddHours(2),
            KhuVucOptions = await GetKhuVucOptionsAsync()
        };
        return View(model);
    }

    // POST /DatBan
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(DatBanCreateVM model)
    {
        if (!ModelState.IsValid)
        {
            model.KhuVucOptions = await GetKhuVucOptionsAsync();
            return View(model);
        }

        var bookingCode = TableService.NewCode("BK");
        var khachHang = await db.KhachHang.SingleOrDefaultAsync(x => x.TaiKhoanId == AccountId);
        if (khachHang is null) return Forbid();
        if (model.MaKhuVuc.HasValue && !await db.KhuVuc.AnyAsync(x => x.Id == model.MaKhuVuc && x.DangSuDung))
        {
            ModelState.AddModelError(nameof(model.MaKhuVuc), "Khu vực không còn sử dụng.");
            model.KhuVucOptions = await GetKhuVucOptionsAsync();
            return View(model);
        }

        var thoiGianDenUtc = TableService.VietnamTime(model.ThoiGianDen);
        var ghiChuDayDu = string.IsNullOrWhiteSpace(model.Email)
            ? model.GhiChu?.Trim()
            : $"Email: {model.Email.Trim()}{(string.IsNullOrWhiteSpace(model.GhiChu) ? "" : " | " + model.GhiChu.Trim())}";

        var entity = new DatBan
        {
            MaDatBan = bookingCode,
            HoTenLienHe = model.HoTen.Trim(),
            SoDienThoaiLienHe = model.SoDienThoai.Trim(),
            KhachHangId = khachHang?.Id,
            KhuVucUuTienId = model.MaKhuVuc,
            ThoiDiemTao = DateTimeOffset.UtcNow,
            GioDen = thoiGianDenUtc,
            GioKetThucDuKien = thoiGianDenUtc.AddHours(2),
            SoNguoiLon = model.SoNguoi,
            SoTreEm = 0,
            YeuCau = ghiChuDayDu,
            TrangThai = TrangThaiDatBan.ChoXacNhan,
            TrangThaiCoc = TrangThaiCoc.ChuaCoc,
            LaKhachTrucTiep = false
        };

        db.DatBan.Add(entity);
        await db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã gửi yêu cầu đặt bàn. Nhà hàng sẽ liên hệ xác nhận và xếp bàn phù hợp.";
        return RedirectToAction(nameof(Success), new { id = entity.Id });
    }

    // GET /DatBan/Success/{id}
    [HttpGet]
    public async Task<IActionResult> Success(int id)
    {
        var datBan = await OwnedBookings()
            .Include(x => x.Ban).ThenInclude(b => b.BanAn).ThenInclude(b => b.KhuVuc)
            .Include(x => x.KhachHang)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (datBan == null) return NotFound();
        return View(datBan);
    }

    // GET /DatBan/LichSu
    [HttpGet]
    public async Task<IActionResult> LichSu(string? soDienThoai, string? bookingCode)
    {
        var model = new DatBanLookupVM
        {
            SoDienThoai = soDienThoai?.Trim(),
            BookingCode = bookingCode?.Trim()
        };

        var query = OwnedBookings()
            .AsSplitQuery()
            .Include(x => x.Ban).ThenInclude(b => b.BanAn)
            .Include(x => x.HoaDon).ThenInclude(h => h.ChiTiet).ThenInclude(c => c.MonAn)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(model.BookingCode))
        {
            query = query.Where(x => x.MaDatBan == model.BookingCode);
        }
        else if (!string.IsNullOrWhiteSpace(model.SoDienThoai))
        {
            query = query.Where(x => x.SoDienThoaiLienHe == model.SoDienThoai);
        }

        var list = await query.OrderByDescending(x => x.GioDen).Take(30).ToListAsync();
        var hoaDonIds = list.SelectMany(x => x.HoaDon).Select(h => h.Id).ToList();
        var reviewedHoaDonIds = await db.DanhGia
            .Where(d => hoaDonIds.Contains(d.HoaDonId) && d.ChiTietHoaDonId == null)
            .Select(d => d.HoaDonId)
            .ToListAsync();

        model.DanhSachDatBan = list.Select(d =>
        {
            var activeInvoice = d.HoaDon.OrderByDescending(h => h.Id).FirstOrDefault();
            var daThanhToan = activeInvoice?.TrangThai == TrangThaiHoaDon.DaThanhToan;
            var isCompleted = d.TrangThai == TrangThaiDatBan.DaNhanBan && daThanhToan;

            string trangThaiText;
            string badgeClass;

            if (isCompleted)
            {
                trangThaiText = "Hoàn tất";
                badgeClass = "badge-status-success";
            }
            else if (d.TrangThai == TrangThaiDatBan.DaNhanBan)
            {
                trangThaiText = "Đang phục vụ";
                badgeClass = "badge-status-success";
            }
            else if (d.TrangThai == TrangThaiDatBan.DaXacNhan)
            {
                trangThaiText = "Đã xác nhận";
                badgeClass = "badge-status-info text-white bg-primary";
            }
            else if (d.TrangThai == TrangThaiDatBan.DaHuy)
            {
                trangThaiText = "Đã hủy";
                badgeClass = "badge-status-danger";
            }
            else
            {
                trangThaiText = "Chờ xác nhận";
                badgeClass = "badge-status-warning";
            }

            return new DatBanItemVM
            {
                MaDatBan = d.Id,
                BookingCode = d.MaDatBan,
                HoTen = d.HoTenLienHe,
                SoDienThoai = d.SoDienThoaiLienHe ?? "",
                ThoiGianDen = d.GioDen,
                SoNguoi = d.SoNguoiLon + d.SoTreEm,
                TenBan = d.Ban.Any() ? string.Join(", ", d.Ban.Select(b => b.BanAn.MaBan)) : "Chưa xếp bàn",
                TrangThai = trangThaiText,
                TrangThaiBadgeClass = badgeClass,
                HoaDonId = activeInvoice?.Id,
                TongTienHoaDon = activeInvoice?.ChiTiet.Sum(c => c.SoLuong * c.DonGia) ?? 0,
                DaThanhToan = daThanhToan,
                DaDanhGia = activeInvoice != null && reviewedHoaDonIds.Contains(activeInvoice.Id),
                GhiChu = d.YeuCau,
                ChiTietMonAn = activeInvoice?.ChiTiet.Select(c => new ChiTietMonAnItemVM
                {
                    TenMon = c.TenMonLucBan,
                    SoLuong = c.SoLuong,
                    DonGia = c.DonGia
                }).ToList() ?? []
            };
        }).ToList();

        return View(model);
    }

    // GET /DatBan/DanhGia/{maDatBan}
    [HttpGet]
    public async Task<IActionResult> DanhGia(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();

        var datBan = await OwnedBookings()
            .Include(x => x.HoaDon)
            .FirstOrDefaultAsync(x => x.MaDatBan == id || x.Id.ToString() == id);

        if (datBan == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy thông tin đơn đặt bàn.";
            return RedirectToAction(nameof(LichSu));
        }

        var hoaDon = datBan.HoaDon.OrderByDescending(h => h.Id).FirstOrDefault();
        var daThanhToan = hoaDon?.TrangThai == TrangThaiHoaDon.DaThanhToan;
        var isCompleted = datBan.TrangThai == TrangThaiDatBan.DaNhanBan && daThanhToan;

        if (!isCompleted || hoaDon == null)
        {
            TempData["ErrorMessage"] = "Chỉ những đơn đặt bàn đã hoàn tất dùng bữa và thanh toán mới có thể gửi đánh giá dịch vụ.";
            return RedirectToAction(nameof(LichSu), new { bookingCode = datBan.MaDatBan });
        }

        var daDanhGia = await db.DanhGia.AnyAsync(d => d.HoaDonId == hoaDon.Id && d.ChiTietHoaDonId == null);
        if (daDanhGia)
        {
            TempData["ErrorMessage"] = "Đơn đặt bàn này đã được gửi đánh giá trước đó. Cảm ơn quý khách!";
            return RedirectToAction(nameof(LichSu), new { bookingCode = datBan.MaDatBan });
        }

        var model = new DanhGiaCreateVM
        {
            MaDatBan = datBan.MaDatBan,
            TenKhachHang = datBan.HoTenLienHe,
            ThoiGianDen = datBan.GioDen
        };

        return View(model);
    }

    // POST /DatBan/DanhGia
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> DanhGia(DanhGiaCreateVM model)
    {
        if (!ModelState.IsValid) return View(model);

        var datBan = await OwnedBookings()
            .Include(x => x.HoaDon)
            .FirstOrDefaultAsync(x => x.MaDatBan == model.MaDatBan || x.Id.ToString() == model.MaDatBan);

        if (datBan == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy thông tin đơn đặt bàn.";
            return RedirectToAction(nameof(LichSu));
        }

        var hoaDon = datBan.HoaDon.OrderByDescending(h => h.Id).FirstOrDefault();
        if (hoaDon == null || hoaDon.TrangThai != TrangThaiHoaDon.DaThanhToan || datBan.TrangThai != TrangThaiDatBan.DaNhanBan)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hóa đơn tương ứng.";
            return RedirectToAction(nameof(LichSu));
        }

        var daDanhGia = await db.DanhGia.AnyAsync(d => d.HoaDonId == hoaDon.Id && d.ChiTietHoaDonId == null);
        if (daDanhGia)
        {
            TempData["ErrorMessage"] = "Đơn đặt bàn này đã được gửi đánh giá trước đó.";
            return RedirectToAction(nameof(LichSu), new { bookingCode = datBan.MaDatBan });
        }

        string? hinhAnhUrl = null;
        if (model.HinhAnhFile != null && model.HinhAnhFile.Length > 0)
        {
            await using var source = model.HinhAnhFile.OpenReadStream();
            var header = new byte[8];
            var read = await source.ReadAsync(header);
            var png = read == 8 && header.AsSpan().SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
            var jpeg = read >= 3 && header[0] == 255 && header[1] == 216 && header[2] == 255;
            if (model.HinhAnhFile.Length > 2 * 1024 * 1024 || (!png && !jpeg))
            { ModelState.AddModelError(nameof(model.HinhAnhFile), "Chỉ nhận ảnh PNG/JPEG tối đa 2 MB."); return View(model); }
            var uploadsFolder = Path.Combine(env.WebRootPath, "uploads", "reviews");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var fileName = $"rev_{Guid.NewGuid():N}" + (png ? ".png" : ".jpg");
            var filePath = Path.Combine(uploadsFolder, fileName);
            await using var stream = new FileStream(filePath, FileMode.Create);
            await stream.WriteAsync(header.AsMemory(0, read));
            await source.CopyToAsync(stream);
            hinhAnhUrl = $"/uploads/reviews/{fileName}";
        }

        var score = Math.Clamp((int)Math.Round((model.SoSaoMonAn + model.SoSaoDichVu) / 2.0), 1, 5);
        var metaTag = $"[Món ăn: {model.SoSaoMonAn}★ | Dịch vụ: {model.SoSaoDichVu}★]";
        var imgTag = string.IsNullOrEmpty(hinhAnhUrl) ? "" : $"\n[Ảnh: {hinhAnhUrl}]";
        var fullNoiDung = $"{model.NoiDung.Trim()}\n{metaTag}{imgTag}";

        var danhGia = new DanhGia
        {
            HoaDonId = hoaDon.Id,
            KhachHangId = datBan.KhachHangId,
            Diem = score,
            NoiDung = fullNoiDung.Length > 2000 ? fullNoiDung[..2000] : fullNoiDung,
            ThoiDiem = DateTimeOffset.UtcNow
        };

        db.DanhGia.Add(danhGia);
        await db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Gửi đánh giá thành công! Cảm ơn quý khách đã đóng góp ý kiến để nhà hàng hoàn thiện chất lượng dịch vụ.";
        return RedirectToAction(nameof(LichSu), new { bookingCode = datBan.MaDatBan });
    }

    private async Task<List<SelectListItem>> GetKhuVucOptionsAsync() =>
        await db.KhuVuc.AsNoTracking()
            .Where(x => x.DangSuDung)
            .OrderBy(x => x.TenKhuVuc)
            .Select(x => new SelectListItem(x.TenKhuVuc + (x.LaPhongVip ? " (Phòng VIP)" : ""), x.Id.ToString()))
            .ToListAsync();
}
