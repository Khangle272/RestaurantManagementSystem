using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Controllers;

public class MonAnController(RestaurantDbContext context, IWebHostEnvironment environment) : ManagementControllerBase(context)
{
    public async Task<IActionResult> Index(string? search, int? danhMucId, string? trangThai, int page = 1)
    {
        var query = Db.MonAn.AsNoTracking();
        search = search?.Trim();
        if (!string.IsNullOrEmpty(search)) query = query.Where(x => x.TenMon.Contains(search));
        if (danhMucId > 0) query = query.Where(x => x.DanhMucId == danhMucId);
        if (Enum.TryParse<TrangThaiMon>(trangThai, out var status) && Enum.IsDefined(status))
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
                    TenDanhMuc = x.DanhMuc.TenDanhMuc, TrangThai = x.TrangThai,
                    GiaBan = x.Sizes.Where(s => s.DangSuDung).Min(s => (decimal?)s.GiaBan),
                    SoSize = x.Sizes.Count(s => s.DangSuDung)
                }).ToListAsync(),
            DanhMucList = await Db.DanhMuc.AsNoTracking().OrderBy(x => x.TenDanhMuc).ToListAsync(),
            Search = search, DanhMucId = danhMucId, TrangThai = trangThai,
            CurrentPage = page, TotalPages = pages, TotalItems = count,
            TotalCount = await Db.MonAn.CountAsync(),
            DangPhucVuCount = await Db.MonAn.CountAsync(x => x.TrangThai == TrangThaiMon.DangPhucVu),
            TamHetCount = await Db.MonAn.CountAsync(x => x.TrangThai == TrangThaiMon.TamHet),
            NgungKinhDoanhCount = await Db.MonAn.CountAsync(x => x.TrangThai == TrangThaiMon.NgungKinhDoanh)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new MonAnFormViewModel { Sizes = new() { new() } };
        await LoadOptionsAsync(model);
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(MonAnFormViewModel model) => SaveFormAsync(model, null);

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await Db.MonAn.Include(x => x.Sizes).SingleOrDefaultAsync(x => x.Id == id);
        if (entity == null) return NotFound();
        var model = new MonAnFormViewModel
        {
            Id = id, RowVersion = VersionOf(entity), TenMon = entity.TenMon, DanhMucId = entity.DanhMucId,
            MoTa = entity.MoTa, HinhAnh = entity.HinhAnh, Loai = entity.Loai, TrangThai = entity.TrangThai,
            LaMonMoi = entity.LaMonMoi, LaMonNoiBat = entity.LaMonNoiBat,
            Sizes = entity.Sizes.OrderBy(x => x.Id).Select(x => new MonAnSizeFormViewModel
            {
                Id = x.Id, TenSize = x.TenSize, GiaBan = x.GiaBan, DangSuDung = x.DangSuDung
            }).ToList(),
            DinhMucItems = await Db.DinhMucMon.Where(x => x.MonAnId == id).OrderBy(x => x.NguyenLieuId)
                .Select(x => new DinhMucItemViewModel { NguyenLieuId = x.NguyenLieuId, SoLuong = x.SoLuong }).ToListAsync(),
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
        return await SaveFormAsync(model, entity);
    }

    private async Task<IActionResult> SaveFormAsync(MonAnFormViewModel model, MonAn? entity)
    {
        var creating = entity == null;
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
                if (creating) Db.Add(entity);
                else
                {
                    // Touch the aggregate even when only sizes/recipe/combo changed.
                    Db.Entry(entity).Property(x => x.TenMon).IsModified = true;
                    var removedSizes = entity.Sizes.Where(x => !model.Sizes.Any(s => s.Id == x.Id)).ToList();
                    Db.MonAnSize.RemoveRange(removedSizes);
                }
                foreach (var size in model.Sizes)
                {
                    var target = size.Id == 0 ? new MonAnSize { MonAn = entity } : entity.Sizes.Single(x => x.Id == size.Id);
                    target.TenSize = size.TenSize.Trim(); target.GiaBan = size.GiaBan; target.DangSuDung = size.DangSuDung;
                    if (size.Id == 0) Db.MonAnSize.Add(target);
                }
                if (await SaveAsync("", "Tên size bị trùng. Vui lòng kiểm tra lại."))
                {
                    var recipes = await Db.DinhMucMon.Where(x => x.MonAnId == entity.Id).ToListAsync();
                    Db.DinhMucMon.RemoveRange(recipes.Where(x => !model.DinhMucItems.Any(i => i.NguyenLieuId == x.NguyenLieuId)));
                    foreach (var item in model.DinhMucItems)
                    {
                        var target = recipes.SingleOrDefault(x => x.NguyenLieuId == item.NguyenLieuId);
                        if (target == null) Db.DinhMucMon.Add(new DinhMucMon { MonAnId = entity.Id, NguyenLieuId = item.NguyenLieuId, SoLuong = item.SoLuong });
                        else target.SoLuong = item.SoLuong;
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
                        TempData["Success"] = creating ? "Thêm món ăn thành công." : "Cập nhật món ăn thành công.";
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
        model.Sizes ??= new(); model.DinhMucItems ??= new(); model.ComboItems ??= new();
        if (!await Db.DanhMuc.AnyAsync(x => x.Id == model.DanhMucId && (x.DangSuDung || x.Id == currentCategoryId)))
            ModelState.AddModelError(nameof(model.DanhMucId), "Danh mục không tồn tại hoặc đã ngừng sử dụng.");
        if (model.Sizes.Count == 0 || !model.Sizes.Any(x => x.DangSuDung))
            ModelState.AddModelError("", "Món ăn cần ít nhất một size đang sử dụng.");
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
            if (entity.TrangThai == TrangThaiMon.DangPhucVu && model.TrangThai != TrangThaiMon.DangPhucVu
                && await Db.ChiTietHoaDon.AnyAsync(x => x.MonAnId == entity.Id && x.TrangThai != TrangThaiCheBien.DaHuy
                    && (x.HoaDon.TrangThai == TrangThaiHoaDon.ChuaThanhToan || x.HoaDon.TrangThai == TrangThaiHoaDon.ThanhToanMotPhan)))
                ModelState.AddModelError(nameof(model.TrangThai), "Món đang có trong hóa đơn chưa thanh toán, chưa thể ngừng phục vụ.");
        }
        var ingredientIds = model.DinhMucItems.Select(x => x.NguyenLieuId).ToList();
        if (ingredientIds.Distinct().Count() != ingredientIds.Count)
            ModelState.AddModelError("", "Nguyên liệu trong định mức không được trùng.");
        var allowedIngredients = await Db.NguyenLieu.Where(x => x.DangSuDung || Db.DinhMucMon.Any(d => d.MonAnId == entityId && d.NguyenLieuId == x.Id)).Select(x => x.Id).ToListAsync();
        if (ingredientIds.Any(id => !allowedIngredients.Contains(id))) ModelState.AddModelError("", "Nguyên liệu không tồn tại hoặc đã ngừng sử dụng.");
        if (model.Loai != LoaiMon.Set && model.ComboItems.Count > 0)
            ModelState.AddModelError(nameof(model.Loai), "Chỉ món loại Set mới có thành phần combo.");
        if (model.Loai == LoaiMon.Set && model.ComboItems.Count == 0)
            ModelState.AddModelError("", "Set cần ít nhất một món thành phần.");
        var dishIds = model.ComboItems.Select(x => x.MonAnId).ToList();
        var allowedDishes = await Db.MonAn.Where(x => x.Loai != LoaiMon.Set && x.Id != entityId).Select(x => x.Id).ToListAsync();
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
        var ingredientIds = model.DinhMucItems.Select(x => x.NguyenLieuId).ToList();
        model.NguyenLieuList = await Db.NguyenLieu.AsNoTracking().Where(x => x.DangSuDung || ingredientIds.Contains(x.Id)).OrderBy(x => x.TenNguyenLieu).ToListAsync();
        model.MonLeList = await Db.MonAn.AsNoTracking().Where(x => x.Id != model.Id && x.Loai != LoaiMon.Set).OrderBy(x => x.TenMon).ToListAsync();
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await Db.MonAn.FindAsync(id);
        return entity == null ? NotFound() : View(DeleteModel(entity, id, entity.TenMon, "món ăn"));
    }

    [HttpPost, ValidateAntiForgeryToken]
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
}
