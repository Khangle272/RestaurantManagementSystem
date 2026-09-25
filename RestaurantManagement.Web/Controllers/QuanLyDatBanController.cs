using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Controllers;

public class QuanLyDatBanController(RestaurantDbContext context) : ManagementControllerBase(context)
{
    [HttpGet]
    public async Task<IActionResult> Index(string? status, DateTime? date)
    {
        var query = Db.DatBan.AsNoTracking()
            .Include(x => x.Ban).ThenInclude(x => x.BanAn).ThenInclude(x => x.KhuVuc)
            .AsQueryable();

        if (date.HasValue)
        {
            var day = DateOnly.FromDateTime(date.Value);
            query = query.Where(x => DateOnly.FromDateTime(x.GioDen.DateTime) == day);
        }

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<TrangThaiDatBan>(status, out var trangThai))
        {
            if (trangThai == TrangThaiDatBan.HoanTat)
                query = query.Where(x => x.TrangThai == TrangThaiDatBan.HoanTat
                    || x.TrangThai == TrangThaiDatBan.DaHuy || x.TrangThai == TrangThaiDatBan.KhongDen);
            else
                query = query.Where(x => x.TrangThai == trangThai);
        }

        var items = await query.OrderByDescending(x => x.GioDen).Take(100).ToListAsync();

