using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Security;
using RestaurantManagement.Web.Services;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class TaiKhoanKhachHangController(RestaurantDbContext db, UserManager<TaiKhoan> users, CustomerHistoryService history)
    : ManagementControllerBase(db)
{
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        search = search?.Trim();
        var query = Db.KhachHang.AsNoTracking().Include(x => x.TaiKhoan).AsQueryable();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(x => x.HoTen.Contains(search) || x.SoDienThoai.Contains(search)
                || (x.Email != null && x.Email.Contains(search))
                || (x.TaiKhoan != null && x.TaiKhoan.Email != null && x.TaiKhoan.Email.Contains(search)));
        return View(await PageAsync(query.OrderByDescending(x => x.Id), page, search, null));
    }

    public async Task<IActionResult> Details(int id, int orderPage = 1, int pointPage = 1, TrangThaiHoaDon? status = null)
    {
        var model = await history.LoadAsync(id, true, orderPage, pointPage, status);
        return model is null ? NotFound() : View("~/Views/Shared/CustomerHistory.cshtml", model);
    }

    public IActionResult Create() => View("Form", new CustomerProfileForm());

    public async Task<IActionResult> CreateAccount(int id)
    {
        var c = await Db.KhachHang.SingleOrDefaultAsync(x => x.Id == id && x.TaiKhoanId == null && x.DangSuDung);
        if (c is null) return NotFound();
        return View(new CustomerAccountForm { Id = id, HoTen = c.HoTen, Email = c.Email ?? "",
            RowVersion = Convert.ToBase64String(Db.Entry(c).Property<byte[]>("RowVersion").CurrentValue ?? []) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAccount(CustomerAccountForm model)
    {
        var c = await Db.KhachHang.SingleOrDefaultAsync(x => x.Id == model.Id && x.TaiKhoanId == null && x.DangSuDung);
        if (c is null) return NotFound();
        model.HoTen = c.HoTen;
        var bytes = new byte[8];
        if (model.RowVersion is null || !Convert.TryFromBase64String(model.RowVersion, bytes, out var length) || length != 8
            || !bytes.SequenceEqual(Db.Entry(c).Property<byte[]>("RowVersion").CurrentValue ?? []))
            ModelState.AddModelError("", "Hồ sơ đã thay đổi. Vui lòng tải lại.");
        model.Email = model.Email?.Trim() ?? "";
        if (await users.FindByEmailAsync(model.Email) is not null) ModelState.AddModelError("Email", "Email đã được sử dụng.");
        if (!ModelState.IsValid) return View(model);
        await using var tx = await Db.Database.BeginTransactionAsync();
        var account = new TaiKhoan { Email = model.Email, UserName = model.Email, PhoneNumber = c.SoDienThoai, EmailConfirmed = true };
        var result = await users.CreateAsync(account, model.Password);
        if (result.Succeeded) result = await users.AddToRoleAsync(account, AppRoles.KhachHang);
        if (!result.Succeeded) {
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View(model);
        }
        c.TaiKhoanId = account.Id; c.Email = model.Email;
        try { await Db.SaveChangesAsync(); }
        catch (DbUpdateException) { ModelState.AddModelError("", "Hồ sơ đã thay đổi hoặc đã có tài khoản. Vui lòng tải lại."); return View(model); }
        await tx.CommitAsync();
        TempData["Success"] = "Đã cấp tài khoản khách hàng và giữ nguyên đơn hàng, voucher, điểm của hồ sơ.";
        return RedirectToAction(nameof(Details), new { id = c.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var c = await Db.KhachHang.SingleOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        if (c.TaiKhoanId.HasValue) return RedirectToAction("EditProfile", "Account", new { id = c.TaiKhoanId });
        return View("Form", new CustomerProfileForm { Id = id, HoTen = c.HoTen, SoDienThoai = c.SoDienThoai,
            Email = c.Email, DangSuDung = c.DangSuDung, RowVersion = Convert.ToBase64String(Db.Entry(c).Property<byte[]>("RowVersion").CurrentValue ?? []) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(CustomerProfileForm model) => SaveProfile(0, model);
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(int id, CustomerProfileForm model) => id != model.Id
        ? Task.FromResult<IActionResult>(NotFound()) : SaveProfile(id, model);

    private async Task<IActionResult> SaveProfile(int id, CustomerProfileForm model)
    {
        var c = id == 0 ? new KhachHang { NgayDangKy = DateTimeOffset.UtcNow } : await Db.KhachHang.SingleOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        if (c.TaiKhoanId.HasValue) return BadRequest();
        model.HoTen = model.HoTen?.Trim() ?? ""; model.SoDienThoai = model.SoDienThoai?.Trim() ?? "";
        if (id != 0) {
            var token = new byte[8];
            if (model.RowVersion is null || !Convert.TryFromBase64String(model.RowVersion, token, out var length) || length != 8
                || !token.SequenceEqual(Db.Entry(c).Property<byte[]>("RowVersion").CurrentValue ?? []))
                ModelState.AddModelError("", "Hồ sơ đã thay đổi. Vui lòng tải lại.");
        }
        if (await Db.KhachHang.AnyAsync(x => x.Id != id && x.SoDienThoai == model.SoDienThoai)) ModelState.AddModelError("SoDienThoai", "Số điện thoại đã có hồ sơ.");
        if (!ModelState.IsValid) return View("Form", model);
        c.HoTen = model.HoTen; c.SoDienThoai = model.SoDienThoai; c.Email = model.Email?.Trim(); c.DangSuDung = model.DangSuDung;
        if (id == 0) Db.KhachHang.Add(c);
        try { await Db.SaveChangesAsync(); }
        catch (DbUpdateException) { ModelState.AddModelError("", "Không lưu được hồ sơ do dữ liệu trùng hoặc đã thay đổi. Vui lòng tải lại."); return View("Form", model); }
        TempData["Success"] = "Đã lưu hồ sơ khách hàng.";
        return RedirectToAction(nameof(Details), new { id = c.Id });
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
