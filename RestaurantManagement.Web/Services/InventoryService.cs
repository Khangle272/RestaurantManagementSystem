using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Services;

public class InventoryService(RestaurantDbContext db) : IKhoService
{
    public async Task<InventoryDeductionResult> DeductInventoryForOrderDishesAsync(int hoaDonId, List<OrderItemInputModel> items, int? staffId = null)
    {
        var result = new InventoryDeductionResult();
        if (items.Count == 0) return result;

        var hoaDon = await db.HoaDon
            .Include(x => x.DatBan)
                .ThenInclude(d => d!.Ban)
                .ThenInclude(b => b.BanAn)
            .SingleOrDefaultAsync(x => x.Id == hoaDonId);

        if (hoaDon == null)
        {
            result.Success = false;
            result.Message = "Không tìm thấy hóa đơn.";
            return result;
        }

        var tableNames = hoaDon.DatBan?.Ban?.Select(b => b.BanAn?.MaBan).Where(x => !string.IsNullOrEmpty(x)).ToList() ?? [];
        var soBan = tableNames.Count > 0 ? string.Join(", ", tableNames) : (hoaDon.LoaiDonHang == LoaiDonHang.TaiBan ? "Tại bàn" : "Mang đi / Giao hàng");

        var effectiveStaffId = staffId ?? hoaDon.NhanVienId;
        if (effectiveStaffId <= 0)
        {
            var firstStaff = await db.NhanVien.Where(x => x.DangLamViec).Select(x => x.Id).FirstOrDefaultAsync();
            effectiveStaffId = firstStaff > 0 ? firstStaff : 1;
        }

        // Flatten dishes including Combos
        var ingredientRequirements = new Dictionary<int, decimal>();

        foreach (var item in items)
        {
            if (item.SoLuong <= 0) continue;

            var dish = await db.MonAn
                .Include(x => x.ThanhPhanCombo)
                .SingleOrDefaultAsync(x => x.Id == item.MonAnId);

            if (dish == null) continue;

            if (dish.Loai == LoaiMon.Set && dish.ThanhPhanCombo.Count > 0)
            {
                foreach (var comboPart in dish.ThanhPhanCombo)
                {
                    var partQty = comboPart.SoLuong * item.SoLuong;
                    await AddDishIngredientsAsync(ingredientRequirements, comboPart.MonAnId, null, partQty);
                }
            }
            else
            {
                await AddDishIngredientsAsync(ingredientRequirements, item.MonAnId, item.MonAnSizeId > 0 ? item.MonAnSizeId : null, item.SoLuong);
            }
        }

        if (ingredientRequirements.Count == 0)
        {
            result.Message = "Các món đã chọn không có định mức nguyên liệu cần xuất.";
            return result;
        }

        var ingredientIds = ingredientRequirements.Keys.ToList();
        var ingredients = await db.NguyenLieu
            .Where(x => ingredientIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);

        var phieuXuat = new PhieuXuat
        {
            MaPhieu = TableService.NewCode("PX-CB"),
            NhanVienId = effectiveStaffId,
            HoaDonId = hoaDon.Id,
            ThoiDiem = DateTimeOffset.UtcNow,
            TrangThai = TrangThaiPhieu.DaGhiSo,
            LyDo = LyDoXuat.CheBien,
            BoPhanNhan = "Bếp",
            NguoiNhan = $"Bàn {soBan}",
            GhiChu = $"Xuất chế biến bàn {soBan} - Hóa đơn #{hoaDon.MaHoaDon}",
            TongTien = 0
        };

        decimal tongTien = 0;

        foreach (var (ingredientId, qty) in ingredientRequirements)
        {
            if (!ingredients.TryGetValue(ingredientId, out var nguyenLieu)) continue;
            if (qty <= 0 || qty > 1000000) continue;

            if (nguyenLieu.SoLuongTon < qty)
            {
                result.Success = false;
                result.Warnings.Add($"Không đủ tồn kho [{nguyenLieu.TenNguyenLieu}] (còn {nguyenLieu.SoLuongTon:N2} {nguyenLieu.DonViTinh}, cần {qty:N2}). Đã ghi nhận thiếu và giữ tồn về 0.");
            }

            nguyenLieu.SoLuongTon = Math.Max(0, nguyenLieu.SoLuongTon - qty);

            var donGiaXuat = nguyenLieu.DonGia;
            var thanhTien = qty * donGiaXuat;
            tongTien += thanhTien;

            phieuXuat.ChiTiet.Add(new ChiTietPhieuXuat
            {
                NguyenLieuId = ingredientId,
                SoLuong = qty,
                DonGiaXuat = donGiaXuat,
                ThanhTien = thanhTien
            });

            if (nguyenLieu.SoLuongTon <= nguyenLieu.NguongCanhBao)
            {
                result.LowStockIngredients.Add(nguyenLieu.TenNguyenLieu);
                result.Warnings.Add($"Cảnh báo: Nguyên liệu [{nguyenLieu.TenNguyenLieu}] tồn kho còn {nguyenLieu.SoLuongTon:N2} {nguyenLieu.DonViTinh}, dưới ngưỡng an toàn ({nguyenLieu.NguongCanhBao:N2})!");
            }
        }

        phieuXuat.TongTien = tongTien;
        db.PhieuXuat.Add(phieuXuat);
        await db.SaveChangesAsync();

        result.PhieuXuatId = phieuXuat.Id;
        result.MaPhieuXuat = phieuXuat.MaPhieu;
        result.Message = $"Đã tự động xuất kho nguyên liệu cho bàn {soBan} (Phiếu {phieuXuat.MaPhieu}).";
        return result;
    }

