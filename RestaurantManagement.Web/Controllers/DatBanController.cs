using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Controllers;

public class DatBanController(RestaurantDbContext context, IWebHostEnvironment environment) : Controller
{
    private readonly RestaurantDbContext _db = context;
    private readonly IWebHostEnvironment _env = environment;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new DatBanCreateVM
        {
            KhuVucOptions = await _db.KhuVuc.AsNoTracking()
                .Where(x => x.DangSuDung)
                .OrderBy(x => x.TenKhuVuc)
                .Select(x => new SelectListItem(
                    x.LaPhongVip ? $"{x.TenKhuVuc} (VIP)" : x.TenKhuVuc, x.Id.ToString()))
                .ToListAsync()
        };
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(DatBanCreateVM model)
    {
        if (model.ThoiGianDen.HasValue && model.ThoiGianDen.Value <= DateTimeOffset.Now.AddMinutes(30))
            ModelState.AddModelError(nameof(model.ThoiGianDen), "Thời gian đến phải ít nhất 30 phút sau thời điểm hiện tại.");

        if (!ModelState.IsValid)
        {
            model.KhuVucOptions = await LoadKhuVucOptionsAsync();
            return View(model);
        }

        var now = DateTimeOffset.Now;
        var rng = Random.Shared;
        var bookingCode = $"BK-{now:yyyyMMdd}-{rng.Next(1000, 9999)}";
        while (await _db.DatBan.AnyAsync(x => x.MaDatBan == bookingCode))
            bookingCode = $"BK-{now:yyyyMMdd}-{rng.Next(1000, 9999)}";

        string? khuVucNote = null;
        if (model.MaKhuVuc.HasValue)
        {
            var kv = await _db.KhuVuc.AsNoTracking().SingleOrDefaultAsync(x => x.Id == model.MaKhuVuc.Value);
            if (kv != null) khuVucNote = $"Khu vực mong muốn: {kv.TenKhuVuc}";
        }
        var ghiChu = string.Join(". ", new[] { khuVucNote, model.GhiChu }.Where(s => !string.IsNullOrEmpty(s)));

        var datBan = new DatBan
        {
            MaDatBan = bookingCode,
            HoTenLienHe = model.HoTen.Trim(),
            SoDienThoaiLienHe = model.SoDienThoai.Trim(),
            EmailLienHe = model.Email?.Trim(),
            LaKhachTrucTiep = false,
            ThoiDiemTao = now,
            GioDen = model.ThoiGianDen!.Value,
            GioKetThucDuKien = model.ThoiGianDen!.Value.AddHours(2),
            SoNguoiLon = model.SoNguoi,
            SoTreEm = 0,
            YeuCau = string.IsNullOrWhiteSpace(ghiChu) ? null : ghiChu,
            YeuCauVip = model.MaKhuVuc.HasValue && await _db.KhuVuc.AnyAsync(x => x.Id == model.MaKhuVuc && x.LaPhongVip),
            TrangThai = TrangThaiDatBan.ChoXacNhan
        };

        _db.DatBan.Add(datBan);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Success), new { id = datBan.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Success(int id)
    {
        var datBan = await _db.DatBan.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (datBan == null) return NotFound();
        ViewBag.BookingCode = datBan.MaDatBan;
        ViewBag.HoTen = datBan.HoTenLienHe;
        ViewBag.GioDen = datBan.GioDen;
        ViewBag.SoNguoi = datBan.SoNguoiLon + datBan.SoTreEm;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> LichSu(string? sdt, string? code)
    {
        var model = new DatBanLookupVM { SoDienThoai = sdt, BookingCode = code };

        if (!string.IsNullOrWhiteSpace(sdt) || !string.IsNullOrWhiteSpace(code))
        {
            var query = _db.DatBan.AsNoTracking()
                .Include(x => x.Ban).ThenInclude(x => x.BanAn)
                .Include(x => x.HoaDon).ThenInclude(h => h.ChiTiet)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(sdt))
                query = query.Where(x => x.SoDienThoaiLienHe == sdt.Trim());
            if (!string.IsNullOrWhiteSpace(code))
                query = query.Where(x => x.MaDatBan == code.Trim().ToUpper());

            var results = await query.OrderByDescending(x => x.GioDen).Take(20).ToListAsync();

            var hoaDonIds = results.SelectMany(x => x.HoaDon).Select(x => x.Id).ToList();
            var reviewedIds = await _db.DanhGia.AsNoTracking()
                .Where(x => hoaDonIds.Contains(x.HoaDonId) && x.ChiTietHoaDonId == null)
                .Select(x => x.HoaDonId).ToListAsync();

            model.DanhSachDatBan = results.Select(db =>
            {
                var firstBan = db.Ban.FirstOrDefault();
                var hoaDon = db.HoaDon.FirstOrDefault();
                bool hoanTat = db.TrangThai == TrangThaiDatBan.HoanTat
                    || (hoaDon != null && hoaDon.TrangThai == TrangThaiHoaDon.DaThanhToan);
                return new DatBanItemVM
                {
                    MaDatBan = db.Id,
                    BookingCode = db.MaDatBan,
                    ThoiGianDen = db.GioDen,
                    SoNguoi = db.SoNguoiLon + db.SoTreEm,
                    TenBan = firstBan?.BanAn.MaBan,
                    TrangThai = hoanTat ? TrangThaiDatBan.HoanTat : db.TrangThai,
                    TongTienHoaDon = hoaDon?.TongThanhToan,
                    ChiTietMonAn = hoaDon?.ChiTiet.Select(ct => new ChiTietMonVM
                    {
                        TenMon = ct.TenMonLucBan,
                        SoLuong = ct.SoLuong,
                        DonGia = ct.DonGia
                    }).ToList() ?? new(),
                    DaDanhGia = hoaDon != null && reviewedIds.Contains(hoaDon.Id),
                    TrangThaiHoaDon = hoaDon?.TrangThai.ToString()
                };
            }).ToList();
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> DanhGia(int maDatBan)
    {
        var datBan = await _db.DatBan.AsNoTracking()
            .Include(x => x.Ban).ThenInclude(x => x.BanAn)
            .Include(x => x.HoaDon)
            .SingleOrDefaultAsync(x => x.Id == maDatBan);
        if (datBan == null) return NotFound();

        var hoaDon = datBan.HoaDon.FirstOrDefault();
        bool isComplete = (datBan.TrangThai == TrangThaiDatBan.HoanTat
            || (hoaDon != null && hoaDon.TrangThai == TrangThaiHoaDon.DaThanhToan))
            && hoaDon != null;
        if (!isComplete)
        {
            TempData["Error"] = "Đơn đặt bàn chưa hoàn tất hoặc chưa có hóa đơn thanh toán.";
            return RedirectToAction(nameof(LichSu));
        }
        if (await _db.DanhGia.AnyAsync(x => x.HoaDonId == hoaDon!.Id && x.ChiTietHoaDonId == null))
        {
            TempData["Error"] = "Bạn đã đánh giá đơn đặt bàn này rồi.";
            return RedirectToAction(nameof(LichSu));
        }

        var firstBan = datBan.Ban.FirstOrDefault();
        return View(new DanhGiaCreateVM
        {
            MaDatBan = datBan.Id,
            TenKhachHang = datBan.HoTenLienHe,
            ThoiGianDen = datBan.GioDen,
            TenBan = firstBan?.BanAn.MaBan
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DanhGia(DanhGiaCreateVM model)
    {
        var datBan = await _db.DatBan.Include(x => x.HoaDon).SingleOrDefaultAsync(x => x.Id == model.MaDatBan);
        if (datBan == null) return NotFound();

        var hoaDon = datBan.HoaDon.FirstOrDefault();
        if (hoaDon == null)
        {
            TempData["Error"] = "Không tìm thấy hóa đơn liên kết.";
            return RedirectToAction(nameof(LichSu));
        }
        if (await _db.DanhGia.AnyAsync(x => x.HoaDonId == hoaDon.Id && x.ChiTietHoaDonId == null))
        {
            TempData["Error"] = "Bạn đã đánh giá đơn này rồi.";
            return RedirectToAction(nameof(LichSu));
        }
        if (!ModelState.IsValid) return View(model);

        string? imagePath = null;
        if (model.HinhAnhFile is { Length: > 0 })
        {
            if (model.HinhAnhFile.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError(nameof(model.HinhAnhFile), "Ảnh tối đa 5 MB.");
                return View(model);
            }
            var folder = Path.Combine(_env.WebRootPath, "uploads", "reviews");
            Directory.CreateDirectory(folder);
            var ext = Path.GetExtension(model.HinhAnhFile.FileName);
            if (string.IsNullOrEmpty(ext)) ext = ".jpg";
            var fileName = Guid.NewGuid().ToString("N") + ext;
            await using (var stream = System.IO.File.Create(Path.Combine(folder, fileName)))
                await model.HinhAnhFile.CopyToAsync(stream);
            imagePath = "/uploads/reviews/" + fileName;
        }

        _db.DanhGia.Add(new DanhGia
        {
            HoaDonId = hoaDon.Id,
            KhachHangId = datBan.KhachHangId,
            Diem = model.SoSaoMonAn,
            DiemDichVu = model.SoSaoDichVu,
            NoiDung = model.NoiDung?.Trim(),
            HinhAnh = imagePath,
            ThoiDiem = DateTimeOffset.Now
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = "Cảm ơn bạn đã gửi đánh giá!";
        return RedirectToAction(nameof(LichSu), new { sdt = datBan.SoDienThoaiLienHe });
    }

    private async Task<List<SelectListItem>> LoadKhuVucOptionsAsync() =>
        await _db.KhuVuc.AsNoTracking()
            .Where(x => x.DangSuDung)
            .OrderBy(x => x.TenKhuVuc)
            .Select(x => new SelectListItem(
                x.LaPhongVip ? $"{x.TenKhuVuc} (VIP)" : x.TenKhuVuc, x.Id.ToString()))
            .ToListAsync();
}
