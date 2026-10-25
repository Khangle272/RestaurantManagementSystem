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
        // Only pre-arrival food needs purchasing plans; received orders already pass through stock deduction.
        var lines = upcoming.SelectMany(x => x.MonDatTruoc.Select(line => (Code: x.MaDatBan, Line: line))).ToList();
        var sizeIds = lines.Where(x => x.Line.MonAnSizeId.HasValue).Select(x => x.Line.MonAnSizeId!.Value).Distinct().ToArray();
        var recipes = await db.DinhMucMon.AsNoTracking().Include(x => x.NguyenLieu).Where(x => sizeIds.Contains(x.MaKichCo)).ToListAsync();
        var demand = new Dictionary<int, (NguyenLieu Ingredient, decimal Quantity)>();
        foreach (var item in lines)
        {
            var recipe = recipes.Where(x => x.MonAnId == item.Line.MonAnId && x.MaKichCo == item.Line.MonAnSizeId).ToList();
            if (recipe.Count == 0)
            {
                model.Warnings.Add($"{item.Code}: {item.Line.TenMonLucDat} / {item.Line.TenSizeLucDat} chưa có định mức riêng; chưa cộng vào dự trù.");
                continue;
            }
            foreach (var part in recipe)
            {
                var current = demand.GetValueOrDefault(part.NguyenLieuId);
                demand[part.NguyenLieuId] = (part.NguyenLieu, current.Quantity + part.SoLuong * item.Line.SoLuong);
            }
        }
        model.Ingredients = demand.Values.OrderBy(x => x.Ingredient.TenNguyenLieu)
            .Select(x => new IngredientPlanningRow(x.Ingredient.TenNguyenLieu, x.Ingredient.DonViTinh, x.Quantity, x.Ingredient.SoLuongTon)).ToList();
        return model;
    }
}
