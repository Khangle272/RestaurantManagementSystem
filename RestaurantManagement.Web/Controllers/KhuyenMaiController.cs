using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Security;
using RestaurantManagement.Web.Services;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class KhuyenMaiController(RestaurantDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? search, string? statusFilter, PhamViUuDai? phamViFilter, int page = 1)
    {
        var query = db.KhuyenMai.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.TenChuongTrinh.Contains(search.Trim()) || x.Vouchers.Any(v => v.Ma.Contains(search.Trim())));
        if (phamViFilter.HasValue) query = query.Where(x => x.PhamVi == phamViFilter || x.Vouchers.Any(v => v.PhamVi == phamViFilter));
        var now = DateTimeOffset.UtcNow;
        query = statusFilter switch {
            "DangDienRa" => query.Where(x => x.DangSuDung && x.BatDau <= now && now <= x.KetThuc),
            "SapDienRa" => query.Where(x => x.DangSuDung && x.BatDau > now),
            "DaKetThuc" => query.Where(x => x.KetThuc < now),
            "NgungSuDung" => query.Where(x => !x.DangSuDung), _ => query };
        var count = await query.CountAsync();
        var pages = Math.Max(1, (int)Math.Ceiling(count / 10d)); page = Math.Clamp(page, 1, pages);
        var items = await query.OrderByDescending(x => x.Id).Skip((page - 1) * 10).Take(10)
            .Select(x => new KhuyenMaiItemViewModel {
                Id = x.Id, TenChuongTrinh = x.TenChuongTrinh, BatDau = x.BatDau, KetThuc = x.KetThuc,
                PhamVi = x.PhamVi, KieuGiam = x.KieuGiam, GiaTri = x.GiaTri, MucGiamToiDa = x.MucGiamToiDa,
                GiaTriToiThieu = x.GiaTriToiThieu, CanVoucher = x.CanVoucher, DangSuDung = x.DangSuDung,
                ChoPhepKetHop = x.ChoPhepKetHop, ThuTuApDung = x.ThuTuApDung,
                SoVoucherDaPhatHanh = x.Vouchers.Count,
                SoMonApDung = db.KhuyenMaiMon.Count(m => m.KhuyenMaiId == x.Id),
                SoLuotDaSuDung = db.HoaDon.Count(h => h.Voucher != null && h.Voucher.KhuyenMaiId == x.Id && h.TrangThai != TrangThaiHoaDon.DaHuy)
            }).ToListAsync();
        return View(new KhuyenMaiListViewModel { Search = search, StatusFilter = statusFilter, PhamViFilter = phamViFilter,
            Page = page, TotalItems = count, TotalPages = pages, Items = items });
    }

    public async Task<IActionResult> Create() => View("Form", new KhuyenMaiFormViewModel {
        BatDau = PromotionTime.Today, KetThuc = PromotionTime.Today.AddMonths(1), AvailableDishes = await Dishes(), AvailableCustomers = await Customers() });

    public async Task<IActionResult> Edit(int id)
    {
        var p = await db.KhuyenMai.Include(x => x.Vouchers).ThenInclude(x => x.MonApDung).SingleOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        var parentDishes = await db.KhuyenMaiMon.Where(x => x.KhuyenMaiId == id).Select(x => x.MonAnId).ToListAsync();
        var model = new KhuyenMaiFormViewModel {
            Id = id, RowVersion = Version(p), TenChuongTrinh = p.TenChuongTrinh,
            BatDau = PromotionTime.Local(p.BatDau), KetThuc = PromotionTime.Local(p.KetThuc),
            GioiHanPhatHanh = p.GioiHanPhatHanh, CanVoucher = p.CanVoucher, DangSuDung = p.DangSuDung,
            ChoPhepKetHop = p.ChoPhepKetHop, ThuTuApDung = p.ThuTuApDung, PhamVi = p.PhamVi, KieuGiam = p.KieuGiam,
            GiaTri = p.GiaTri, MucGiamToiDa = p.MucGiamToiDa, GiaTriToiThieu = p.GiaTriToiThieu,
            SelectedMonAnIds = parentDishes, AvailableDishes = await Dishes(), AvailableCustomers = await Customers(),
            Vouchers = p.Vouchers.OrderBy(x => x.Id).Select(v => new CampaignVoucherForm {
                Id = v.Id, RowVersion = Version(v), Ma = v.Ma, BatDau = PromotionTime.Local(v.BatDau ?? p.BatDau),
                KetThuc = PromotionTime.Local(v.KetThuc ?? p.KetThuc), PhamVi = v.PhamVi ?? p.PhamVi,
                KieuGiam = v.KieuGiam ?? p.KieuGiam, GiaTri = v.GiaTri ?? p.GiaTri,
                MucGiamToiDa = v.GiaTri.HasValue ? v.MucGiamToiDa : p.MucGiamToiDa,
                GiaTriToiThieu = v.GiaTri.HasValue ? v.GiaTriToiThieu : p.GiaTriToiThieu,
                GioiHanTongLuot = v.GioiHanTongLuot, GioiHanPhatHanh = v.GioiHanPhatHanh,
                KhachHangId = v.KhachHangId, DangSuDung = v.DangSuDung,
                SelectedMonAnIds = v.GiaTri.HasValue ? v.MonApDung.Select(m => m.MonAnId).ToList() : parentDishes
            }).ToList()
        };
        model.SoVeDaPhatHanh = await VoucherInventory.ProgramIssuedAsync(db, id);
        foreach (var row in model.Vouchers) {
            row.SoVeDaPhatHanh = await VoucherInventory.IssuedAsync(db, row.Id);
            row.SoLuotDaDung = await db.HoaDon.CountAsync(x => x.VoucherId == row.Id && x.TrangThai != TrangThaiHoaDon.DaHuy);
        }
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(KhuyenMaiFormViewModel model) => Save(model, 0);
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(int id, KhuyenMaiFormViewModel model) => id != model.Id
        ? Task.FromResult<IActionResult>(NotFound()) : Save(model, id);

    private async Task<IActionResult> Save(KhuyenMaiFormViewModel model, int id)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var p = id == 0 ? new KhuyenMai() : await db.KhuyenMai
                .FromSqlInterpolated($"SELECT * FROM [KhuyenMai] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id}").SingleOrDefaultAsync();
            if (p is null) return NotFound();
            var children = id == 0 ? new List<Voucher>() : await db.Voucher.Include(x => x.MonApDung).Where(x => x.KhuyenMaiId == id).ToListAsync();
            model.Vouchers ??= []; model.SelectedMonAnIds ??= [];
            model.TenChuongTrinh = model.TenChuongTrinh?.Trim() ?? "";
            if (id != 0 && !Matches(p, model.RowVersion)) Error("", "Chương trình đã thay đổi. Vui lòng tải lại trang.");
            if (model.BatDau.Year < 2000 || model.KetThuc.Year > 2100 || model.KetThuc <= model.BatDau)
                Error("KetThuc", "Thời gian kết thúc phải sau bắt đầu, trong khoảng năm 2000–2100.");
            if (await db.KhuyenMai.AnyAsync(x => x.Id != id && x.TenChuongTrinh.ToUpper() == model.TenChuongTrinh.ToUpper())) Error("TenChuongTrinh", "Tên chương trình đã tồn tại.");
            if (model.ThuTuApDung < 0) Error("ThuTuApDung", "Thứ tự phải không âm.");
            if (!model.CanVoucher && (children.Count > 0 || model.Vouchers.Any(x => !x.Xoa))) Error("CanVoucher", "Chương trình có voucher phải giữ chế độ voucher.");
            if (!model.CanVoucher) {
                await ValidateDiscount(model.PhamVi, model.KieuGiam, model.GiaTri, model.SelectedMonAnIds, "");
                if (model.MucGiamToiDa <= 0) Error("MucGiamToiDa", "Trần giảm phải lớn hơn 0 hoặc để trống.");
            }
            var issued = id == 0 ? 0 : await VoucherInventory.ProgramIssuedAsync(db, id);
            if (model.GioiHanPhatHanh < issued) Error("GioiHanPhatHanh", $"Đã phát hành {issued} vé, không thể giảm giới hạn thấp hơn.");
            if (model.Vouchers.Count > 100) Error("", "Mỗi lần lưu tối đa 100 mã.");
            var seenIds = new HashSet<int>(); var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < model.Vouchers.Count; i++)
            {
                var v = model.Vouchers[i]; var prefix = $"Vouchers[{i}].";
                v.SelectedMonAnIds ??= []; v.Ma = v.Ma?.Trim().ToUpperInvariant() ?? "";
                var old = children.SingleOrDefault(x => x.Id == v.Id);
                if (v.Id != 0 && (old is null || !seenIds.Add(v.Id))) Error(prefix + "Ma", "Voucher không thuộc chương trình hoặc bị lặp.");
                if (old is not null && !Matches(old, v.RowVersion)) Error(prefix + "Ma", "Voucher đã thay đổi. Vui lòng tải lại.");
                if (v.Xoa) {
                    foreach (var key in ModelState.Keys.Where(k => k.StartsWith(prefix)).ToList()) ModelState.Remove(key);
                    // Deletion still requires the original token and ownership.
                    if (v.Id != 0 && (old is null || !Matches(old, v.RowVersion) || model.Vouchers.Count(x => x.Id == v.Id) > 1)) Error("", "Không thể xóa voucher bị lặp, đã thay đổi hoặc thuộc chương trình khác.");
                    continue;
                }
                if (!seenCodes.Add(v.Ma) || await db.Voucher.AnyAsync(x => x.Id != v.Id && x.Ma.ToUpper() == v.Ma)) Error(prefix + "Ma", "Mã voucher đã tồn tại hoặc bị lặp.");
                if (v.BatDau < model.BatDau || v.KetThuc > model.KetThuc || v.KetThuc < v.BatDau) Error(prefix + "KetThuc", "Thời hạn voucher phải nằm trong thời hạn chương trình.");
                await ValidateDiscount(v.PhamVi, v.KieuGiam, v.GiaTri, v.SelectedMonAnIds, prefix);
                if (v.KhachHangId.HasValue && !await db.KhachHang.AnyAsync(x => x.Id == v.KhachHangId)) Error(prefix + "KhachHangId", "Khách hàng không tồn tại.");
                if (old is not null) {
                    var uses = await db.HoaDon.CountAsync(x => x.VoucherId == old.Id && x.TrangThai != TrangThaiHoaDon.DaHuy);
                    var saved = await VoucherInventory.IssuedAsync(db, old.Id);
                    if (v.GioiHanTongLuot < uses || v.GioiHanPhatHanh < saved) Error(prefix + "Ma", "Giới hạn phải đủ cho số vé đã phát hành và lượt đang dùng.");
                    if ((saved > 0 || await db.HoaDon.AnyAsync(x => x.VoucherId == old.Id)) && (old.Ma != v.Ma || old.KhachHangId != v.KhachHangId)) Error(prefix + "Ma", "Mã đã phát hành/sử dụng phải giữ nguyên mã và khách nhận.");
                }
            }
            // Do not silently invalidate issued child periods when shortening a campaign.
            var submitted = model.Vouchers.Select(x => x.Id).ToHashSet();
            if (model.BatDau.Year >= 2000 && model.KetThuc.Year <= 2100 && children.Any(v => !submitted.Contains(v.Id) && ((v.BatDau ?? p.BatDau) < PromotionTime.FromLocal(model.BatDau) || (v.KetThuc ?? p.KetThuc) > PromotionTime.FromLocal(model.KetThuc))))
                Error("KetThuc", "Cần cập nhật thời hạn các voucher con trước khi thu hẹp chương trình.");
            if (!ModelState.IsValid) { model.AvailableDishes = await Dishes(); model.AvailableCustomers = await Customers(); return View("Form", model); }
            p.TenChuongTrinh = model.TenChuongTrinh; p.BatDau = PromotionTime.FromLocal(model.BatDau); p.KetThuc = PromotionTime.FromLocal(model.KetThuc);
            p.GioiHanPhatHanh = model.GioiHanPhatHanh; p.CanVoucher = model.CanVoucher; p.DangSuDung = model.DangSuDung;
            p.ChoPhepKetHop = model.ChoPhepKetHop; p.ThuTuApDung = model.ThuTuApDung;
            if (!p.CanVoucher) { p.PhamVi = model.PhamVi; p.KieuGiam = model.KieuGiam; p.GiaTri = model.GiaTri;
                p.MucGiamToiDa = model.MucGiamToiDa; p.GiaTriToiThieu = model.GiaTriToiThieu; }
            else if (id == 0) p.GiaTri = 10; // Legacy automatic-discount columns remain valid for old integrations.
            if (id == 0) db.KhuyenMai.Add(p);
            else db.Entry(p).Property(x => x.DangSuDung).IsModified = true;
            var links = await db.KhuyenMaiMon.Where(x => x.KhuyenMaiId == id).ToListAsync();
            db.KhuyenMaiMon.RemoveRange(links.Where(x => p.CanVoucher || !model.SelectedMonAnIds.Contains(x.MonAnId)));
            if (!p.CanVoucher && p.PhamVi == PhamViUuDai.MonAn)
                foreach (var dish in model.SelectedMonAnIds.Distinct().Except(links.Select(x => x.MonAnId))) db.KhuyenMaiMon.Add(new KhuyenMaiMon { KhuyenMai = p, MonAnId = dish });
            foreach (var row in model.Vouchers) {
                var v = children.SingleOrDefault(x => x.Id == row.Id);
                if (row.Xoa) {
                    if (v is null) continue;
                    if (await db.HoaDon.AnyAsync(x => x.VoucherId == v.Id) || await db.VoucherKhachHang.AnyAsync(x => x.VoucherId == v.Id)) v.DangSuDung = false;
                    else { db.VoucherMon.RemoveRange(v.MonApDung); db.Voucher.Remove(v); }
                    continue;
                }
                if (v is null) { v = new Voucher { KhuyenMai = p }; db.Voucher.Add(v); }
                v.Ma = row.Ma; v.BatDau = PromotionTime.FromLocal(row.BatDau); v.KetThuc = PromotionTime.FromLocal(row.KetThuc);
                v.PhamVi = row.PhamVi; v.KieuGiam = row.KieuGiam; v.GiaTri = row.GiaTri;
                v.MucGiamToiDa = row.MucGiamToiDa; v.GiaTriToiThieu = row.GiaTriToiThieu;
                v.GioiHanTongLuot = row.GioiHanTongLuot; v.GioiHanPhatHanh = row.GioiHanPhatHanh;
                v.KhachHangId = row.KhachHangId; v.DangSuDung = row.DangSuDung;
                var wanted = row.PhamVi == PhamViUuDai.MonAn ? row.SelectedMonAnIds.Distinct().ToHashSet() : [];
                db.VoucherMon.RemoveRange(v.MonApDung.Where(m => !wanted.Contains(m.MonAnId)));
                foreach (var dish in wanted.Except(v.MonApDung.Select(m => m.MonAnId)).ToList()) v.MonApDung.Add(new VoucherMon { MonAnId = dish });
            }
            await db.SaveChangesAsync(); await tx.CommitAsync();
            TempData["Success"] = "Đã lưu chương trình và toàn bộ mã voucher. Mã đã có lịch sử sẽ được ngừng sử dụng khi xóa.";
            return RedirectToAction(nameof(Edit), new { id = p.Id });
        }
        catch (DbUpdateConcurrencyException) { Error("", "Dữ liệu đã thay đổi. Vui lòng tải lại trang."); }
        catch (DbUpdateException e) when (e.GetBaseException() is SqlException { Number: 2601 or 2627 }) { Error("", "Mã đã được tạo bởi thao tác khác. Vui lòng tải lại."); }
        catch (Exception e) when (e is SqlException { Number: 1205 or 1222 } || e is DbUpdateException && e.GetBaseException() is SqlException { Number: 1205 or 1222 }) { Error("", "Chương trình đang được xử lý. Vui lòng tải lại và thử lại."); }
        await tx.DisposeAsync();
        model.AvailableDishes = await Dishes(); model.AvailableCustomers = await Customers(); return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id) {
        var p = await db.KhuyenMai.FindAsync(id); if (p is null) return NotFound();
        p.DangSuDung = !p.DangSuDung; await db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id) {
        // Archive the aggregate so collected vouchers and historical invoices remain addressable.
        var p = await db.KhuyenMai.FindAsync(id); if (p is null) return NotFound();
        p.DangSuDung = false; await db.SaveChangesAsync(); TempData["Success"] = "Đã ngừng chương trình, giữ lịch sử voucher và hóa đơn.";
        return RedirectToAction(nameof(Index));
    }
    private async Task ValidateDiscount(PhamViUuDai scope, KieuGiam type, decimal value, List<int> dishes, string prefix) {
        if (!Enum.IsDefined(scope) || !Enum.IsDefined(type)) Error(prefix + "GiaTri", "Phạm vi/kiểu giảm không hợp lệ.");
        if (value <= 0 || (type == KieuGiam.PhanTram && value > 100)) Error(prefix + "GiaTri", "Giảm phần trăm phải trên 0 đến 100%; số tiền phải lớn hơn 0.");
        if (scope == PhamViUuDai.MonAn && (dishes.Count == 0 || await db.MonAn.CountAsync(x => dishes.Contains(x.Id)) != dishes.Distinct().Count())) Error(prefix + "SelectedMonAnIds", "Chọn ít nhất một món hợp lệ.");
    }
    private void Error(string key, string message) => ModelState.AddModelError(key, message);
    private string Version<T>(T row) where T : class => Convert.ToBase64String(db.Entry(row).Property<byte[]>("RowVersion").CurrentValue ?? []);
    private bool Matches<T>(T row, string? token) where T : class {
        var bytes = new byte[8]; return token is not null && Convert.TryFromBase64String(token, bytes, out var count) && count == 8 && bytes.SequenceEqual(db.Entry(row).Property<byte[]>("RowVersion").CurrentValue ?? []);
    }
    private Task<List<MonAnSelectionItem>> Dishes() => db.MonAn.AsNoTracking().OrderBy(x => x.TenMon)
        .Select(x => new MonAnSelectionItem { Id = x.Id, TenMon = x.TenMon }).ToListAsync();
    private Task<List<CustomerOption>> Customers() => db.KhachHang.AsNoTracking().OrderBy(x => x.HoTen)
        .Select(x => new CustomerOption(x.Id, x.HoTen, x.SoDienThoai)).ToListAsync();
}
