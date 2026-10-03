using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using System.Diagnostics;

namespace RestaurantManagement.Web.Controllers
{
    [AllowAnonymous]
    public class HomeController(RestaurantDbContext db) : Controller
    {
        public async Task<IActionResult> Index(int? danhMucId = null)
        {
            var query = PublicMenu();
            var menu = await query.OrderBy(x => x.TenMon).Select(x => new MenuItemViewModel
            {
                Id = x.Id, CategoryId = x.DanhMucId, Name = x.TenMon, Description = x.MoTa,
                Image = x.HinhAnh, Category = x.DanhMuc.TenDanhMuc, Type = x.Loai,
                Status = x.TrangThai, IsNew = x.LaMonMoi, IsFeatured = x.LaMonNoiBat,
                FromPrice = x.Sizes.Where(s => s.DangSuDung).Min(s => (decimal?)s.GiaBan)
            }).ToListAsync();
            var now = DateTimeOffset.UtcNow;
            var currentPromotions = await db.KhuyenMai.AsNoTracking()
                .Where(x => x.DangSuDung && !x.CanVoucher && x.BatDau <= now && x.KetThuc >= now)
                .OrderBy(x => x.KetThuc).Take(3).ToListAsync();
            return View(new HomeViewModel
            {
                Categories = await db.DanhMuc.AsNoTracking().Where(x => x.DangSuDung)
                    .OrderBy(x => x.TenDanhMuc).ToListAsync(),
                Menu = danhMucId is int id ? menu.Where(x => x.CategoryId == id).ToList() : menu,
                Featured = menu.Where(x => x.IsFeatured).Take(3).ToList(),
                NewItems = menu.Where(x => x.IsNew).Take(4).ToList(),
                Promotions = currentPromotions.Select(ToPromotion).ToList(), CategoryId = danhMucId
            });
        }

        public async Task<IActionResult> MonAn(int id)
        {
            var dish = await PublicMenu().Include(x => x.DanhMuc)
                .Include(x => x.Sizes).SingleOrDefaultAsync(x => x.Id == id);
            if (dish is null) return NotFound();
            var now = DateTimeOffset.UtcNow;
            var promotions = await db.KhuyenMaiMon.AsNoTracking().Include(x => x.KhuyenMai)
                .Where(x => x.MonAnId == id && x.KhuyenMai.DangSuDung && !x.KhuyenMai.CanVoucher
                    && x.KhuyenMai.BatDau <= now && x.KhuyenMai.KetThuc >= now).ToListAsync();
            var invoicePromotions = await db.KhuyenMai.AsNoTracking()
                .Where(x => x.PhamVi == PhamViUuDai.HoaDon && x.DangSuDung && !x.CanVoucher
                    && x.BatDau <= now && x.KetThuc >= now).ToListAsync();
            return View(new MenuItemViewModel
            {
                Id = dish.Id, Name = dish.TenMon, Description = dish.MoTa, Image = dish.HinhAnh,
                Category = dish.DanhMuc.TenDanhMuc, Type = dish.Loai, Status = dish.TrangThai,
                IsNew = dish.LaMonMoi, IsFeatured = dish.LaMonNoiBat,
                Sizes = dish.Sizes.Where(x => x.DangSuDung && x.GiaBan > 0).OrderBy(x => x.GiaBan)
                    .Select(x => new MenuSizeViewModel { Name = x.TenSize, Price = x.GiaBan }).ToList(),
                ComboItems = await db.ChiTietCombo.AsNoTracking().Where(x => x.ComboId == id)
                    .Select(x => x.SoLuong + " × " + x.MonAn.TenMon).ToListAsync(),
                Promotions = promotions.Select(x => ToPromotion(x.KhuyenMai))
                    .Concat(invoicePromotions.Select(ToPromotion)).ToList()
            });
        }

        private IQueryable<MonAn> PublicMenu() => db.MonAn.AsNoTracking().Where(x => x.DaDuyet
            && x.DanhMuc.DangSuDung && x.TrangThai != TrangThaiMon.NgungKinhDoanh
            && x.Sizes.Any(s => s.DangSuDung) && !x.Sizes.Any(s => s.DangSuDung && s.GiaBan <= 0)
            && (x.Loai != LoaiMon.Set || !x.ThanhPhanCombo.Any(c => !c.MonAn.DaDuyet
                || c.MonAn.TrangThai == TrangThaiMon.NgungKinhDoanh || !c.MonAn.DanhMuc.DangSuDung)));

        private static PromotionViewModel ToPromotion(KhuyenMai item) => new()
        {
            Name = item.TenChuongTrinh, End = item.KetThuc,
            Summary = (item.PhamVi == PhamViUuDai.HoaDon ? "Hóa đơn: " : "Món áp dụng: ")
                + (item.KieuGiam == KieuGiam.PhanTram
                ? $"Giảm {item.GiaTri:0.#}%" : $"Giảm {item.GiaTri:N0} ₫")
                + (item.MucGiamToiDa is decimal max ? $", tối đa {max:N0} ₫" : "")
                + (item.GiaTriToiThieu > 0 ? $", từ {item.GiaTriToiThieu:N0} ₫" : "")
        };

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
