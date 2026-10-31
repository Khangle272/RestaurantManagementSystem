using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Services;

// Khách bắt buộc chọn bàn trên sơ đồ: chọn một bàn đủ chỗ, chưa có lịch trong ngày đến.
public static class TestTables
{
    public static string For(RestaurantDbContext db, int? areaId, string arrival, int guests = 2)
    {
        var day = TableService.VietnamTime(DateTime.Parse(arrival).Date); var next = day.AddDays(1);
        return db.BanAn.Where(t => (areaId == null ? !t.KhuVuc.LaPhongVip && t.KhuVuc.DangSuDung : t.KhuVucId == areaId) && t.SoChoNgoi >= guests && t.TrangThai != TrangThaiBan.NgungSuDung
                && !db.ChiTietDatBan.Any(c => c.BanAnId == t.Id && c.DatBan.GioDen >= day && c.DatBan.GioDen < next && c.DatBan.TrangThai != TrangThaiDatBan.DaHuy))
            .OrderBy(t => t.SoChoNgoi).ThenBy(t => t.Id).Select(t => t.Id).First().ToString();
    }
}
