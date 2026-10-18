using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Security;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = AppRoles.Admin + "," + AppRoles.Bep)]
public class MonAnController(RestaurantDbContext context, IWebHostEnvironment environment) : ManagementControllerBase(context)
{
    private bool IsAdmin => User.IsInRole(AppRoles.Admin);
    private const string SizeMacDinh = "Vừa (M)";
    private static readonly string[] SizeGoiY = ["Nhỏ (S)", SizeMacDinh, "Lớn (L)", "Mặc định"];

    // Danh mục không có nhóm chuẩn nên suy đoán thức uống theo tên danh mục.
    public static bool LaDanhMucThucUong(string? tenDanhMuc)
    {
        if (string.IsNullOrWhiteSpace(tenDanhMuc)) return false;
        var ten = tenDanhMuc.ToLowerInvariant();
        return new[] { "uống", "nước", "trà", "cà phê", "cafe", "sinh tố", "bia", "rượu", "cocktail" }.Any(x => ten.Contains(x));
    }

    private static decimal ChuanHoaTyLe(decimal heSoTyLe) => heSoTyLe >= 0.0001m && heSoTyLe <= 9999m ? heSoTyLe : 1m;

    private static decimal TinhDinhLuong(decimal soLuongGoc, decimal heSoTyLe) =>
        Math.Round(soLuongGoc * heSoTyLe, 4, MidpointRounding.AwayFromZero);

    // Hệ số lưu trong DB có thể lệch với định lượng thực tế của dữ liệu cũ; ưu tiên số liệu đang có.
    private static decimal TyLeTuDuLieu(IEnumerable<DinhMucMon> recipes, int baseSizeId, int sizeId, decimal heSoLuu)
    {
        if (sizeId == baseSizeId) return 1m;
        var list = recipes as IList<DinhMucMon> ?? recipes.ToList();
        foreach (var goc in list.Where(x => x.MaKichCo == baseSizeId && !x.LaNguyenLieuPhu && x.SoLuong > 0))
        {
            var khac = list.FirstOrDefault(x => x.MaKichCo == sizeId && x.NguyenLieuId == goc.NguyenLieuId && !x.LaNguyenLieuPhu && x.SoLuong > 0);
            if (khac != null) return ChuanHoaTyLe(Math.Round(khac.SoLuong / goc.SoLuong, 4, MidpointRounding.AwayFromZero));
        }
        return ChuanHoaTyLe(heSoLuu);
    }

