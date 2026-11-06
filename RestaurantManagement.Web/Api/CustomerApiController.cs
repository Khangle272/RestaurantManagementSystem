using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Shared.Mobile;
using RestaurantManagement.Web.Security;
using RestaurantManagement.Web.Services;

namespace RestaurantManagement.Web.Api;

// Khách hàng trên mobile: theo dõi trạng thái đơn, lịch sử mua hàng và lịch sử tích điểm. Chỉ đọc dữ liệu của chính mình.
[Route("api/customer")]
[Authorize(Roles = AppRoles.KhachHang)]
public class CustomerApiController(RestaurantDbContext db, UserManager<TaiKhoan> users,
    CustomerHistoryService history, OrderService orders, IPromotionService promotions) : MobileApiControllerBase(db, users)
{
    [HttpGet("summary")]
    public async Task<ActionResult<CustomerSummaryDto>> Summary()
    {
        if (await CurrentCustomerIdAsync() is not int id) return Forbid();
        var customer = await Db.KhachHang.AsNoTracking().SingleAsync(x => x.Id == id);
        var bills = Db.HoaDon.Where(x => x.KhachHangId == id);
        var spent = await bills.Where(x => x.TrangThai == TrangThaiHoaDon.DaThanhToan)
            .SumAsync(x => (decimal?)(x.TongTienHang - x.TienGiam)) ?? 0;
        return new CustomerSummaryDto(customer.HoTen, customer.SoDienThoai, customer.Email,
            await history.BalanceAsync(id), spent, await bills.CountAsync());
    }

    [HttpGet("orders")]
    public async Task<ActionResult<List<OrderSummaryDto>>> Orders(int page = 1)
    {
        if (await CurrentCustomerIdAsync() is not int id) return Forbid();
        page = Math.Max(1, page);
        var bills = await Db.HoaDon.AsNoTracking().AsSplitQuery()
            .Include(x => x.ChiTiet).Include(x => x.KhachHang)
            .Include(x => x.DatBan).ThenInclude(x => x!.Ban).ThenInclude(x => x.BanAn)
            .Where(x => x.KhachHangId == id)
            .OrderByDescending(x => x.ThoiDiemLap).ThenByDescending(x => x.Id)
            .Skip((page - 1) * 20).Take(20).ToListAsync();
        return bills.Select(MobileApi.Summary).ToList();
    }

    [HttpGet("orders/{orderId:int}")]
    public async Task<ActionResult<OrderDetailDto>> Order(int orderId)
    {
        if (await CurrentCustomerIdAsync() is not int id) return Forbid();
        return await LoadOrderAsync(orderId, id, staff: false, orders, promotions) is { } order
            ? order : NotFound(new ApiError("Không tìm thấy đơn hàng."));
    }

    [HttpGet("points")]
    public async Task<ActionResult<List<PointEntryDto>>> Points(int page = 1)
    {
        if (await CurrentCustomerIdAsync() is not int id) return Forbid();
        page = Math.Max(1, page);
        return await Db.LichSuTichDiem.AsNoTracking().Where(x => x.KhachHangId == id)
            .OrderByDescending(x => x.ThoiDiem).ThenByDescending(x => x.Id)
            .Skip((page - 1) * 30).Take(30)
            .Select(x => new PointEntryDto(x.ThoiDiem, x.SoDiem, x.NoiDung, x.HoaDon != null ? x.HoaDon.MaHoaDon : null))
            .ToListAsync();
    }
}
