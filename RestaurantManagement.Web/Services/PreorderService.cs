using System.Data;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Services;

public class PreorderService(RestaurantDbContext db, OrderService orders)
{
    public static bool CanEdit(DatBan booking) => booking.ThoiDiemKetThuc is null
        && !ReservationDepositPolicy.Expired(booking)
        && booking.YeuCauHuy is null && booking.GioKetThucDuKien > DateTimeOffset.UtcNow
        && booking.TrangThai is TrangThaiDatBan.ChoXacNhan or TrangThaiDatBan.ChoCoc
            or TrangThaiDatBan.DaXacNhan or TrangThaiDatBan.DaNhanBan;

    public string Version(DatBan booking) => Convert.ToBase64String((byte[])db.Entry(booking).Property("RowVersion").CurrentValue!);
    private bool Matches(DatBan booking, string? version) => !string.IsNullOrEmpty(version) && Version(booking) == version;
    private Task<DatBan?> Locked(int id) => db.DatBan
        .FromSqlInterpolated($"SELECT * FROM [DatBan] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id}").SingleOrDefaultAsync();

    public async Task<(string? Error, List<MonDatTruoc> Lines)> PrepareLines(List<OrderItemInputModel> items)
    {
        if (items.Count > 60 || items.Any(x => x.SoLuong is < 1 or > 99 || (x.YeuCauCheBien?.Length ?? 0) > 500))
            return ("Chọn tối đa 60 dòng món, mỗi dòng từ 1 đến 99 phần; ghi chú tối đa 500 ký tự.", []);
        var ids = items.Select(x => x.MonAnSizeId).ToArray();
        var sizes = await db.MonAnSize.Include(x => x.MonAn).Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        var dishIds = sizes.Values.Select(s => s.MonAnId).Distinct().ToArray();
        var available = await MenuRules.Available(db).Where(x => dishIds.Contains(x.Id)).Select(x => x.Id).ToListAsync();
        var lines = new List<MonDatTruoc>();
        foreach (var item in items)
        {
            if (!sizes.TryGetValue(item.MonAnSizeId, out var size) || !size.DangSuDung || size.GiaBan <= 0
                || !available.Contains(size.MonAnId) || (item.MonAnId != 0 && item.MonAnId != size.MonAnId))
                return ("Có món hoặc size không còn phục vụ. Giỏ của bạn được giữ lại để kiểm tra.", []);
            var combo = size.MonAn.Loai == LoaiMon.Set
                ? await db.ChiTietCombo.Where(x => x.ComboId == size.MonAnId).Select(x => new { x.MonAn.TenMon, x.SoLuong }).ToListAsync() : null;
            lines.Add(new MonDatTruoc { MonAnId = size.MonAnId, MonAnSizeId = size.Id,
                TenMonLucDat = size.MonAn.TenMon, TenSizeLucDat = size.TenSize, DonGiaThoaThuan = size.GiaBan,
                SoLuong = item.SoLuong, YeuCauCheBien = item.YeuCauCheBien?.Trim(),
                ChiTietComboSnapshot = combo is null ? null : JsonSerializer.Serialize(combo) });
        }
        return (null, lines);
    }

