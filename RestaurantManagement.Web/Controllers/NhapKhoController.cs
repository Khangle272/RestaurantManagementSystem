using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Security;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin + "," + AppRoles.Kho)]
public class NhapKhoController(RestaurantDbContext context) : ManagementControllerBase(context)
{
    public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, int? nccId, int page = 1)
    {
        var query = Db.PhieuNhap
            .Include(x => x.NhaCungCap)
            .Include(x => x.NhanVien)
            .Include(x => x.ChiTiet)
                .ThenInclude(c => c.NguyenLieu)
            .AsNoTracking();

        if (fromDate.HasValue)
        {
            var fromOffset = new DateTimeOffset(fromDate.Value.Date);
            query = query.Where(x => x.ThoiDiem >= fromOffset);
        }

        if (toDate.HasValue)
        {
            var toOffset = new DateTimeOffset(toDate.Value.Date.AddDays(1).AddTicks(-1));
            query = query.Where(x => x.ThoiDiem <= toOffset);
        }

        if (nccId.HasValue && nccId > 0)
        {
            query = query.Where(x => x.NhaCungCapId == nccId.Value);
        }

        const int pageSize = 10;
        var total = await query.CountAsync();
        var pages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, pages);

        var items = await query.OrderByDescending(x => x.ThoiDiem)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalValue = await query.SumAsync(x => x.TongTien);

        return View(new PhieuNhapIndexVM
        {
            Items = items,
            Pagination = new PaginationViewModel
            {
                CurrentPage = page,
                TotalPages = pages,
                TotalItems = total
            },
            FromDate = fromDate,
            ToDate = toDate,
            NhaCungCapId = nccId,
            NhaCungCapList = await Db.NhaCungCap.OrderBy(x => x.TenNhaCungCap).ToListAsync(),
            TongGiaTriNhap = totalValue,
            TongSoPhieu = total
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadDropdownsAsync();
        return View(new PhieuNhapCreateVM());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PhieuNhapCreateVM model)
    {
        model.ChiTiets = model.ChiTiets?.Where(c => c.MaNguyenLieu > 0 && c.SoLuong > 0).ToList() ?? new();

        if (!model.ChiTiets.Any())
        {
            ModelState.AddModelError("", "Vui lòng thêm ít nhất một nguyên liệu nhập kho với số lượng hợp lệ.");
        }

        if (ModelState.IsValid)
        {
            await using var trans = await Db.Database.BeginTransactionAsync();
            try
            {
                var staffId = await GetCurrentStaffIdAsync();
                var phieuNhap = new PhieuNhap
                {
                    MaPhieu = "PN-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
                    NhaCungCapId = model.MaNhaCungCap,
                    NhanVienId = staffId,
                    ThoiDiem = new DateTimeOffset(model.NgayNhap),
                    TrangThai = TrangThaiPhieu.DaGhiSo,
                    LyDo = LyDoNhap.MuaHang,
                    SoHoaDon = model.SoHoaDon?.Trim(),
                    GhiChu = model.GhiChu?.Trim(),
                    TongTien = model.ChiTiets.Sum(c => c.SoLuong * c.DonGiaNhap)
                };

                Db.PhieuNhap.Add(phieuNhap);
                await Db.SaveChangesAsync();

                foreach (var item in model.ChiTiets)
                {
                    var nl = await Db.NguyenLieu.FindAsync(item.MaNguyenLieu);
                    if (nl != null)
                    {
                        nl.SoLuongTon += item.SoLuong;
                        nl.DonGia = item.DonGiaNhap; // Cập nhật giá nhập mới nhất
                    }

                    Db.ChiTietPhieuNhap.Add(new ChiTietPhieuNhap
                    {
                        PhieuNhapId = phieuNhap.Id,
                        NguyenLieuId = item.MaNguyenLieu,
                        SoLuong = item.SoLuong,
                        DonGia = item.DonGiaNhap,
                        ThanhTien = item.SoLuong * item.DonGiaNhap,
                        HanSuDung = item.HanSuDung,
                        MaLo = item.SoLo?.Trim()
                    });
                }

                await Db.SaveChangesAsync();
                await trans.CommitAsync();

                TempData["Success"] = $"Lập phiếu nhập kho {phieuNhap.MaPhieu} thành công. Tồn kho và đơn giá đã được cập nhật.";
                return RedirectToAction(nameof(Details), new { id = phieuNhap.Id });
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                ModelState.AddModelError("", "Có lỗi xảy ra trong quá trình lưu phiếu nhập: " + ex.Message);
            }
        }

        await LoadDropdownsAsync();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var phieu = await Db.PhieuNhap
            .Include(x => x.NhaCungCap)
            .Include(x => x.NhanVien)
            .Include(x => x.ChiTiet)
                .ThenInclude(c => c.NguyenLieu)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (phieu == null) return NotFound();

        return View(new PhieuNhapDetailsVM { PhieuNhap = phieu });
    }

    private async Task LoadDropdownsAsync()
    {
        ViewBag.NhaCungCapList = await Db.NhaCungCap
            .Where(x => x.DangSuDung)
            .OrderBy(x => x.TenNhaCungCap)
            .ToListAsync();

        ViewBag.NguyenLieuList = await Db.NguyenLieu
            .Where(x => x.DangSuDung)
            .OrderBy(x => x.TenNguyenLieu)
            .Select(x => new
            {
                x.Id,
                x.TenNguyenLieu,
                x.DonViTinh,
                x.DonGia,
                x.SoLuongTon
            })
            .ToListAsync();
    }

    private async Task<int> GetCurrentStaffIdAsync()
    {
        var email = User.Identity?.Name;
        if (!string.IsNullOrEmpty(email))
        {
            var nv = await Db.NhanVien.FirstOrDefaultAsync(x => x.Email == email);
            if (nv != null) return nv.Id;
        }

        var warehouseNv = await Db.NhanVien.FirstOrDefaultAsync(x => x.ChucVu == "Nhân viên kho" || x.MaNhanVien == "NV003");
        if (warehouseNv != null) return warehouseNv.Id;

        var firstNv = await Db.NhanVien.FirstOrDefaultAsync();
        return firstNv?.Id ?? 1;
    }
}
