using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Services;

public class OrderService(RestaurantDbContext db, IKhoService? khoService = null)
{
    public async Task<List<BanAnCardItem>> GetOccupiedTablesAsync()
    {
        var occupiedTables = await db.BanAn.AsNoTracking()
            .Include(x => x.KhuVuc)
            .Where(x => x.TrangThai == TrangThaiBan.DangPhucVu && x.KhuVuc.DangSuDung)
            .OrderBy(x => x.KhuVuc.Tang).ThenBy(x => x.KhuVuc.TenKhuVuc).ThenBy(x => x.MaBan)
            .ToListAsync();

        var tableIds = occupiedTables.Select(x => x.Id).ToArray();
        var bookings = await db.ChiTietDatBan.AsNoTracking()
            .Include(x => x.DatBan)
            .Where(x => tableIds.Contains(x.BanAnId)
                && x.DatBan.TrangThai == TrangThaiDatBan.DaNhanBan
                && x.DatBan.ThoiDiemKetThuc == null)
            .ToListAsync();

        return occupiedTables.Select(t =>
        {
            var b = bookings.Where(x => x.BanAnId == t.Id)
                .OrderByDescending(x => x.DatBan.ThoiDiemNhanBan)
                .Select(x => x.DatBan)
                .FirstOrDefault();

            return new BanAnCardItem
            {
                Id = t.Id,
                MaBan = t.MaBan,
                TenKhuVuc = t.KhuVuc.TenKhuVuc,
                Tang = t.KhuVuc.Tang,
                SoChoNgoi = t.SoChoNgoi,
                DatBanId = b?.Id,
                KhachHang = b?.HoTenLienHe,
                SoKhach = (b?.SoNguoiLon ?? 0) + (b?.SoTreEm ?? 0),
                GioNhan = b?.ThoiDiemNhanBan
            };
        }).ToList();
    }

    public async Task<OrderCreateViewModel> PrepareCreateViewModelAsync(int? banAnId = null)
    {
        var model = new OrderCreateViewModel
        {
            BanAnId = banAnId,
            OccupiedTables = await GetOccupiedTablesAsync(),
            Categories = await db.DanhMuc.AsNoTracking()
                .Where(x => x.DangSuDung)
                .OrderBy(x => x.TenDanhMuc)
                .ToListAsync(),
            MenuDishes = await db.MonAn.AsNoTracking()
                .Include(x => x.Sizes)
                .Where(x => x.DaDuyet && x.TrangThai == TrangThaiMon.DangPhucVu)
                .OrderBy(x => x.DanhMucId).ThenBy(x => x.TenMon)
                .Select(x => new MenuDishItem
                {
                    Id = x.Id,
                    DanhMucId = x.DanhMucId,
                    TenMon = x.TenMon,
                    HinhAnh = x.HinhAnh,
                    MoTa = x.MoTa,
                    Loai = x.Loai,
                    Sizes = x.Sizes.Where(s => s.DangSuDung)
                        .OrderBy(s => s.GiaBan)
                        .Select(s => new MenuSizeItem
                        {
                            Id = s.Id,
                            TenSize = s.TenSize,
                            GiaBan = s.GiaBan
                        }).ToList()
                })
                .Where(x => x.Sizes.Count > 0)
                .ToListAsync()
        };

        if (banAnId.HasValue)
        {
            var match = model.OccupiedTables.FirstOrDefault(x => x.Id == banAnId.Value);
            if (match != null)
            {
                model.DatBanId = match.DatBanId;
                model.LoaiDonHang = LoaiDonHang.TaiBan;
            }
        }

        return model;
    }

