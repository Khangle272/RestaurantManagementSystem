using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Security;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class TaiKhoanKhachHangController(RestaurantDbContext db, UserManager<TaiKhoan> users)
    : ManagementControllerBase(db)
{
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        search = search?.Trim();
        var query = Db.KhachHang.AsNoTracking().Include(x => x.TaiKhoan).Where(x => x.TaiKhoanId != null);
        if (!string.IsNullOrEmpty(search))
            query = query.Where(x => x.HoTen.Contains(search) || x.SoDienThoai.Contains(search)
                || (x.TaiKhoan != null && x.TaiKhoan.Email != null && x.TaiKhoan.Email.Contains(search)));
        return View(await PageAsync(query.OrderByDescending(x => x.Id), page, search, null));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetLocked(int id, bool locked)
    {
        var customer = await Db.KhachHang.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (customer?.TaiKhoanId is not int accountId) return NotFound();
        var user = await users.FindByIdAsync(accountId.ToString());
        if (user is null) return NotFound();
        if (string.IsNullOrEmpty(user.SecurityStamp)) return BadRequest();
        var roles = await users.GetRolesAsync(user);
        if (roles.Count != 1 || roles[0] != AppRoles.KhachHang) return BadRequest();
        await using var transaction = await Db.Database.BeginTransactionAsync();
        var result = await users.SetLockoutEndDateAsync(user, locked ? DateTimeOffset.UtcNow.AddYears(20) : null);
        if (!result.Succeeded) return BadRequest();
        result = locked ? await users.UpdateSecurityStampAsync(user) : await users.ResetAccessFailedCountAsync(user);
        if (!result.Succeeded) return BadRequest();
        await transaction.CommitAsync();
        TempData["Success"] = locked ? "Đã khóa tài khoản khách hàng." : "Đã mở khóa tài khoản khách hàng.";
        return RedirectToAction(nameof(Index));
    }
}
