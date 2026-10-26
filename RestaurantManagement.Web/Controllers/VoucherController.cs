using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.Web.Security;

namespace RestaurantManagement.Web.Controllers;

// Preserve bookmarked URLs while all editing takes place inside the campaign aggregate.
[Authorize(Roles = AppRoles.Admin)]
public class VoucherController(RestaurantDbContext db) : Controller
{
    public IActionResult Index(int? khuyenMaiId) => khuyenMaiId.HasValue
        ? RedirectToAction("Edit", "KhuyenMai", new { id = khuyenMaiId.Value })
        : RedirectToAction("Index", "KhuyenMai");
    public IActionResult Create(int? khuyenMaiId) => khuyenMaiId.HasValue
        ? RedirectToAction("Edit", "KhuyenMai", new { id = khuyenMaiId.Value })
        : RedirectToAction("Create", "KhuyenMai");
    public async Task<IActionResult> Edit(int id) {
        var parent = await db.Voucher.Where(x => x.Id == id).Select(x => (int?)x.KhuyenMaiId).SingleOrDefaultAsync();
        return parent.HasValue ? RedirectToAction("Edit", "KhuyenMai", new { id = parent.Value }) : NotFound();
    }
}
