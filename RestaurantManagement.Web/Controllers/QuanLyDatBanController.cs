using System.Security.Claims;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Security;
using RestaurantManagement.Web.Services;
using RestaurantManagement.Shared;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin + "," + AppRoles.TiepTan)]
public class QuanLyDatBanController(RestaurantDbContext context, TableService tables) : ManagementControllerBase(context)
{
    // GET /QuanLyDatBan
    [HttpGet]
    public async Task<IActionResult> Index(string? tab = "ChoXacNhan", string? filterNgay = null)
    {
        tab ??= "ChoXacNhan";
        if (tab == "HoanTatHuy") tab = "HoanTat";
        if (tab is not ("ChoXacNhan" or "ChoDoiChieu" or "DaXacNhan" or "DangPhucVu" or "HoanTat" or "DaHuy")) return BadRequest("Trạng thái lọc không hợp lệ.");
        ViewData["ActiveMenu"] = "QuanLyDatBan";
        ViewBag.CurrentTab = tab ?? "ChoXacNhan";

        var allQuery = Db.DatBan
            .AsSplitQuery()
            .Include(x => x.Ban).ThenInclude(b => b.BanAn).ThenInclude(b => b.KhuVuc)
            .Include(x => x.HoaDon).ThenInclude(h => h.ChiTiet)
            .AsTracking();

        DateOnly? parsedDate = null;
        if (!string.IsNullOrWhiteSpace(filterNgay))
        {
            if (!DateOnly.TryParse(filterNgay, System.Globalization.CultureInfo.InvariantCulture, out var d) || d.Year is < 1900 or > 2100) return BadRequest("Ngày lọc không hợp lệ.");
            parsedDate = d;
            var startUtc = TableService.VietnamTime(d.ToDateTime(TimeOnly.MinValue));
            var endUtc = startUtc.AddDays(1);
            allQuery = allQuery.Where(x => x.GioDen >= startUtc && x.GioDen < endUtc);
        }

        var todayStart = TableService.VietnamTime(TableService.VietnamNow.Date);
        var todayEnd = todayStart.AddDays(1);

        // Thống kê nhanh theo tab
        var now = DateTimeOffset.UtcNow;
        // Khách đã báo chuyển khoản cọc: tách riêng để thu ngân đối chiếu, không lẫn với lịch chưa nộp cọc.
        Expression<Func<DatBan, bool>> paymentNotice = x => (x.TrangThai == TrangThaiDatBan.ChoXacNhan || x.TrangThai == TrangThaiDatBan.ChoCoc)
            && x.ThoiDiemBaoChuyenKhoan != null;
        Expression<Func<DatBan, bool>> waiting = x => (x.TrangThai == TrangThaiDatBan.ChoXacNhan || x.TrangThai == TrangThaiDatBan.ChoCoc)
            && x.ThoiDiemBaoChuyenKhoan == null
            && (parsedDate.HasValue || x.GioKetThucDuKien > now || x.TienCocDaNop > x.TienCocDaHoan + x.TienCocDaGiu);
        var choXacNhanCount = await allQuery.CountAsync(waiting);
        var choDoiChieuCount = await allQuery.CountAsync(paymentNotice);
        var daXacNhanCount = await allQuery.CountAsync(x => x.TrangThai == TrangThaiDatBan.DaXacNhan);
        var dangPhucVuCount = await allQuery.CountAsync(x => x.TrangThai == TrangThaiDatBan.DaNhanBan && x.ThoiDiemKetThuc == null);
        var daHuyCount = await allQuery.CountAsync(x => x.TrangThai == TrangThaiDatBan.DaHuy);
        var homNayCount = await Db.DatBan.CountAsync(x => x.GioDen >= todayStart && x.GioDen < todayEnd);

        // Lọc dữ liệu theo tab đã chọn
        var tabQuery = tab switch
        {
            "ChoDoiChieu" => allQuery.Where(paymentNotice),
            "DaXacNhan" => allQuery.Where(x => x.TrangThai == TrangThaiDatBan.DaXacNhan),
            "DangPhucVu" => allQuery.Where(x => x.TrangThai == TrangThaiDatBan.DaNhanBan && x.ThoiDiemKetThuc == null),
            "HoanTat" => allQuery.Where(x => x.TrangThai == TrangThaiDatBan.KhongDen
                                                || (x.TrangThai == TrangThaiDatBan.DaNhanBan && x.ThoiDiemKetThuc != null)),
            "DaHuy" => allQuery.Where(x => x.TrangThai == TrangThaiDatBan.DaHuy),
            _ => allQuery.Where(waiting)
        };

        var rawList = await tabQuery.OrderBy(x => x.GioDen).ToListAsync();

        var model = new DatBanAdminVM
        {
            FilterTrangThai = tab,
            FilterNgay = parsedDate,
            ChoXacNhanCount = choXacNhanCount,
            ChoDoiChieuCount = choDoiChieuCount,
            DaXacNhanCount = daXacNhanCount,
            DangPhucVuCount = dangPhucVuCount,
            DaHuyCount = daHuyCount,
            HomNayCount = homNayCount,
            DanhSachBanTrong = await GetBanAnSelectOptionsAsync(),
            DanhSachDatBan = rawList.Select(x =>
            {
                var invoice = ResolveBookingInvoice(x);
                var daThanhToan = invoice?.TrangThai == TrangThaiHoaDon.DaThanhToan;
                var isCompleted = x.TrangThai == TrangThaiDatBan.DaNhanBan && x.ThoiDiemKetThuc.HasValue;

                string badgeClass = x.TrangThai switch
                {
                    TrangThaiDatBan.ChoXacNhan => "badge-status-warning",
                    TrangThaiDatBan.DaXacNhan => "badge-status-info text-white bg-primary",
                    TrangThaiDatBan.DaNhanBan when !isCompleted => "badge-status-success",
                    TrangThaiDatBan.DaHuy => "badge-status-danger",
                    _ => isCompleted ? "badge-status-success" : "badge-secondary"
                };

                string trangThaiStr = x.TrangThai switch
                {
                    TrangThaiDatBan.ChoXacNhan => "Chờ xác nhận",
                    TrangThaiDatBan.ChoCoc => "Chờ cọc",
                    TrangThaiDatBan.DaXacNhan => "Đã xác nhận",
                    TrangThaiDatBan.DaNhanBan when !isCompleted => "Đang phục vụ",
                    TrangThaiDatBan.DaHuy => "Đã hủy",
                    TrangThaiDatBan.KhongDen => "Không đến",
                    _ => isCompleted ? "Hoàn tất" : x.TrangThai.ToString()
                };

                return new DatBanItemVM
                {
                    MaDatBan = x.Id,
                    RowVersion = VersionOf(x),
                    BookingCode = x.MaDatBan,
                    HoTen = x.HoTenLienHe,
                    SoDienThoai = x.SoDienThoaiLienHe ?? "",
                    ThoiGianDen = x.GioDen,
                    SoNguoi = x.SoNguoiLon + x.SoTreEm,
                    TenBan = x.Ban.Any() ? string.Join(", ", x.Ban.Select(b => $"{b.BanAn.MaBan} ({b.BanAn.KhuVuc.TenKhuVuc})")) : "Chưa chọn bàn",
                    BanAnId = x.Ban.Select(b => b.BanAnId).FirstOrDefault(),
                    TrangThai = trangThaiStr,
                    TrangThaiBadgeClass = badgeClass,
                    GhiChu = x.YeuCau,
                    GioKetThuc = x.GioKetThucDuKien,
                    TinhTrangCoc = ReservationDepositPolicy.StatusText(x),
                    GiaiDoanCoc = ReservationDepositPolicy.Stage(x).Text,
                    GiaiDoanCocClass = ReservationDepositPolicy.Stage(x).Css,
                    ChoDoiChieuCoc = x.ThoiDiemBaoChuyenKhoan != null,
                    SoTienKhachBao = x.SoTienKhachBao,
                    MaGiaoDichKhachBao = x.MaGiaoDichKhachBao,
                    HoaDonId = invoice?.Id,
                    TongTienHoaDon = invoice?.ChiTiet.Sum(c => c.SoLuong * c.DonGia) ?? 0,
                    DaThanhToan = daThanhToan
                };
            }).ToList()
        };

        return View(model);
    }

