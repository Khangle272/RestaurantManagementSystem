using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Controllers
{
    public class NguyenLieuController : Controller
    {
        private readonly RestaurantDbContext _context;

        public NguyenLieuController(RestaurantDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search, string? donViTinh, string? trangThai, int page = 1)
        {
            ViewData["ActiveMenu"] = "kho-nguyen-lieu";
            ViewBag.Search = search;
            ViewBag.DonViTinh = donViTinh;
            ViewBag.TrangThai = trangThai;

            var query = _context.NguyenLieu.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(n => n.TenNguyenLieu.Contains(search));
            }

            if (!string.IsNullOrEmpty(donViTinh))
            {
                query = query.Where(n => n.DonViTinh == donViTinh);
            }

            if (!string.IsNullOrEmpty(trangThai))
            {
                if (trangThai == "dang-su-dung")
                {
                    query = query.Where(n => n.DangSuDung == true);
                }
                else if (trangThai == "ngung-su-dung")
                {
                    query = query.Where(n => n.DangSuDung == false);
                }
            }

            int pageSize = 10;
            var totalItems = await query.CountAsync();
            var totalPages = (int)System.Math.Ceiling(totalItems / (double)pageSize);
            
            page = page < 1 ? 1 : page;
            page = page > totalPages && totalPages > 0 ? totalPages : page;

            var items = await query
                .OrderByDescending(n => n.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(items);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewData["ActiveMenu"] = "kho-nguyen-lieu";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NguyenLieu model)
        {
            ViewData["ActiveMenu"] = "kho-nguyen-lieu";
            if (ModelState.IsValid)
            {
                try
                {
                    _context.NguyenLieu.Add(model);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Thêm nguyên liệu thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_") == true || ex.InnerException?.Message.Contains("UNIQUE") == true)
                {
                    ModelState.AddModelError("TenNguyenLieu", "Tên nguyên liệu này đã tồn tại.");
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(string.Empty, "Không thể lưu dữ liệu. Vui lòng thử lại.");
                }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["ActiveMenu"] = "kho-nguyen-lieu";
            var item = await _context.NguyenLieu.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NguyenLieu model)
        {
            ViewData["ActiveMenu"] = "kho-nguyen-lieu";
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(model);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật nguyên liệu thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NguyenLieuExists(model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Dữ liệu đã bị thay đổi bởi người khác. Vui lòng tải lại trang.");
                    }
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_") == true || ex.InnerException?.Message.Contains("UNIQUE") == true)
                {
                    ModelState.AddModelError("TenNguyenLieu", "Tên nguyên liệu này đã tồn tại.");
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(string.Empty, "Không thể lưu dữ liệu. Vui lòng thử lại.");
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var hasDinhMuc = await _context.DinhMucMon.AnyAsync(d => d.NguyenLieuId == id);
            if (hasDinhMuc)
            {
                TempData["Error"] = "Nguyên liệu này đang nằm trong công thức định mức của món ăn. Không thể xóa!";
                return RedirectToAction(nameof(Index));
            }

            var item = await _context.NguyenLieu.FindAsync(id);
            if (item != null)
            {
                try
                {
                    _context.NguyenLieu.Remove(item);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Xóa nguyên liệu thành công!";
                }
                catch (DbUpdateException)
                {
                    TempData["Error"] = "Không thể xóa do ràng buộc dữ liệu.";
                }
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool NguyenLieuExists(int id)
        {
            return _context.NguyenLieu.Any(e => e.Id == id);
        }
    }
}