        return View(new DatBanAdminVM
        {
            FilterTrangThai = status,
            FilterNgay = date.HasValue ? DateOnly.FromDateTime(date.Value) : null,
            DanhSachDatBan = items.Select(x =>
            {
                var ban = x.Ban.FirstOrDefault();
                return new DatBanAdminItemVM
                {
                    Id = x.Id, MaDatBan = x.MaDatBan, HoTen = x.HoTenLienHe,
                    SoDienThoai = x.SoDienThoaiLienHe, GioDen = x.GioDen,
                    SoNguoi = x.SoNguoiLon + x.SoTreEm,
                    TenKhuVuc = ban?.BanAn.KhuVuc?.TenKhuVuc,
                    TenBan = ban?.BanAn.MaBan, MaBanId = ban?.BanAnId,
                    TrangThai = x.TrangThai, YeuCau = x.YeuCau
                };
            }).ToList(),
            DanhSachBanTrong = await Db.BanAn.AsNoTracking()
                .Include(x => x.KhuVuc)
                .Where(x => x.TrangThai == TrangThaiBan.SanSang)
                .OrderBy(x => x.KhuVuc.TenKhuVuc).ThenBy(x => x.MaBan)
                .Select(x => new BanAnOption
                {
                    Id = x.Id, MaBan = x.MaBan,
                    TenKhuVuc = x.KhuVuc.TenKhuVuc, SoChoNgoi = x.SoChoNgoi
                }).ToListAsync(),
            ChoXacNhanCount = await Db.DatBan.CountAsync(x => x.TrangThai == TrangThaiDatBan.ChoXacNhan),
            DaXacNhanCount = await Db.DatBan.CountAsync(x => x.TrangThai == TrangThaiDatBan.DaXacNhan),
            DaNhanBanCount = await Db.DatBan.CountAsync(x => x.TrangThai == TrangThaiDatBan.DaNhanBan),
            HoanTatHuyCount = await Db.DatBan.CountAsync(x => x.TrangThai == TrangThaiDatBan.HoanTat
                || x.TrangThai == TrangThaiDatBan.DaHuy || x.TrangThai == TrangThaiDatBan.KhongDen)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> XacNhan(int maDatBan, int maBan)
    {
        var datBan = await Db.DatBan.Include(x => x.Ban).SingleOrDefaultAsync(x => x.Id == maDatBan);
        if (datBan == null) return NotFound();
        if (datBan.TrangThai != TrangThaiDatBan.ChoXacNhan)
        {
            TempData["Error"] = "Phiếu không ở trạng thái chờ xác nhận.";
            return RedirectToAction(nameof(Index));
        }
        var banAn = await Db.BanAn.FindAsync(maBan);
        if (banAn == null) { TempData["Error"] = "Bàn không tồn tại."; return RedirectToAction(nameof(Index)); }

        var gioDen = datBan.GioDen;
        var conflict = await Db.ChiTietDatBan.AnyAsync(ct =>
            ct.BanAnId == maBan && ct.DatBanId != maDatBan
            && (ct.DatBan.TrangThai == TrangThaiDatBan.DaXacNhan || ct.DatBan.TrangThai == TrangThaiDatBan.DaNhanBan)
            && ct.DatBan.GioDen >= gioDen.AddHours(-2) && ct.DatBan.GioDen <= gioDen.AddHours(2));
        if (conflict)
        {
            TempData["Error"] = $"Bàn {banAn.MaBan} đã có lịch đặt trong khoảng ±2 giờ. Vui lòng chọn bàn khác.";
            return RedirectToAction(nameof(Index));
        }

        datBan.Ban.Clear();
        datBan.Ban.Add(new ChiTietDatBan { DatBanId = datBan.Id, BanAnId = maBan });
        datBan.TrangThai = TrangThaiDatBan.DaXacNhan;
        await Db.SaveChangesAsync();

        TempData["Success"] = $"Đã xác nhận và gán bàn {banAn.MaBan}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NhanBan(int maDatBan)
    {
        await using var transaction = await Db.Database.BeginTransactionAsync();
        var datBan = await Db.DatBan
            .Include(x => x.Ban).ThenInclude(x => x.BanAn)
            .SingleOrDefaultAsync(x => x.Id == maDatBan);
        if (datBan == null) return NotFound();
        if (datBan.TrangThai != TrangThaiDatBan.DaXacNhan)
        {
            TempData["Error"] = "Phiếu không ở trạng thái đã xác nhận.";
            return RedirectToAction(nameof(Index));
        }
        var banLink = datBan.Ban.FirstOrDefault();
        if (banLink == null) { TempData["Error"] = "Chưa gán bàn."; return RedirectToAction(nameof(Index)); }

        datBan.TrangThai = TrangThaiDatBan.DaNhanBan;
        datBan.ThoiDiemNhanBan = DateTimeOffset.Now;
        banLink.BanAn.TrangThai = TrangThaiBan.DangPhucVu;

        // Find first active employee for NhanVienId
        var nhanVienId = await Db.NhanVien.Where(x => x.DangLamViec).Select(x => x.Id).FirstOrDefaultAsync();
        if (nhanVienId == 0) nhanVienId = await Db.NhanVien.Select(x => x.Id).FirstAsync();

        var now = DateTimeOffset.Now;
        var hdCode = $"HD-{now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        while (await Db.HoaDon.AnyAsync(x => x.MaHoaDon == hdCode))
            hdCode = $"HD-{now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";

        Db.HoaDon.Add(new HoaDon
        {
            MaHoaDon = hdCode, ThoiDiemLap = now, DatBanId = datBan.Id,
            KhachHangId = datBan.KhachHangId, NhanVienId = nhanVienId,
            TrangThai = TrangThaiHoaDon.ChuaThanhToan,
            TongTienHang = 0, TienGiam = 0, TienCocDaTru = 0,
            PhuongThucThanhToan = PhuongThucThanhToan.TienMat
        });

        await Db.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["Success"] = $"Check-in thành công. Hóa đơn {hdCode} đã được tạo.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> HoanTat(int maDatBan)
    {
        var datBan = await Db.DatBan
            .Include(x => x.Ban).ThenInclude(x => x.BanAn)
            .SingleOrDefaultAsync(x => x.Id == maDatBan);
        if (datBan == null) return NotFound();
        if (datBan.TrangThai != TrangThaiDatBan.DaNhanBan)
        {
            TempData["Error"] = "Chỉ có thể hoàn tất đơn đang phục vụ.";
            return RedirectToAction(nameof(Index));
        }
        datBan.TrangThai = TrangThaiDatBan.HoanTat;
        foreach (var b in datBan.Ban)
            b.BanAn.TrangThai = TrangThaiBan.SanSang;

        var hoaDon = await Db.HoaDon.Where(h => h.DatBanId == datBan.Id).FirstOrDefaultAsync();
        if (hoaDon != null && hoaDon.TrangThai == TrangThaiHoaDon.ChuaThanhToan)
        {
            hoaDon.TrangThai = TrangThaiHoaDon.DaThanhToan;
        }

        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã hoàn tất đơn đặt bàn.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> HuyBan(int maDatBan, string? lyDo)
    {
        var datBan = await Db.DatBan.Include(x => x.Ban).ThenInclude(x => x.BanAn)
            .SingleOrDefaultAsync(x => x.Id == maDatBan);
        if (datBan == null) return NotFound();
        if (datBan.TrangThai != TrangThaiDatBan.ChoXacNhan && datBan.TrangThai != TrangThaiDatBan.DaXacNhan)
        {
            TempData["Error"] = "Không thể hủy phiếu ở trạng thái này.";
            return RedirectToAction(nameof(Index));
        }
        datBan.TrangThai = TrangThaiDatBan.DaHuy;
        datBan.ThoiDiemHuy = DateTimeOffset.Now;
        datBan.LyDoHuy = lyDo?.Trim();
        foreach (var b in datBan.Ban)
            if (b.BanAn.TrangThai != TrangThaiBan.SanSang)
                b.BanAn.TrangThai = TrangThaiBan.SanSang;
        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã hủy đơn đặt bàn.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> DanhGia(string? soSao, string? phanHoi)
    {
        var query = Db.DanhGia.AsNoTracking()
            .Include(x => x.HoaDon).ThenInclude(x => x!.DatBan)
            .Where(x => x.ChiTietHoaDonId == null)
            .AsQueryable();

        if (int.TryParse(soSao, out var sf) && sf >= 1 && sf <= 5)
            query = query.Where(x => x.Diem == sf);
        if (phanHoi == "DaPhanHoi") query = query.Where(x => x.PhanHoi != null);
        else if (phanHoi == "ChuaPhanHoi") query = query.Where(x => x.PhanHoi == null);

        var items = await query.OrderByDescending(x => x.ThoiDiem).Take(50).ToListAsync();
        return View(new DanhGiaAdminVM
        {
            FilterSoSao = soSao, FilterPhanHoi = phanHoi,
            DanhSachDanhGia = items.Select(x => new DanhGiaAdminItemVM
            {
                Id = x.Id,
                TenKhachHang = x.HoaDon?.DatBan?.HoTenLienHe ?? "Khách vãng lai",
                SoDienThoai = x.HoaDon?.DatBan?.SoDienThoaiLienHe,
                ThoiDiem = x.ThoiDiem, DiemMonAn = x.Diem, DiemDichVu = x.DiemDichVu,
                NoiDung = x.NoiDung, HinhAnh = x.HinhAnh,
                PhanHoi = x.PhanHoi, ThoiDiemPhanHoi = x.ThoiDiemPhanHoi,
                MaDatBan = x.HoaDon?.DatBan?.MaDatBan
            }).ToList()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PhanHoiDanhGia(int maDanhGia, string phanHoi)
    {
        var dg = await Db.DanhGia.FindAsync(maDanhGia);
        if (dg == null) return NotFound();
        if (string.IsNullOrWhiteSpace(phanHoi))
        {
            TempData["Error"] = "Vui lòng nhập nội dung phản hồi.";
            return RedirectToAction(nameof(DanhGia));
        }
        dg.PhanHoi = phanHoi.Trim();
        dg.ThoiDiemPhanHoi = DateTimeOffset.Now;
        await Db.SaveChangesAsync();
        TempData["Success"] = "Phản hồi đã được gửi thành công.";
        return RedirectToAction(nameof(DanhGia));
    }
}
