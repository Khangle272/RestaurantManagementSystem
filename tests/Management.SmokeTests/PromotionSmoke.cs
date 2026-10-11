using System.Net;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Services;

namespace Management.SmokeTests;

public static class PromotionSmoke
{
    public static async Task Run(RestaurantDbContext db, HttpClient admin, Func<HttpClient> newClient,
        Func<string,string,string> hidden, Action<bool,string> check, string connection)
    {
        var options = new DbContextOptionsBuilder<RestaurantDbContext>().UseSqlServer(connection).Options;
        var now = DateTimeOffset.UtcNow;
        var employeeId = await db.NhanVien.Where(x=>x.DangLamViec).Select(x=>x.Id).FirstAsync();
        var dish = await db.MonAn.FirstAsync();
        var service = new PromotionService(db);
        var orders = new OrderService(db);
        var auto = new KhuyenMai { TenChuongTrinh="TEST-AUTO", BatDau=now.AddDays(-1), KetThuc=now.AddDays(1),
            PhamVi=PhamViUuDai.HoaDon, KieuGiam=KieuGiam.PhanTram, GiaTri=10, ChoPhepKetHop=true };
        db.KhuyenMai.Add(auto); await db.SaveChangesAsync();

        var booking = new DatBan { MaDatBan="DB-PROMO-TEST", HoTenLienHe="Promo test", SoDienThoaiLienHe="0901234567",
            ThoiDiemTao=now, GioDen=now, GioKetThucDuKien=now.AddHours(3), ThoiDiemNhanBan=now,
            SoNguoiLon=2, TrangThai=TrangThaiDatBan.DaNhanBan, TienCocDaNop=50000, TrangThaiCoc=TrangThaiCoc.DaCoc };
        var bill = NewBill("TEST-PROMO-BILL",100000); bill.DatBan=booking; bill.TienGiam=5000;
        db.HoaDon.Add(bill); await db.SaveChangesAsync();
        var billId=bill.Id; var bookingId=booking.Id;
        var bookingVersion=VersionEntity(booking);
        var quote=await service.EvaluatePromotionsAsync(billId);
        check(quote.IsSuccess && quote.TongTienGiam==15000 && quote.TienGiamThuCong==5000,"Auto plus manual has one authoritative discount");
        var html=await Get($"/HoaDon/Details/{billId}");
        var expectedAmount = hidden(html,"SoTienPhaiThuDuKien");
        check(decimal.TryParse(expectedAmount,System.Globalization.NumberStyles.Number,System.Globalization.CultureInfo.InvariantCulture,out var renderedAmount)
            && renderedAmount==35000 && html.Contains("TEST-AUTO"),"Invoice renders automatic discount and correct deposit-adjusted expected amount: "+expectedAmount);
        var originalVersion=await Version(billId);
        auto=await db.KhuyenMai.SingleAsync(x=>x.Id==auto.Id); auto.GiaTri=20; await db.SaveChangesAsync();
        var staleAmount=await orders.ProcessPaymentAsync(new PaymentFormModel {Id=billId,RowVersion=originalVersion,
            BookingRowVersion=bookingVersion,TienGiamThuCong=5000,SoTienPhaiThuDuKien=35000,TienKhachDua=35000},employeeId);
        check(staleAmount?.Contains("Số tiền phải thu đã thay đổi")==true,"Changed promotion cannot silently settle an old displayed/QR amount");
        db.ChangeTracker.Clear(); auto=await db.KhuyenMai.SingleAsync(x=>x.Id==auto.Id); auto.GiaTri=10; await db.SaveChangesAsync();
        html=await Get($"/HoaDon/Details/{billId}");
        await Post("/HoaDon/ThanhToan",html,new() { ["Id"]=billId.ToString(),["RowVersion"]=hidden(html,"RowVersion"),
            ["BookingRowVersion"]=hidden(html,"BookingRowVersion"),["TienGiamThuCong"]="5000",["SoTienPhaiThuDuKien"]="35000",
            ["TienKhachDua"]="35000",["PhuongThuc"]="TienMat" },HttpStatusCode.Redirect);
        db.ChangeTracker.Clear(); bill=await db.HoaDon.SingleAsync(x=>x.Id==billId);
        var history=PromotionService.ReadSnapshot(bill);
        check(bill.TrangThai==TrangThaiHoaDon.DaThanhToan && bill.TongThanhToan==35000 && bill.TienCocDaTru==50000,
            "Payment preserves deposit allocation and charges only the remainder");
        check(history?.TienGiamThuCong==5000 && history.UuDai.Single().SoTienGiam==10000,"Payment captures actual discount breakdown once");
        var repeat=await orders.ProcessPaymentAsync(new PaymentFormModel {Id=billId,RowVersion=originalVersion,TienKhachDua=35000},employeeId);
        check(repeat is not null && await orders.AvailableDepositAsync(bookingId)==0,"Duplicate payment rejected and consumed deposit cannot be reused");
        auto=await db.KhuyenMai.SingleAsync(x=>x.Id==auto.Id); auto.TenChuongTrinh="TEST-AUTO-RENAMED"; auto.GiaTri=99; auto.DangSuDung=false;
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var frozen=await service.EvaluatePromotionsAsync(billId);
        check(frozen.TongTienGiam==15000 && frozen.AppliedPromotions.Single().TenChuongTrinh=="TEST-AUTO",
            "Paid invoice keeps historical promotion after rename/rate change/disable");
        await Get($"/HoaDon/Details/{billId}");

        var scoped = new KhuyenMai { TenChuongTrinh="TEST-DISH-MINIMUM", BatDau=now.AddDays(-1), KetThuc=now.AddDays(1),
            PhamVi=PhamViUuDai.MonAn, KieuGiam=KieuGiam.PhanTram, GiaTri=10, GiaTriToiThieu=80000, MucGiamToiDa=3000 };
        db.KhuyenMai.Add(scoped); await db.SaveChangesAsync();
        db.KhuyenMaiMon.Add(new KhuyenMaiMon {KhuyenMaiId=scoped.Id,MonAnId=dish.Id});
        var dishBill=NewBill("TEST-DISH-SCOPE",100000); dishBill.ChiTiet.Single().DonGia=40000;
        var otherDishId=await db.MonAn.Where(x=>x.Id!=dish.Id).Select(x=>x.Id).FirstAsync();
        dishBill.ChiTiet.Add(new ChiTietHoaDon {MonAnId=otherDishId,TenMonLucBan="Other dish",SoLuong=1,DonGia=60000,TrangThai=TrangThaiCheBien.DaPhucVu});
        dishBill.ChiTiet.Add(new ChiTietHoaDon {MonAnId=dish.Id,TenMonLucBan="Cancelled",SoLuong=1,DonGia=10000,TrangThai=TrangThaiCheBien.DaHuy});
        db.HoaDon.Add(dishBill); await db.SaveChangesAsync();
        var dishEval=await service.EvaluatePromotionsAsync(dishBill.Id);
        check(dishEval.TongTienHang==100000 && dishEval.TongTienGiam==3000,
            "Dish promotion checks whole-bill minimum, discounts only eligible active lines, and respects cap");
        scoped.DangSuDung=false; await db.SaveChangesAsync();
        var stackA=new KhuyenMai {TenChuongTrinh="TEST-STACK-A",BatDau=now.AddDays(-1),KetThuc=now.AddDays(1),PhamVi=PhamViUuDai.HoaDon,
            KieuGiam=KieuGiam.PhanTram,GiaTri=20,ChoPhepKetHop=true};
        var stackB=new KhuyenMai {TenChuongTrinh="TEST-STACK-B",BatDau=now.AddDays(-1),KetThuc=now.AddDays(1),PhamVi=PhamViUuDai.HoaDon,
            KieuGiam=KieuGiam.PhanTram,GiaTri=10,ChoPhepKetHop=true};
        var exclusive=new KhuyenMai {TenChuongTrinh="TEST-EXCLUSIVE",BatDau=now.AddDays(-1),KetThuc=now.AddDays(1),PhamVi=PhamViUuDai.HoaDon,
            KieuGiam=KieuGiam.PhanTram,GiaTri=25,ChoPhepKetHop=false};
        var stackBill=NewBill("TEST-STACK-BILL",100000); db.KhuyenMai.AddRange(stackA,stackB,exclusive); db.HoaDon.Add(stackBill); await db.SaveChangesAsync();
        var stacked=await service.EvaluatePromotionsAsync(stackBill.Id);
        check(stacked.TongTienGiam==30000 && stacked.AppliedPromotions.Count==2,"Combining compatible promotions beats a smaller exclusive offer");
        exclusive.GiaTri=40; await db.SaveChangesAsync();
        var best=await service.EvaluatePromotionsAsync(stackBill.Id);
        check(best.TongTienGiam==40000 && best.AppliedPromotions.Count==1,"A better exclusive promotion replaces the combined group rather than stacking with it");
        stackA.DangSuDung=false; stackB.DangSuDung=false; exclusive.DangSuDung=false; await db.SaveChangesAsync();
        check(CsvExport.Cell("=2+2")=="\"'=2+2\"" && CsvExport.Cell("a\"b")=="\"a\"\"b\"","CSV names are quoted and spreadsheet formulas neutralized");

        var create=await Get("/Voucher/Create");
        var campaignForm=await Get("/KhuyenMai/Create");
        await Post("/KhuyenMai/Create",campaignForm,new() { ["TenChuongTrinh"]="TEST-INVALID-DISH-LINK",
            ["BatDau"]=now.ToString("yyyy-MM-dd"),["KetThuc"]=now.AddDays(1).ToString("yyyy-MM-dd"),["PhamVi"]="MonAn",
            ["KieuGiam"]="PhanTram",["GiaTri"]="10",["SelectedMonAnIds[0]"]="2147483647" },HttpStatusCode.OK);
        check(!await db.KhuyenMai.AnyAsync(x=>x.TenChuongTrinh=="TEST-INVALID-DISH-LINK"),"Invalid dish selection cannot leave a half-created promotion");
        await Post("/KhuyenMai/Create",campaignForm,new() { ["TenChuongTrinh"]="TEST-INVALID-NEGATIVE",
            ["BatDau"]=now.ToString("yyyy-MM-dd"),["KetThuc"]=now.AddDays(1).ToString("yyyy-MM-dd"),["PhamVi"]="HoaDon",
            ["KieuGiam"]="PhanTram",["GiaTri"]="10",["MucGiamToiDa"]="-1" },HttpStatusCode.OK);
        check(!await db.KhuyenMai.AnyAsync(x=>x.TenChuongTrinh=="TEST-INVALID-NEGATIVE"),"Negative caps rejected at the form boundary rather than by a SQL crash");
        await Post("/Voucher/Create",create,new() { ["Ma"]="TEST-AUTO-INVALID",["KhuyenMaiId"]=auto.Id.ToString(),
            ["GioiHanTongLuot"]="1",["DangSuDung"]="true" },HttpStatusCode.OK);
        check(!await db.Voucher.AnyAsync(x=>x.Ma=="TEST-AUTO-INVALID"),"Server rejects voucher for an automatic/disabled promotion");
        var voucherPromotion=new KhuyenMai {TenChuongTrinh="TEST-VOUCHER",BatDau=now.AddDays(-1),KetThuc=now.AddDays(1),
            PhamVi=PhamViUuDai.HoaDon,KieuGiam=KieuGiam.PhanTram,GiaTri=15,CanVoucher=true,ChoPhepKetHop=true};
        var voucher=new Voucher {Ma="TEST-LAST-USE",KhuyenMai=voucherPromotion,GioiHanTongLuot=1};
        var one=NewBill("TEST-VOUCHER-ONE",50000); var two=NewBill("TEST-VOUCHER-TWO",50000);
        db.Voucher.Add(voucher); db.HoaDon.AddRange(one,two); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var v1=await Version(one.Id); var v2=await Version(two.Id);
        async Task<bool> Reserve(int id,string version)
        {
            await using var context=new RestaurantDbContext(options);
            return (await new PromotionService(context).ApplyVoucherToInvoiceAsync(id,voucher.Ma,version)).Success;
        }
        var reservations=await Task.WhenAll(Reserve(one.Id,v1),Reserve(two.Id,v2));
        check(reservations.Count(x=>x)==1 && await service.GetVoucherUsageCountAsync(voucher.Id)==1,
            "Two concurrent invoices cannot reserve the last voucher use twice");
        var winner=reservations[0]?one.Id:two.Id;
        var staleReserve=await service.ApplyVoucherToInvoiceAsync(winner,voucher.Ma,reservations[0]?v1:v2);
        check(!staleReserve.Success,"Stale voucher submission does not overwrite invoice changes");
        db.ChangeTracker.Clear(); var anotherProgram=new KhuyenMai {TenChuongTrinh="TEST-OTHER-PROGRAM",BatDau=now.AddDays(-1),
            KetThuc=now.AddDays(1),PhamVi=PhamViUuDai.HoaDon,KieuGiam=KieuGiam.SoTien,GiaTri=1000,CanVoucher=true};
        db.KhuyenMai.Add(anotherProgram); await db.SaveChangesAsync();
        var edit=await Get($"/Voucher/Edit/{voucher.Id}");
        await Post($"/Voucher/Edit/{voucher.Id}",edit,new() { ["Id"]=voucher.Id.ToString(),["RowVersion"]=hidden(edit,"RowVersion"),
            ["Ma"]=voucher.Ma,["KhuyenMaiId"]=anotherProgram.Id.ToString(),["GioiHanTongLuot"]="1",["DangSuDung"]="true" },HttpStatusCode.OK);
        db.ChangeTracker.Clear();
        check((await db.Voucher.SingleAsync(x=>x.Id==voucher.Id)).KhuyenMaiId==voucherPromotion.Id,
            "Used/reserved voucher cannot be reassigned to another promotion");
        var parent=await db.KhuyenMai.SingleAsync(x=>x.Id==voucherPromotion.Id); parent.DangSuDung=false; await db.SaveChangesAsync();
        var disabled=await service.EvaluatePromotionsAsync(winner,voucher.Ma);
        check(disabled.AppliedVoucherId is null,"Disabled parent promotion invalidates a still-enabled voucher");
        var invalidPay=await orders.ProcessPaymentAsync(new PaymentFormModel {Id=winner,RowVersion=await Version(winner),TienKhachDua=50000},employeeId);
        check(invalidPay?.Contains("Voucher không còn đủ điều kiện")==true,"Payment does not silently consume an ineligible voucher");
        var removed=await service.RemoveVoucherFromInvoiceAsync(winner,await Version(winner));
        check(removed.Success && await service.GetVoucherUsageCountAsync(voucher.Id)==0,"Removing voucher releases the reserved use");

        var gift=new KhuyenMai {TenChuongTrinh="TEST-GIFT",BatDau=now.AddDays(-1),KetThuc=now.AddDays(1),PhamVi=PhamViUuDai.HoaDon,
            KieuGiam=KieuGiam.PhanTram,GiaTri=100};
        db.KhuyenMai.Add(gift); var free=NewBill("TEST-FREE",20000); db.HoaDon.Add(free); await db.SaveChangesAsync();
        var freeError=await orders.ProcessPaymentAsync(new PaymentFormModel {Id=free.Id,RowVersion=await Version(free.Id),
            SoTienPhaiThuDuKien=0,TienKhachDua=0},employeeId);
        db.ChangeTracker.Clear();
        check(freeError is null && (await db.HoaDon.SingleAsync(x=>x.Id==free.Id)).TrangThai==TrangThaiHoaDon.DaThanhToan,
            "Verified 100-percent promotion can settle a zero-amount invoice without bypassing payment validation");
        gift=await db.KhuyenMai.SingleAsync(x=>x.Id==gift.Id); gift.DangSuDung=false; await db.SaveChangesAsync();
        var manualFree=NewBill("TEST-MANUAL-FREE",20000); db.HoaDon.Add(manualFree); await db.SaveChangesAsync();
        var manualError=await orders.ProcessPaymentAsync(new PaymentFormModel {Id=manualFree.Id,RowVersion=await Version(manualFree.Id),
            TienGiamThuCong=20000,TienKhachDua=0},employeeId);
        check(manualError is not null,"Manual discount cannot bypass the existing no-free-invoice guard");

        var date=new DateTime(2030,5,7);
        var legacy=NewBill("TEST-REPORT-LEGACY",1000000); legacy.TrangThai=TrangThaiHoaDon.DaThanhToan;
        legacy.ThoiDiemLap=new DateTimeOffset(2030,5,6,8,0,0,TimeSpan.FromHours(7));
        legacy.ThoiDiemThanhToan=new DateTimeOffset(2030,5,6,17,0,0,TimeSpan.Zero);
        legacy.TienGiam=100000; legacy.TienCocDaTru=500000;
        var nextDay=NewBill("TEST-REPORT-NEXT-DAY",1000000); nextDay.TrangThai=TrangThaiHoaDon.DaThanhToan;
        nextDay.ThoiDiemThanhToan=new DateTimeOffset(2030,5,7,17,0,0,TimeSpan.Zero);
        db.HoaDon.AddRange(legacy,nextDay); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var reports=new BaoCaoService(db);
        var revenue=await reports.GetRevenueReportAsync("TheoNgay",date,date,null);
        check(revenue.TongDoanhThu==900000 && revenue.TongTienCocDaTru==500000 && revenue.DoanhThuTienMat==400000,
            "Revenue includes allocated deposit but payment-channel cash remains the additional collection");
        check(revenue.SoDonThanhCong==1 && revenue.TongSoDonHang==0 && revenue.ChiTietTimeline.Single().DoanhThuThuc==900000,
            "Vietnam midnight and payment-date reporting do not miscount creation dates or the next day");
        var top=await reports.GetTopDishesReportAsync(date,date,null,10);
        check(top.DanhSachMon.Sum(x=>x.SoLuongDaBan)==1,"Top dishes uses payment-date bounds and excludes next-day payment");
        var effectiveness=await reports.GetPromotionEffectivenessReportAsync(date,date);
        check(effectiveness.TongChiPhiKhuyenMai==0 && effectiveness.SoHoaDonThieuLichSu==1
            && effectiveness.DanhSachChuongTrinh.All(x=>x.SoLuotApDung==0),"Legacy/manual discount is not guessed into any campaign");
        var historical=await reports.GetPromotionEffectivenessReportAsync(now.ToOffset(TimeSpan.FromHours(7)).Date,now.ToOffset(TimeSpan.FromHours(7)).Date);
        check(historical.DanhSachChuongTrinh.Any(x=>x.TenChuongTrinh=="TEST-AUTO" && x.TongTienGiam==10000),
            "Report uses captured historical name and actual campaign discount, not current definitions or manual discount");
        using var warehouse=newClient(); var login=await warehouse.GetStringAsync("/admin");
        using var logged=await warehouse.PostAsync("/admin",new FormUrlEncodedContent(new Dictionary<string,string> {
            ["Email"]="kho.demo@example.test",["Password"]="Demo@2026!",["__RequestVerificationToken"]=hidden(login,"__RequestVerificationToken") }));
        check(logged.StatusCode==HttpStatusCode.Redirect,"Warehouse test account logs in");
        using var stock=await warehouse.GetAsync("/BaoCao/TonKho");
        using var denied=await warehouse.GetAsync("/BaoCao/DoanhThu");
        using var deniedExport=await warehouse.GetAsync("/BaoCao/XuatDoanhThuCsv");
        check(stock.StatusCode==HttpStatusCode.OK && denied.StatusCode==HttpStatusCode.Redirect && deniedExport.StatusCode==HttpStatusCode.Redirect,
            "Warehouse sees stock report without gaining business report/export permissions");
        using var longRange=await admin.GetAsync("/BaoCao/DoanhThu?tuNgay=2020-01-01&denNgay=2026-01-01");
        using var badYear=await admin.GetAsync("/BaoCao/DoanhThu?cheDo=TheoNam&nam=9999");
        check(longRange.StatusCode==HttpStatusCode.BadRequest && badYear.StatusCode==HttpStatusCode.BadRequest,
            "Invalid/extreme report filters cannot allocate unbounded day charts or overflow dates");
        await MigrationProof(connection,check);

        HoaDon NewBill(string code,decimal amount) => new() {MaHoaDon=code,ThoiDiemLap=now,NhanVienId=employeeId,
            TrangThai=TrangThaiHoaDon.ChuaThanhToan,TongTienHang=amount,ChiTiet=[new ChiTietHoaDon {MonAnId=dish.Id,
                TenMonLucBan=dish.TenMon,SoLuong=1,DonGia=amount,TrangThai=TrangThaiCheBien.DaPhucVu}]};
        string VersionEntity<T>(T entity) where T:class => Convert.ToBase64String(db.Entry(entity).Property<byte[]>("RowVersion").CurrentValue!);
        async Task<string> Version(int id) { db.ChangeTracker.Clear(); var row=await db.HoaDon.SingleAsync(x=>x.Id==id); return VersionEntity(row); }
        async Task<string> Get(string path) { using var response=await admin.GetAsync(path); check(response.StatusCode==HttpStatusCode.OK,"Promotion GET "+path); return await response.Content.ReadAsStringAsync(); }
        async Task Post(string path,string form,Dictionary<string,string> fields,HttpStatusCode expected)
        {
            fields["__RequestVerificationToken"]=hidden(form,"__RequestVerificationToken");
            using var response=await admin.PostAsync(path,new FormUrlEncodedContent(fields));
            check(response.StatusCode==expected,"Promotion POST "+path+": "+response.StatusCode);
        }
    }

