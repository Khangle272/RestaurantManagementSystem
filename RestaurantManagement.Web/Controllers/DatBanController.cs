using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Security;
using RestaurantManagement.Web.Services;
using System.Security.Claims;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.KhachHang)]
public class DatBanController(RestaurantDbContext db, IWebHostEnvironment env, PreorderService preorders, OrderService orders, PaymentQrStore qrStore) : Controller
{
    private int AccountId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    private IQueryable<DatBan> OwnedBookings() => db.DatBan.Where(x => x.KhachHang != null && x.KhachHang.TaiKhoanId == AccountId);
    private bool IsAjax => Request.Headers["X-Requested-With"] == "XMLHttpRequest";
    // GET /DatBan
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new DatBanCreateVM
        {
            ThoiGianDen = TableService.VietnamNow.AddHours(2),
            RequestId = Guid.NewGuid(),
            KhuVucOptions = await GetKhuVucOptionsAsync()
        };
        return View(model);
    }

    // POST /DatBan
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(DatBanCreateVM model)
    {
        var khachHang = await db.KhachHang.SingleOrDefaultAsync(x => x.TaiKhoanId == AccountId);
        if (khachHang is null) return Forbid();
        if (model.RequestId is Guid requestId && requestId != Guid.Empty)
        {
            var previous = await FindCreateRequest(khachHang.Id, requestId);
            if (previous is not null) return await ReplayCreate(previous, model);
        }
        else ModelState.AddModelError(nameof(model.RequestId), "Thiếu mã gửi yêu cầu. Hãy mở lại trang đặt bàn.");
        if (model.SoTreEm > model.SoNguoi)
            ModelState.AddModelError(nameof(model.SoTreEm), "Số trẻ em không được lớn hơn tổng số khách.");
        if (!ModelState.IsValid) return await CreateFailure(model);
        if (model.ThoiGianDen < TableService.VietnamNow.AddMinutes(30) || model.ThoiGianDen > TableService.VietnamNow.AddDays(180))
            ModelState.AddModelError(nameof(model.ThoiGianDen), "Vui lòng đặt trước ít nhất 30 phút và trong 180 ngày tới.");
        var ghiChuDayDu = ContactNote(model);
        if ((ghiChuDayDu?.Length ?? 0) > 1000)
            ModelState.AddModelError(nameof(model.GhiChu), "Email và ghi chú gộp lại không quá 1000 ký tự.");
        if (!ModelState.IsValid) return await CreateFailure(model);
        var thoiGianDenUtc = TableService.VietnamTime(model.ThoiGianDen);

        DatBan? created = null, replay = null;
        var committed = false;
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            replay = await FindCreateRequest(khachHang.Id, model.RequestId!.Value);
            if (replay is null)
            {
                var area = model.MaKhuVuc is int areaId
                    ? await db.KhuVuc.SingleOrDefaultAsync(x => x.Id == areaId && x.DangSuDung) : null;
                if (model.MaKhuVuc.HasValue && area is null)
                    ModelState.AddModelError(nameof(model.MaKhuVuc), "Khu vực không còn sử dụng.");
                var (error, lines) = await preorders.PrepareLines(model.Items);
                if (error is not null) ModelState.AddModelError(nameof(model.Items), error);
                if (ModelState.IsValid)
                {
                    created = new DatBan
                    {
                        MaDatBan = await TableService.NewBookingCode(db), HoTenLienHe = model.HoTen.Trim(),
                        SoDienThoaiLienHe = model.SoDienThoai.Trim(), KhachHangId = khachHang.Id,
                        KhuVucUuTienId = model.MaKhuVuc, YeuCauVip = area?.LaPhongVip == true,
                        ThoiDiemTao = DateTimeOffset.UtcNow, GioDen = thoiGianDenUtc,
                        GioKetThucDuKien = thoiGianDenUtc.AddHours(TableService.MaxDiningHours), SoNguoiLon = model.SoNguoi - model.SoTreEm,
                        SoTreEm = model.SoTreEm, YeuCau = ghiChuDayDu, YeuCauTrangTri = model.YeuCauTrangTri,
                        ChuanBiTruoc = model.ChuanBiTruoc,
                        YeuCauTaoId = model.RequestId, MonDatTruoc = lines,
                        TrangThai = TrangThaiDatBan.ChoCoc,
                        TrangThaiCoc = TrangThaiCoc.ChuaCoc, LaKhachTrucTiep = false
                    };
                    ReservationDepositPolicy.Apply(created, lines.Sum(x => x.DonGiaThoaThuan * x.SoLuong));
                    var reservationError = await new TableService(db, preorders).ReserveOnlineWithinTransaction(created);
                    if (reservationError is not null) ModelState.AddModelError("", reservationError);
                    else { db.DatBan.Add(created); await db.SaveChangesAsync(); await tx.CommitAsync(); committed = true; }
                }
            }
        }
        catch (DbUpdateException)
        {
            replay = await FindCreateRequest(khachHang.Id, model.RequestId!.Value);
            if (replay is null) ModelState.AddModelError("", "Không thể lưu yêu cầu lúc này. Giỏ món được giữ để bạn kiểm tra và thử lại.");
        }
        catch (SqlException ex) when (ex.Number is 1205 or 1222)
        {
            replay = await FindCreateRequest(khachHang.Id, model.RequestId!.Value);
            if (replay is null) ModelState.AddModelError("", "Yêu cầu đang được xử lý. Giỏ món được giữ; vui lòng thử lại sau giây lát.");
        }
        if (replay is not null) return await ReplayCreate(replay, model);
        if (!committed || created is null) return await CreateFailure(model);
        TempData["SuccessMessage"] = "Đã giữ chỗ tạm. Thanh toán cọc và chờ thu ngân xác nhận để hoàn tất đặt bàn.";
        return CreateSuccess(created.Id);
    }

    private static string? ContactNote(DatBanCreateVM model) => string.IsNullOrWhiteSpace(model.Email)
        ? model.GhiChu?.Trim()
        : $"Email: {model.Email.Trim()}{(string.IsNullOrWhiteSpace(model.GhiChu) ? "" : " | " + model.GhiChu.Trim())}";

    private Task<DatBan?> FindCreateRequest(int customerId, Guid requestId) => db.DatBan.AsNoTracking()
        .Include(x => x.MonDatTruoc).SingleOrDefaultAsync(x => x.KhachHangId == customerId && x.YeuCauTaoId == requestId);

    private async Task<IActionResult> ReplayCreate(DatBan booking, DatBanCreateVM model)
    {
        var submitted = model.Items.Select(x => (SizeId: (int?)x.MonAnSizeId, Quantity: x.SoLuong, Note: x.YeuCauCheBien?.Trim() ?? ""))
            .OrderBy(x => x.SizeId).ThenBy(x => x.Note, StringComparer.Ordinal).ThenBy(x => x.Quantity);
        var original = booking.MonDatTruoc.Select(x => (SizeId: x.MonAnSizeId, Quantity: x.SoLuong, Note: x.YeuCauCheBien?.Trim() ?? ""))
            .OrderBy(x => x.SizeId).ThenBy(x => x.Note, StringComparer.Ordinal).ThenBy(x => x.Quantity);
        if (booking.HoTenLienHe == (model.HoTen?.Trim() ?? "") && booking.SoDienThoaiLienHe == (model.SoDienThoai?.Trim() ?? "")
            && model.ThoiGianDen >= DateTime.MinValue.AddHours(7) && booking.GioDen == TableService.VietnamTime(model.ThoiGianDen)
            && booking.SoNguoiLon + booking.SoTreEm == model.SoNguoi && booking.SoTreEm == model.SoTreEm
            && booking.KhuVucUuTienId == model.MaKhuVuc && (booking.YeuCau ?? "") == (ContactNote(model) ?? "")
            && booking.ChuanBiTruoc == model.ChuanBiTruoc && booking.YeuCauTrangTri == model.YeuCauTrangTri
            && submitted.SequenceEqual(original)) return CreateSuccess(booking.Id);
        ModelState.AddModelError(nameof(model.RequestId), "Mã gửi này đã tạo một lịch với nội dung khác. Hãy xem lịch đã tạo; nếu cần lịch mới, hãy điều chỉnh biểu mẫu rồi gửi lại.");
        return await CreateFailure(model);
    }

    private IActionResult CreateSuccess(int id) => IsAjax
        ? Json(new { ok = true, redirectUrl = Url.Action(nameof(ThanhToanCoc), new { id }) })
        : RedirectToAction(nameof(ThanhToanCoc), new { id });

    private IActionResult AjaxErrors(int statusCode = 422) => StatusCode(statusCode, new { ok = false,
        errors = ModelState.Where(x => x.Value?.Errors.Count > 0).ToDictionary(x => x.Key,
            x => x.Value!.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Giá trị không hợp lệ." : e.ErrorMessage).ToArray()) });

    private async Task<IActionResult> CreateFailure(DatBanCreateVM model)
    {
        if (IsAjax) return AjaxErrors();
        model.KhuVucOptions = await GetKhuVucOptionsAsync();
        return View(nameof(Index), model);
    }

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> CartContext(int id)
    {
        var booking = await OwnedBookings().SingleOrDefaultAsync(x => x.Id == id);
        if (booking is null) return NotFound();
        return Json(new { bookingId = booking.Id, code = booking.MaDatBan, arrival = booking.GioDen,
            version = preorders.Version(booking), canEdit = PreorderService.CanEdit(booking) && booking.ThoiDiemBaoChuyenKhoan is null,
            requiresDeposit = booking.YeuCauCoc, chuanBiTruoc = booking.ChuanBiTruoc,
            status = booking.TrangThai.ToString(), depositRemaining = ReservationDepositPolicy.Remaining(booking),
            paymentPending = booking.ThoiDiemBaoChuyenKhoan is not null,
            paymentExpired = ReservationDepositPolicy.Expired(booking),
            items = await preorders.Lines(id) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePreorder(int id, SavePreorderModel model)
    {
        if (!await OwnedBookings().AnyAsync(x => x.Id == id)) return NotFound();
        var statusCode = 422;
        if (ModelState.IsValid)
        {
            var error = await preorders.Save(id, AccountId, model);
            if (error is null) return IsAjax ? Json(new { ok = true, redirectUrl = Url.Action(nameof(ChiTiet), new { id }) }) : RedirectToAction(nameof(ChiTiet), new { id });
            if (error.Contains("thay đổi", StringComparison.OrdinalIgnoreCase) || error.StartsWith("Mã gửi giỏ đã được dùng", StringComparison.Ordinal)) statusCode = 409;
            ModelState.AddModelError("", error);
        }
        if (IsAjax) return AjaxErrors(statusCode);
        TempData["ErrorMessage"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> ChiTiet(int id)
    {
        var booking = await OwnedBookings().AsSplitQuery()
            .Include(x => x.Ban).ThenInclude(x => x.BanAn).ThenInclude(x => x.KhuVuc)
            .Include(x => x.KhuVucUuTien).Include(x => x.HoaDon).SingleOrDefaultAsync(x => x.Id == id);
        if (booking is null) return NotFound();
        return View(new BookingDetailViewModel { Booking = booking, Version = preorders.Version(booking),
            Items = await preorders.Lines(id), CanEdit = PreorderService.CanEdit(booking),
            Transactions = await db.GiaoDichCoc.AsNoTracking().Where(x => x.DatBanId == id).OrderByDescending(x => x.ThoiDiem).ToListAsync(),
            AvailableDeposit = await orders.AvailableDepositAsync(id) });
    }

    [HttpGet]
    public async Task<IActionResult> ThanhToanCoc(int id)
    {
        var booking = await OwnedBookings().Include(x => x.Ban).ThenInclude(x => x.BanAn)
            .Include(x => x.MonDatTruoc).SingleOrDefaultAsync(x => x.Id == id);
        if (booking is null) return NotFound();
        return View(new ReservationPaymentViewModel { Booking = booking, Version = preorders.Version(booking),
            Remaining = ReservationDepositPolicy.Remaining(booking),
            FoodTotal = booking.MonDatTruoc.Sum(x => x.DonGiaThoaThuan * x.SoLuong), IsDemo = env.IsDevelopment(),
            QrVersion = qrStore.CurrentVersion });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BatDauThanhToan(int id, string rowVersion)
    {
        var error = await preorders.PreparePayment(id, AccountId, rowVersion);
        if (error is not null) { TempData["ErrorMessage"] = error; return RedirectToAction(nameof(ChiTiet), new { id }); }
        return RedirectToAction(nameof(ThanhToanCoc), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BaoChuyenKhoan(int id, PaymentNoticeModel model)
    {
        if (!await OwnedBookings().AnyAsync(x => x.Id == id)) return NotFound();
        var error = ModelState.IsValid ? await preorders.NotifyPayment(id, AccountId, model) : "Thông tin chuyển khoản không hợp lệ.";
        if (error is not null) { ModelState.AddModelError("", error); if (IsAjax) return AjaxErrors(409); TempData["ErrorMessage"] = error; }
        else TempData["SuccessMessage"] = "Đã gửi thông báo chuyển khoản. Thu ngân sẽ đối chiếu ngân hàng và mã lịch; lịch chưa được chốt khi chưa xác nhận tiền.";
        return error is null && IsAjax ? Json(new { ok = true, redirectUrl = Url.Action(nameof(ThanhToanCoc), new { id }) })
            : RedirectToAction(nameof(ThanhToanCoc), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> YeuCauHuy(int id, string RowVersion, string LyDo)
    {
        if (!await OwnedBookings().AnyAsync(x => x.Id == id)) return NotFound();
        if (ModelState.IsValid)
        {
            var error = await preorders.RequestCancel(id, AccountId, RowVersion, LyDo);
            if (error is null) return IsAjax ? Json(new { ok = true, redirectUrl = Url.Action(nameof(ChiTiet), new { id }) }) : RedirectToAction(nameof(ChiTiet), new { id });
            ModelState.AddModelError("", error);
        }
        if (IsAjax) return AjaxErrors(409);
        TempData["ErrorMessage"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    // GET /DatBan/Success/{id}
    [HttpGet]
    public async Task<IActionResult> Success(int id)
    {
        var datBan = await OwnedBookings()
            .Include(x => x.Ban).ThenInclude(b => b.BanAn).ThenInclude(b => b.KhuVuc)
            .Include(x => x.KhachHang)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (datBan == null) return NotFound();
        return View(datBan);
    }

    // GET /DatBan/LichSu
    [HttpGet]
    public async Task<IActionResult> LichSu(string? soDienThoai, string? bookingCode)
    {
        var model = new DatBanLookupVM
        {
            SoDienThoai = soDienThoai?.Trim(),
            BookingCode = bookingCode?.Trim()
        };

        var query = OwnedBookings()
            .AsSplitQuery()
            .Include(x => x.Ban).ThenInclude(b => b.BanAn)
            .Include(x => x.HoaDon).ThenInclude(h => h.ChiTiet).ThenInclude(c => c.MonAn)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(model.BookingCode))
        {
            query = query.Where(x => x.MaDatBan == model.BookingCode);
        }
        else if (!string.IsNullOrWhiteSpace(model.SoDienThoai))
        {
            query = query.Where(x => x.SoDienThoaiLienHe == model.SoDienThoai);
        }

        var list = await query.OrderByDescending(x => x.GioDen).Take(30).ToListAsync();
        var hoaDonIds = list.SelectMany(x => x.HoaDon).Select(h => h.Id).ToList();
        var reviewedHoaDonIds = await db.DanhGia
            .Where(d => hoaDonIds.Contains(d.HoaDonId) && d.ChiTietHoaDonId == null)
            .Select(d => d.HoaDonId)
            .ToListAsync();

        model.DanhSachDatBan = list.Select(d =>
        {
            var activeInvoice = d.HoaDon.OrderByDescending(h => h.Id).FirstOrDefault();
            var daThanhToan = activeInvoice?.TrangThai == TrangThaiHoaDon.DaThanhToan;
            var isCompleted = d.ThoiDiemKetThuc.HasValue || d.TrangThai == TrangThaiDatBan.DaNhanBan && daThanhToan;

            string trangThaiText;
            string badgeClass;

            if (isCompleted)
            {
                trangThaiText = "Hoàn tất";
                badgeClass = "badge-status-success";
            }
            else if (d.TrangThai == TrangThaiDatBan.DaNhanBan)
            {
                trangThaiText = "Đang phục vụ";
                badgeClass = "badge-status-success";
            }
            else if (d.TrangThai == TrangThaiDatBan.DaXacNhan)
            {
                trangThaiText = "Đã xác nhận";
                badgeClass = "badge-status-info text-white bg-primary";
            }
            else if (d.TrangThai == TrangThaiDatBan.ChoCoc)
            {
                trangThaiText = d.TienCocYeuCau > 0 ? "Chờ nhận cọc" : "Chờ nhà hàng liên hệ về cọc";
                badgeClass = "badge-status-warning";
            }
            else if (d.TrangThai == TrangThaiDatBan.DaHuy)
            {
                trangThaiText = "Đã hủy";
                badgeClass = "badge-status-danger";
            }
            else if (d.TrangThai == TrangThaiDatBan.KhongDen)
            {
                trangThaiText = "Không đến";
                badgeClass = "badge-status-danger";
            }
            else
            {
                trangThaiText = "Chờ xác nhận";
                badgeClass = "badge-status-warning";
            }

            return new DatBanItemVM
            {
                MaDatBan = d.Id,
                CanEditPreorder = !isCompleted && PreorderService.CanEdit(d),
                BookingCode = d.MaDatBan,
                HoTen = d.HoTenLienHe,
                SoDienThoai = d.SoDienThoaiLienHe ?? "",
                ThoiGianDen = d.GioDen,
                SoNguoi = d.SoNguoiLon + d.SoTreEm,
                TenBan = d.Ban.Any() ? string.Join(", ", d.Ban.Select(b => b.BanAn.MaBan)) : "Chưa xếp bàn",
                TrangThai = trangThaiText,
                TrangThaiBadgeClass = badgeClass,
                HoaDonId = activeInvoice?.Id,
                TongTienHoaDon = activeInvoice?.ChiTiet.Sum(c => c.SoLuong * c.DonGia) ?? 0,
                DaThanhToan = daThanhToan,
                DaDanhGia = activeInvoice != null && reviewedHoaDonIds.Contains(activeInvoice.Id),
                GhiChu = d.YeuCau,
                ChiTietMonAn = activeInvoice?.ChiTiet.Select(c => new ChiTietMonAnItemVM
                {
                    TenMon = c.TenMonLucBan,
                    SoLuong = c.SoLuong,
                    DonGia = c.DonGia
                }).ToList() ?? []
            };
        }).ToList();

        return View(model);
    }

    // GET /DatBan/DanhGia/{maDatBan}
    [HttpGet]
    public async Task<IActionResult> DanhGia(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();

        var datBan = await OwnedBookings()
            .Include(x => x.HoaDon)
            .FirstOrDefaultAsync(x => x.MaDatBan == id || x.Id.ToString() == id);

        if (datBan == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy thông tin đơn đặt bàn.";
            return RedirectToAction(nameof(LichSu));
        }

        var hoaDon = datBan.HoaDon.OrderByDescending(h => h.Id).FirstOrDefault();
        var daThanhToan = hoaDon?.TrangThai == TrangThaiHoaDon.DaThanhToan;
        var isCompleted = datBan.TrangThai == TrangThaiDatBan.DaNhanBan && daThanhToan;

        if (!isCompleted || hoaDon == null)
        {
            TempData["ErrorMessage"] = "Chỉ những đơn đặt bàn đã hoàn tất dùng bữa và thanh toán mới có thể gửi đánh giá dịch vụ.";
            return RedirectToAction(nameof(LichSu), new { bookingCode = datBan.MaDatBan });
        }

        var daDanhGia = await db.DanhGia.AnyAsync(d => d.HoaDonId == hoaDon.Id && d.ChiTietHoaDonId == null);
        if (daDanhGia)
        {
            TempData["ErrorMessage"] = "Đơn đặt bàn này đã được gửi đánh giá trước đó. Cảm ơn quý khách!";
            return RedirectToAction(nameof(LichSu), new { bookingCode = datBan.MaDatBan });
        }

        var model = new DanhGiaCreateVM
        {
            MaDatBan = datBan.MaDatBan,
            TenKhachHang = datBan.HoTenLienHe,
            ThoiGianDen = datBan.GioDen
        };

        return View(model);
    }

    // POST /DatBan/DanhGia
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> DanhGia(DanhGiaCreateVM model)
    {
        if (!ModelState.IsValid) return View(model);

        var datBan = await OwnedBookings()
            .Include(x => x.HoaDon)
            .FirstOrDefaultAsync(x => x.MaDatBan == model.MaDatBan || x.Id.ToString() == model.MaDatBan);

        if (datBan == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy thông tin đơn đặt bàn.";
            return RedirectToAction(nameof(LichSu));
        }

        var hoaDon = datBan.HoaDon.OrderByDescending(h => h.Id).FirstOrDefault();
        if (hoaDon == null || hoaDon.TrangThai != TrangThaiHoaDon.DaThanhToan || datBan.TrangThai != TrangThaiDatBan.DaNhanBan)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hóa đơn tương ứng.";
            return RedirectToAction(nameof(LichSu));
        }

        var daDanhGia = await db.DanhGia.AnyAsync(d => d.HoaDonId == hoaDon.Id && d.ChiTietHoaDonId == null);
        if (daDanhGia)
        {
            TempData["ErrorMessage"] = "Đơn đặt bàn này đã được gửi đánh giá trước đó.";
            return RedirectToAction(nameof(LichSu), new { bookingCode = datBan.MaDatBan });
        }

        string? hinhAnhUrl = null;
        if (model.HinhAnhFile != null && model.HinhAnhFile.Length > 0)
        {
            await using var source = model.HinhAnhFile.OpenReadStream();
            var header = new byte[8];
            var read = await source.ReadAsync(header);
            var png = read == 8 && header.AsSpan().SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
            var jpeg = read >= 3 && header[0] == 255 && header[1] == 216 && header[2] == 255;
            if (model.HinhAnhFile.Length > 2 * 1024 * 1024 || (!png && !jpeg))
            { ModelState.AddModelError(nameof(model.HinhAnhFile), "Chỉ nhận ảnh PNG/JPEG tối đa 2 MB."); return View(model); }
            var uploadsFolder = Path.Combine(env.WebRootPath, "uploads", "reviews");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var fileName = $"rev_{Guid.NewGuid():N}" + (png ? ".png" : ".jpg");
            var filePath = Path.Combine(uploadsFolder, fileName);
            await using var stream = new FileStream(filePath, FileMode.Create);
            await stream.WriteAsync(header.AsMemory(0, read));
            await source.CopyToAsync(stream);
            hinhAnhUrl = $"/uploads/reviews/{fileName}";
        }

        var score = Math.Clamp((int)Math.Round((model.SoSaoMonAn + model.SoSaoDichVu) / 2.0), 1, 5);
        var metaTag = $"[Món ăn: {model.SoSaoMonAn}★ | Dịch vụ: {model.SoSaoDichVu}★]";
        var imgTag = string.IsNullOrEmpty(hinhAnhUrl) ? "" : $"\n[Ảnh: {hinhAnhUrl}]";
        var fullNoiDung = $"{model.NoiDung.Trim()}\n{metaTag}{imgTag}";

        var danhGia = new DanhGia
        {
            HoaDonId = hoaDon.Id,
            KhachHangId = datBan.KhachHangId,
            Diem = score,
            NoiDung = fullNoiDung.Length > 2000 ? fullNoiDung[..2000] : fullNoiDung,
            ThoiDiem = DateTimeOffset.UtcNow
        };

        db.DanhGia.Add(danhGia);
        await db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Gửi đánh giá thành công! Cảm ơn quý khách đã đóng góp ý kiến để nhà hàng hoàn thiện chất lượng dịch vụ.";
        return RedirectToAction(nameof(LichSu), new { bookingCode = datBan.MaDatBan });
    }

    private async Task<List<SelectListItem>> GetKhuVucOptionsAsync() =>
        await db.KhuVuc.AsNoTracking()
            .Where(x => x.DangSuDung)
            .OrderBy(x => x.TenKhuVuc)
            .Select(x => new SelectListItem(x.TenKhuVuc + (x.LaPhongVip ? " (Phòng VIP)" : ""), x.Id.ToString()))
            .ToListAsync();
}
