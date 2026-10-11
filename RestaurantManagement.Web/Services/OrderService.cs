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
            MenuDishes = await MenuRules.Available(db).AsNoTracking()
                .Include(x => x.Sizes)
                .OrderBy(x => x.DanhMucId).ThenBy(x => x.TenMon)
                .Select(x => new MenuDishItem
                {
                    Id = x.Id,
                    DanhMucId = x.DanhMucId,
                    TenMon = x.TenMon,
                    HinhAnh = x.HinhAnh,
                    MoTa = x.MoTa,
                    Loai = x.Loai,
                    Sizes = x.Sizes.Where(s => s.DangSuDung && s.GiaBan > 0)
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

        if (model.Items.Count > 60)
            return ("Mỗi đơn hàng chỉ được tối đa 60 dòng món.", null);

        if (model.Items.Any(x => x.SoLuong <= 0))
            return ("Số lượng từng món phải lớn hơn 0.", null);

        if (model.Items.Any(x => x.SoLuong > 99))
            return ("Số lượng mỗi món không được vượt quá 99.", null);

        if (model.Items.Any(x => x.YeuCauCheBien != null && x.YeuCauCheBien.Trim().Length > 500))
            return ("Yêu cầu chế biến mỗi món tối đa 500 ký tự.", null);

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

                if (model.TenNguoiNhan.Trim().Length > 120 || model.SoDienThoaiNhan.Trim().Length > 20)
                    return ("Tên hoặc số điện thoại người nhận quá dài.", null);
                if (model.DiaChiGiaoHang != null && model.DiaChiGiaoHang.Trim().Length > 300)
                    return ("Địa chỉ giao hàng tối đa 300 ký tự.", null);
                if (model.GhiChuDonHang != null && model.GhiChuDonHang.Trim().Length > 500)
                    return ("Ghi chú đơn hàng tối đa 500 ký tự.", null);

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
            var availableMenu = MenuRules.Available(db);
            var sizes = await db.MonAnSize
                .Include(x => x.MonAn)
                .Where(x => sizeIds.Contains(x.Id) && x.DangSuDung && x.GiaBan > 0
                    && availableMenu.Any(d => d.Id == x.MonAnId))
                .ToDictionaryAsync(x => x.Id);

            var now = DateTimeOffset.UtcNow;
            foreach (var item in model.Items)
            {
                if (!sizes.TryGetValue(item.MonAnSizeId, out var size) || size.MonAnId != item.MonAnId)
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
                try
                {
                    await khoService.DeductInventoryForOrderDishesAsync(hoaDon.Id, model.Items, staffId);
                }
                catch
                {
                    // Do not fail the committed order if background inventory deduction encounters errors
                }
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
        if (items.Count > 60)
            return ("Mỗi lần gọi thêm chỉ được tối đa 60 dòng món.", null);
        if (items.Any(x => x.SoLuong <= 0))
            return ("Số lượng từng món phải lớn hơn 0.", null);
        if (items.Any(x => x.SoLuong > 99))
            return ("Số lượng mỗi món không được vượt quá 99.", null);
        if (items.Any(x => x.YeuCauCheBien != null && x.YeuCauCheBien.Trim().Length > 500))
            return ("Yêu cầu chế biến mỗi món tối đa 500 ký tự.", null);

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var bill = await db.HoaDon.Include(x => x.ChiTiet).SingleOrDefaultAsync(x => x.Id == hoaDonId);
            if (bill == null) return ("Hóa đơn không tồn tại.", null);
            if (bill.TrangThai is TrangThaiHoaDon.DaThanhToan or TrangThaiHoaDon.DaHuy)
                return ("Hóa đơn đã thanh toán hoặc đã hủy, không thể gọi thêm món.", null);

            var sizeIds = items.Select(x => x.MonAnSizeId).Distinct().ToList();
            var availableMenu = MenuRules.Available(db);
            var sizes = await db.MonAnSize
                .Include(x => x.MonAn)
                .Where(x => sizeIds.Contains(x.Id) && x.DangSuDung && x.GiaBan > 0
                    && availableMenu.Any(d => d.Id == x.MonAnId))
                .ToDictionaryAsync(x => x.Id);

            var now = DateTimeOffset.UtcNow;
            foreach (var item in items)
            {
                if (!sizes.TryGetValue(item.MonAnSizeId, out var size) || size.MonAnId != item.MonAnId)
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
                try
                {
                    await khoService.DeductInventoryForOrderDishesAsync(bill.Id, items);
                }
                catch
                {
                    // Do not fail the committed addition if background inventory deduction encounters errors
                }
            }

            return (null, bill.Id);
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

    public async Task<string?> UpdateDishStatusAsync(int detailId, TrangThaiCheBien newStatus, bool isWaiterOrAdmin)
    {
        var item = await db.ChiTietHoaDon
            .Include(x => x.HoaDon)
            .SingleOrDefaultAsync(x => x.Id == detailId);

        if (item == null) return "Không tìm thấy món ăn trong đơn hàng.";
        if (item.HoaDon.TrangThai == TrangThaiHoaDon.DaHuy)
            return "Đơn hàng đã hủy, không thể thay đổi trạng thái món.";
        if (item.HoaDon.DatBanId is int reservationId
            && await db.DatBan.AnyAsync(x => x.Id == reservationId && x.TrangThai == TrangThaiDatBan.DaHuy))
            return "Lịch đặt bàn đã hủy, không thể tiếp tục chế biến/phục vụ món.";
        if (newStatus == TrangThaiCheBien.DaHuy
            && item.HoaDon.TrangThai is TrangThaiHoaDon.DaThanhToan or TrangThaiHoaDon.ThanhToanMotPhan)
            return "Không thể hủy món trên hóa đơn đã thu tiền.";

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
            item.HoaDon.TienGiam = Math.Min(item.HoaDon.TienGiam, otherActiveTotal);
            item.HoaDon.TienCocDaTru = Math.Min(item.HoaDon.TienCocDaTru, otherActiveTotal - item.HoaDon.TienGiam);
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

    public async Task<decimal> AvailableDepositAsync(int bookingId, int? excludeInvoiceId = null)
    {
        var booking = await db.DatBan.AsNoTracking().SingleAsync(x => x.Id == bookingId);
        if (booking.TrangThaiCoc == TrangThaiCoc.DaHoan) return 0;
        var allocated = await db.HoaDon.Where(x => x.DatBanId == bookingId
                && x.Id != excludeInvoiceId && x.TrangThai != TrangThaiHoaDon.DaHuy)
            .SumAsync(x => x.TienCocDaTru);
        return Math.Max(0, booking.TienCocDaNop - booking.TienCocDaHoan - booking.TienCocDaGiu - allocated);
    }

    private bool ApplyPaymentVersion<T>(T entity, string? token) where T : class
    {
        var expected = new byte[8];
        if (string.IsNullOrWhiteSpace(token)
            || !Convert.TryFromBase64String(token, expected, out var length) || length != 8)
            return false;
        var version = db.Entry(entity).Property<byte[]>("RowVersion");
        if (version.CurrentValue is null || !version.CurrentValue.SequenceEqual(expected)) return false;
        version.OriginalValue = expected;
        return true;
    }

    public async Task<string?> ProcessPaymentAsync(PaymentFormModel form, int cashierId)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var invoice = await db.HoaDon.Include(x => x.ChiTiet).Include(x => x.DatBan)
                .SingleOrDefaultAsync(x => x.Id == form.Id);
            if (invoice is null) return "Không tìm thấy hóa đơn cần thanh toán.";
            if (invoice.TrangThai != TrangThaiHoaDon.ChuaThanhToan)
                return "Hóa đơn đã thu tiền hoặc đã hủy, không thể thanh toán lại.";
            if (!ApplyPaymentVersion(invoice, form.RowVersion))
                return "Phiên bản hóa đơn không hợp lệ hoặc đã thay đổi. Vui lòng tải lại và đối chiếu.";
            if (invoice.DatBan is not null && !ApplyPaymentVersion(invoice.DatBan, form.BookingRowVersion))
                return "Phiên bản đặt bàn đã thay đổi hoặc không hợp lệ. Vui lòng tải lại để đối chiếu tiền cọc.";
            if (form.PhuongThuc is not (PhuongThucThanhToan.TienMat or PhuongThucThanhToan.ChuyenKhoan or PhuongThucThanhToan.The)
                || form.TienKhachDua < 0 || cashierId <= 0)
                return "Thông tin thanh toán không hợp lệ.";
            if (!await db.NhanVien.AnyAsync(x => x.Id == cashierId && x.DangLamViec))
                return "Không tìm thấy nhân viên đang làm việc để ghi nhận thanh toán.";

            var total = invoice.ChiTiet.Where(x => x.TrangThai != TrangThaiCheBien.DaHuy)
                .Sum(x => x.SoLuong * x.DonGia);
            var discount = form.TienGiam ?? invoice.TienGiam;
            if (discount < 0 || discount > total)
                return "Giảm giá phải từ 0 đến tổng tiền món.";
            var availableDeposit = invoice.DatBanId is int bookingId
                ? await AvailableDepositAsync(bookingId, invoice.Id) : 0;
            var deposit = Math.Min(availableDeposit, total - discount);
            var amountDue = total - discount - deposit;
            if (amountDue <= 0 && deposit <= 0)
                return "Giảm giá không được miễn phí toàn bộ hóa đơn khi không có tiền cọc trừ.";
            if (form.PhuongThuc == PhuongThucThanhToan.TienMat && form.TienKhachDua < amountDue)
                return $"Số tiền khách đưa ({form.TienKhachDua:N0} đ) không đủ để thanh toán ({amountDue:N0} đ).";
            if (form.PhuongThuc != PhuongThucThanhToan.TienMat && form.TienGiam is decimal submittedDiscount
                && submittedDiscount != invoice.TienGiam)
                return "Vui lòng tải lại hóa đơn sau khi thay đổi giảm giá để đối chiếu số tiền thanh toán.";
            var last4 = form.SoThe4SoCuoi?.Trim();
            if (form.PhuongThuc == PhuongThucThanhToan.The && !string.IsNullOrEmpty(last4)
                && (last4.Length != 4 || last4.Any(c => c < '0' || c > '9')))
                return "Vui lòng nhập đúng 4 số cuối của thẻ.";
            var reference = string.IsNullOrWhiteSpace(form.MaGiaoDich) ? null : form.MaGiaoDich.Trim();
            if (form.PhuongThuc != PhuongThucThanhToan.TienMat && reference is null)
                return "Vui lòng nhập mã giao dịch/tham chiếu thanh toán.";
            var transactionReference = form.PhuongThuc == PhuongThucThanhToan.TienMat ? null : reference;
            if (form.PhuongThuc == PhuongThucThanhToan.The)
            {
                var cardType = string.IsNullOrWhiteSpace(form.LoaiThe) ? "Thẻ ngân hàng" : form.LoaiThe.Trim();
                transactionReference = $"{cardType} *{last4 ?? "XXXX"}" + (reference is null ? "" : $" (Tham chiếu: {reference})");
            }
            if (transactionReference?.Length > 100) return "Mã tham chiếu thanh toán quá dài.";

            invoice.TongTienHang = total;
            invoice.TienGiam = discount;
            invoice.TienCocDaTru = deposit;
            if (invoice.DatBan is not null)
            {
                if (deposit > 0 && deposit == availableDeposit)
                    invoice.DatBan.TrangThaiCoc = TrangThaiCoc.DaDoiTru;
                // Advance the booking token even when a partial allocation keeps its deposit status.
                db.Entry(invoice.DatBan).Property(x => x.TrangThaiCoc).IsModified = true;
            }
            invoice.TienKhachDua = form.PhuongThuc == PhuongThucThanhToan.TienMat ? form.TienKhachDua : amountDue;
            invoice.TienThoiLai = form.PhuongThuc == PhuongThucThanhToan.TienMat ? form.TienKhachDua - amountDue : 0;
            invoice.MaGiaoDich = transactionReference;
            invoice.NhanVienId = cashierId;
            invoice.PhuongThucThanhToan = form.PhuongThuc;
            invoice.ThoiDiemThanhToan = DateTimeOffset.UtcNow;
            invoice.TrangThai = TrangThaiHoaDon.DaThanhToan;
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return null;
        }
        catch (DbUpdateConcurrencyException)
        {
            return "Hóa đơn hoặc tiền cọc đã thay đổi. Vui lòng tải lại và kiểm tra.";
        }
        catch (SqlException ex) when (ex.Number is 1205 or 1222)
        {
            return "Hệ thống đang xử lý thanh toán khác. Vui lòng tải lại và thử lại.";
        }
    }
}
