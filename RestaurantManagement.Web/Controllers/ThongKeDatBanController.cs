using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Web.Security;
using RestaurantManagement.Web.Services;
namespace RestaurantManagement.Web.Controllers;
[Authorize(Roles = AppRoles.Admin + "," + AppRoles.TiepTan + "," + AppRoles.Bep + "," + AppRoles.Kho)]
public class ThongKeDatBanController(ReservationPlanningService planning) : Controller
{
    public async Task<IActionResult> Index(DateOnly? tuNgay, DateOnly? denNgay, string? ca, bool? vip, string? cheDo)
    {
        var today = DateOnly.FromDateTime(TableService.VietnamNow);
        var from = tuNgay ?? today;
        if (!ModelState.IsValid || from.Year is < 1900 or > 2100 || denNgay?.Year is < 1900 or > 2100)
            return BadRequest("Ngày thống kê phải trong năm 1900–2100.");
        if (cheDo == "tuan") from = from.AddDays(-(((int)from.DayOfWeek + 6) % 7));
        var until = cheDo == "tuan" ? from.AddDays(6) : denNgay ?? from;
        if (!ModelState.IsValid || from.Year is < 1900 or > 2100 || until.Year is < 1900 or > 2100 || until < from || until.DayNumber - from.DayNumber > 31
            || (!string.IsNullOrEmpty(ca) && !ReservationSchedule.Slots.Any(x => x.Id == ca))) return BadRequest("Chọn khoảng tối đa 31 ngày và ca hợp lệ.");
        return View(await planning.Build(from, until, string.IsNullOrEmpty(ca) ? null : ca, vip));
    }
}
