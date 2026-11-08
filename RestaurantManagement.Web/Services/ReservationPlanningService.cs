using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
namespace RestaurantManagement.Web.Services;

public class ReservationPlanningService(RestaurantDbContext db)
{
    public async Task<ReservationPlanningViewModel> Build(DateOnly from, DateOnly until, string? shift, bool? vip)
    {
        var start = TableService.VietnamTime(from.ToDateTime(TimeOnly.MinValue)); var end = TableService.VietnamTime(until.AddDays(1).ToDateTime(TimeOnly.MinValue));
        var all = await db.DatBan.AsNoTracking().AsSplitQuery().Include(x => x.Ban).Include(x => x.MonDatTruoc)
            .Where(x => x.GioDen >= start && x.GioDen < end && x.TrangThai != TrangThaiDatBan.DaHuy && x.TrangThai != TrangThaiDatBan.KhongDen).ToListAsync();
        all = all.Where(x => (shift is null || ReservationSchedule.Match(x.GioDen.ToOffset(TimeSpan.FromHours(7)).DateTime)?.Id == shift)
            && (!vip.HasValue || x.YeuCauVip == vip)).ToList();
        var confirmed = all.Where(x => x.TrangThai is TrangThaiDatBan.DaXacNhan or TrangThaiDatBan.DaNhanBan).ToList();
        var upcoming = confirmed.Where(x => x.ThoiDiemNhanBan is null && x.GioKetThucDuKien > DateTimeOffset.UtcNow).ToList();
        var model = new ReservationPlanningViewModel { From = from, Until = until, Shift = shift, Vip = vip,
            Waiting = all.Count - confirmed.Count, WithoutFood = upcoming.Count(x => x.MonDatTruoc.Count == 0) };
        model.Rows = confirmed.GroupBy(x => new { Day = DateOnly.FromDateTime(x.GioDen.ToOffset(TimeSpan.FromHours(7)).DateTime),
            Shift = ReservationSchedule.Match(x.GioDen.ToOffset(TimeSpan.FromHours(7)).DateTime)?.Label ?? "Ngoài giờ (lịch cũ)", Vip = x.YeuCauVip })
            .OrderBy(x => x.Key.Day).ThenBy(x => x.Key.Shift).ThenBy(x => x.Key.Vip)
            .Select(g => new ReservationPlanningRow(g.Key.Day, g.Key.Shift, g.Key.Vip, g.Count(), g.Sum(x => x.Ban.Count), g.Sum(x => x.SoNguoiLon + x.SoTreEm))).ToList();
        // Tổng hợp theo từng mốc giờ hẹn thực tế: tách lượt đã xác nhận và lượt còn chờ cọc/chờ xác nhận.
        model.ArrivalTimes = all.GroupBy(x =>
            {
                var local = x.GioDen.ToOffset(TimeSpan.FromHours(7)).DateTime;
                return (Day: DateOnly.FromDateTime(local), Time: new TimeOnly(local.Hour, local.Minute));
            })
            .OrderBy(g => g.Key.Day).ThenBy(g => g.Key.Time)
            .Select(g => new ArrivalTimeRow(g.Key.Day, g.Key.Time,
                g.Count(x => x.TrangThai is TrangThaiDatBan.DaXacNhan or TrangThaiDatBan.DaNhanBan),
                g.Count(x => x.TrangThai is TrangThaiDatBan.ChoXacNhan or TrangThaiDatBan.ChoCoc),
                g.Sum(x => x.Ban.Count), g.Sum(x => x.SoNguoiLon + x.SoTreEm))).ToList();
        // Only pre-arrival food needs purchasing plans; received orders already pass through stock deduction.
        var lines = upcoming.SelectMany(x => x.MonDatTruoc.Select(line => (Code: x.MaDatBan, Line: line))).ToList();
        // Combo trải ra món thành phần giống lúc trừ kho thật (InventoryService): món trong combo không chọn size nên dùng
        // định mức của size có mã nhỏ nhất. Món lẻ chỉ dùng đúng định mức của size khách chọn, thiếu thì cảnh báo.
        var dishIds = lines.Select(x => x.Line.MonAnId).Distinct().ToList();
        var comboParts = await db.ChiTietCombo.AsNoTracking()
            .Where(x => dishIds.Contains(x.ComboId) && x.Combo.Loai == LoaiMon.Set).ToListAsync();
        var recipeDishIds = dishIds.Concat(comboParts.Select(x => x.MonAnId)).Distinct().ToList();
        var recipes = await db.DinhMucMon.AsNoTracking().Include(x => x.NguyenLieu).Where(x => recipeDishIds.Contains(x.MonAnId)).ToListAsync();
        List<DinhMucMon> RecipeOf(int dishId, int? sizeId)
        {
            if (sizeId is > 0) return recipes.Where(x => x.MonAnId == dishId && x.MaKichCo == sizeId).ToList();
            var fallback = recipes.Where(x => x.MonAnId == dishId).Select(x => (int?)x.MaKichCo).Min();
            return fallback is int size ? recipes.Where(x => x.MonAnId == dishId && x.MaKichCo == size).ToList() : [];
        }
        var demand = new Dictionary<int, (NguyenLieu Ingredient, decimal Quantity)>();
        foreach (var item in lines)
        {
            var parts = comboParts.Where(x => x.ComboId == item.Line.MonAnId).ToList();
            var recipe = parts.Count > 0
                ? parts.SelectMany(p => RecipeOf(p.MonAnId, null).Select(r => (r.NguyenLieu, Quantity: r.SoLuong * p.SoLuong))).ToList()
                : RecipeOf(item.Line.MonAnId, item.Line.MonAnSizeId).Select(r => (r.NguyenLieu, Quantity: r.SoLuong)).ToList();
            if (recipe.Count == 0)
            {
                model.Warnings.Add($"{item.Code}: {item.Line.TenMonLucDat} / {item.Line.TenSizeLucDat} chưa có định mức riêng; chưa cộng vào dự trù.");
                continue;
            }
            foreach (var part in recipe)
            {
                var current = demand.GetValueOrDefault(part.NguyenLieu.Id);
                demand[part.NguyenLieu.Id] = (part.NguyenLieu, current.Quantity + part.Quantity * item.Line.SoLuong);
            }
        }
        model.Ingredients = demand.Values.OrderBy(x => x.Ingredient.TenNguyenLieu)
            .Select(x => new IngredientPlanningRow(x.Ingredient.TenNguyenLieu, x.Ingredient.DonViTinh, x.Quantity, x.Ingredient.SoLuongTon)).ToList();
        return model;
    }
}
