using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Security;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin + "," + AppRoles.Kho)]
public class XuatKhoController(RestaurantDbContext context) : ManagementControllerBase(context)
{
    public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, string? boPhan, int page = 1)
    {
        var query = Db.PhieuXuat
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

        if (!string.IsNullOrEmpty(boPhan))
        {
            query = query.Where(x => x.BoPhanNhan == boPhan);
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

        return View(new PhieuXuatIndexVM
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
            BoPhan = boPhan,
            TongGiaTriXuat = totalValue,
            TongSoPhieu = total
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadDropdownsAsync();
        return View(new PhieuXuatCreateVM());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PhieuXuatCreateVM model)
    {
        model.ChiTiets = model.ChiTiets?.Where(c => c.MaNguyenLieu > 0 && c.SoLuong > 0).ToList() ?? new();

        if (!model.ChiTiets.Any())
        {
            ModelState.AddModelError("", "Vui lòng chọn ít nhất một nguyên liệu cần xuất với số lượng hợp lệ.");
        }

        if (ModelState.IsValid)
        {
            await using var trans = await Db.Database.BeginTransactionAsync();
            try
            {
                // Validate stock availability for all items before modifying
                foreach (var item in model.ChiTiets)
                {
                    var nl = await Db.NguyenLieu.FindAsync(item.MaNguyenLieu);
                    if (nl == null || nl.SoLuongTon < item.SoLuong)
                    {
                        throw new InvalidOperationException($"Nguyên liệu '{nl?.TenNguyenLieu ?? "N/A"}' không đủ tồn kho để xuất (Hiện tồn: {nl?.SoLuongTon.ToString("0.##") ?? "0"}, Yêu cầu xuất: {item.SoLuong.ToString("0.##")}).");
                    }
                }

                var staffId = await GetCurrentStaffIdAsync();
                var phieuXuat = new PhieuXuat
                {
                    MaPhieu = "PX-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(),
                    NhanVienId = staffId,
                    ThoiDiem = new DateTimeOffset(model.NgayXuat),
                    TrangThai = TrangThaiPhieu.DaGhiSo,
                    LyDo = LyDoXuat.CheBien,
                    BoPhanNhan = model.BoPhanNhan,
                    NguoiNhan = model.NguoiNhan?.Trim(),
                    GhiChu = model.GhiChu?.Trim()
                };

                decimal totalVal = 0;
                foreach (var item in model.ChiTiets)
                {
                    var nl = await Db.NguyenLieu.FindAsync(item.MaNguyenLieu);
                    if (nl != null)
                    {
                        nl.SoLuongTon -= item.SoLuong;
                        var itemTotal = item.SoLuong * nl.DonGia;
                        totalVal += itemTotal;

                        phieuXuat.ChiTiet.Add(new ChiTietPhieuXuat
                        {
                            NguyenLieuId = item.MaNguyenLieu,
                            SoLuong = item.SoLuong,
                            DonGiaXuat = nl.DonGia,
                            ThanhTien = itemTotal
                        });
                    }
                }

                phieuXuat.TongTien = totalVal;
                Db.PhieuXuat.Add(phieuXuat);

                await Db.SaveChangesAsync();
                await trans.CommitAsync();

                TempData["Success"] = $"Lập phiếu xuất kho {phieuXuat.MaPhieu} thành công. Tồn kho đã được giảm trừ.";
                return RedirectToAction(nameof(Details), new { id = phieuXuat.Id });
            }
            catch (InvalidOperationException ex)
            {
                await trans.RollbackAsync();
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                ModelState.AddModelError("", "Lỗi khi lưu phiếu xuất: " + ex.Message);
            }
        }

        await LoadDropdownsAsync();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var phieu = await Db.PhieuXuat
            .Include(x => x.NhanVien)
            .Include(x => x.ChiTiet)
                .ThenInclude(c => c.NguyenLieu)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (phieu == null) return NotFound();

        return View(new PhieuXuatDetailsVM { PhieuXuat = phieu });
    }

    private async Task LoadDropdownsAsync()
    {
        ViewBag.NguyenLieuList = await Db.NguyenLieu
            .Where(x => x.DangSuDung)
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
            var nv = await Db.NhanVien.FirstOrDefaultAsync(x => x.Email == email);
            if (nv != null) return nv.Id;
        }

        var warehouseNv = await Db.NhanVien.FirstOrDefaultAsync(x => x.ChucVu == "Nhân viên kho" || x.MaNhanVien == "NV003");
        if (warehouseNv != null) return warehouseNv.Id;

        var firstNv = await Db.NhanVien.FirstOrDefaultAsync();
        return firstNv?.Id ?? 1;
    }
}
