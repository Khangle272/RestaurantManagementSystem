using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Services;

public static class AppCulture
{
    // Hiển thị tiền kiểu Việt Nam (139.000 đ) trên mọi máy chủ. Dấu thập phân vẫn là "." vì ô nhập số
    // và dữ liệu nhúng trong trang đều dùng dấu chấm; đổi sang "," sẽ làm sai giá trị khi lưu.
    public static CultureInfo Create()
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("en-US").Clone();
        culture.NumberFormat.NumberGroupSeparator = ".";
        // Ngày mặc định kiểu Việt Nam (dd/MM/yyyy, 24 giờ) cho mọi chỗ in ngày không ghi định dạng.
        // Ô nhập date/datetime-local gửi ISO (yyyy-MM-dd) nên vẫn đọc đúng.
        culture.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
        culture.DateTimeFormat.ShortTimePattern = "HH:mm";
        culture.DateTimeFormat.LongTimePattern = "HH:mm:ss";
        culture.DateTimeFormat.AMDesignator = culture.DateTimeFormat.PMDesignator = "";
        return culture;
    }
}

// Lỗi trả về dạng chuỗi: người dùng trên trình duyệt thấy trang lỗi của hệ thống thay vì chữ trần, mã lỗi giữ nguyên.
public sealed class FriendlyTextErrorFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Controller is not Controller controller
            || context.Result is not ObjectResult { Value: string message, StatusCode: int code }
            || code is < 400 or >= 500
            || !context.HttpContext.Request.Headers.Accept.ToString().Contains("text/html")) return;
        var model = StatusPageViewModel.ForCode(code);
        model.Message = message;
        var view = controller.View("HttpStatus", model);
        view.StatusCode = code;
        context.Result = view;
    }

    public void OnResultExecuted(ResultExecutedContext context) { }
}

// Lưu xong quay về danh sách vẫn giữ bộ lọc và trang đang xem; admin-polish.js gửi kèm chuỗi lọc trong trường ẩn.
public sealed class KeepListFilterResultFilter : IAsyncResultFilter
{
    private const string FieldName = "__listQuery";

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        if (HttpMethods.IsPost(request.Method) && request.HasFormContentType
            && context.Result is RedirectToActionResult { ActionName: "Index" } redirect
            && (redirect.ControllerName is null || redirect.ControllerName.Equals(context.RouteData.Values["controller"]?.ToString(), StringComparison.OrdinalIgnoreCase)))
        {
            var saved = (await request.ReadFormAsync())[FieldName].ToString();
            if (!string.IsNullOrEmpty(saved))
            {
                var routeValues = redirect.RouteValues ?? new RouteValueDictionary();
                foreach (var (key, values) in QueryHelpers.ParseQuery(saved))
                {
                    if (key is "area" or "controller" or "action" or "id") continue;
                    if (routeValues.TryGetValue(key, out var current) && current is not null) continue;
                    routeValues[key] = values.Count == 1 ? values[0] : values.ToArray();
                }
                redirect.RouteValues = routeValues;
            }
        }
        await next();
    }
}