    // POST /QuanLyDatBan/XacNhan
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> XacNhan(int datBanId, int[] banAnIds, string? rowVersion, int banAnId = 0)
    {
        if (banAnIds.Length == 0 && banAnId > 0) banAnIds = [banAnId];
        var hadTables = await Db.ChiTietDatBan.AnyAsync(x => x.DatBanId == datBanId);
        var error = await tables.Assign(datBanId, banAnIds, rowVersion, await StaffId());
        var done = hadTables ? "Đã đổi bàn." : "Đã chọn bàn.";
        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            return error is null ? Json(new { ok = true, message = $"{done} Lịch chỉ xác nhận khi đủ cọc đã đối chiếu." })
                : StatusCode(409, new { ok = false, message = error });
        TempData[error is null ? "SuccessMessage" : "ErrorMessage"] = error ?? $"{done} Lịch cần cọc chỉ được xác nhận sau khi đủ cọc đã thỏa thuận.";
        return RedirectToAction("Index", "SoDoBan", error is null ? null : new { datBanId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NhanBan(int datBanId, string? rowVersion)
    {
        var error = await tables.CheckIn(datBanId, rowVersion, await CheckInStaffId());
        TempData[error is null ? "SuccessMessage" : "ErrorMessage"] = error ?? "Khách đã nhận bàn. Bàn chuyển sang đang phục vụ.";
        return RedirectToAction(nameof(Index), new { tab = error is null ? "DangPhucVu" : "DaXacNhan" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> KhongDen(int datBanId, string? rowVersion)
    {
        var error = await tables.MarkNoShow(datBanId, rowVersion, await StaffId());
        TempData[error is null ? "SuccessMessage" : "ErrorMessage"] = error ?? "Đã ghi nhận khách không đến, bàn được trả cho lượt khác. Tiền đặt cọc (nếu có) xử lý trong mục Món & cọc.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> HuyBan(int datBanId, string? lyDoHuy, string? rowVersion)
    {
        var error = await tables.Cancel(datBanId, rowVersion, lyDoHuy, await StaffId());
        TempData[error is null ? "SuccessMessage" : "ErrorMessage"] = error ?? "Đã hủy yêu cầu đặt bàn.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> DoiGio(int id, bool dialog = false)
    {
        var booking = await Db.DatBan.SingleOrDefaultAsync(x => x.Id == id);
        if (booking is null) return NotFound();
        var model = new BookingRescheduleViewModel { Id = id, Code = booking.MaDatBan, Customer = booking.HoTenLienHe,
            GioDen = booking.GioDen.ToOffset(TimeSpan.FromHours(7)).DateTime, RowVersion = VersionOf(booking) };
        return dialog ? PartialView("DoiGio", model) : View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiGio(BookingRescheduleViewModel model)
    {
        var error = ModelState.IsValid ? await tables.Reschedule(model.Id, model.RowVersion, model.GioDen, model.LyDo, await StaffId()) : "Điền giờ đến và lý do hợp lệ.";
        if (Request.Headers.XRequestedWith == "XMLHttpRequest") return error is null
            ? Json(new { ok = true, message = "Đã đổi giờ. Mã đặt bàn, món và khoản cọc được giữ nguyên." })
            : StatusCode(409, new { ok = false, message = error });
        if (error is not null) { ModelState.AddModelError("", error); return View(model); }
        TempData["SuccessMessage"] = "Đã cập nhật giờ đến."; return RedirectToAction(nameof(Index));
    }

    private Task<int?> StaffId()
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var accountId);
        return Db.NhanVien.Where(x => x.TaiKhoanId == accountId && x.DangLamViec).Select(x => (int?)x.Id).SingleOrDefaultAsync();
    }

    // Nhận bàn gửi món đặt trước xuống bếp và trừ kho dưới tên một nhân viên. Admin chưa có hồ sơ nhân viên được ghi nhận
    // bằng một nhân viên đang làm việc (như màn gọi món/thu ngân và API mobile); không thì món đặt trước bị bỏ sót âm thầm.
    private async Task<int?> CheckInStaffId()
    {
        var staffId = await StaffId();
        if (staffId is null && User.IsInRole(AppRoles.Admin))
            staffId = await Db.NhanVien.Where(x => x.DangLamViec).Select(x => (int?)x.Id).FirstOrDefaultAsync();
        return staffId;
    }

    private static HoaDon? ResolveBookingInvoice(DatBan booking)
    {
        if (booking.TrangThai == TrangThaiDatBan.DaNhanBan && booking.ThoiDiemKetThuc is null)
        {
            var open = booking.HoaDon.Where(x => x.TrangThai == TrangThaiHoaDon.ChuaThanhToan).Take(2).ToList();
            if (open.Count == 1) return open[0];
            if (open.Count > 1) return null;
            return booking.HoaDon.Where(x => x.TrangThai == TrangThaiHoaDon.DaThanhToan)
                .OrderByDescending(x => x.ThoiDiemThanhToan ?? x.ThoiDiemLap).FirstOrDefault();
        }
        return booking.HoaDon.OrderByDescending(x => x.ThoiDiemThanhToan ?? x.ThoiDiemLap).FirstOrDefault();
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? banAnId)
    {
        var model = new ReceptionBookingViewModel { Areas = await Db.KhuVuc.Where(x => x.DangSuDung).ToListAsync() };
        if (banAnId.HasValue)
        {
            var table = await RequestedTable(banAnId.Value);
            if (table is null) return NotFound();
            model.BanAnId = table.Id;
            model.KhuVucId = table.KhuVucId;
            model.RequestedTableName = $"{table.MaBan} · {table.KhuVuc.TenKhuVuc} · {table.SoChoNgoi} chỗ";
        }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReceptionBookingViewModel model)
    {
        model.Areas = await Db.KhuVuc.Where(x => x.DangSuDung).ToListAsync();
        if (model.KhachDenNgay)
        {
            // Khách đã có mặt: giờ đến là lúc này, không lấy giờ còn sót lại trên biểu mẫu.
            var current = TableService.VietnamNow;
            model.GioDen = current.Date.AddHours(current.Hour).AddMinutes(current.Minute);
            model.NgayDat = null;
            ModelState.Remove(nameof(model.GioDen));
        }
        var arrival = TableService.VietnamTime(model.GioDen);
        var until = arrival.AddMinutes(1);
        {
            if (ReservationSchedule.Validate(model.GioDen, TableService.VietnamNow, -15) is { } scheduleError)
                ModelState.AddModelError(nameof(model.GioDen), scheduleError);
            else until = ReservationSchedule.EndOf(model.GioDen);
            if (model.NgayDat.HasValue && model.NgayDat != DateOnly.FromDateTime(model.GioDen))
                ModelState.AddModelError(nameof(model.GioDen), "Ngày và ca chưa khớp. Chọn lại ca đặt bàn.");
        }
        if (arrival < DateTimeOffset.UtcNow.AddMinutes(-15) || arrival > DateTimeOffset.UtcNow.AddDays(180))
            ModelState.AddModelError(nameof(model.GioDen), "Chọn giờ đến từ hiện tại đến tối đa 180 ngày.");
        if (model.KhuVucId.HasValue && !model.Areas.Any(x => x.Id == model.KhuVucId))
            ModelState.AddModelError(nameof(model.KhuVucId), "Khu vực không còn sử dụng.");
        if (!model.KhachDenNgay && string.IsNullOrWhiteSpace(model.HoTen)) ModelState.AddModelError(nameof(model.HoTen), "Nhập họ tên khách.");
        if (!model.KhachDenNgay && string.IsNullOrWhiteSpace(model.SoDienThoai)) ModelState.AddModelError(nameof(model.SoDienThoai), "Nhập số điện thoại để liên hệ khi khách đặt trước.");
        BanAn? chosen = null;
        if (model.BanAnId is not int requestedId) ModelState.AddModelError(nameof(model.BanAnId), "Chọn bàn/phòng khách muốn trên sơ đồ.");
        else if ((chosen = await RequestedTable(requestedId)) is null)
            ModelState.AddModelError(nameof(model.BanAnId), "Bàn đã ngừng sử dụng hoặc không tồn tại. Chọn bàn khác trên sơ đồ.");
        else
        {
            model.KhuVucId = chosen.KhuVucId;
            model.RequestedTableName = $"{chosen.MaBan} · {chosen.KhuVuc.TenKhuVuc} · {chosen.SoChoNgoi} chỗ";
            if (ModelState.IsValid)
            {
                var (availabilityError, map) = await tables.TableAvailability(model.GioDen, model.SoNguoi, chosen.KhuVucId, -15);
                var status = map.SingleOrDefault(x => x.Id == chosen.Id);
                if (availabilityError is not null) ModelState.AddModelError(nameof(model.GioDen), availabilityError);
                else if (status?.TrangThai != "trong")
                    ModelState.AddModelError(nameof(model.BanAnId), $"Bàn {chosen.MaBan}: {status?.GhiChu ?? "không còn trống"}. Chọn bàn khác.");
            }
        }
        if (!ModelState.IsValid) return View(model);
        var phone = string.IsNullOrWhiteSpace(model.SoDienThoai) ? null : model.SoDienThoai.Trim();
        int? customerId = phone is null ? null : await Db.KhachHang.Where(x => x.SoDienThoai == phone).Select(x => (int?)x.Id).SingleOrDefaultAsync();
        var staffId = await StaffId();
        var booking = new DatBan {
            MaDatBan = await TableService.NewBookingCode(Db), HoTenLienHe = string.IsNullOrWhiteSpace(model.HoTen) ? "Khách lẻ" : model.HoTen.Trim(), SoDienThoaiLienHe = phone,
            KhachHangId = customerId, LaKhachTrucTiep = true, ThoiDiemTao = DateTimeOffset.UtcNow,
            GioDen = arrival, GioKetThucDuKien = until, SoNguoiLon = model.SoNguoi,
            KhuVucUuTienId = model.KhuVucId, YeuCau = model.GhiChu?.Trim(),
            YeuCauVip = chosen!.KhuVuc.LaPhongVip,
            // Đặt giữ chỗ không kèm món: tiền đặt cọc theo khu vực của bàn; khách walk-in ăn ngay thì không cọc.
            YeuCauCoc = !model.KhachDenNgay && chosen.KhuVuc.TienCocGiuCho > 0,
            TienCocYeuCau = model.KhachDenNgay ? 0 : chosen.KhuVuc.TienCocGiuCho,
            DieuKienCocDaThoaThuan = model.KhachDenNgay ? null : ReservationDepositPolicy.Terms(chosen.KhuVuc.TienCocGiuCho, false),
            NhanVienTiepNhanId = staffId
        };
        booking.TrangThai = booking.YeuCauCoc ? TrangThaiDatBan.ChoCoc : TrangThaiDatBan.DaXacNhan;
        // Giữ bàn trong giao dịch serializable: hai lễ tân cùng chọn một bàn thì chỉ một người thành công.
        if (await tables.SaveReceptionBooking(booking, chosen.Id) is { } saveError)
        {
            ModelState.AddModelError(nameof(model.BanAnId), saveError);
            return View(model);
        }
        var seatError = !model.KhachDenNgay ? null
            : await tables.CheckIn(booking.Id, Convert.ToBase64String(Db.Entry(booking).Property<byte[]>("RowVersion").CurrentValue ?? []), staffId ?? await CheckInStaffId());
        if (seatError is not null) TempData["ErrorMessage"] = $"Đã tạo lịch {booking.MaDatBan} nhưng chưa nhận bàn được: {seatError}";
        else TempData["SuccessMessage"] = model.KhachDenNgay
            ? $"Đã nhận {chosen.MaBan} cho {booking.HoTenLienHe}. Bàn đang phục vụ."
            : booking.YeuCauCoc
            ? $"Đã giữ {chosen.MaBan} cho {booking.HoTenLienHe}. Chưa đặt cọc — cần cọc {booking.TienCocYeuCau:N0} đ."
            : $"Đã xếp {chosen.MaBan} cho {booking.HoTenLienHe}.";
        var nextTab = model.KhachDenNgay && seatError is null ? "DangPhucVu" : null;
        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            return Json(new { ok = true, redirectUrl = Url.Action(nameof(Index), new { tab = nextTab }) });
        return RedirectToAction(nameof(Index), new { tab = nextTab });
    }

    private Task<BanAn?> RequestedTable(int id) => Db.BanAn.AsNoTracking().Include(x => x.KhuVuc)
        .SingleOrDefaultAsync(x => x.Id == id && x.KhuVuc.DangSuDung && x.TrangThai != TrangThaiBan.NgungSuDung);

    // GET /QuanLyDatBan/DanhGia
    [HttpGet]
    public async Task<IActionResult> DanhGia()
    {
        ViewData["ActiveMenu"] = "QuanLyDanhGia";

        var list = await Db.DanhGia
            .Include(d => d.HoaDon).ThenInclude(h => h.DatBan)
            .Include(d => d.KhachHang)
            .OrderByDescending(d => d.ThoiDiem)
            .ToListAsync();

        var model = new DanhGiaAdminVM
        {
            DanhSachDanhGia = list.Select(d =>
            {
                var parsed = ParseDanhGiaContent(d.NoiDung, d.Diem);
                return new DanhGiaItemVM
                {
                    Id = d.Id,
                    BookingCode = d.HoaDon?.DatBan?.MaDatBan ?? $"HD-{d.HoaDonId}",
                    TenKhachHang = d.KhachHang?.HoTen ?? d.HoaDon?.DatBan?.HoTenLienHe ?? "Khách hàng",
                    ThoiDiem = d.ThoiDiem,
                    Diem = d.Diem,
                    SoSaoMonAn = parsed.SoSaoMonAn,
                    SoSaoDichVu = parsed.SoSaoDichVu,
                    NoiDung = parsed.CleanNoiDung,
                    HinhAnhUrl = parsed.HinhAnhUrl,
                    PhanHoiAdmin = parsed.PhanHoiAdmin
                };
            }).ToList()
        };

        return View(model);
    }

    // POST /QuanLyDatBan/PhanHoiDanhGia
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PhanHoiDanhGia(int danhGiaId, string noiDungPhanHoi)
    {
        if (danhGiaId <= 0 || string.IsNullOrWhiteSpace(noiDungPhanHoi))
        {
            TempData["ErrorMessage"] = "Nội dung phản hồi không hợp lệ.";
            return RedirectToAction(nameof(DanhGia));
        }

        var danhGia = await Db.DanhGia.FindAsync(danhGiaId);
        if (danhGia == null) return NotFound();

        var replyTag = "\n[Phản hồi từ Nhà hàng]: ";
        var currentNoiDung = danhGia.NoiDung ?? "";
        int markerIdx = currentNoiDung.IndexOf("\n[Phản hồi từ Nhà hàng]:", StringComparison.Ordinal);
        var baseContent = markerIdx >= 0 ? currentNoiDung[..markerIdx] : currentNoiDung;

        var fullContent = $"{baseContent}{replyTag}{noiDungPhanHoi.Trim()} ({VnTime.NgayGio(TableService.VietnamNow)})";
        danhGia.NoiDung = fullContent.Length > 2000 ? fullContent[..2000] : fullContent;

        await Db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã gửi câu trả lời phản hồi cho khách hàng thành công.";
        return RedirectToAction(nameof(DanhGia));
    }

    private async Task<List<SelectListItem>> GetBanAnSelectOptionsAsync() =>
        await Db.BanAn.AsNoTracking()
            .Include(x => x.KhuVuc)
            .Where(x => x.TrangThai != TrangThaiBan.NgungSuDung)
            .OrderBy(x => x.KhuVuc.TenKhuVuc).ThenBy(x => x.MaBan)
            .Select(x => new SelectListItem(
                $"{x.MaBan} ({x.KhuVuc.TenKhuVuc} - {x.SoChoNgoi} chỗ) - [{x.TrangThai}]",
                x.Id.ToString()))
            .ToListAsync();

    private static (int SoSaoMonAn, int SoSaoDichVu, string CleanNoiDung, string? HinhAnhUrl, string? PhanHoiAdmin)
        ParseDanhGiaContent(string? raw, int defaultScore)
    {
        if (string.IsNullOrWhiteSpace(raw)) return (defaultScore, defaultScore, "", null, null);

        int monAnScore = defaultScore;
        int dichVuScore = defaultScore;
        string? hinhAnh = null;
        string? phanHoi = null;
        string clean = raw;

        var scoreMatch = Regex.Match(clean, @"\[Món ăn:\s*(\d+)★\s*\|\s*Dịch vụ:\s*(\d+)★\]");
        if (scoreMatch.Success)
        {
            int.TryParse(scoreMatch.Groups[1].Value, out monAnScore);
            int.TryParse(scoreMatch.Groups[2].Value, out dichVuScore);
            clean = clean.Replace(scoreMatch.Value, "").Trim();
        }

        var imgMatch = Regex.Match(clean, @"\[Ảnh:\s*([^\]]+)\]");
        if (imgMatch.Success)
        {
            // Chỉ nhận ảnh do hệ thống lưu; đường dẫn khác (javascript:, trang ngoài) bị bỏ để không thành liên kết độc hại.
            var candidate = imgMatch.Groups[1].Value.Trim();
            if (Regex.IsMatch(candidate, @"\A/uploads/reviews/rev_[0-9a-f]{32}\.(png|jpg)\z")) hinhAnh = candidate;
            clean = clean.Replace(imgMatch.Value, "").Trim();
        }

        var replyMatch = Regex.Match(clean, @"\[Phản hồi từ Nhà hàng\]:\s*([\s\S]+)$");
        if (replyMatch.Success)
        {
            phanHoi = replyMatch.Groups[1].Value.Trim();
            clean = clean.Replace(replyMatch.Value, "").Trim();
        }

        return (monAnScore, dichVuScore, clean, hinhAnh, phanHoi);
    }
}
