using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Controllers;

[AllowAnonymous]
public class DatBanController(RestaurantDbContext db, IWebHostEnvironment env) : Controller
{
    // GET /DatBan
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new DatBanCreateVM
        {
            ThoiGianDen = DateTime.Now.Date.AddHours(18).AddMinutes(30) > DateTime.Now.AddMinutes(30)
                ? DateTime.Now.Date.AddHours(18).AddMinutes(30)
                : DateTime.Now.AddHours(2),
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

        // Sinh mã booking dạng BK-YYYYMMDD-XXXX
        string bookingCode;
        do
        {
            bookingCode = $"BK-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        } while (await db.DatBan.AnyAsync(x => x.MaDatBan == bookingCode));

        // Liên kết khách hàng nếu số điện thoại đã có trong hệ thống
        var khachHang = await db.KhachHang.FirstOrDefaultAsync(x => x.SoDienThoai == model.SoDienThoai.Trim());

        var thoiGianDenUtc = new DateTimeOffset(model.ThoiGianDen);
        var ghiChuDayDu = string.IsNullOrWhiteSpace(model.Email)
            ? model.GhiChu?.Trim()
            : $"Email: {model.Email.Trim()}{(string.IsNullOrWhiteSpace(model.GhiChu) ? "" : " | " + model.GhiChu.Trim())}";

        var entity = new DatBan
        {
            MaDatBan = bookingCode,
            HoTenLienHe = model.HoTen.Trim(),
            SoDienThoaiLienHe = model.SoDienThoai.Trim(),
            KhachHangId = khachHang?.Id,
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

        TempData["SuccessMessage"] = "Đặt bàn thành công! Quý khách vui lòng lưu lại mã đặt bàn để tiện theo dõi.";
        return RedirectToAction(nameof(Success), new { id = entity.Id });
    }

    // GET /DatBan/Success/{id}
    [HttpGet]
    public async Task<IActionResult> Success(int id)
    {
        var datBan = await db.DatBan
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

        if (string.IsNullOrWhiteSpace(model.SoDienThoai) && string.IsNullOrWhiteSpace(model.BookingCode))
        {
            return View(model);
        }

        var query = db.DatBan
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

        var datBan = await db.DatBan
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
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DanhGia(DanhGiaCreateVM model)
    {
        if (!ModelState.IsValid) return View(model);

        var datBan = await db.DatBan
            .Include(x => x.HoaDon)
            .FirstOrDefaultAsync(x => x.MaDatBan == model.MaDatBan || x.Id.ToString() == model.MaDatBan);

        if (datBan == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy thông tin đơn đặt bàn.";
            return RedirectToAction(nameof(LichSu));
        }

        var hoaDon = datBan.HoaDon.OrderByDescending(h => h.Id).FirstOrDefault();
        if (hoaDon == null)
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
            var uploadsFolder = Path.Combine(env.WebRootPath, "uploads", "reviews");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var ext = Path.GetExtension(model.HinhAnhFile.FileName);
            var fileName = $"rev_{DateTime.UtcNow:yyyyMMddHHmmss}_{Random.Shared.Next(1000, 9999)}{ext}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            await using var stream = new FileStream(filePath, FileMode.Create);
            await model.HinhAnhFile.CopyToAsync(stream);
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