    public Task<string?> Save(int id, int accountId, SavePreorderModel model) => Run(async () =>
    {
        var booking = await Locked(id);
        if (booking is null || !await db.KhachHang.AnyAsync(x => x.Id == booking.KhachHangId && x.TaiKhoanId == accountId))
            return "Không tìm thấy yêu cầu đặt bàn của bạn.";
        if (model.RequestId is null || model.RequestId == Guid.Empty) return "Thiếu mã gửi giỏ hàng. Hãy mở lại trang.";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {
            model.ChuanBiTruoc, Items = model.Items.Select(x => new { x.MonAnSizeId, x.SoLuong, Note = x.YeuCauCheBien?.Trim() ?? "" })
                .OrderBy(x => x.MonAnSizeId).ThenBy(x => x.Note).ThenBy(x => x.SoLuong) }))));
        if (booking.LanLuuMonId == model.RequestId)
            return booking.LanLuuMonHash == hash ? null : "Mã gửi giỏ đã được dùng cho nội dung khác. Hãy tải lại lịch.";
        if (!CanEdit(booking)) return "Lịch này không còn nhận món bổ sung; liên hệ nhà hàng để xử lý.";
        if (booking.ThoiDiemBaoChuyenKhoan is not null) return "Khoản chuyển tiền đang được đối chiếu. Đợi thu ngân xác nhận trước khi sửa món.";
        if (!Matches(booking, model.RowVersion)) return "Danh sách món đã thay đổi. Hãy tải lại lịch để đối chiếu; giỏ nháp vẫn được giữ.";
        var (error, lines) = await PrepareLines(model.Items);
        if (error is not null) return error;
        var sentIds = db.ChiTietHoaDon.Where(x => x.MonDatTruocId != null).Select(x => x.MonDatTruocId!.Value);
        var unsent = await db.MonDatTruoc.Where(x => x.DatBanId == id && !sentIds.Contains(x.Id)).ToListAsync();
        db.MonDatTruoc.RemoveRange(unsent);
        foreach (var line in lines) { line.DatBanId = id; db.MonDatTruoc.Add(line); }
        if (booking.CocTuDong && booking.TrangThai != TrangThaiDatBan.DaNhanBan)
        {
            await db.Entry(booking).Collection(x => x.Ban).LoadAsync();
            var sentTotal = await db.MonDatTruoc.Where(x => x.DatBanId == id && sentIds.Contains(x.Id))
                .SumAsync(x => x.DonGiaThoaThuan * x.SoLuong);
            ReservationDepositPolicy.Apply(booking, sentTotal + lines.Sum(x => x.DonGiaThoaThuan * x.SoLuong));
        }
        booking.ChuanBiTruoc = model.ChuanBiTruoc;
        // Dishes added during service are normal additions, not a new deposit requirement.
        booking.YeuCauCoc |= lines.Count > 0 && booking.TrangThai != TrangThaiDatBan.DaNhanBan;
        booking.LanLuuMonId = model.RequestId;
        booking.LanLuuMonHash = hash;
        // Updating the parent makes every draft writer and kitchen release use the same version.
        db.Entry(booking).Property("LanLuuMonId").IsModified = true;
        return null;
    });

    public Task<string?> RequestCancel(int id, int accountId, string version, string? reason) => Run(async () =>
    {
        var booking = await Locked(id);
        if (booking is null || !await db.KhachHang.AnyAsync(x => x.Id == booking.KhachHangId && x.TaiKhoanId == accountId))
            return "Không tìm thấy lịch của bạn.";
        if (!Matches(booking, version)) return "Lịch đã thay đổi. Hãy tải lại trước khi yêu cầu hủy.";
        if (!CanEdit(booking) || booking.TrangThai == TrangThaiDatBan.DaNhanBan)
            return "Lịch đã nhận bàn hoặc kết thúc; liên hệ nhà hàng để xử lý.";
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500) return "Nhập lý do từ 1 đến 500 ký tự.";
        booking.YeuCauHuy = reason.Trim();
        return null;
    });

    public Task<string?> PreparePayment(int id, int accountId, string version) => Run(async () =>
    {
        var booking = await Locked(id);
        if (booking is null || !await db.KhachHang.AnyAsync(x => x.Id == booking.KhachHangId && x.TaiKhoanId == accountId))
            return "Không tìm thấy lịch đặt bàn của bạn.";
        if (!Matches(booking, version) || !CanEdit(booking)) return "Lịch đã thay đổi hoặc hết hạn. Hãy mở lại lịch.";
        if (booking.CocTuDong || booking.TienCocYeuCau > 0 || booking.TienCocDaNop > 0) return null;
        if (booking.TrangThai is not (TrangThaiDatBan.ChoXacNhan or TrangThaiDatBan.ChoCoc))
            return "Lịch đã xác nhận/nhận bàn không được tự đổi điều kiện cọc.";
        await db.Entry(booking).Collection(x => x.Ban).LoadAsync();
        var total = await db.MonDatTruoc.Where(x => x.DatBanId == id).SumAsync(x => x.DonGiaThoaThuan * x.SoLuong);
        ReservationDepositPolicy.Apply(booking, total);
        return await new TableService(db, this).ReserveOnlineWithinTransaction(booking);
    });

    public Task<string?> NotifyPayment(int id, int accountId, PaymentNoticeModel model) => Run(async () =>
    {
        var booking = await Locked(id);
        if (booking is null || !await db.KhachHang.AnyAsync(x => x.Id == booking.KhachHangId && x.TaiKhoanId == accountId))
            return "Không tìm thấy lịch đặt bàn của bạn.";
        if (!CanEdit(booking) || booking.TrangThai == TrangThaiDatBan.DaNhanBan || booking.TienCocYeuCau <= 0)
            return "Lịch đã hết hạn hoặc không còn cần thanh toán cọc.";
        if (!await db.ChiTietDatBan.AnyAsync(x => x.DatBanId == id))
            return "Lịch chưa được kiểm tra và xếp bàn. Liên hệ nhà hàng trước khi chuyển tiền.";
        var reference = model.MaGiaoDich?.Trim();
        if (booking.ThoiDiemBaoChuyenKhoan is not null)
            return booking.SoTienKhachBao == model.SoTien && booking.MaGiaoDichKhachBao == reference
                ? null : "Đã có khoản chuyển đang chờ đối chiếu. Không báo thêm hoặc sửa món lúc này.";
        if (!Matches(booking, model.RowVersion) || model.SoTien != ReservationDepositPolicy.Remaining(booking) || model.SoTien <= 0)
            return "Số cọc đã thay đổi. Tải lại trang thanh toán để xem đúng số tiền.";
        booking.ThoiDiemBaoChuyenKhoan = DateTimeOffset.UtcNow;
        booking.MaGiaoDichKhachBao = reference;
        booking.SoTienKhachBao = model.SoTien;
        booking.LyDoTuChoiChuyenKhoan = null;
        return null;
    });

    public Task<string?> RejectPaymentNotice(int id, string version, string? reason) => Run(async () =>
    {
        var booking = await Locked(id);
        if (booking is null || !Matches(booking, version) || booking.ThoiDiemBaoChuyenKhoan is null)
            return "Khoản chuyển tiền đã được xử lý hoặc thay đổi. Hãy tải lại trước khi đối chiếu.";
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 300) return "Nhập lý do đối chiếu từ 1 đến 300 ký tự.";
        booking.LyDoTuChoiChuyenKhoan = reason.Trim();
        booking.ThoiDiemBaoChuyenKhoan = null;
        booking.MaGiaoDichKhachBao = null;
        booking.SoTienKhachBao = 0;
        return null;
    });

    public Task<string?> ResolveCancel(int id, string version, string? reason, int? staffId, bool acknowledge) => Run(async () =>
    {
        var booking = await Locked(id);
        if (booking is null || !Matches(booking, version)) return "Lịch đã thay đổi. Hãy đối chiếu lại.";
        if (!acknowledge || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500)
            return "Quản lý phải xác nhận đã đối chiếu món và tiền, đồng thời ghi rõ lý do xử lý.";
        if (booking.TrangThai is not (TrangThaiDatBan.ChoXacNhan or TrangThaiDatBan.ChoCoc or TrangThaiDatBan.DaXacNhan))
            return "Không dùng xử lý hủy trước giờ đến cho lượt khách đã nhận bàn/kết thúc.";
        if (await orders.AvailableDepositAsync(id) > 0) return "Còn cọc chưa xử lý. Hoàn hoặc giữ theo thỏa thuận trước khi đóng lịch.";
        var bills = await db.HoaDon.Include(x => x.ChiTiet).Where(x => x.DatBanId == id && x.TrangThai != TrangThaiHoaDon.DaHuy).ToListAsync();
        if (bills.Any(x => x.TrangThai == TrangThaiHoaDon.ThanhToanMotPhan))
            return "Hóa đơn đã thu một phần; thu ngân phải đối chiếu hoàn tất khoản tiền trước khi đóng lịch.";
        foreach (var bill in bills)
        {
            foreach (var line in bill.ChiTiet.Where(x => x.TrangThai == TrangThaiCheBien.ChoCheBien)) line.TrangThai = TrangThaiCheBien.DaHuy;
            // Preserve cooked items and their cost history; never fabricate payment or restore used stock.
            if (bill.TrangThai == TrangThaiHoaDon.ChuaThanhToan)
            {
                bill.TongTienHang = bill.ChiTiet.Where(x => x.TrangThai != TrangThaiCheBien.DaHuy).Sum(x => x.SoLuong * x.DonGia);
                bill.TienGiam = Math.Min(bill.TienGiam, bill.TongTienHang);
                bill.TrangThai = TrangThaiHoaDon.DaHuy;
            }
        }
        booking.TrangThai = TrangThaiDatBan.DaHuy; booking.ThoiDiemHuy = DateTimeOffset.UtcNow;
        booking.LyDoHuy = reason.Trim(); booking.NhanVienHuyId = staffId;
        return null;
    });

    public Task<string?> Release(int id, string version, int staffId, bool acknowledgeEarly) => Run(async () =>
    {
        var booking = await Locked(id);
        if (booking is null || !Matches(booking, version)) return "Yêu cầu đã thay đổi. Hãy mở lại trước khi gửi bếp.";
        if (booking.TrangThai == TrangThaiDatBan.DaXacNhan && (!booking.ChuanBiTruoc || !acknowledgeEarly))
            return "Khách chưa nhận bàn. Cần yêu cầu chuẩn bị trước và xác nhận của nhân viên.";
        return await ReleaseWithinTransaction(booking, staffId);
    });

    public async Task<string?> ReleaseWithinTransaction(DatBan booking, int staffId)
    {
        if (!CanEdit(booking) || booking.TrangThai is not (TrangThaiDatBan.DaXacNhan or TrangThaiDatBan.DaNhanBan))
            return "Chỉ gửi bếp cho lịch đã xác nhận hoặc đang phục vụ, chưa yêu cầu hủy.";
        if (!await db.NhanVien.AnyAsync(x => x.Id == staffId && x.DangLamViec)) return "Chọn nhân viên đang làm việc phụ trách phiếu gọi món.";
        if (booking.TrangThai == TrangThaiDatBan.DaXacNhan && booking.YeuCauCoc
            && (booking.TienCocYeuCau <= 0 || booking.TienCocDaNop - booking.TienCocDaHoan - booking.TienCocDaGiu < booking.TienCocYeuCau))
            return "Chưa nhận đủ cọc đã thỏa thuận, chưa thể chuẩn bị món trước.";
        var linked = db.ChiTietHoaDon.Where(x => x.MonDatTruocId != null).Select(x => x.MonDatTruocId!.Value);
        var lines = await db.MonDatTruoc.Where(x => x.DatBanId == booking.Id && !linked.Contains(x.Id)).ToListAsync();
        if (lines.Count == 0) return null;
        var sizeIds = lines.Select(x => x.MonAnSizeId).ToArray();
        var sizes = await db.MonAnSize.Where(x => sizeIds.Contains(x.Id) && x.DangSuDung && x.GiaBan > 0).ToListAsync();
        var dishIds = await MenuRules.Available(db).Select(x => x.Id).ToListAsync();
        if (lines.Any(x => !sizes.Any(s => s.Id == x.MonAnSizeId && s.MonAnId == x.MonAnId) || !dishIds.Contains(x.MonAnId)))
            return "Món đặt trước đã tạm hết hoặc ngừng phục vụ. Liên hệ khách để thay món trước khi gửi bếp.";
        var bill = await db.HoaDon.Include(x => x.ChiTiet).Where(x => x.DatBanId == booking.Id && x.TrangThai == TrangThaiHoaDon.ChuaThanhToan)
            .OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (bill is null)
        {
            bill = new HoaDon { MaHoaDon = TableService.NewCode("HD"), DatBanId = booking.Id,
                KhachHangId = booking.KhachHangId, NhanVienId = staffId, ThoiDiemLap = DateTimeOffset.UtcNow,
                TrangThai = TrangThaiHoaDon.ChuaThanhToan, LoaiDonHang = LoaiDonHang.TaiBan };
            db.HoaDon.Add(bill);
        }
        var now = DateTimeOffset.UtcNow;
        foreach (var line in lines)
        {
            bill.ChiTiet.Add(new ChiTietHoaDon { MonDatTruocId = line.Id, MonAnId = line.MonAnId,
                MonAnSizeId = line.MonAnSizeId, TenMonLucBan = line.TenMonLucDat, TenSizeLucBan = line.TenSizeLucDat,
                DonGia = line.DonGiaThoaThuan, SoLuong = line.SoLuong, YeuCauCheBien = line.YeuCauCheBien,
                ChiTietComboSnapshot = line.ChiTietComboSnapshot, ThoiDiemGoi = now, TrangThai = TrangThaiCheBien.ChoCheBien });
        }
        bill.TongTienHang = bill.ChiTiet.Where(x => x.TrangThai != TrangThaiCheBien.DaHuy).Sum(x => x.DonGia * x.SoLuong);
        db.Entry(booking).Property("ChuanBiTruoc").IsModified = true;
        return null;
    }

    public Task<string?> Quote(int id, DepositQuoteModel model) => Run(async () =>
    {
        var booking = await Locked(id);
        if (booking is null || !Matches(booking, model.RowVersion)) return "Yêu cầu đã thay đổi. Hãy tải lại.";
        if (booking.CocTuDong) return "Cọc của lịch online được tính tự động theo món đã chọn; không nhập số cọc tùy ý.";
        if (!CanEdit(booking)) return "Không thay điều kiện cọc của lịch đã kết thúc hoặc đang yêu cầu hủy.";
        if (model.SoTien < 0 || model.SoTien > 1000000000 || string.IsNullOrWhiteSpace(model.DieuKien)
            || model.DieuKien.Length > 1000 || (booking.YeuCauCoc && model.SoTien == 0))
            return "Nhập số cọc và điều kiện hủy/hoàn rõ ràng; lịch cần cọc phải có số tiền lớn hơn 0.";
        booking.TienCocYeuCau = model.SoTien; booking.DieuKienCocDaThoaThuan = model.DieuKien.Trim();
        if (booking.TrangThai != TrangThaiDatBan.DaNhanBan && model.SoTien > booking.TienCocDaNop - booking.TienCocDaHoan - booking.TienCocDaGiu)
            booking.TrangThai = TrangThaiDatBan.ChoCoc;
        return null;
    });

    public Task<string?> RecordDeposit(int id, int actorId, DepositTransactionModel model) => Run(async () =>
    {
        var booking = await Locked(id);
        if (booking is null) return "Không tìm thấy lịch đặt bàn.";
        var existing = await db.GiaoDichCoc.SingleOrDefaultAsync(x => x.YeuCauId == model.RequestId);
        if (existing is not null) return existing.DatBanId == id && existing.Loai == model.Loai && existing.SoTien == model.SoTien
            && existing.MaThamChieu == (string.IsNullOrWhiteSpace(model.MaThamChieu) ? null : model.MaThamChieu.Trim())
            ? null : "Mã giao dịch đã được dùng cho thông tin khác.";
        if (!Matches(booking, model.RowVersion)) return "Số tiền hoặc lịch đã thay đổi. Hãy đối chiếu lại.";
        if (model.RequestId == Guid.Empty || !Enum.IsDefined(model.Loai) || model.SoTien is <= 0 or > 1000000000
            || string.IsNullOrWhiteSpace(model.LyDo) || model.LyDo.Length > 500 || (model.MaThamChieu?.Length ?? 0) > 100)
            return "Thông tin xử lý cọc không hợp lệ.";
        if (model.Loai == LoaiGiaoDichCoc.Thu)
        {
            if (ReservationDepositPolicy.Expired(booking)) return "Đã hết hạn giữ chỗ. Đối chiếu khoản chuyển muộn với khách trước khi tạo lịch mới/hoàn tiền.";
            if (booking.CocTuDong && (booking.ThoiDiemBaoChuyenKhoan is null || model.SoTien != booking.SoTienKhachBao))
                return "Khách chưa báo chuyển khoản hoặc số tiền không khớp khoản đang chờ đối chiếu.";
            if (booking.CocTuDong && !model.DaDoiChieuNganHang) return "Phải đối chiếu tiền thực nhận trên ngân hàng và mã đặt bàn trước khi xác nhận.";
            if (!string.IsNullOrWhiteSpace(model.MaThamChieu) && await db.GiaoDichCoc.AnyAsync(x => x.Loai == LoaiGiaoDichCoc.Thu && x.MaThamChieu == model.MaThamChieu.Trim()))
                return "Mã giao dịch ngân hàng đã được ghi nhận; không dùng lại để xác nhận lần nữa.";
            var cancellationPending = booking.YeuCauHuy is not null && booking.ThoiDiemKetThuc is null
                && booking.GioKetThucDuKien > DateTimeOffset.UtcNow
                && booking.TrangThai is TrangThaiDatBan.ChoCoc or TrangThaiDatBan.DaXacNhan;
            if ((!CanEdit(booking) && !cancellationPending) || booking.TienCocYeuCau <= 0 || string.IsNullOrWhiteSpace(booking.DieuKienCocDaThoaThuan))
                return "Cần thỏa thuận cọc cho lịch còn hiệu lực trước khi ghi nhận tiền.";
            var remaining = booking.TienCocYeuCau - booking.TienCocDaNop + booking.TienCocDaHoan + booking.TienCocDaGiu;
            if (model.SoTien > remaining) return "Khoản thu vượt phần cọc còn thiếu. Đối chiếu hoặc cập nhật thỏa thuận trước.";
            booking.TienCocDaNop += model.SoTien; booking.ThoiDiemCoc = DateTimeOffset.UtcNow;
            booking.TrangThaiCoc = TrangThaiCoc.DaCoc;
            if (booking.YeuCauHuy is null && booking.TrangThai == TrangThaiDatBan.ChoCoc && booking.TienCocDaNop - booking.TienCocDaHoan - booking.TienCocDaGiu >= booking.TienCocYeuCau)
                booking.TrangThai = await db.ChiTietDatBan.AnyAsync(x => x.DatBanId == id) ? TrangThaiDatBan.DaXacNhan : TrangThaiDatBan.ChoXacNhan;
            booking.ThoiDiemBaoChuyenKhoan = null;
            booking.MaGiaoDichKhachBao = null;
            booking.SoTienKhachBao = 0;
        }
        else
        {
            var available = await orders.AvailableDepositAsync(id);
            if (model.SoTien > available) return "Số tiền vượt cọc còn lại; không hoàn/giữ khoản đã đối trừ vào hóa đơn.";
            if (model.Loai == LoaiGiaoDichCoc.Hoan) booking.TienCocDaHoan += model.SoTien;
            else booking.TienCocDaGiu += model.SoTien;
            if (booking.TienCocDaNop == booking.TienCocDaHoan + booking.TienCocDaGiu)
                booking.TrangThaiCoc = booking.TienCocDaHoan > 0 ? TrangThaiCoc.DaHoan : TrangThaiCoc.DaDoiTru;
        }
        db.GiaoDichCoc.Add(new GiaoDichCoc { DatBanId = id, TaiKhoanXuLyId = actorId, Loai = model.Loai,
            SoTien = model.SoTien, ThoiDiem = DateTimeOffset.UtcNow, MaThamChieu = model.MaThamChieu?.Trim(),
            LyDo = model.LyDo.Trim(), YeuCauId = model.RequestId });
        return null;
    });

    public async Task<List<PreorderLineViewModel>> Lines(int id)
    {
        var lines = await db.MonDatTruoc.Include(x => x.MonAn).Where(x => x.DatBanId == id).OrderBy(x => x.Id).ToListAsync();
        var sent = await db.ChiTietHoaDon.Where(x => x.MonDatTruocId != null && x.MonDatTruoc!.DatBanId == id)
            .Select(x => new { Id = x.MonDatTruocId!.Value, x.TrangThai }).ToDictionaryAsync(x => x.Id);
        return lines.Select(x => new PreorderLineViewModel { Id = x.Id, MonAnId = x.MonAnId, SizeId = x.MonAnSizeId,
            Name = x.TenMonLucDat, SizeName = x.TenSizeLucDat, Image = x.MonAn?.HinhAnh, Price = x.DonGiaThoaThuan,
            Quantity = x.SoLuong, Note = x.YeuCauCheBien, Sent = sent.ContainsKey(x.Id),
            Status = sent.TryGetValue(x.Id, out var detail) ? detail.TrangThai.ToString() : "Đặt trước" }).ToList();
    }

    private async Task<string?> Run(Func<Task<string?>> action)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var error = await action();
            if (error is not null) return error;
            await db.SaveChangesAsync(); await tx.CommitAsync(); return null;
        }
        catch (DbUpdateException) { return "Dữ liệu vừa thay đổi. Hãy đối chiếu lại trước khi gửi."; }
        catch (SqlException ex) when (ex.Number is 1205 or 1222) { return "Lịch đang được xử lý. Hãy thử lại sau giây lát."; }
    }
}