    public async Task<(string? Error, int? HoaDonId)> CreateOrderAsync(OrderCreateViewModel model, int staffId)
    {
        if (model.Items.Count == 0)
            return ("Vui lòng chọn ít nhất một món ăn vào đơn hàng.", null);

        if (model.Items.Any(x => x.SoLuong <= 0))
            return ("Số lượng từng món phải lớn hơn 0.", null);

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            HoaDon hoaDon;

            if (model.LoaiDonHang == LoaiDonHang.TaiBan)
            {
                if (!model.BanAnId.HasValue)
                    return ("Vui lòng chọn bàn đang phục vụ để gọi món tại bàn.", null);

                var table = await db.BanAn.SingleOrDefaultAsync(x => x.Id == model.BanAnId.Value);
                if (table == null || table.TrangThai != TrangThaiBan.DangPhucVu)
                    return ("Bàn đã chọn không ở trạng thái đang phục vụ.", null);

                var booking = await db.ChiTietDatBan
                    .Include(x => x.DatBan)
                    .Where(x => x.BanAnId == model.BanAnId.Value
                        && x.DatBan.TrangThai == TrangThaiDatBan.DaNhanBan
                        && x.DatBan.ThoiDiemKetThuc == null)
                    .OrderByDescending(x => x.DatBan.ThoiDiemNhanBan)
                    .Select(x => x.DatBan)
                    .FirstOrDefaultAsync();

                if (booking == null)
                    return ("Không tìm thấy thông tin lượt khách đang phục vụ tại bàn này.", null);

                // Find active unpaid bill for this table booking
                var activeBill = await db.HoaDon
                    .Include(x => x.ChiTiet)
                    .FirstOrDefaultAsync(x => x.DatBanId == booking.Id && x.TrangThai != TrangThaiHoaDon.DaHuy && x.TrangThai != TrangThaiHoaDon.DaThanhToan);

                if (activeBill == null)
                {
                    activeBill = new HoaDon
                    {
                        MaHoaDon = TableService.NewCode("HD"),
                        LoaiDonHang = LoaiDonHang.TaiBan,
                        DatBanId = booking.Id,
                        KhachHangId = booking.KhachHangId,
                        NhanVienId = staffId,
                        ThoiDiemLap = DateTimeOffset.UtcNow,
                        TrangThai = TrangThaiHoaDon.ChuaThanhToan,
                        GhiChuDonHang = model.GhiChuDonHang
                    };
                    db.HoaDon.Add(activeBill);
                    await db.SaveChangesAsync();
                }

                hoaDon = activeBill;
            }
            else
            {
                // Takeaway or Delivery
                if (string.IsNullOrWhiteSpace(model.TenNguoiNhan))
                    return ("Vui lòng nhập tên khách hàng / người nhận.", null);

                if (string.IsNullOrWhiteSpace(model.SoDienThoaiNhan))
                    return ("Vui lòng nhập số điện thoại người nhận.", null);

                if (model.LoaiDonHang == LoaiDonHang.GiaoHang && string.IsNullOrWhiteSpace(model.DiaChiGiaoHang))
                    return ("Vui lòng nhập địa chỉ giao hàng.", null);

                hoaDon = new HoaDon
                {
                    MaHoaDon = TableService.NewCode(model.LoaiDonHang == LoaiDonHang.MangDi ? "HD-MD" : "HD-GH"),
                    LoaiDonHang = model.LoaiDonHang,
                    TenNguoiNhan = model.TenNguoiNhan.Trim(),
                    SoDienThoaiNhan = model.SoDienThoaiNhan.Trim(),
                    DiaChiGiaoHang = model.DiaChiGiaoHang?.Trim(),
                    GhiChuDonHang = model.GhiChuDonHang?.Trim(),
                    NhanVienId = staffId,
                    ThoiDiemLap = DateTimeOffset.UtcNow,
                    TrangThai = TrangThaiHoaDon.ChuaThanhToan
                };
                db.HoaDon.Add(hoaDon);
                await db.SaveChangesAsync();
            }

            // Validate all dishes and sizes
            var sizeIds = model.Items.Select(x => x.MonAnSizeId).Distinct().ToList();
            var sizes = await db.MonAnSize
                .Include(x => x.MonAn)
                .Where(x => sizeIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            var now = DateTimeOffset.UtcNow;
            foreach (var item in model.Items)
            {
                if (!sizes.TryGetValue(item.MonAnSizeId, out var size))
                    return ($"Không tìm thấy size món ăn hợp lệ cho một số món được chọn.", null);

                var dish = size.MonAn;
                if (!dish.DaDuyet || dish.TrangThai != TrangThaiMon.DangPhucVu)
                    return ($"Món '{dish.TenMon}' hiện không khả dụng để phục vụ.", null);

                if (!size.DangSuDung)
                    return ($"Size '{size.TenSize}' của món '{dish.TenMon}' đã tạm ngưng sử dụng.", null);

                var detail = new ChiTietHoaDon
                {
                    HoaDonId = hoaDon.Id,
                    MonAnId = dish.Id,
                    MonAnSizeId = size.Id,
                    TenMonLucBan = dish.TenMon,
                    TenSizeLucBan = size.TenSize,
                    DonGia = size.GiaBan,
                    SoLuong = item.SoLuong,
                    YeuCauCheBien = item.YeuCauCheBien?.Trim(),
                    TrangThai = TrangThaiCheBien.ChoCheBien,
                    ThoiDiemGoi = now
                };
                db.ChiTietHoaDon.Add(detail);
            }

            await db.SaveChangesAsync();

            // Recalculate bill total for active items
            var allActiveItems = await db.ChiTietHoaDon
                .Where(x => x.HoaDonId == hoaDon.Id && x.TrangThai != TrangThaiCheBien.DaHuy)
                .ToListAsync();

            hoaDon.TongTienHang = allActiveItems.Sum(x => x.SoLuong * x.DonGia);
            await db.SaveChangesAsync();
            await tx.CommitAsync();

            if (khoService != null)
            {
                await khoService.DeductInventoryForOrderDishesAsync(hoaDon.Id, model.Items, staffId);
            }

            return (null, hoaDon.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ("Dữ liệu vừa thay đổi từ phiên làm việc khác. Vui lòng thử lại.", null);
        }
        catch (SqlException ex) when (ex.Number is 1205 or 1222)
        {
            return ("Hệ thống đang bận xử lý dữ liệu đơn hàng. Vui lòng thử lại sau giây lát.", null);
        }
    }

    public async Task<(string? Error, int? HoaDonId)> AddDishesAsync(int hoaDonId, List<OrderItemInputModel> items)
    {
        if (items.Count == 0)
            return ("Vui lòng chọn ít nhất một món ăn.", null);

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var bill = await db.HoaDon.Include(x => x.ChiTiet).SingleOrDefaultAsync(x => x.Id == hoaDonId);
            if (bill == null) return ("Hóa đơn không tồn tại.", null);
            if (bill.TrangThai is TrangThaiHoaDon.DaThanhToan or TrangThaiHoaDon.DaHuy)
                return ("Hóa đơn đã thanh toán hoặc đã hủy, không thể gọi thêm món.", null);

            var sizeIds = items.Select(x => x.MonAnSizeId).Distinct().ToList();
            var sizes = await db.MonAnSize
                .Include(x => x.MonAn)
                .Where(x => sizeIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            var now = DateTimeOffset.UtcNow;
            foreach (var item in items)
            {
                if (item.SoLuong <= 0) continue;
                if (!sizes.TryGetValue(item.MonAnSizeId, out var size))
                    return ("Size món ăn không hợp lệ.", null);

                var dish = size.MonAn;
                if (!dish.DaDuyet || dish.TrangThai != TrangThaiMon.DangPhucVu)
                    return ($"Món '{dish.TenMon}' hiện không khả dụng để phục vụ.", null);

                var detail = new ChiTietHoaDon
                {
                    HoaDonId = bill.Id,
                    MonAnId = dish.Id,
                    MonAnSizeId = size.Id,
                    TenMonLucBan = dish.TenMon,
                    TenSizeLucBan = size.TenSize,
                    DonGia = size.GiaBan,
                    SoLuong = item.SoLuong,
                    YeuCauCheBien = item.YeuCauCheBien?.Trim(),
                    TrangThai = TrangThaiCheBien.ChoCheBien,
                    ThoiDiemGoi = now
                };
                db.ChiTietHoaDon.Add(detail);
            }

            await db.SaveChangesAsync();

            var activeItems = await db.ChiTietHoaDon
                .Where(x => x.HoaDonId == bill.Id && x.TrangThai != TrangThaiCheBien.DaHuy)
                .ToListAsync();

            bill.TongTienHang = activeItems.Sum(x => x.SoLuong * x.DonGia);
            await db.SaveChangesAsync();
            await tx.CommitAsync();

            if (khoService != null)
            {
                await khoService.DeductInventoryForOrderDishesAsync(bill.Id, items);
            }

            return (null, bill.Id);
        }
        catch (Exception ex)
        {
            return (ex.Message, null);
        }
    }

    public async Task<string?> UpdateDishStatusAsync(int detailId, TrangThaiCheBien newStatus, bool isWaiterOrAdmin)
    {
        var item = await db.ChiTietHoaDon
            .Include(x => x.HoaDon)
            .SingleOrDefaultAsync(x => x.Id == detailId);

        if (item == null) return "Không tìm thấy món ăn trong đơn hàng.";
        if (item.HoaDon.TrangThai == TrangThaiHoaDon.DaHuy)
            return "Đơn hàng đã hủy, không thể thay đổi trạng thái món.";

        bool valid = false;

        // Bếp transitions: ChoCheBien -> DangCheBien -> SanSang
        if (!isWaiterOrAdmin)
        {
            valid = (item.TrangThai == TrangThaiCheBien.ChoCheBien && newStatus == TrangThaiCheBien.DangCheBien)
                 || (item.TrangThai == TrangThaiCheBien.DangCheBien && newStatus == TrangThaiCheBien.SanSang);
        }
        else
        {
            // Waiter / Admin transitions:
            // SanSang -> DaPhucVu (mang ra bàn)
            // ChoCheBien -> DaHuy (khách đổi ý hủy món trước khi nấu)
            valid = (item.TrangThai == TrangThaiCheBien.SanSang && newStatus == TrangThaiCheBien.DaPhucVu)
                 || (item.TrangThai == TrangThaiCheBien.ChoCheBien && newStatus == TrangThaiCheBien.DaHuy);
        }

        if (!valid)
            return $"Không thể chuyển trạng thái từ '{item.TrangThai}' sang '{newStatus}'.";

        item.TrangThai = newStatus;

        // If cancelled, adjust invoice total
        if (newStatus == TrangThaiCheBien.DaHuy)
        {
            var otherActiveTotal = await db.ChiTietHoaDon
                .Where(x => x.HoaDonId == item.HoaDonId && x.Id != item.Id && x.TrangThai != TrangThaiCheBien.DaHuy)
                .SumAsync(x => x.SoLuong * x.DonGia);

            item.HoaDon.TongTienHang = otherActiveTotal;
        }

        try
        {
            await db.SaveChangesAsync();
            return null;
        }
        catch (DbUpdateConcurrencyException)
        {
            return "Món ăn đã được cập nhật bởi thao tác khác. Vui lòng tải lại.";
        }
    }

    public async Task<string?> ProcessPaymentAsync(PaymentFormModel form, int cashierId)
    {
        var invoice = await db.HoaDon
            .Include(x => x.ChiTiet)
            .Include(x => x.DatBan)
            .SingleOrDefaultAsync(x => x.Id == form.Id);

        if (invoice == null) return "Không tìm thấy hóa đơn cần thanh toán.";
        if (invoice.TrangThai == TrangThaiHoaDon.DaThanhToan) return "Hóa đơn này đã được thanh toán trước đó.";
        if (invoice.TrangThai == TrangThaiHoaDon.DaHuy) return "Hóa đơn đã bị hủy, không thể thanh toán.";

        // Concurrency token validation
        if (!string.IsNullOrEmpty(form.RowVersion))
        {
            var expectedVersion = new byte[8];
            if (Convert.TryFromBase64String(form.RowVersion, expectedVersion, out var len) && len == 8)
            {
                var currentVersion = db.Entry(invoice).Property<byte[]>("RowVersion").CurrentValue;
                if (currentVersion == null || !currentVersion.SequenceEqual(expectedVersion))
                {
                    return "Hóa đơn đã thay đổi từ lần xem trước. Vui lòng đối chiếu lại thông tin.";
                }
                db.Entry(invoice).Property<byte[]>("RowVersion").OriginalValue = expectedVersion;
            }
        }

        // Recalculate bill items
        var activeItemsTotal = invoice.ChiTiet
            .Where(x => x.TrangThai != TrangThaiCheBien.DaHuy)
            .Sum(x => x.SoLuong * x.DonGia);

        invoice.TongTienHang = activeItemsTotal;

        // Apply discount if provided
        decimal discount = Math.Max(0, form.TienGiam);
        if (discount > invoice.TongTienHang)
            discount = invoice.TongTienHang;
        invoice.TienGiam = discount;

        // Apply deposit deduction from DatBan if applicable
        decimal depositDeducted = 0;
        if (invoice.DatBan != null && invoice.DatBan.TienCocDaNop > 0)
        {
            var maxDeductible = invoice.TongTienHang - invoice.TienGiam;
            depositDeducted = Math.Min(invoice.DatBan.TienCocDaNop, maxDeductible);
            invoice.TienCocDaTru = depositDeducted;
            if (depositDeducted >= invoice.DatBan.TienCocDaNop)
            {
                invoice.DatBan.TrangThaiCoc = TrangThaiCoc.DaDoiTru;
            }
        }

        var amountDue = invoice.TongTienHang - invoice.TienGiam - invoice.TienCocDaTru;

        // Check payment methods
        if (form.PhuongThuc == PhuongThucThanhToan.TienMat)
        {
            if (amountDue > 0 && form.TienKhachDua < amountDue)
                return $"Số tiền khách đưa ({form.TienKhachDua:N0} đ) không đủ để thanh toán ({amountDue:N0} đ).";

            invoice.TienKhachDua = form.TienKhachDua;
            invoice.TienThoiLai = Math.Max(0, form.TienKhachDua - amountDue);
            invoice.MaGiaoDich = null;
        }
        else if (form.PhuongThuc == PhuongThucThanhToan.ChuyenKhoan)
        {
            invoice.TienKhachDua = amountDue;
            invoice.TienThoiLai = 0;
            invoice.MaGiaoDich = string.IsNullOrWhiteSpace(form.MaGiaoDich)
                ? $"QR-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}"
                : form.MaGiaoDich.Trim();
        }
        else if (form.PhuongThuc == PhuongThucThanhToan.The)
        {
            invoice.TienKhachDua = amountDue;
            invoice.TienThoiLai = 0;
            var cardType = string.IsNullOrWhiteSpace(form.LoaiThe) ? "Thẻ ngân hàng" : form.LoaiThe.Trim();
            var last4 = string.IsNullOrWhiteSpace(form.SoThe4SoCuoi) ? "XXXX" : form.SoThe4SoCuoi.Trim();
            var authCode = string.IsNullOrWhiteSpace(form.MaGiaoDich) ? Guid.NewGuid().ToString("N")[..8].ToUpperInvariant() : form.MaGiaoDich.Trim();
            invoice.MaGiaoDich = $"{cardType} *{last4} (Auth: {authCode})";
        }
        else
        {
            invoice.TienKhachDua = amountDue;
            invoice.TienThoiLai = 0;
            invoice.MaGiaoDich = form.MaGiaoDich?.Trim();
        }

        invoice.NhanVienId = cashierId;
        invoice.PhuongThucThanhToan = form.PhuongThuc;
        invoice.ThoiDiemThanhToan = DateTimeOffset.UtcNow;
        invoice.TrangThai = TrangThaiHoaDon.DaThanhToan;

        try
        {
            await db.SaveChangesAsync();
            return null;
        }
        catch (DbUpdateConcurrencyException)
        {
            return "Hóa đơn đã bị thay đổi bởi thao tác khác. Vui lòng tải lại và kiểm tra.";
        }
    }
}