    public async Task<IActionResult> Index(string? search, int? danhMucId, string? trangThai, int page = 1)
    {
        var admin = IsAdmin;
        var all = Db.MonAn.AsNoTracking();
        if (!admin) all = all.Where(x => x.Loai != LoaiMon.Set);
        var query = all;
        search = search?.Trim();
        if (!string.IsNullOrEmpty(search)) query = query.Where(x => x.TenMon.Contains(search));
        if (danhMucId > 0) query = query.Where(x => x.DanhMucId == danhMucId);
        if (trangThai == "ChoDuyet") query = query.Where(x => !x.DaDuyet);
        else if (Enum.TryParse<TrangThaiMon>(trangThai, out var status) && Enum.IsDefined(status))
            query = query.Where(x => x.TrangThai == status);
        var count = await query.CountAsync();
        var pages = Math.Max(1, (int)Math.Ceiling(count / 6d));
        page = Math.Clamp(page, 1, pages);
        return View(new MonAnIndexViewModel
        {
            Items = await query.OrderByDescending(x => x.Id).Skip((page - 1) * 6).Take(6)
                .Select(x => new MonAnCardViewModel
                {
                    Id = x.Id, TenMon = x.TenMon, MoTa = x.MoTa, HinhAnh = x.HinhAnh,
                    TenDanhMuc = x.DanhMuc.TenDanhMuc, TrangThai = x.TrangThai, DaDuyet = x.DaDuyet,
                    GiaBan = admin ? x.Sizes.Where(s => s.DangSuDung).Min(s => (decimal?)s.GiaBan) : null,
                    SoSize = x.Sizes.Count(s => s.DangSuDung)
                }).ToListAsync(),
            DanhMucList = await Db.DanhMuc.AsNoTracking().OrderBy(x => x.TenDanhMuc).ToListAsync(),
            Search = search, DanhMucId = danhMucId, TrangThai = trangThai,
            CurrentPage = page, TotalPages = pages, TotalItems = count,
            TotalCount = await all.CountAsync(), ChoDuyetCount = await all.CountAsync(x => !x.DaDuyet),
            DangPhucVuCount = await all.CountAsync(x => x.DaDuyet && x.TrangThai == TrangThaiMon.DangPhucVu),
            TamHetCount = await all.CountAsync(x => x.DaDuyet && x.TrangThai == TrangThaiMon.TamHet),
            NgungKinhDoanhCount = await all.CountAsync(x => x.DaDuyet && x.TrangThai == TrangThaiMon.NgungKinhDoanh)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new MonAnFormViewModel { Sizes = new() { new() { TenSize = SizeMacDinh } } };
        await LoadOptionsAsync(model);
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(MonAnFormViewModel model) => SaveFormAsync(model, null);

    [Authorize(Roles = AppRoles.Admin), HttpGet]
    public async Task<IActionResult> CreateCombo()
    {
        var model = new MonAnFormViewModel { Sizes = new() { new() { TenSize = SizeMacDinh } }, Loai = LoaiMon.Set };
        await LoadOptionsAsync(model);
        return View("Form", model);
    }

    [Authorize(Roles = AppRoles.Admin), HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> CreateCombo(MonAnFormViewModel model) => SaveFormAsync(model, null, combo: true);

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await Db.MonAn.Include(x => x.Sizes).SingleOrDefaultAsync(x => x.Id == id);
        if (entity == null) return NotFound();
        if (!IsAdmin && entity.Loai == LoaiMon.Set) return Forbid();
        var existingRecipes = await Db.DinhMucMon.Where(x => x.MonAnId == id).ToListAsync();
        var baseSizeId = entity.Sizes.OrderBy(x => x.Id).Select(x => x.Id).FirstOrDefault();
        var model = new MonAnFormViewModel
        {
            Id = id, RowVersion = VersionOf(entity), TenMon = entity.TenMon, DanhMucId = entity.DanhMucId,
            MoTa = entity.MoTa, HinhAnh = entity.HinhAnh, Loai = entity.Loai, TrangThai = entity.TrangThai,
            LaMonMoi = entity.LaMonMoi, LaMonNoiBat = entity.LaMonNoiBat, DaDuyet = entity.DaDuyet,
            LyDoNgung = entity.LyDoNgung,
            Sizes = entity.Sizes.OrderBy(x => x.Id).Select(x => new MonAnSizeFormViewModel
            {
                Id = x.Id, TenSize = x.TenSize, GiaBan = IsAdmin ? x.GiaBan : 0, DangSuDung = x.DangSuDung,
                HeSoTyLe = TyLeTuDuLieu(existingRecipes, baseSizeId, x.Id, x.HeSoTyLe),
                GhiChuThanhPhanKhacBiet = x.GhiChuThanhPhanKhacBiet,
                DinhMucItems = existingRecipes.Where(r => r.MaKichCo == x.Id && r.LaNguyenLieuPhu).OrderBy(r => r.NguyenLieuId)
                    .Select(r => new DinhMucItemViewModel { NguyenLieuId = r.NguyenLieuId, SoLuong = r.SoLuong }).ToList()
            }).ToList(),
            DinhMucItems = existingRecipes.Where(r => r.MaKichCo == baseSizeId && !r.LaNguyenLieuPhu).OrderBy(x => x.NguyenLieuId)
                .Select(x => new DinhMucItemViewModel { NguyenLieuId = x.NguyenLieuId, SoLuong = x.SoLuong }).ToList(),
            ComboItems = await Db.ChiTietCombo.Where(x => x.ComboId == id).OrderBy(x => x.MonAnId)
                .Select(x => new ComboItemViewModel { MonAnId = x.MonAnId, SoLuong = x.SoLuong }).ToListAsync()
        };
        await LoadOptionsAsync(model, entity.DanhMucId);
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MonAnFormViewModel model)
    {
        if (id != model.Id) return NotFound();
        var entity = await Db.MonAn.Include(x => x.Sizes).SingleOrDefaultAsync(x => x.Id == id);
        if (entity == null) return NotFound();
        if (!IsAdmin && entity.Loai == LoaiMon.Set) return Forbid();
        return await SaveFormAsync(model, entity);
    }

    private async Task<IActionResult> SaveFormAsync(MonAnFormViewModel model, MonAn? entity, bool combo = false)
    {
        var creating = entity == null;
        model.Sizes ??= new(); model.DinhMucItems ??= new(); model.ComboItems ??= new();
        if (!creating)
        {
            // Set/Combo được tạo ở luồng riêng; món thường không thể tự chuyển thành Set qua POST sửa.
            if (entity!.Loai == LoaiMon.Set) { model.Loai = LoaiMon.Set; ModelState.Remove(nameof(model.Loai)); }
            else if (model.Loai == LoaiMon.Set) return Forbid();
        }
        if (!IsAdmin)
        {
            if (model.Loai == LoaiMon.Set || model.ComboItems.Count > 0) return Forbid();
            // Business fields are server-owned; never trust prices/approval posted by kitchen users.
            model.DaDuyet = false;
            model.TrangThai = entity?.TrangThai ?? TrangThaiMon.DangPhucVu;
            model.LyDoNgung = entity?.LyDoNgung;
            model.LaMonMoi = entity?.LaMonMoi ?? true;
            model.LaMonNoiBat = entity?.LaMonNoiBat ?? false;
            foreach (var field in new[] { nameof(model.DaDuyet), nameof(model.TrangThai), nameof(model.LyDoNgung), nameof(model.LaMonMoi), nameof(model.LaMonNoiBat) })
                ModelState.Remove(field);
            for (var i = 0; i < model.Sizes.Count; i++)
            {
                var existingSize = entity?.Sizes.SingleOrDefault(x => x.Id == model.Sizes[i].Id);
                model.Sizes[i].GiaBan = existingSize?.GiaBan ?? 0;
                model.Sizes[i].DangSuDung = existingSize?.DangSuDung ?? true;
                ModelState.Remove($"Sizes[{i}].GiaBan");
                ModelState.Remove($"Sizes[{i}].DangSuDung");
            }
        }
        if (creating)
        {
            // Món tạo mới luôn là món lẻ/thức uống đang phục vụ, món mới, chờ Quản lý duyệt giá.
            model.TrangThai = TrangThaiMon.DangPhucVu;
            model.LyDoNgung = null;
            model.LaMonMoi = true;
            model.LaMonNoiBat = false;
            if (IsAdmin) model.DaDuyet = true;
            if (combo) model.Loai = LoaiMon.Set;
            else
            {
                var tenDanhMuc = await Db.DanhMuc.Where(x => x.Id == model.DanhMucId).Select(x => x.TenDanhMuc).FirstOrDefaultAsync();
                model.Loai = LaDanhMucThucUong(tenDanhMuc) ? LoaiMon.ThucUong : LoaiMon.MonLe;
            }
            ModelState.Remove(nameof(model.Loai));
            ModelState.Remove(nameof(model.LyDoNgung));
        }
        var currentCategory = entity?.DanhMucId;
        model.HinhAnh = entity?.HinhAnh;
        var imageExtension = await ValidateFormAsync(model, entity);
        if (ModelState.IsValid && (creating || ApplyVersion(entity!, model.RowVersion)))
        {
            await using var transaction = await Db.Database.BeginTransactionAsync();
            string? createdImage = null;
            var committed = false;
            try
            {
                if (model.HinhAnhFile != null)
                {
                    var folder = Path.Combine(environment.WebRootPath, "uploads", "monan");
                    Directory.CreateDirectory(folder);
                    var name = Guid.NewGuid().ToString("N") + imageExtension;
                    createdImage = Path.Combine(folder, name);
                    await using (var stream = System.IO.File.Create(createdImage))
                        await model.HinhAnhFile.CopyToAsync(stream);
                    model.HinhAnh = "/uploads/monan/" + name;
                }
                else if (model.XoaHinhAnh) model.HinhAnh = null;

                entity ??= new MonAn();
                entity.TenMon = model.TenMon.Trim(); entity.DanhMucId = model.DanhMucId;
                entity.MoTa = model.MoTa?.Trim(); entity.HinhAnh = model.HinhAnh;
                entity.Loai = model.Loai; entity.TrangThai = model.TrangThai;
                entity.LaMonMoi = model.LaMonMoi; entity.LaMonNoiBat = model.LaMonNoiBat;
                entity.DaDuyet = model.DaDuyet; entity.LyDoNgung = model.LyDoNgung?.Trim();
                if (creating) Db.Add(entity);
                else
                {
                    // Touch the aggregate even when only sizes/recipe/combo changed.
                    Db.Entry(entity).Property(x => x.TenMon).IsModified = true;
                    var removedSizes = entity.Sizes.Where(x => !model.Sizes.Any(s => s.Id == x.Id)).ToList();
                    Db.MonAnSize.RemoveRange(removedSizes);
                }
                var sizeMap = new Dictionary<MonAnSizeFormViewModel, MonAnSize>();
                for (var k = 0; k < model.Sizes.Count; k++)
                {
                    var size = model.Sizes[k];
                    var target = size.Id == 0 ? new MonAnSize { MonAn = entity } : entity.Sizes.Single(x => x.Id == size.Id);
                    target.TenSize = size.TenSize.Trim(); target.GiaBan = size.GiaBan; target.DangSuDung = creating || size.DangSuDung;
                    target.HeSoTyLe = k == 0 ? 1m : ChuanHoaTyLe(size.HeSoTyLe);
                    target.GhiChuThanhPhanKhacBiet = string.IsNullOrWhiteSpace(size.GhiChuThanhPhanKhacBiet) ? null : size.GhiChuThanhPhanKhacBiet.Trim();
                    if (size.Id == 0) Db.MonAnSize.Add(target);
                    sizeMap[size] = target;
                }
                if (await SaveAsync("", "Tên size bị trùng. Vui lòng kiểm tra lại."))
                {
                    var baseSizeId = model.Sizes.Count > 0 && sizeMap.TryGetValue(model.Sizes[0], out var baseTarget) && baseTarget.Id > 0
                        ? baseTarget.Id : 0;
                    var baseItems = model.DinhMucItems.Where(x => x.NguyenLieuId > 0 && x.SoLuong > 0)
                        .GroupBy(x => x.NguyenLieuId).Select(g => g.First()).ToList();
                    var recipes = await Db.DinhMucMon.Where(x => x.MonAnId == entity.Id).ToListAsync();
                    Db.DinhMucMon.RemoveRange(recipes);

                    foreach (var size in model.Sizes)
                    {
                        var targetSizeId = sizeMap.TryGetValue(size, out var targetEntity) && targetEntity.Id > 0
                            ? targetEntity.Id
                            : baseSizeId;
                        if (targetSizeId == 0) continue;

                        var laSizeGoc = targetSizeId == baseSizeId;
                        var heSoTyLe = laSizeGoc ? 1m : ChuanHoaTyLe(size.HeSoTyLe);
                        foreach (var item in baseItems)
                        {
                            var soLuong = laSizeGoc ? item.SoLuong : TinhDinhLuong(item.SoLuong, heSoTyLe);
                            if (soLuong <= 0) continue;
                            if (!Db.DinhMucMon.Local.Any(x => x.MonAnId == entity.Id && x.MaKichCo == targetSizeId && x.NguyenLieuId == item.NguyenLieuId))
                            {
                                Db.DinhMucMon.Add(new DinhMucMon
                                {
                                    MonAnId = entity.Id,
                                    MaKichCo = targetSizeId,
                                    NguyenLieuId = item.NguyenLieuId,
                                    SoLuong = soLuong
                                });
                            }
                        }

                        if (size.DinhMucItems != null)
                        {
                            foreach (var item in size.DinhMucItems)
                            {
                                if (item.NguyenLieuId <= 0 || item.SoLuong <= 0) continue;
                                if (baseItems.Any(x => x.NguyenLieuId == item.NguyenLieuId)) continue;
                                if (!Db.DinhMucMon.Local.Any(x => x.MonAnId == entity.Id && x.MaKichCo == targetSizeId && x.NguyenLieuId == item.NguyenLieuId))
                                {
                                    Db.DinhMucMon.Add(new DinhMucMon
                                    {
                                        MonAnId = entity.Id,
                                        MaKichCo = targetSizeId,
                                        NguyenLieuId = item.NguyenLieuId,
                                        SoLuong = item.SoLuong,
                                        LaNguyenLieuPhu = true
                                    });
                                }
                            }
                        }
                    }

                    var components = await Db.ChiTietCombo.Where(x => x.ComboId == entity.Id).ToListAsync();
                    Db.ChiTietCombo.RemoveRange(components.Where(x => !model.ComboItems.Any(i => i.MonAnId == x.MonAnId)));
                    foreach (var item in model.ComboItems)
                    {
                        var target = components.SingleOrDefault(x => x.MonAnId == item.MonAnId);
                        if (target == null) Db.ChiTietCombo.Add(new ChiTietCombo { ComboId = entity.Id, MonAnId = item.MonAnId, SoLuong = item.SoLuong });
                        else target.SoLuong = item.SoLuong;
                    }
                    if (await SaveAsync("", "Định mức hoặc thành phần combo bị trùng."))
                    {
                        await transaction.CommitAsync();
                        committed = true;
                    TempData["Success"] = !IsAdmin ? "Đã lưu thông tin kỹ thuật. Món chờ Quản lý duyệt trước khi mở bán."
                        : creating ? "Thêm món ăn thành công." : "Cập nhật món ăn thành công.";
                        return RedirectToAction(nameof(Index));
                    }
                }
            }
            catch (IOException)
            {
                ModelState.AddModelError(nameof(model.HinhAnhFile), "Không thể lưu hình ảnh. Vui lòng thử lại.");
            }
            finally
            {
                if (!committed)
                {
                    await transaction.RollbackAsync();
                    if (createdImage != null && System.IO.File.Exists(createdImage)) System.IO.File.Delete(createdImage);
                }
            }
        }
        await LoadOptionsAsync(model, currentCategory);
        // The browser cannot restore a file input after validation failure.
        if (model.HinhAnhFile != null) ModelState.AddModelError(nameof(model.HinhAnhFile), "Vui lòng chọn lại ảnh trước khi lưu.");
        model.HinhAnh = creating ? null : await Db.MonAn.AsNoTracking().Where(x => x.Id == model.Id).Select(x => x.HinhAnh).SingleOrDefaultAsync();
        return View("Form", model);
    }

    private async Task<string?> ValidateFormAsync(MonAnFormViewModel model, MonAn? entity)
    {
        var entityId = entity?.Id ?? 0;
        var currentCategoryId = entity?.DanhMucId ?? 0;
        model.Sizes ??= new();
        foreach (var s in model.Sizes) s.DinhMucItems ??= new();
        model.DinhMucItems ??= new();
        model.ComboItems ??= new();

        if (!await Db.DanhMuc.AnyAsync(x => x.Id == model.DanhMucId && (x.DangSuDung || x.Id == currentCategoryId)))
            ModelState.AddModelError(nameof(model.DanhMucId), "Danh mục không tồn tại hoặc đã ngừng sử dụng.");
        if (model.Sizes.Count == 0 || !model.Sizes.Any(x => x.DangSuDung))
            ModelState.AddModelError("", "Món ăn cần ít nhất một size đang sử dụng.");
        if (IsAdmin && model.DaDuyet && model.Sizes.Any(x => x.DangSuDung && x.GiaBan <= 0))
            ModelState.AddModelError("", "Mỗi size đang sử dụng phải có giá lớn hơn 0 trước khi duyệt mở bán.");
        if (IsAdmin && model.TrangThai == TrangThaiMon.NgungKinhDoanh && string.IsNullOrWhiteSpace(model.LyDoNgung))
            ModelState.AddModelError(nameof(model.LyDoNgung), "Nhập lý do ngừng kinh doanh.");
        if (model.Sizes.GroupBy(x => x.TenSize?.Trim(), StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
            ModelState.AddModelError("", "Tên size không được trùng nhau.");
        var ids = model.Sizes.Where(x => x.Id != 0).Select(x => x.Id).ToList();
        if (ids.Distinct().Count() != ids.Count || ids.Any(id => entity == null || !entity.Sizes.Any(x => x.Id == id)))
            ModelState.AddModelError("", "Size không thuộc món ăn này.");
        if (entity != null)
        {
            var removed = entity.Sizes.Where(x => !ids.Contains(x.Id)).Select(x => x.Id).ToList();
            if (await Db.ChiTietHoaDon.AnyAsync(x => x.MonAnSizeId.HasValue && removed.Contains(x.MonAnSizeId.Value))
                || await Db.MonDatTruoc.AnyAsync(x => x.MonAnSizeId.HasValue && removed.Contains(x.MonAnSizeId.Value)))
                ModelState.AddModelError("", "Size đã có trong đặt bàn/hóa đơn. Hãy ngừng sử dụng size thay vì xóa.");
            if (entity.Loai != model.Loai && await Db.ChiTietCombo.AnyAsync(x => x.MonAnId == entity.Id) && model.Loai == LoaiMon.Set)
                ModelState.AddModelError(nameof(model.Loai), "Món đang là thành phần combo nên không thể chuyển thành set.");
            if (model.TrangThai == TrangThaiMon.NgungKinhDoanh
                && await Db.ChiTietHoaDon.AnyAsync(x => x.MonAnId == entity.Id && x.TrangThai != TrangThaiCheBien.DaHuy
                    && (x.HoaDon.TrangThai == TrangThaiHoaDon.ChuaThanhToan || x.HoaDon.TrangThai == TrangThaiHoaDon.ThanhToanMotPhan)))
                ModelState.AddModelError(nameof(model.TrangThai), "Món đang có trong hóa đơn chưa thanh toán, chưa thể ngừng phục vụ.");
        }

        var allDinhMuc = model.Sizes.SelectMany(s => s.DinhMucItems).Concat(model.DinhMucItems).ToList();
        if (model.DinhMucItems.Where(i => i.NguyenLieuId > 0).GroupBy(x => x.NguyenLieuId).Any(g => g.Count() > 1))
            ModelState.AddModelError("", "Nguyên liệu trong định lượng gốc không được trùng.");
        foreach (var s in model.Sizes)
        {
            if (s.DinhMucItems.Where(i => i.NguyenLieuId > 0).GroupBy(x => x.NguyenLieuId).Any(g => g.Count() > 1))
                ModelState.AddModelError("", $"Nguyên liệu phụ của size '{s.TenSize}' không được trùng.");
            if (s.DinhMucItems.Any(i => i.NguyenLieuId > 0 && model.DinhMucItems.Any(b => b.NguyenLieuId == i.NguyenLieuId)))
                ModelState.AddModelError("", $"Nguyên liệu phụ của size '{s.TenSize}' không được trùng với định lượng gốc.");
        }

        var ingredientIds = allDinhMuc.Select(x => x.NguyenLieuId).Where(id => id > 0).Distinct().ToList();
        var allowedIngredients = await Db.NguyenLieu.Where(x => x.DangSuDung || Db.DinhMucMon.Any(d => d.MonAnId == entityId && d.NguyenLieuId == x.Id)).Select(x => x.Id).ToListAsync();
        if (ingredientIds.Any(id => !allowedIngredients.Contains(id))) ModelState.AddModelError("", "Nguyên liệu không tồn tại hoặc đã ngừng sử dụng.");
        if (model.Loai != LoaiMon.Set && model.ComboItems.Count > 0)
            ModelState.AddModelError(nameof(model.Loai), "Chỉ món loại Set mới có thành phần combo.");
        if (model.Loai == LoaiMon.Set && model.ComboItems.Count == 0)
            ModelState.AddModelError("", "Set cần ít nhất một món thành phần.");
        if (model.Loai == LoaiMon.Set && allDinhMuc.Count > 0)
            ModelState.AddModelError("", "Combo dùng công thức của các món thành phần, không nhập lại nguyên liệu thô.");
        var dishIds = model.ComboItems.Select(x => x.MonAnId).ToList();
        var allowedDishes = await Db.MonAn.Where(x => x.Loai != LoaiMon.Set && x.Id != entityId
            && (!model.DaDuyet || (x.DaDuyet && x.TrangThai == TrangThaiMon.DangPhucVu && x.DanhMuc.DangSuDung
                && x.Sizes.Any(s => s.DangSuDung && s.GiaBan > 0)))).Select(x => x.Id).ToListAsync();
        if (dishIds.Distinct().Count() != dishIds.Count || dishIds.Any(id => !allowedDishes.Contains(id)))
            ModelState.AddModelError("", "Món thành phần bị trùng, không tồn tại hoặc là một set khác.");
        if (model.HinhAnhFile == null) return null;
        if (model.HinhAnhFile.Length == 0 || model.HinhAnhFile.Length > 5 * 1024 * 1024)
        {
            ModelState.AddModelError(nameof(model.HinhAnhFile), "Ảnh phải có dung lượng từ 1 byte đến 5 MB.");
            return null;
        }
        var header = new byte[12];
        await using var image = model.HinhAnhFile.OpenReadStream();
        var read = await image.ReadAtLeastAsync(header, 12, throwOnEndOfStream: false);
        string? extension = read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}) ? ".png"
            : read >= 3 && header[0] == 255 && header[1] == 216 && header[2] == 255 ? ".jpg"
            : read >= 12 && System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP" ? ".webp" : null;
        if (extension == null) ModelState.AddModelError(nameof(model.HinhAnhFile), "Chỉ chấp nhận ảnh PNG, JPEG hoặc WebP.");
        return extension;
    }

    private async Task LoadOptionsAsync(MonAnFormViewModel model, int? currentCategory = null)
    {
        model.DanhMucList = await Db.DanhMuc.AsNoTracking().Where(x => x.DangSuDung || x.Id == currentCategory).OrderBy(x => x.TenDanhMuc).ToListAsync();
        var ingredientIds = model.Sizes.SelectMany(s => s.DinhMucItems ?? []).Concat(model.DinhMucItems ?? []).Select(x => x.NguyenLieuId).Where(id => id > 0).Distinct().ToList();
        model.NguyenLieuList = await Db.NguyenLieu.AsNoTracking().Where(x => x.DangSuDung || ingredientIds.Contains(x.Id)).OrderBy(x => x.TenNguyenLieu).ToListAsync();
        model.MonLeList = IsAdmin ? await Db.MonAn.AsNoTracking().Where(x => x.Id != model.Id && x.Loai != LoaiMon.Set).OrderBy(x => x.TenMon).ToListAsync() : [];
        var existingSizes = await Db.MonAnSize.AsNoTracking().Select(x => x.TenSize).Distinct().ToListAsync();
        model.TenSizeOptions = SizeGoiY.Concat(existingSizes)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    [Authorize(Roles = AppRoles.Admin), HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await Db.MonAn.FindAsync(id);
        return entity == null ? NotFound() : View(DeleteModel(entity, id, entity.TenMon, "món ăn"));
    }

    [Authorize(Roles = AppRoles.Admin), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, DeleteRecordViewModel model)
    {
        if (id != model.Id) return NotFound();
        var entity = await Db.MonAn.FindAsync(id);
        if (entity == null) return NotFound();
        model.Name = entity.TenMon; model.Description = "món ăn";
        if (await Db.ChiTietHoaDon.AnyAsync(x => x.MonAnId == id) || await Db.MonDatTruoc.AnyAsync(x => x.MonAnId == id)
            || await Db.ChiTietCombo.AnyAsync(x => x.MonAnId == id) || await Db.KhuyenMaiMon.AnyAsync(x => x.MonAnId == id))
            ModelState.AddModelError("", "Món đã được dùng trong đặt bàn, hóa đơn, combo hoặc khuyến mãi. Không thể xóa.");
        if (ModelState.IsValid && ApplyVersion(entity, model.RowVersion))
        {
            await using var transaction = await Db.Database.BeginTransactionAsync();
            Db.DinhMucMon.RemoveRange(await Db.DinhMucMon.Where(x => x.MonAnId == id).ToListAsync());
            Db.ChiTietCombo.RemoveRange(await Db.ChiTietCombo.Where(x => x.ComboId == id).ToListAsync());
            Db.MonAnSize.RemoveRange(await Db.MonAnSize.Where(x => x.MonAnId == id).ToListAsync());
            Db.MonAn.Remove(entity);
            if (await SaveAsync("", "Không thể xóa món ăn."))
            {
                await transaction.CommitAsync();
                TempData["Success"] = "Xóa món ăn thành công.";
                return RedirectToAction(nameof(Index));
            }
        }
        return View("Delete", model);
    }

    [HttpGet]
    public async Task<IActionResult> DinhMuc(int id)
    {
        var dish = await Db.MonAn
            .Include(x => x.DanhMuc)
            .Include(x => x.Sizes)
            .SingleOrDefaultAsync(x => x.Id == id);

        if (dish == null) return NotFound();
        if (!IsAdmin && dish.Loai == LoaiMon.Set) return Forbid();

        var allIngredients = await Db.NguyenLieu.AsNoTracking()
            .Where(x => x.DangSuDung)
            .OrderBy(x => x.TenNguyenLieu)
            .Select(x => new NguyenLieuOptionVM
            {
                Id = x.Id,
                TenNguyenLieu = x.TenNguyenLieu,
                DonViTinh = x.DonViTinh,
                DonGia = x.DonGia
            })
            .ToListAsync();

        var existingRecipes = await Db.DinhMucMon
            .Include(x => x.NguyenLieu)
            .Where(x => x.MonAnId == id)
            .ToListAsync();

        var baseSizeId = dish.Sizes.OrderBy(x => x.Id).Select(x => x.Id).FirstOrDefault();
        var vm = new DinhMucTheoSizeVM
        {
            MonAnId = dish.Id,
            TenMon = dish.TenMon,
            TenDanhMuc = dish.DanhMuc.TenDanhMuc,
            Loai = dish.Loai,
            LaThucUong = LaDanhMucThucUong(dish.DanhMuc.TenDanhMuc),
            AllIngredients = allIngredients,
            DinhMucItems = existingRecipes
                .Where(r => r.MaKichCo == baseSizeId && !r.LaNguyenLieuPhu)
                .OrderBy(r => r.NguyenLieuId)
                .Select(r => new DinhMucItemViewModel { NguyenLieuId = r.NguyenLieuId, SoLuong = r.SoLuong })
                .ToList(),
            Sizes = dish.Sizes.OrderBy(s => s.Id).Select(s => new SizeDinhMucGroupVM
            {
                MonAnSizeId = s.Id,
                TenSize = s.TenSize,
                GiaBan = s.GiaBan,
                DangSuDung = s.DangSuDung,
                HeSoTyLe = TyLeTuDuLieu(existingRecipes, baseSizeId, s.Id, s.HeSoTyLe),
                GhiChuThanhPhanKhacBiet = s.GhiChuThanhPhanKhacBiet,
                Items = existingRecipes
                    .Where(r => r.MaKichCo == s.Id && r.LaNguyenLieuPhu)
                    .OrderBy(r => r.NguyenLieuId)
                    .Select(r => new SizeDinhMucItemVM
                    {
                        NguyenLieuId = r.NguyenLieuId,
                        TenNguyenLieu = r.NguyenLieu.TenNguyenLieu,
                        DonViTinh = r.NguyenLieu.DonViTinh,
                        DonGia = r.NguyenLieu.DonGia,
                        SoLuong = r.SoLuong
                    })
                    .ToList()
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDinhMucTheoSize(DinhMucTheoSizeVM model)
    {
        if (model == null || model.MonAnId <= 0) return NotFound();
        var dish = await Db.MonAn.Include(x => x.Sizes).SingleOrDefaultAsync(x => x.Id == model.MonAnId);
        if (dish == null) return NotFound();
        if (!IsAdmin && dish.Loai == LoaiMon.Set) return Forbid();

        var validSizeIds = dish.Sizes.Select(s => s.Id).ToHashSet();
        if (model.Sizes != null && model.Sizes.Any(g => !validSizeIds.Contains(g.MonAnSizeId)))
            return BadRequest("Kích cỡ món không thuộc món ăn này.");

        var sizeGroups = model.Sizes?.GroupBy(x => x.MonAnSizeId).Select(g => g.First()).ToList() ?? [];
        var baseItems = (model.DinhMucItems ?? []).Where(x => x.NguyenLieuId > 0 && x.SoLuong > 0 && x.SoLuong <= 1000000)
            .GroupBy(x => x.NguyenLieuId).Select(g => g.First()).ToList();
        var allIngredientIds = sizeGroups.SelectMany(g => g.Items ?? []).Select(i => i.NguyenLieuId)
            .Concat(baseItems.Select(i => i.NguyenLieuId)).Where(id => id > 0).Distinct().ToList();
        if (allIngredientIds.Count > 0)
        {
            var validIngredients = await Db.NguyenLieu.Where(x => allIngredientIds.Contains(x.Id) && x.DangSuDung).Select(x => x.Id).ToListAsync();
            if (validIngredients.Count != allIngredientIds.Count)
                return BadRequest("Một số nguyên liệu không tồn tại hoặc đã ngưng sử dụng.");
        }

        var baseSizeId = dish.Sizes.OrderBy(x => x.Id).Select(x => x.Id).FirstOrDefault();
        await using var tx = await Db.Database.BeginTransactionAsync();
        try
        {
            var postedSizeIds = sizeGroups.Select(g => g.MonAnSizeId).ToHashSet();
            var oldRecipes = await Db.DinhMucMon.Where(x => x.MonAnId == model.MonAnId).ToListAsync();
            Db.DinhMucMon.RemoveRange(oldRecipes.Where(x => postedSizeIds.Contains(x.MaKichCo)));

            foreach (var sizeGroup in sizeGroups)
            {
                var laSizeGoc = sizeGroup.MonAnSizeId == baseSizeId;
                var heSoTyLe = laSizeGoc ? 1m : ChuanHoaTyLe(sizeGroup.HeSoTyLe);
                var daDung = new HashSet<int>(baseItems.Select(x => x.NguyenLieuId));

                foreach (var item in baseItems)
                {
                    var soLuong = laSizeGoc ? item.SoLuong : TinhDinhLuong(item.SoLuong, heSoTyLe);
                    if (soLuong <= 0) continue;
                    Db.DinhMucMon.Add(new DinhMucMon
                    {
                        MonAnId = model.MonAnId,
                        MaKichCo = sizeGroup.MonAnSizeId,
                        NguyenLieuId = item.NguyenLieuId,
                        SoLuong = soLuong
                    });
                }

                // ponytail: nguyên liệu phụ trùng định lượng gốc bị bỏ qua (đã có ở dòng trên).
                foreach (var item in sizeGroup.Items ?? [])
                {
                    if (item.NguyenLieuId <= 0 || item.SoLuong <= 0 || item.SoLuong > 1000000) continue;
                    if (!daDung.Add(item.NguyenLieuId)) continue;

                    Db.DinhMucMon.Add(new DinhMucMon
                    {
                        MonAnId = model.MonAnId,
                        MaKichCo = sizeGroup.MonAnSizeId,
                        NguyenLieuId = item.NguyenLieuId,
                        SoLuong = item.SoLuong,
                        LaNguyenLieuPhu = true
                    });
                }

                var sizeEntity = dish.Sizes.Single(x => x.Id == sizeGroup.MonAnSizeId);
                sizeEntity.HeSoTyLe = heSoTyLe;
                sizeEntity.GhiChuThanhPhanKhacBiet = string.IsNullOrWhiteSpace(sizeGroup.GhiChuThanhPhanKhacBiet) ? null : sizeGroup.GhiChuThanhPhanKhacBiet.Trim();
            }

            await Db.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["Success"] = $"Đã cập nhật định mức (BOM) đa kích cỡ cho món '{dish.TenMon}' thành công.";
            return RedirectToAction(nameof(DinhMuc), new { id = model.MonAnId });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            TempData["Error"] = "Không thể lưu định mức: " + ex.Message;
            return RedirectToAction(nameof(DinhMuc), new { id = model.MonAnId });
        }
    }
}