    public async Task<InventoryDeductionResult> DeductInventoryForBookingPreOrdersAsync(int datBanId, int hoaDonId, int? staffId = null)
    {
        var preOrders = await db.MonDatTruoc
            .Where(x => x.DatBanId == datBanId && x.SoLuong > 0)
            .ToListAsync();

        if (preOrders.Count == 0)
        {
            return new InventoryDeductionResult { Message = "Đơn đặt bàn không có món đặt trước." };
        }

        var orderItems = preOrders.Select(x => new OrderItemInputModel
        {
            MonAnId = x.MonAnId,
            MonAnSizeId = x.MonAnSizeId ?? 0,
            SoLuong = x.SoLuong,
            YeuCauCheBien = x.YeuCauCheBien
        }).ToList();

        return await DeductInventoryForOrderDishesAsync(hoaDonId, orderItems, staffId);
    }

    private async Task AddDishIngredientsAsync(Dictionary<int, decimal> requirements, int dishId, int? sizeId, int dishQuantity)
    {
        List<DinhMucMon> recipes;

        if (sizeId.HasValue && sizeId.Value > 0)
        {
            recipes = await db.DinhMucMon
                .Where(x => x.MonAnId == dishId && x.MaKichCo == sizeId.Value)
                .ToListAsync();

            if (recipes.Count == 0)
            {
                // Fallback to first available size recipes for this dish
                recipes = await db.DinhMucMon
                    .Where(x => x.MonAnId == dishId)
                    .GroupBy(x => x.MaKichCo)
                    .Select(g => g.ToList())
                    .FirstOrDefaultAsync() ?? [];
            }
        }
        else
        {
            recipes = await db.DinhMucMon
                .Where(x => x.MonAnId == dishId)
                .GroupBy(x => x.MaKichCo)
                .Select(g => g.ToList())
                .FirstOrDefaultAsync() ?? [];
        }

        foreach (var r in recipes)
        {
            var total = r.SoLuong * dishQuantity;
            if (requirements.ContainsKey(r.NguyenLieuId))
                requirements[r.NguyenLieuId] += total;
            else
                requirements[r.NguyenLieuId] = total;
        }
    }
}
