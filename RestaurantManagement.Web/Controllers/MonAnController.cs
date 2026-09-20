namespace RestaurantManagement.Web.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

public class MonAnController : Controller
{
    private readonly RestaurantDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public MonAnController(RestaurantDbContext context, IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<IActionResult> Index(string? search, int? danhMucId, string? trangThai, int page = 1)
    {
        const int pageSize = 6;
        var query = _context.MonAn
            .Include(m => m.DanhMuc)
            .Include(m => m.Sizes)
            .AsQueryable();

        // Count totals for KPI cards
        var dangPhucVuCount = await _context.MonAn.CountAsync(m => m.TrangThai == TrangThaiMon.DangPhucVu);
        var tamHetCount = await _context.MonAn.CountAsync(m => m.TrangThai == TrangThaiMon.TamHet);
        var ngungKinhDoanhCount = await _context.MonAn.CountAsync(m => m.TrangThai == TrangThaiMon.NgungKinhDoanh);

        // Filters
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(m => m.TenMon.Contains(search));
        }
        if (danhMucId.HasValue && danhMucId.Value > 0)
        {
            query = query.Where(m => m.DanhMucId == danhMucId.Value);
        }
        if (!string.IsNullOrWhiteSpace(trangThai) && Enum.TryParse<TrangThaiMon>(trangThai, out var parsedTrangThai))
        {
            query = query.Where(m => m.TrangThai == parsedTrangThai);
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        if (page < 1) page = 1;

        var items = await query
            .OrderByDescending(m => m.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MonAnCardViewModel
            {
                Id = m.Id,
                TenMon = m.TenMon,
                MoTa = m.MoTa,
                HinhAnh = m.HinhAnh,
                TenDanhMuc = m.DanhMuc.TenDanhMuc,
                TrangThai = m.TrangThai,
                GiaBan = m.Sizes.Any() ? m.Sizes.First().GiaBan : 0
            })
            .ToListAsync();

        var danhMucList = await _context.DanhMuc.Where(d => d.DangSuDung).ToListAsync();

        var viewModel = new MonAnIndexViewModel
        {
            Items = items,
            DanhMucList = danhMucList,
            Search = search,
            DanhMucId = danhMucId,
            TrangThai = trangThai,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            DangPhucVuCount = dangPhucVuCount,
            TamHetCount = tamHetCount,
            NgungKinhDoanhCount = ngungKinhDoanhCount
        };

        ViewData["ActiveMenu"] = "thuc-don-mon";
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new MonAnCreateViewModel
        {
            DanhMucList = await _context.DanhMuc.Where(d => d.DangSuDung).ToListAsync(),
            NguyenLieuList = await _context.NguyenLieu.Where(n => n.DangSuDung).ToListAsync()
        };
        ViewData["ActiveMenu"] = "thuc-don-mon";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MonAnCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.DanhMucList = await _context.DanhMuc.Where(d => d.DangSuDung).ToListAsync();
            model.NguyenLieuList = await _context.NguyenLieu.Where(n => n.DangSuDung).ToListAsync();
            ViewData["ActiveMenu"] = "thuc-don-mon";
            return View(model);
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            string? hinhAnhPath = null;
            if (model.HinhAnhFile != null)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "monan");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);
                
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.HinhAnhFile.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.HinhAnhFile.CopyToAsync(fileStream);
                }
                hinhAnhPath = "/uploads/monan/" + uniqueFileName;
            }

            var monAn = new MonAn
            {
                TenMon = model.TenMon,
                DanhMucId = model.DanhMucId,
                MoTa = model.MoTa,
                HinhAnh = hinhAnhPath,
                Loai = model.Loai,
                TrangThai = model.TrangThai,
                LaMonMoi = true,
                LaMonNoiBat = false
            };

            _context.MonAn.Add(monAn);
            await _context.SaveChangesAsync();

            var monAnSize = new MonAnSize
            {
                MonAnId = monAn.Id,
                TenSize = "Mặc định",
                GiaBan = model.GiaBan,
                DangSuDung = true
            };
            _context.MonAnSize.Add(monAnSize);
            await _context.SaveChangesAsync();

            if (model.DinhMucItems != null && model.DinhMucItems.Any())
            {
                var dinhMucList = model.DinhMucItems.Select(item => new DinhMucMon
                {
                    MonAnId = monAn.Id,
                    NguyenLieuId = item.NguyenLieuId,
                    SoLuong = item.SoLuong
                });
                _context.DinhMucMon.AddRange(dinhMucList);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            TempData["Success"] = "Thêm món ăn thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError("", "Đã xảy ra lỗi trong quá trình lưu món ăn. Vui lòng thử lại.");
            model.DanhMucList = await _context.DanhMuc.Where(d => d.DangSuDung).ToListAsync();
            model.NguyenLieuList = await _context.NguyenLieu.Where(n => n.DangSuDung).ToListAsync();
            ViewData["ActiveMenu"] = "thuc-don-mon";
            return View(model);
        }
    }
}
