using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Security;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin + "," + AppRoles.Kho)]
public class ThanhLyController(RestaurantDbContext context) : ManagementControllerBase(context)
{
    public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        var query = Db.PhieuThanhLy
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

        const int pageSize = 10;
        var total = await query.CountAsync();
        var pages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, pages);

        var items = await query.OrderByDescending(x => x.ThoiDiem)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalLoss = await query.SumAsync(x => x.TongTienThietHai);

        return View(new PhieuThanhLyIndexVM
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
            TongThietHai = totalLoss,
            TongSoPhieu = total
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadDropdownsAsync();
        return View(new PhieuThanhLyCreateVM());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PhieuThanhLyCreateVM model)
    {
        model.ChiTiets = model.ChiTiets?.Where(c => c.MaNguyenLieu > 0 && c.SoLuong > 0).ToList() ?? new();

        if (!model.ChiTiets.Any())
        {
            ModelState.AddModelError("", "Vui lòng chọn ít nhất một nguyên liệu cần thanh lý / tiêu hủy.");
        }

        if (ModelState.IsValid)
        {
            await using var trans = await Db.Database.BeginTransactionAsync();
            try
            {
                // Validate stock availability
                var requiredTotals = model.ChiTiets
                    .GroupBy(c => c.MaNguyenLieu)
                    .Select(g => new { MaNguyenLieu = g.Key, TongSoLuong = g.Sum(x => x.SoLuong) })
                    .ToList();

                foreach (var req in requiredTotals)
                {
                    var nl = await Db.NguyenLieu.FindAsync(req.MaNguyenLieu);
                    if (nl == null || nl.SoLuongTon < req.TongSoLuong)
                    {
                        throw new InvalidOperationException($"Nguyên liệu '{nl?.TenNguyenLieu ?? "N/A"}' không đủ tồn kho để thanh lý (Hiện tồn: {nl?.SoLuongTon.ToString("0.##") ?? "0"}, Đề nghị thanh lý: {req.TongSoLuong.ToString("0.##")}).");
                    }
                }

                var staffId = await GetCurrentStaffIdAsync();
                var phieuThanhLy = new PhieuThanhLy
                {
                    MaPhieu = "TL-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(),
                    NhanVienId = staffId,
                    ThoiDiem = new DateTimeOffset(model.NgayThanhLy),
                    LyDoThanhLy = model.LyDoThanhLy,
                    GhiChu = model.GhiChu?.Trim(),
                    TrangThai = "DaThanhLy"
                };

                decimal totalLoss = 0;
                foreach (var item in model.ChiTiets)
                {
                    var nl = await Db.NguyenLieu.FindAsync(item.MaNguyenLieu);
                    if (nl != null)
                    {
                        nl.SoLuongTon -= item.SoLuong;
                        var cost = item.SoLuong * nl.DonGia;
                        totalLoss += cost;

                        phieuThanhLy.ChiTiet.Add(new ChiTietPhieuThanhLy
                        {
                            NguyenLieuId = item.MaNguyenLieu,
                            SoLuong = item.SoLuong,
                            DonGiaVon = nl.DonGia,
                            ThanhTien = cost,
                            GhiChu = item.GhiChu?.Trim()
                        });
                    }
                }

                phieuThanhLy.TongTienThietHai = totalLoss;
                Db.PhieuThanhLy.Add(phieuThanhLy);

                await Db.SaveChangesAsync();
                await trans.CommitAsync();

                TempData["Success"] = $"Lập phiếu thanh lý {phieuThanhLy.MaPhieu} thành công. Tổng thiệt hại: {phieuThanhLy.TongTienThietHai:N0} VNĐ.";
                return RedirectToAction(nameof(Details), new { id = phieuThanhLy.Id });
            }
            catch (InvalidOperationException ex)
            {
                await trans.RollbackAsync();
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                ModelState.AddModelError("", "Lỗi khi lưu phiếu thanh lý: " + ex.Message);
            }
        }

        await LoadDropdownsAsync();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var phieu = await Db.PhieuThanhLy
            .Include(x => x.NhanVien)
            .Include(x => x.ChiTiet)
                .ThenInclude(c => c.NguyenLieu)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (phieu == null) return NotFound();

        return View(new PhieuThanhLyDetailsVM { PhieuThanhLy = phieu });
    }

    private async Task LoadDropdownsAsync()
    {
        ViewBag.NguyenLieuList = await Db.NguyenLieu
            .Where(x => x.DangSuDung && x.SoLuongTon > 0)
            .OrderBy(x => x.TenNguyenLieu)
            .Select(x => new
            {
                x.Id,
                x.TenNguyenLieu,
                x.DonViTinh,
                x.SoLuongTon,
                x.DonGia
            })
            .ToListAsync();
    }

    private async Task<int> GetCurrentStaffIdAsync()
    {
        var email = User.Identity?.Name;
        if (!string.IsNullOrEmpty(email))
        {
            var nv = await Db.NhanVien.FirstOrDefaultAsync(x => x.Email == email && x.DangLamViec);
            if (nv != null) return nv.Id;

            if (int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var accountId))
            {
                var nvAccount = await Db.NhanVien.FirstOrDefaultAsync(x => x.TaiKhoanId == accountId && x.DangLamViec);
                if (nvAccount != null) return nvAccount.Id;
            }
        }

        var warehouseNv = await Db.NhanVien.FirstOrDefaultAsync(x => (x.ChucVu == "Nhân viên kho" || x.MaNhanVien == "NV003") && x.DangLamViec);
        if (warehouseNv != null) return warehouseNv.Id;

        var firstNv = await Db.NhanVien.FirstOrDefaultAsync(x => x.DangLamViec);
        return firstNv?.Id ?? 1;
    }
}
