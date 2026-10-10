using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Services;

public static class MenuRules
{
    public static IQueryable<MonAn> Listed(RestaurantDbContext db) => db.MonAn.Where(x =>
        x.DaDuyet && x.DanhMuc.DangSuDung && x.TrangThai != TrangThaiMon.NgungKinhDoanh
        && x.Sizes.Any(s => s.DangSuDung && s.GiaBan > 0)
        && (x.Loai != LoaiMon.Set || (x.ThanhPhanCombo.Any()
            && x.ThanhPhanCombo.All(c => c.SoLuong > 0 && c.MonAn.Loai != LoaiMon.Set
                && c.MonAn.DaDuyet && c.MonAn.DanhMuc.DangSuDung
                && c.MonAn.TrangThai == TrangThaiMon.DangPhucVu
                && c.MonAn.Sizes.Any(s => s.DangSuDung && s.GiaBan > 0)))));

    public static IQueryable<MonAn> Available(RestaurantDbContext db) =>
        Listed(db).Where(x => x.TrangThai == TrangThaiMon.DangPhucVu);
}
