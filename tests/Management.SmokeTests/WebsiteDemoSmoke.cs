using System.Net;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Services;

namespace Management.SmokeTests;

public static class WebsiteDemoSmoke
{
    public static async Task Run(RestaurantDbContext db, HttpClient admin, Func<HttpClient> newClient,
        Func<string,string,string> hidden, Action<bool,string> check)
    {
        await WebsiteDemoSetup.InitializeAsync(db);
        var day = PromotionTime.Today;
        var prefix = $"WEB-{day:yyyyMMdd}";
        var customer = await db.KhachHang.SingleAsync(x=>x.Email=="khach.demo@example.test");
        var history = await db.HoaDon.Include(x=>x.ChiTiet).Where(x=>x.MaHoaDon.StartsWith("WEB-H-")).ToListAsync();
        check(history.Count==29 && history.All(x=>x.TrangThai==TrangThaiHoaDon.DaThanhToan),"Demo includes four weeks and today's paid invoices");
        check(history.All(h=>h.TongTienHang==h.ChiTiet.Sum(x=>x.SoLuong*x.DonGia)
            && PromotionService.ReadSnapshot(h) is not null && h.TienGiam>=0 && h.TienGiam<=h.TongTienHang),"Demo invoices have consistent totals and frozen discount breakdowns");
        var ledger=await db.LichSuTichDiem.ToListAsync();
        check(ledger.Count==29 && ledger.All(x=>x.SoDiem==(int)Math.Floor(x.SoTienTinhDiem/10000)),"Each synthetic paid invoice has one correctly calculated points entry");
        check(await db.VoucherKhachHang.CountAsync(x=>x.KhachHangId==customer.Id)==2,"Customer starts with two unused wallet tickets");
        var programs=await db.KhuyenMai.Include(x=>x.Vouchers).Where(x=>x.TenChuongTrinh.StartsWith("Demo toàn website")).ToListAsync();
        check(programs.Count==1 && programs[0].Vouchers.Count==4 && programs[0].Vouchers.Any(x=>x.PhamVi==PhamViUuDai.MonAn)
            && programs[0].Vouchers.Any(x=>x.GioiHanTongLuot==null),"Demo campaign contains mixed independent voucher types including unlimited use");
        var promo=new PromotionService(db);var open=await db.HoaDon.SingleAsync(x=>x.MaHoaDon==prefix+"-POS2");
        foreach(var code in programs[0].Vouchers) check((await promo.EvaluatePromotionsAsync(open.Id,code.Ma)).AppliedVoucherId==code.Id,"Each active demo code can be applied on the cashier's ready-to-pay invoice: "+code.Ma);
        var bookings=await db.DatBan.Where(x=>x.MaDatBan.StartsWith(prefix)).ToListAsync();
        check(new[]{TrangThaiDatBan.ChoXacNhan,TrangThaiDatBan.ChoCoc,TrangThaiDatBan.DaXacNhan,TrangThaiDatBan.DaNhanBan,TrangThaiDatBan.DaHuy,TrangThaiDatBan.KhongDen}.All(s=>bookings.Any(x=>x.TrangThai==s)),"Booking workflow includes pending, deposit, confirmed, serving, canceled and no-show cases");
        check(bookings.Any(x=>x.ThoiDiemBaoChuyenKhoan!=null && x.TrangThai==TrangThaiDatBan.ChoCoc),"Cashier has a payment notice awaiting reconciliation");
        check(await db.PhieuNhap.AnyAsync(x=>x.MaPhieu==prefix+"-NHAPNHAP" && x.TrangThai==TrangThaiPhieu.Nhap)
            && await db.PhieuThanhLy.AnyAsync(x=>x.MaPhieu==prefix+"-THANHLY"),"Warehouse demo includes a draft receipt and a matching disposal");
        var codes=await db.HoaDon.CountAsync();var receipts=await db.PhieuNhap.CountAsync();var outputs=await db.PhieuXuat.CountAsync();
        var points=await db.LichSuTichDiem.CountAsync();var stock=await db.NguyenLieu.OrderBy(x=>x.Id).Select(x=>x.SoLuongTon).ToListAsync();
        // A second run must not recreate transactions, replenish stock, rewind kitchen state or duplicate points.
        var kitchenLine=await db.ChiTietHoaDon.FirstAsync(x=>x.HoaDon.MaHoaDon==prefix+"-POS1" && x.TrangThai==TrangThaiCheBien.DangCheBien);
        kitchenLine.TrangThai=TrangThaiCheBien.SanSang;await db.SaveChangesAsync();
        db.ChangeTracker.Clear();await WebsiteDemoSetup.InitializeAsync(db);db.ChangeTracker.Clear();
        check(await db.HoaDon.CountAsync()==codes && await db.PhieuNhap.CountAsync()==receipts && await db.PhieuXuat.CountAsync()==outputs
            && await db.LichSuTichDiem.CountAsync()==points && stock.SequenceEqual(await db.NguyenLieu.OrderBy(x=>x.Id).Select(x=>x.SoLuongTon).ToListAsync()),"Repeated initialization neither duplicates records nor changes stock/points");
        check((await db.ChiTietHoaDon.SingleAsync(x=>x.Id==kitchenLine.Id)).TrangThai==TrangThaiCheBien.SanSang,"Repeated initialization preserves progress made during the demo");
        foreach(var path in new[]{"/","/KhuyenMai","/TaiKhoanKhachHang","/SoDoBan","/QuanLyDatBan","/DonHang","/HoaDon","/Bep","/Kho/TonKho","/NhapKho","/XuatKho","/ThanhLy","/BaoCao","/BaoCao/DoanhThu","/BaoCao/HieuQuaKhuyenMai","/ThongKeDatBan"}) await Get(admin,path);
        foreach(var (email,path,loginPath) in new[]{
            ("khach.demo@example.test","/KhachHang","/Account/Login"),
            ("tieptan.demo@example.test","/SoDoBan","/admin"),
            ("boiban.demo@example.test","/DonHang","/admin"),
            ("thungan.demo@example.test","/HoaDon","/admin"),
            ("bep.demo@example.test","/Bep","/admin"),
            ("kho.demo@example.test","/Kho/TonKho","/admin")})
        {
            using var client=newClient();var form=await Get(client,loginPath);
            using var response=await client.PostAsync(loginPath,new FormUrlEncodedContent(new Dictionary<string,string>{["Email"]=email,["Password"]="Demo@2026!",["__RequestVerificationToken"]=hidden(form,"__RequestVerificationToken")}));
            check(response.StatusCode==HttpStatusCode.Redirect,"Demo account can log in: "+email);await Get(client,path);
            if(email.StartsWith("khach")){await Get(client,"/KhachHang/Vouchers?wallet=true");await Get(client,$"/KhachHang/Order/{open.Id}");}
        }
        async Task<string> Get(HttpClient client,string path) {using var response=await client.GetAsync(path);check(response.StatusCode==HttpStatusCode.OK,"Website demo GET "+path+": "+response.StatusCode);return await response.Content.ReadAsStringAsync();}
    }
}
