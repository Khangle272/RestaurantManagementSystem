using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Shared.Mobile;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Security;
using RestaurantManagement.Web.Services;

namespace RestaurantManagement.Web.Api;

// Thực đơn, gọi món, phục vụ và thanh toán trên mobile. Dùng chung OrderService với màn POS/thu ngân của website.
[Route("api/staff")]
[Authorize(Roles = MobileApi.StaffRoles)]
public class StaffOrdersApiController(RestaurantDbContext db, UserManager<TaiKhoan> users,
    OrderService orders, IPromotionService promotions) : MobileApiControllerBase(db, users)
{
    [HttpGet("menu")]
    public async Task<ActionResult<MenuDto>> Menu()
    {
        var categories = await Db.DanhMuc.AsNoTracking().Where(x => x.DangSuDung).OrderBy(x => x.TenDanhMuc)
            .Select(x => new MenuCategoryDto(x.Id, x.TenDanhMuc)).ToListAsync();
        var dishes = await MenuRules.Available(Db).AsNoTracking()
            .OrderBy(x => x.DanhMuc.TenDanhMuc).ThenBy(x => x.TenMon)
            .Select(x => new MenuDishDto(x.Id, x.DanhMucId, x.DanhMuc.TenDanhMuc, x.TenMon, x.MoTa, x.HinhAnh,
                x.Sizes.Where(s => s.DangSuDung && s.GiaBan > 0).OrderBy(s => s.GiaBan)
                    .Select(s => new MenuSizeDto(s.Id, s.TenSize, s.GiaBan)).ToList()))
            .ToListAsync();
        return new MenuDto(categories, dishes.Where(x => x.Sizes.Count > 0).ToList());
    }

    // Đơn chưa thanh toán (mặc định) hoặc đơn hôm nay.
    [HttpGet("orders")]
    public async Task<ActionResult<List<OrderSummaryDto>>> Orders(bool open = true)
    {
        var today = TableService.VietnamTime(TableService.VietnamNow.Date);
        var query = Db.HoaDon.AsNoTracking().AsSplitQuery()
            .Include(x => x.ChiTiet).Include(x => x.KhachHang)
            .Include(x => x.DatBan).ThenInclude(x => x!.Ban).ThenInclude(x => x.BanAn)
            .Where(x => open ? x.TrangThai == TrangThaiHoaDon.ChuaThanhToan : x.ThoiDiemLap >= today);
        var bills = await query.OrderByDescending(x => x.ThoiDiemLap).Take(100).ToListAsync();
        return bills.Select(MobileApi.Summary).ToList();
    }

    [HttpGet("orders/{id:int}")]
    public async Task<ActionResult<OrderDetailDto>> Order(int id) =>
        await LoadOrderAsync(id, null, staff: true, orders, promotions) is { } order
            ? order : NotFound(new ApiError("Không tìm thấy đơn hàng."));

    [HttpPost("orders")]
    public async Task<IActionResult> Create(CreateOrderRequest request)
    {
        if (!Enum.TryParse<LoaiDonHang>(request.LoaiDonHang, true, out var type) || !Enum.IsDefined(type))
            return Fail("Loại đơn hàng không hợp lệ.");
        if (await CurrentStaffIdAsync() is not int staffId)
            return Fail("Tài khoản hiện tại chưa được liên kết với hồ sơ nhân viên đang làm việc.", StatusCodes.Status403Forbidden);
        var (error, billId) = await orders.CreateOrderAsync(new OrderCreateViewModel
        {
            LoaiDonHang = type, BanAnId = type == LoaiDonHang.TaiBan ? request.BanAnId : null,
            TenNguoiNhan = request.TenNguoiNhan, SoDienThoaiNhan = request.SoDienThoaiNhan,
            DiaChiGiaoHang = request.DiaChiGiaoHang, GhiChuDonHang = request.GhiChu,
            Items = Items(request.Items)
        }, staffId);
        return error is not null ? Fail(error)
            : Ok(new OrderCreatedDto(billId!.Value, WithStockWarning("Đã ghi nhận gọi món và gửi vào bếp.")));
    }

    [HttpPost("orders/{id:int}/items")]
    public async Task<IActionResult> AddItems(int id, AddItemsRequest request)
    {
        var (error, billId) = await orders.AddDishesAsync(id, Items(request.Items));
        return error is not null ? Fail(error)
            : Ok(new OrderCreatedDto(billId!.Value, WithStockWarning("Đã gọi thêm món và gửi vào bếp.")));
    }

    // Bồi bàn mang món đã sẵn sàng ra bàn hoặc hủy món chưa chế biến xong.
    [HttpPost("orders/{id:int}/lines/{lineId:int}"), Authorize(Roles = MobileApi.ServeRoles)]
    public async Task<IActionResult> UpdateLine(int id, int lineId, UpdateLineRequest request)
    {
        if (!Enum.TryParse<TrangThaiCheBien>(request.TrangThai, true, out var status)
            || status is not (TrangThaiCheBien.DaPhucVu or TrangThaiCheBien.DaHuy))
            return Fail("Chỉ được xác nhận đã phục vụ hoặc hủy món.");
        if (!await Db.ChiTietHoaDon.AnyAsync(x => x.Id == lineId && x.HoaDonId == id))
            return NotFound(new ApiError("Không tìm thấy món trong đơn hàng."));
        var error = await orders.UpdateDishStatusAsync(lineId, status, isWaiterOrAdmin: true);
        return error is not null ? Fail(error, StatusCodes.Status409Conflict)
            : Ok(new MessageDto(status == TrangThaiCheBien.DaPhucVu ? "Đã xác nhận phục vụ món." : "Đã hủy món."));
    }

    // Thu tiền mặt hoặc chuyển khoản. Không cho sửa giảm giá trên mobile; dùng đúng ưu đãi đang áp dụng.
    [HttpPost("orders/{id:int}/pay"), Authorize(Roles = MobileApi.PayRoles)]
    public async Task<IActionResult> Pay(int id, PaymentRequest request)
    {
        if (!Enum.TryParse<PhuongThucThanhToan>(request.PhuongThuc, true, out var method)
            || method is not (PhuongThucThanhToan.TienMat or PhuongThucThanhToan.ChuyenKhoan))
            return Fail("Chỉ nhận thanh toán bằng tiền mặt hoặc chuyển khoản ngân hàng.");
        if (await CurrentStaffIdAsync() is not int cashierId)
            return Fail("Tài khoản hiện tại chưa được liên kết với hồ sơ nhân viên đang làm việc.", StatusCodes.Status403Forbidden);
        if (method == PhuongThucThanhToan.ChuyenKhoan && string.IsNullOrWhiteSpace(request.MaGiaoDich))
            return Fail("Nhập mã giao dịch ngân hàng sau khi đối chiếu chuyển khoản.");

        var error = await orders.ProcessPaymentAsync(new PaymentFormModel
        {
            Id = id, RowVersion = request.RowVersion, BookingRowVersion = request.BookingRowVersion,
            PhuongThuc = method, SoTienPhaiThuDuKien = request.SoTienPhaiThu,
            TienKhachDua = method == PhuongThucThanhToan.TienMat ? request.TienKhachDua : request.SoTienPhaiThu,
            MaGiaoDich = request.MaGiaoDich
        }, cashierId);
        if (error is not null) return Fail(error, StatusCodes.Status409Conflict);

        var change = await Db.HoaDon.Where(x => x.Id == id).Select(x => x.TienThoiLai).SingleAsync();
        var message = $"Đã thanh toán bằng {PaymentLedger.HinhThucText(method).ToLower()}.";
        var bookingId = await Db.HoaDon.Where(x => x.Id == id).Select(x => x.DatBanId).SingleAsync();
        if (bookingId is int booking && await orders.AvailableDepositAsync(booking) is > 0 and var excess)
            message += $" Còn {excess:N0} đ tiền cọc dư: thu ngân hoàn cho khách trên website trước khi kết thúc bàn.";
        return Ok(new PaymentResultDto(message, change));
    }

    private static List<OrderItemInputModel> Items(List<OrderItemRequest>? items) => (items ?? [])
        .Select(x => new OrderItemInputModel
        {
            MonAnId = x.MonAnId, MonAnSizeId = x.MonAnSizeId, SoLuong = x.SoLuong,
            YeuCauCheBien = string.IsNullOrWhiteSpace(x.GhiChu) ? null : x.GhiChu
        }).ToList();

    private string WithStockWarning(string message) => orders.InventoryWarnings.Count == 0 ? message
        : message + " Lưu ý kho: " + string.Join(" ", orders.InventoryWarnings);
}
