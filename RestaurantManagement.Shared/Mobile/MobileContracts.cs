namespace RestaurantManagement.Shared.Mobile;

// Hợp đồng dữ liệu của REST API /api dành cho ứng dụng mobile.
// Trạng thái gửi kèm mã (tên enum phía server) và chữ hiển thị tiếng Việt để app không phải tự dịch.

public sealed record ApiError(string Message);

// ----- Xác thực -----
public sealed record LoginRequest(string Email, string Password);

// Định dạng do ASP.NET Core Identity bearer token trả về.
public sealed record TokenResponse(string TokenType, string AccessToken, long ExpiresIn, string? RefreshToken);

public sealed record RefreshRequest(string RefreshToken);

public sealed record UserProfileDto(string Email, string HoTen, string Role, string RoleLabel, bool IsCustomer,
    bool CanOrder, bool CanServe, bool CanPay, bool CanReceive);

// ----- Nhân viên: sơ đồ bàn -----
public sealed record TableDto(int Id, string MaBan, string KhuVuc, int Tang, int SoCho, string TrangThai,
    string TrangThaiText, string RowVersion, int? DatBanId, string? KhachHang, int SoKhach,
    int? HoaDonId, string? MaHoaDon, decimal TongTien, int SoMon, int MonSanSang, string? LichSapToi);

public sealed record TableActionRequest(string RowVersion);

// ----- Nhân viên: thực đơn và gọi món -----
public sealed record MenuSizeDto(int Id, string TenSize, decimal GiaBan);

public sealed record MenuDishDto(int Id, int DanhMucId, string DanhMuc, string TenMon, string? MoTa,
    string? HinhAnh, List<MenuSizeDto> Sizes);

public sealed record MenuDto(List<MenuCategoryDto> DanhMuc, List<MenuDishDto> Mon);

public sealed record MenuCategoryDto(int Id, string Ten);

public sealed record OrderItemRequest(int MonAnId, int MonAnSizeId, int SoLuong, string? GhiChu);

// LoaiDonHang: TaiBan | MangDi | GiaoHang.
public sealed record CreateOrderRequest(string LoaiDonHang, int? BanAnId, string? TenNguoiNhan,
    string? SoDienThoaiNhan, string? DiaChiGiaoHang, string? GhiChu, List<OrderItemRequest> Items);

public sealed record AddItemsRequest(List<OrderItemRequest> Items);

public sealed record OrderCreatedDto(int HoaDonId, string Message);

public sealed record OrderSummaryDto(int Id, string MaHoaDon, string LoaiDon, string LoaiDonText, string Ban,
    string? KhachHang, DateTimeOffset ThoiDiemLap, string TrangThai, string TrangThaiText, decimal TongTien,
    int SoMon, int MonSanSang);

public sealed record OrderLineDto(int Id, string TenMon, string? TenSize, int SoLuong, decimal DonGia,
    string? GhiChu, string TrangThai, string TrangThaiText, DateTimeOffset ThoiDiemGoi,
    bool CoTheMangRa, bool CoTheHuy);

public sealed record OrderDetailDto(int Id, string MaHoaDon, string LoaiDon, string LoaiDonText, string Ban,
    string? KhachHang, string? SoDienThoai, string? DiaChiGiaoHang, string? GhiChu, string TrangThai,
    string TrangThaiText, decimal TongTienHang, decimal TienGiam, decimal TienCocTru, decimal PhaiThu,
    string RowVersion, string? BookingRowVersion, bool CoTheGoiThem, bool CoTheThanhToan,
    string? PhuongThucDaTra, List<OrderLineDto> Lines);

// TrangThai: DaPhucVu (mang món ra) | DaHuy (hủy món chưa nấu xong).
public sealed record UpdateLineRequest(string TrangThai);

// PhuongThuc: TienMat | ChuyenKhoan. SoTienPhaiThu là số app đang hiển thị, server đối chiếu lại.
public sealed record PaymentRequest(string PhuongThuc, decimal TienKhachDua, decimal SoTienPhaiThu,
    string? MaGiaoDich, string RowVersion, string? BookingRowVersion);

public sealed record PaymentResultDto(string Message, decimal TienThoiLai);

// ----- Nhân viên: tiếp nhận đặt bàn -----
public sealed record SuggestedTableDto(int Id, string MaBan, string KhuVuc, int SoCho);

public sealed record ReservationDto(int Id, string MaDatBan, string HoTen, string? SoDienThoai,
    DateTimeOffset GioDen, int SoKhach, string TrangThai, string TrangThaiText, string? Ban, string? YeuCau,
    bool YeuCauVip, string RowVersion, bool CoTheXepBan, bool CoTheNhanBan, List<SuggestedTableDto> GoiYBan);

public sealed record AssignTableRequest(int BanAnId, string RowVersion);

public sealed record MessageDto(string Message);

// ----- Khách hàng -----
public sealed record CustomerSummaryDto(string HoTen, string SoDienThoai, string? Email, int DiemHienCo,
    decimal TongChiTieu, int SoDonHang);

public sealed record PointEntryDto(DateTimeOffset ThoiDiem, int SoDiem, string NoiDung, string? MaHoaDon);
