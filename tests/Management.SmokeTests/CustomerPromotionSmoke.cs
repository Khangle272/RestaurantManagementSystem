using System.Net;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Services;
using RestaurantManagement.Web.Models;

namespace Management.SmokeTests;

public static class CustomerPromotionSmoke
{
    public static async Task Run(RestaurantDbContext db, HttpClient admin, Func<HttpClient> newClient,
        Func<string,string,string> hidden, Action<bool,string> check, string connection)
    {
        await db.KhuyenMai.ExecuteUpdateAsync(x => x.SetProperty(p => p.DangSuDung, false)); db.ChangeTracker.Clear();
        var options = new DbContextOptionsBuilder<RestaurantDbContext>().UseSqlServer(connection).Options;
        var day = PromotionTime.Today; var now = DateTimeOffset.UtcNow;
        string Date(DateTime value) => value.ToString("yyyy-MM-ddTHH:mm:ss");
        check(PromotionTime.FromLocal(new DateTime(2026,10,25,0,0,0)) == new DateTimeOffset(2026,10,24,17,0,0,TimeSpan.Zero),"Vietnam wall clock is converted without using server timezone");
        check(PromotionTime.IsActive(now,now.AddDays(1),now) && PromotionTime.IsActive(now.AddDays(-1),now,now)
            && !PromotionTime.IsActive(now.AddTicks(1),now.AddDays(1),now),"Promotion interval includes both exact endpoints");
        var dishes = await db.MonAn.Take(2).ToListAsync(); var dish = dishes[0]; var other = dishes[1];
        var employee = await db.NhanVien.Where(x=>x.DangLamViec).Select(x=>x.Id).FirstAsync();
        var create = await Get(admin,"/KhuyenMai/Create");
        var fields = new Dictionary<string,string> { ["TenChuongTrinh"]="CUSTOMER-MULTI-CAMPAIGN", ["BatDau"]=Date(day), ["KetThuc"]=Date(day.AddDays(2)),
            ["CanVoucher"]="true",["DangSuDung"]="true",["ChoPhepKetHop"]="true",["GioiHanPhatHanh"]="5" };
        AddRow(fields,0,"CUSTOMER-PERCENT","HoaDon","PhanTram","10");
        AddRow(fields,1,"CUSTOMER-DISH","MonAn","PhanTram","50");
        fields["Vouchers[1].SelectedMonAnIds[0]"]=dish.Id.ToString(); fields["Vouchers[1].MucGiamToiDa"]="15000";
        AddRow(fields,2,"CUSTOMER-CASH","HoaDon","SoTien","50000"); fields["Vouchers[0].GioiHanPhatHanh"]="1";
        await Post(admin,"/KhuyenMai/Create",create,fields,HttpStatusCode.Redirect);
        var parent = await db.KhuyenMai.Include(x=>x.Vouchers).SingleAsync(x=>x.TenChuongTrinh=="CUSTOMER-MULTI-CAMPAIGN");
        check(parent.Vouchers.Count==3 && parent.CanVoucher,"One campaign is atomically created with three independently configured vouchers");
        var percent=parent.Vouchers.Single(x=>x.Ma=="CUSTOMER-PERCENT"); var scoped=parent.Vouchers.Single(x=>x.Ma=="CUSTOMER-DISH"); var cash=parent.Vouchers.Single(x=>x.Ma=="CUSTOMER-CASH");
        check(await db.VoucherMon.AnyAsync(x=>x.VoucherId==scoped.Id && x.MonAnId==dish.Id),"Dish selection is stored on the child voucher");
        var form=await Get(admin,$"/KhuyenMai/Edit/{parent.Id}");
        check(form.Contains("Vouchers[0].Ma") && form.Contains("Vouchers[2].Ma") && form.Contains("datetime-local"),"Campaign form shows all child codes with explicit local date/time controls");
        var bad=new Dictionary<string,string>(fields) { ["TenChuongTrinh"]="CUSTOMER-INVALID-ATOMIC" };
        bad["Vouchers[0].Ma"]="CUSTOMER-NEW-ATOMIC"; bad["Vouchers[1].Ma"]="CUSTOMER-INVALID-DISH"; bad["Vouchers[2].Ma"]="CUSTOMER-INVALID-CASH";
        bad["Vouchers[1].SelectedMonAnIds[0]"]="2147483647";
        await Post(admin,"/KhuyenMai/Create",create,bad,HttpStatusCode.OK);
        check(!await db.KhuyenMai.AnyAsync(x=>x.TenChuongTrinh=="CUSTOMER-INVALID-ATOMIC") && !await db.Voucher.AnyAsync(x=>x.Ma=="CUSTOMER-NEW-ATOMIC"),"An invalid child cannot leave a half-created campaign or sibling");
        using var customer=newClient(); using var stranger=newClient();
        await Register(customer,"voucher-owner@example.test","0909000001"); await Register(stranger,"voucher-other@example.test","0909000002");
        var owner=await db.KhachHang.SingleAsync(x=>x.Email=="voucher-owner@example.test"); var otherCustomer=await db.KhachHang.SingleAsync(x=>x.Email=="voucher-other@example.test");
        var bill=Bill("CUSTOMER-BILL",owner.Id); db.HoaDon.Add(bill); await db.SaveChangesAsync();
        var service=new PromotionService(db);
        check((await service.EvaluatePromotionsAsync(bill.Id,percent.Ma)).TongTienGiam==10000,"A newly created voucher applies immediately on its start day");
        check((await service.EvaluatePromotionsAsync(bill.Id,scoped.Ma)).TongTienGiam==15000,"A sibling dish voucher discounts only its own eligible dish and respects its own cap");
        check((await service.EvaluatePromotionsAsync(bill.Id,cash.Ma)).TongTienGiam==50000,"A cash sibling uses its own amount instead of the parent's legacy percentage");
        async Task<string?> Collect(int customerId,int voucherId) { await using var context=new RestaurantDbContext(options); return await new CustomerVoucherService(context,new PromotionService(context)).CollectAsync(customerId,voucherId); }
        var race=await Task.WhenAll(Collect(owner.Id,percent.Id),Collect(otherCustomer.Id,percent.Id));
        check(race.Count(x=>x is null)==1 && await db.VoucherKhachHang.CountAsync(x=>x.VoucherId==percent.Id)==1,"Two customers cannot collect the last issued ticket twice");
        // Give the owner a different, unlimited code through the authenticated endpoint.
        var catalog=await Get(customer,"/KhachHang/Vouchers");
        await Post(customer,"/KhachHang/Collect",catalog,new(){["id"]=cash.Id.ToString()},HttpStatusCode.Redirect);
        await Post(customer,"/KhachHang/Collect",catalog,new(){["id"]=cash.Id.ToString()},HttpStatusCode.Redirect);
        check(await db.VoucherKhachHang.CountAsync(x=>x.KhachHangId==owner.Id && x.VoucherId==cash.Id)==1,"Repeated collection is idempotent");
        check(await Collect(otherCustomer.Id,cash.Id) is null,"Unlimited child issuance accepts multiple customers");
        await Get(customer,"/KhachHang/Vouchers?wallet=true");
        var orderHtml=await Get(customer,$"/KhachHang/Order/{bill.Id}");
        check(orderHtml.Contains("CUSTOMER-CASH") && orderHtml.Contains("select name=\"voucherCode\"") && orderHtml.Contains("input name=\"voucherCode\""),"Customer checkout offers both wallet selection and direct code entry");
        await Post(customer,"/KhachHang/ApplyVoucher",orderHtml,new(){["id"]=bill.Id.ToString(),["voucherCode"]=cash.Ma,["rowVersion"]=hidden(orderHtml,"rowVersion")},HttpStatusCode.Redirect);
        db.ChangeTracker.Clear(); bill=await db.HoaDon.Include(x=>x.ChiTiet).SingleAsync(x=>x.Id==bill.Id);
        check(bill.VoucherId==cash.Id && bill.TienGiam==50000,"Customer can select a saved voucher on their own unpaid invoice");
        using(var denied=await stranger.GetAsync($"/KhachHang/Order/{bill.Id}")) check(denied.StatusCode==HttpStatusCode.NotFound,"Other customers cannot view an invoice by guessing its ID");
        using(var denied=await stranger.GetAsync($"/KhachHang/OrderStatus/{bill.Id}")) check(denied.StatusCode==HttpStatusCode.NotFound,"Live order status also checks invoice ownership");
        using(var live=await customer.GetAsync($"/KhachHang/OrderStatus/{bill.Id}")) check(live.StatusCode==HttpStatusCode.OK
            && (await live.Content.ReadAsStringAsync()).Contains("lines"),"Customer can poll the current status of their own dishes");
        var strangerForm=await Get(stranger,"/KhachHang/Vouchers");
        await Post(stranger,"/KhachHang/ApplyVoucher",strangerForm,new(){["id"]=bill.Id.ToString(),["voucherCode"]=cash.Ma,["rowVersion"]=Version(bill)},HttpStatusCode.NotFound);
        using(var denied=await customer.GetAsync($"/TaiKhoanKhachHang/Details/{otherCustomer.Id}")) check(denied.StatusCode==HttpStatusCode.Redirect,"Customers cannot read the management view of another customer");
        var second=Bill("CUSTOMER-SECOND",owner.Id); db.HoaDon.Add(second); await db.SaveChangesAsync();
        check((await service.EvaluatePromotionsAsync(second.Id,cash.Ma)).AppliedVoucherId is null,"One saved ticket cannot be held on two invoices at once");
        var staff=await Get(admin,$"/HoaDon/Details/{bill.Id}"); check(staff.Contains("CUSTOMER-CASH"),"Cashier invoice includes the customer's voucher selection");
        var orders=new OrderService(db);
        var version=Version(bill);
        var error=await orders.ProcessPaymentAsync(new PaymentFormModel {Id=bill.Id,RowVersion=version,TienKhachDua=50000,SoTienPhaiThuDuKien=50000},employee);
        check(error is null,"Voucher invoice settles through the authoritative payment service: "+error);
        db.ChangeTracker.Clear(); var points=await db.LichSuTichDiem.SingleAsync(x=>x.HoaDonId==bill.Id);
        check(points.SoDiem==5 && points.SoTienTinhDiem==50000,"Loyalty points are based on net purchase value after the voucher discount");
        check(await orders.ProcessPaymentAsync(new PaymentFormModel {Id=bill.Id,RowVersion=version,TienKhachDua=50000},employee) is not null
            && await db.LichSuTichDiem.CountAsync(x=>x.HoaDonId==bill.Id)==1,"Duplicate payment cannot grant points twice");
        var history=await Get(customer,"/KhachHang"); var managed=await Get(admin,$"/TaiKhoanKhachHang/Details/{owner.Id}");
        check(history.Contains("CUSTOMER-BILL") && managed.Contains("CUSTOMER-BILL") && history.Contains("10.000"),"Customer and administrator see order history and the loyalty ledger");
        // Direct entry consumes issuance capacity even when the code was never saved to a wallet.
        var directParent=new KhuyenMai {TenChuongTrinh="CUSTOMER-DIRECT-LIMIT",BatDau=now.AddDays(-1),KetThuc=now.AddDays(1),GiaTri=10,KieuGiam=KieuGiam.PhanTram,PhamVi=PhamViUuDai.HoaDon,CanVoucher=true,GioiHanPhatHanh=1};
        var directCode=new Voucher {Ma="CUSTOMER-DIRECT-CODE",KhuyenMai=directParent,GioiHanPhatHanh=1};
        var directBill=Bill("CUSTOMER-DIRECT-BILL",owner.Id);var directOther=Bill("CUSTOMER-DIRECT-OTHER",otherCustomer.Id);
        db.Voucher.Add(directCode);db.HoaDon.AddRange(directBill,directOther);await db.SaveChangesAsync();
        var directResult=await service.ApplyVoucherToInvoiceAsync(directBill.Id,directCode.Ma,Version(directBill));
        check(directResult.Success && await VoucherInventory.ProgramIssuedAsync(db,directParent.Id)==1,"Unsaved direct code reserves a ticket against the campaign issuance quota");
        check(!(await service.ApplyVoucherToInvoiceAsync(directOther.Id,directCode.Ma,Version(directOther))).Success,"Typing a code cannot bypass exhausted issuance quotas");
        check((await service.EvaluatePromotionsAsync(directBill.Id,directCode.Ma)).AppliedVoucherId==directCode.Id,"Re-evaluating the current direct reservation does not count its own ticket twice");
        check(await Collect(otherCustomer.Id,directCode.Id) is not null,"Collection and direct entry share one authoritative ticket count");
        check((await service.RemoveVoucherFromInvoiceAsync(directBill.Id,Version(directBill))).Success
            && await VoucherInventory.ProgramIssuedAsync(db,directParent.Id)==0,"Removing a directly entered code releases its ticket reservation");
        // A deposit pays part of the purchase; it must not reduce the loyalty earning base.
        var depositBill=Bill("CUSTOMER-DEPOSIT-POINTS",owner.Id);
        var booking=new DatBan {MaDatBan="CUSTOMER-DEPOSIT-BOOKING",KhachHangId=owner.Id,HoTenLienHe="Voucher customer",SoDienThoaiLienHe=owner.SoDienThoai,
            ThoiDiemTao=now,GioDen=now,GioKetThucDuKien=now.AddHours(3),ThoiDiemNhanBan=now,SoNguoiLon=2,TrangThai=TrangThaiDatBan.DaNhanBan,TienCocDaNop=50000,TrangThaiCoc=TrangThaiCoc.DaCoc};
        depositBill.DatBan=booking;db.HoaDon.Add(depositBill);await db.SaveChangesAsync();
        var applied=await service.ApplyVoucherToInvoiceAsync(depositBill.Id,scoped.Ma,Version(depositBill));check(applied.Success,"Unsaved direct dish voucher is accepted");
        var depositError=await orders.ProcessPaymentAsync(new PaymentFormModel {Id=depositBill.Id,RowVersion=Version(depositBill),BookingRowVersion=Version(booking),TienKhachDua=35000,SoTienPhaiThuDuKien=35000},employee);
        check(depositError is null,"Deposit and child dish voucher settle together: "+depositError);
        var depositPoints=await db.LichSuTichDiem.SingleAsync(x=>x.HoaDonId==depositBill.Id);
        check(depositPoints.SoDiem==8 && depositPoints.SoTienTinhDiem==85000,"Points include the portion paid by deposit and round down the net 85,000 purchase");
        directParent.DangSuDung=false;await db.SaveChangesAsync();
        var parentLimited=new KhuyenMai {TenChuongTrinh="CUSTOMER-PARENT-LIMIT",BatDau=now.AddDays(-1),KetThuc=now.AddDays(1),GiaTri=10,KieuGiam=KieuGiam.PhanTram,PhamVi=PhamViUuDai.HoaDon,CanVoucher=true,GioiHanPhatHanh=1};
        var a=new Voucher {Ma="CUSTOMER-LIMIT-A",KhuyenMai=parentLimited};var b=new Voucher {Ma="CUSTOMER-LIMIT-B",KhuyenMai=parentLimited};
        db.Voucher.AddRange(a,b); await db.SaveChangesAsync();
        var parentRace=await Task.WhenAll(Collect(owner.Id,a.Id),Collect(otherCustomer.Id,b.Id));
        check(parentRace.Count(x=>x is null)==1 && await db.VoucherKhachHang.CountAsync(x=>x.Voucher.KhuyenMaiId==parentLimited.Id)==1,"Sibling codes share the parent's issuance ceiling under concurrency");
        // Edit all sibling rows together, then reject a stale submission of the same form.
        db.ChangeTracker.Clear(); parent=await db.KhuyenMai.Include(x=>x.Vouchers).SingleAsync(x=>x.Id==parent.Id);
        var editHtml=await Get(admin,$"/KhuyenMai/Edit/{parent.Id}");
        var edits=new Dictionary<string,string>(fields) { ["Id"]=parent.Id.ToString(),["RowVersion"]=hidden(editHtml,"RowVersion") };
        var childRows=parent.Vouchers.OrderBy(x=>x.Id).ToList();
        for(var i=0;i<childRows.Count;i++) {edits[$"Vouchers[{i}].Id"]=childRows[i].Id.ToString();edits[$"Vouchers[{i}].RowVersion"]=Version(childRows[i]);}
        edits["Vouchers[2].GiaTri"]="10000";
        await Post(admin,$"/KhuyenMai/Edit/{parent.Id}",editHtml,edits,HttpStatusCode.Redirect);
        db.ChangeTracker.Clear();check((await db.Voucher.SingleAsync(x=>x.Id==cash.Id)).GiaTri==10000
            && await db.VoucherMon.CountAsync(x=>x.VoucherId==scoped.Id)==1,"Editing sibling discount terms preserves child dish links in one save");
        await Post(admin,$"/KhuyenMai/Edit/{parent.Id}",editHtml,edits,HttpStatusCode.OK);
        check((await service.EvaluatePromotionsAsync(bill.Id,cash.Ma)).TongTienGiam==50000,"Changing current child terms does not rewrite the paid invoice snapshot");
        editHtml=await Get(admin,$"/KhuyenMai/Edit/{parent.Id}");
        parent=await db.KhuyenMai.Include(x=>x.Vouchers).SingleAsync(x=>x.Id==parent.Id);
        edits["RowVersion"]=hidden(editHtml,"RowVersion");childRows=parent.Vouchers.OrderBy(x=>x.Id).ToList();
        for(var i=0;i<childRows.Count;i++) edits[$"Vouchers[{i}].RowVersion"]=Version(childRows[i]);
        edits["Vouchers[2].Xoa"]="true";
        foreach(var key in edits.Keys.Where(x=>x.StartsWith("Vouchers[2].") && !new[]{"Vouchers[2].Id","Vouchers[2].RowVersion","Vouchers[2].Xoa"}.Contains(x)).ToList()) edits.Remove(key);
        await Post(admin,$"/KhuyenMai/Edit/{parent.Id}",editHtml,edits,HttpStatusCode.Redirect);
        db.ChangeTracker.Clear();check(!((await db.Voucher.SingleAsync(x=>x.Id==cash.Id)).DangSuDung)
            && await db.VoucherKhachHang.AnyAsync(x=>x.VoucherId==cash.Id),"Removing an issued/used child archives it without deleting wallet or purchase history");
        // Independent child and parent time gates, including exact boundary checks with a deterministic clock.
        db.ChangeTracker.Clear(); percent=await db.Voucher.SingleAsync(x=>x.Id==percent.Id); percent.GioiHanTongLuot=null;
        percent.GioiHanPhatHanh=null;percent.BatDau=now;percent.KetThuc=now.AddHours(1);await db.SaveChangesAsync();
        check((await new PromotionService(db,new FixedClock(now)).EvaluatePromotionsAsync(second.Id,percent.Ma)).AppliedVoucherId==percent.Id,"A child voucher is valid exactly at its start instant");
        check((await new PromotionService(db,new FixedClock(now.AddHours(1))).EvaluatePromotionsAsync(second.Id,percent.Ma)).AppliedVoucherId==percent.Id,"A child voucher is valid exactly at its end instant");
        check((await new PromotionService(db,new FixedClock(now.AddHours(1).AddTicks(1))).EvaluatePromotionsAsync(second.Id,percent.Ma)).AppliedVoucherId is null,"A child voucher expires immediately after its own end");
        parent=await db.KhuyenMai.SingleAsync(x=>x.Id==parent.Id);parent.DangSuDung=false;await db.SaveChangesAsync();
        check((await service.EvaluatePromotionsAsync(second.Id,scoped.Ma)).AppliedVoucherId is null,"Disabling the parent invalidates every enabled child");
        var paid=await service.EvaluatePromotionsAsync(bill.Id,cash.Ma);check(paid.TongTienGiam==50000,"Paid invoice retains the captured voucher amount after its parent is disabled");
        var profileForm=await Get(admin,"/TaiKhoanKhachHang/Create");
        await Post(admin,"/TaiKhoanKhachHang/Create",profileForm,new(){["HoTen"]="CUSTOMER-WALK-IN",["SoDienThoai"]="0909000003",["DangSuDung"]="true"},HttpStatusCode.Redirect);
        var walkIn=await db.KhachHang.SingleAsync(x=>x.SoDienThoai=="0909000003");
        check(walkIn.TaiKhoanId is null,"Management creates a customer profile without inventing a login account");
        await Get(admin,$"/TaiKhoanKhachHang/Details/{walkIn.Id}");
        var walkInBill=Bill("CUSTOMER-WALK-IN-BILL",owner.Id);walkInBill.KhachHangId=null;db.HoaDon.Add(walkInBill);await db.SaveChangesAsync();
        var invoiceForm=await Get(admin,$"/HoaDon/Details/{walkInBill.Id}");
        await Post(admin,"/HoaDon/SetCustomer",invoiceForm,new(){["id"]=walkInBill.Id.ToString(),["phone"]=walkIn.SoDienThoai,["rowVersion"]=hidden(invoiceForm,"RowVersion")},HttpStatusCode.Redirect);
        db.ChangeTracker.Clear();check((await db.HoaDon.SingleAsync(x=>x.Id==walkInBill.Id)).KhachHangId==walkIn.Id,"Cashier can explicitly link a walk-in invoice to a customer profile before checkout");
        var profiles=await Get(admin,"/TaiKhoanKhachHang?search=CUSTOMER-WALK-IN");check(profiles.Contains("CUSTOMER-WALK-IN"),"Management includes customers who have no login account");
        var accountForm=await Get(admin,$"/TaiKhoanKhachHang/CreateAccount/{walkIn.Id}");
        await Post(admin,"/TaiKhoanKhachHang/CreateAccount",accountForm,new(){["Id"]=walkIn.Id.ToString(),["RowVersion"]=hidden(accountForm,"RowVersion"),
            ["Email"]="walkin-login@example.test",["Password"]="TestOnly123!",["ConfirmPassword"]="TestOnly123!"},HttpStatusCode.Redirect);
        db.ChangeTracker.Clear();check((await db.KhachHang.SingleAsync(x=>x.Id==walkIn.Id)).TaiKhoanId.HasValue
            && await db.KhachHang.CountAsync(x=>x.SoDienThoai==walkIn.SoDienThoai)==1,"Provisioning login reuses the original customer profile and preserves its history");
        using var walkInClient=newClient();
        var loginForm=await Get(walkInClient,"/Account/Login");
        await Post(walkInClient,"/Account/Login",loginForm,new(){["Email"]="walkin-login@example.test",["Password"]="TestOnly123!"},HttpStatusCode.Redirect);
        check((await Get(walkInClient,"/KhachHang")).Contains("CUSTOMER-WALK-IN-BILL"),"Customer can log in to the provisioned account and see their pre-existing order");
        parentLimited.DangSuDung=false;await db.SaveChangesAsync();

        void AddRow(Dictionary<string,string> target,int i,string code,string scope,string type,string amount) {
            var prefix=$"Vouchers[{i}].";target[prefix+"Ma"]=code;target[prefix+"PhamVi"]=scope;target[prefix+"KieuGiam"]=type;target[prefix+"GiaTri"]=amount;
            target[prefix+"BatDau"]=Date(day);target[prefix+"KetThuc"]=Date(day.AddDays(2));target[prefix+"DangSuDung"]="true";
        }
        HoaDon Bill(string code,int customerId) => new() {MaHoaDon=code,KhachHangId=customerId,NhanVienId=employee,ThoiDiemLap=now,TrangThai=TrangThaiHoaDon.ChuaThanhToan,TongTienHang=100000,
            ChiTiet=[new ChiTietHoaDon {MonAnId=dish.Id,TenMonLucBan=dish.TenMon,SoLuong=1,DonGia=40000,TrangThai=TrangThaiCheBien.DaPhucVu},new ChiTietHoaDon {MonAnId=other.Id,TenMonLucBan=other.TenMon,SoLuong=1,DonGia=60000,TrangThai=TrangThaiCheBien.DaPhucVu}]};
        string Version<T>(T entity) where T:class => Convert.ToBase64String(db.Entry(entity).Property<byte[]>("RowVersion").CurrentValue!);
        async Task Register(HttpClient client,string email,string phone) {
            var form=await Get(client,"/Account/Register"); await Post(client,"/Account/Register",form,new(){["HoTen"]="Voucher customer",["Email"]=email,["SoDienThoai"]=phone,["Password"]="TestOnly123!",["ConfirmPassword"]="TestOnly123!"},HttpStatusCode.Redirect);
        }
        async Task<string> Get(HttpClient client,string path) {using var response=await client.GetAsync(path);check(response.StatusCode==HttpStatusCode.OK,"Customer promotion GET "+path);return await response.Content.ReadAsStringAsync();}
        async Task Post(HttpClient client,string path,string form,Dictionary<string,string> data,HttpStatusCode expected) {
            data["__RequestVerificationToken"]=hidden(form,"__RequestVerificationToken");using var response=await client.PostAsync(path,new FormUrlEncodedContent(data));
            var body=await response.Content.ReadAsStringAsync();check(response.StatusCode==expected,"Customer promotion POST "+path+": "+response.StatusCode+(response.StatusCode!=expected ? " "+body[..Math.Min(body.Length,300)] : ""));
        }
    }
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow()=>now; }
}
