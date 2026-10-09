using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Security;
using RestaurantManagement.Web.Services;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public sealed class CauHinhQrController(PaymentQrStore store, ILogger<CauHinhQrController> logger) : Controller
{
    [HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Index() => View(new PaymentQrViewModel { Version = store.CurrentVersion });

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> Index(PaymentQrViewModel model)
    {
        if (model.AnhQr is { } image && (image.Length == 0 || image.Length > 2 * 1024 * 1024))
            ModelState.AddModelError(nameof(model.AnhQr), "Ảnh QR phải có dung lượng từ 1 byte đến 2 MB.");
        if (ModelState.IsValid)
        {
            await using var buffer = new MemoryStream();
            await model.AnhQr!.CopyToAsync(buffer);
            var bytes = buffer.ToArray();
            if (PaymentQrStore.ContentType(bytes) is null)
                ModelState.AddModelError(nameof(model.AnhQr), "Chỉ nhận ảnh PNG hoặc JPEG; không nhận SVG hay tệp khác đổi tên thành ảnh.");
            else
            {
                try
                {
                    if (!store.Save(bytes, model.Version))
                        ModelState.AddModelError("", "QR đã được thay đổi ở tab khác. Tải lại trang rồi chọn ảnh mới.");
                    else
                    {
                        TempData["SuccessMessage"] = "Đã cập nhật ảnh QR nhận cọc.";
                        return Request.Headers.XRequestedWith == "XMLHttpRequest"
                            ? Json(new { ok = true, redirectUrl = Url.Action(nameof(Index)) }) : RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException)
                {
                    logger.LogError(error, "Không lưu được ảnh QR nhận cọc.");
                    ModelState.AddModelError("", "Không lưu được ảnh QR. Ảnh đang dùng vẫn được giữ; vui lòng thử lại.");
                }
            }
        }
        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            return BadRequest(new { ok = false, errors = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).ToArray() });
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return View(model);
    }

    [HttpGet, AllowAnonymous, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Anh(string? v)
    {
        var bytes = store.Read(v);
        var type = bytes is null ? null : PaymentQrStore.ContentType(bytes);
        if (type is null) return NotFound();
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(bytes!, type);
    }
}
