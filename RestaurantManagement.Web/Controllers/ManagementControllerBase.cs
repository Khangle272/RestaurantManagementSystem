using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Controllers;

public abstract class ManagementControllerBase(RestaurantDbContext context) : Controller
{
    protected RestaurantDbContext Db { get; } = context;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ViewData["ActiveMenu"] = ControllerContext.ActionDescriptor.ControllerName;
        base.OnActionExecuting(context);
    }

    protected async Task<PagedListViewModel<T>> PageAsync<T>(IQueryable<T> query, int page,
        string? search, string? trangThai, int? khuVucId = null)
    {
        const int pageSize = 10;
        var count = await query.CountAsync();
        var pages = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize));
        page = Math.Clamp(page, 1, pages);
        return new PagedListViewModel<T>
        {
            Items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(),
            Pagination = new PaginationViewModel
            {
                CurrentPage = page, TotalPages = pages, TotalItems = count,
                Search = search, TrangThai = trangThai, KhuVucId = khuVucId
            }
        };
    }

    protected string VersionOf<T>(T entity) where T : class =>
        Convert.ToBase64String(Db.Entry(entity).Property<byte[]>("RowVersion").CurrentValue!);

    protected bool ApplyVersion<T>(T entity, string? version) where T : class
    {
        var bytes = new byte[8];
        if (version == null || !Convert.TryFromBase64String(version, bytes, out var length) || length != 8)
        {
            ModelState.AddModelError("", "Phiên bản dữ liệu không hợp lệ. Vui lòng tải lại trang.");
            return false;
        }
        Db.Entry(entity).Property<byte[]>("RowVersion").OriginalValue = bytes;
        return true;
    }

    protected async Task<bool> SaveAsync(string duplicateField, string duplicateMessage)
    {
        try
        {
            await Db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError("", "Bản ghi đã bị thay đổi hoặc xóa. Vui lòng quay lại danh sách và mở lại để cập nhật dữ liệu mới nhất.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            ModelState.AddModelError(duplicateField, duplicateMessage);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Không thể lưu vì dữ liệu đang được sử dụng hoặc không còn hợp lệ. Vui lòng kiểm tra và thử lại.");
        }
        return false;
    }

    protected DeleteRecordViewModel DeleteModel<T>(T entity, int id, string name, string description) where T : class =>
        new() { Id = id, Name = name, Description = description, RowVersion = VersionOf(entity) };

    protected async Task<IActionResult> DeleteRecordAsync<T>(T entity, DeleteRecordViewModel model,
        string name, string description, bool inUse) where T : class
    {
        model.Name = name;
        model.Description = description;
        if (inUse)
            ModelState.AddModelError("", "Không thể xóa bản ghi đang được sử dụng. Bạn có thể sửa trạng thái để ngừng sử dụng.");
        if (ModelState.IsValid && ApplyVersion(entity, model.RowVersion))
        {
            Db.Remove(entity);
            if (await SaveAsync("", "Không thể xóa bản ghi này."))
            {
                TempData["Success"] = "Xóa thành công.";
                return RedirectToAction("Index");
            }
        }
        return View("Delete", model);
    }
}