    private static async Task MigrationProof(string original,Action<bool,string> check)
    {
        var connection=new SqlConnectionStringBuilder(original) {InitialCatalog="RestaurantMigrationTests_"+Guid.NewGuid().ToString("N")};
        await using var db=new RestaurantDbContext(new DbContextOptionsBuilder<RestaurantDbContext>().UseSqlServer(connection.ConnectionString).Options);
        try
        {
            var all=db.Database.GetMigrations().ToList(); var previous=all[^2];
            await db.GetService<IMigrator>().MigrateAsync(previous);
            await DbSeeder.SeedAsync(db); var employeeId=await db.NhanVien.Select(x=>x.Id).FirstAsync();
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [HoaDon] ([MaHoaDon],[ThoiDiemLap],[NhanVienId],[TrangThai],[TongTienHang],[TienGiam],[TienCocDaTru],[PhuongThucThanhToan],[TienKhachDua],[TienThoiLai],[LoaiDonHang]) VALUES ('MIGRATION-LEGACY',SYSDATETIMEOFFSET(),{employeeId},'DaThanhToan',1000000,100000,500000,'TienMat',400000,0,'TaiBan')");
            await db.Database.MigrateAsync();
            var legacy=await db.HoaDon.SingleAsync(x=>x.MaHoaDon=="MIGRATION-LEGACY");
            check(legacy.UuDaiSnapshot is null && legacy.TongThanhToan==400000 && legacy.TienGiam==100000 && legacy.TienCocDaTru==500000,
                "Additive nullable migration preserves existing invoice money and leaves unknown history null");
            await db.Database.MigrateAsync();
            check(await db.HoaDon.CountAsync()==1,"Migration retry does not duplicate or replace legacy data");
        }
        finally { SqlConnection.ClearAllPools(); await db.Database.EnsureDeletedAsync(); }
    }
}
