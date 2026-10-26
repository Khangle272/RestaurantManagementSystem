using System.Data;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.API.Data;
using RestaurantManagement.API.Models;
using RestaurantManagement.Web.Security;

namespace RestaurantManagement.Web.Services;

/// <summary>Explicit Development-only synthetic dataset. Existing records are never reset.</summary>
public static class WebsiteDemoSetup
{
    public const string Marker = "Dữ liệu demo toàn website; không phải giao dịch thực tế.";
    public const string AreaName = "Sảnh demo website";

    public static async Task InitializeAsync(RestaurantDbContext db)
    {
        await DbSeeder.SeedAsync(db);
        var now = DateTimeOffset.UtcNow;
        var today = PromotionTime.Local(now).Date;
        var prefix = $"WEB-{today:yyyyMMdd}";
        var month = new DateTime(today.Year, today.Month, 1);
        var customer = await db.KhachHang.SingleAsync(x => x.TaiKhoan != null && x.TaiKhoan.Email == "khach.demo@example.test");
        var cashier = await db.NhanVien.SingleAsync(x => x.MaNhanVien == "DEMO-TN");
        var reception = await db.NhanVien.SingleAsync(x => x.MaNhanVien == "DEMO-TT");
        var warehouse = await db.NhanVien.SingleAsync(x => x.MaNhanVien == "DEMO-KHO");
        var cashierAccount = cashier.TaiKhoanId!.Value;
        var dishes = await db.MonAn.Include(x => x.Sizes).Where(x => x.TenMon == "Gỏi ngó sen tôm thịt"
            || x.TenMon == "Bò lúc lắc" || x.TenMon == "Nước cam").ToListAsync();
        var sizes = new[] { "Gỏi ngó sen tôm thịt", "Bò lúc lắc", "Nước cam" }
            .Select(name => dishes.Single(x => x.TenMon == name).Sizes.Where(s => s.DangSuDung).OrderBy(s => s.GiaBan).First()).ToList();

        var customers = new List<KhachHang> { customer };
        foreach (var (name, phone, email) in new[] {
            ("Nguyễn Minh Anh (demo)", "0988800001", "minhanh.website@example.test"),
            ("Trần Hoàng Nam (demo)", "0988800002", "hoangnam.website@example.test"),
            ("Lê Thu Hà (demo)", "0988800003", "thuha.website@example.test") })
        {
            var existing = await db.KhachHang.SingleOrDefaultAsync(x => x.SoDienThoai == phone);
            if (existing is null) { existing = new KhachHang { HoTen = name, SoDienThoai = phone, Email = email, NgayDangKy = now.AddDays(-30) }; db.KhachHang.Add(existing); }
            else if (existing.Email != email) throw new InvalidOperationException("SĐT demo đã thuộc hồ sơ khác; không tự liên kết.");
            customers.Add(existing);
        }
        await db.SaveChangesAsync();

        await StockAsync(db, prefix, warehouse.Id, today, now);
        var programName = $"Demo toàn website · {month:MM/yyyy}";
        var program = await db.KhuyenMai.Include(x => x.Vouchers).ThenInclude(x => x.MonApDung).SingleOrDefaultAsync(x => x.TenChuongTrinh == programName);
        if (program is null)
        {
            program = new KhuyenMai { TenChuongTrinh = programName, BatDau = PromotionTime.FromLocal(month.AddMonths(-1)),
                KetThuc = PromotionTime.FromLocal(month.AddMonths(2).AddSeconds(-1)), CanVoucher = true,
                KieuGiam = KieuGiam.PhanTram, PhamVi = PhamViUuDai.HoaDon, GiaTri = 10, GioiHanPhatHanh = 5000, ThuTuApDung = 1 };
            foreach (var (suffix, type, value, minimum, cap, scope) in new[] {
                ("P10", KieuGiam.PhanTram, 10m, 100000m, (decimal?)50000m, PhamViUuDai.HoaDon),
                ("10K", KieuGiam.SoTien, 10000m, 50000m, (decimal?)null, PhamViUuDai.HoaDon),
                ("50K", KieuGiam.SoTien, 50000m, 300000m, (decimal?)null, PhamViUuDai.HoaDon),
                ("MON20", KieuGiam.PhanTram, 20m, 0m, (decimal?)50000m, PhamViUuDai.MonAn) })
            {
                var code = $"WEB{month:yyMM}-{suffix}";
                if (await db.Voucher.AnyAsync(x => x.Ma == code)) throw new InvalidOperationException($"Mã {code} đã thuộc chương trình khác.");
                var voucher = new Voucher { Ma = code, BatDau = program.BatDau, KetThuc = program.KetThuc, PhamVi = scope,
                    KieuGiam = type, GiaTri = value, GiaTriToiThieu = minimum, MucGiamToiDa = cap,
                    GioiHanPhatHanh = suffix == "10K" ? null : 1000, GioiHanTongLuot = suffix == "10K" ? null : 1000 };
                if (scope == PhamViUuDai.MonAn) voucher.MonApDung.Add(new VoucherMon { MonAnId = sizes[0].MonAnId });
                program.Vouchers.Add(voucher);
            }
            db.KhuyenMai.Add(program); await db.SaveChangesAsync();
        }
        var cash = program.Vouchers.Single(x => x.Ma.EndsWith("-50K"));
        var scoped = program.Vouchers.Single(x => x.Ma.EndsWith("-MON20"));
        var promotions = new PromotionService(db);
        var wallet = new CustomerVoucherService(db, promotions);
        foreach (var voucher in program.Vouchers.Where(x => x.Ma.EndsWith("-P10") || x.Ma.EndsWith("-10K")))
        {
            var error = await wallet.CollectAsync(customer.Id, voucher.Id);
            if (error is not null) throw new InvalidOperationException(error);
        }
        // Separate expired/scheduled campaigns make the management status filters demonstrable.
        foreach (var (suffix, start, end) in new[] { ("Hết hạn", month.AddMonths(-2), month.AddMonths(-1)), ("Sắp diễn ra", month.AddMonths(2), month.AddMonths(3)) })
        {
            var name = $"Demo {suffix} · {month:MM/yyyy}";
            if (!await db.KhuyenMai.AnyAsync(x => x.TenChuongTrinh == name))
                db.KhuyenMai.Add(new KhuyenMai { TenChuongTrinh = name, BatDau = PromotionTime.FromLocal(start), KetThuc = PromotionTime.FromLocal(end),
                    CanVoucher = true, GiaTri = 10, KieuGiam = KieuGiam.PhanTram, PhamVi = PhamViUuDai.HoaDon });
        }
        await db.SaveChangesAsync();

        // Four weeks of actual linked synthetic invoices, discount snapshots, points and reviews.
        for (var day = 1; day <= 28; day++)
        {
            var date = today.AddDays(-day).AddHours(day % 2 == 0 ? 12 : 19);
            var buyer = customers[(day - 1) % customers.Count];
            await BillAsync(db, $"WEB-H-{date:yyyyMMdd}", buyer, cashier.Id, sizes, PromotionTime.FromLocal(date), true,
                day % 3 == 0 ? cash : day % 3 == 1 ? scoped : null, day % 3, null);
        }
        var minutesToday = PromotionTime.Local(now).TimeOfDay.TotalMinutes;
        await BillAsync(db, $"WEB-H-{today:yyyyMMdd}", customer, cashier.Id, sizes,
            now.AddMinutes(-Math.Min(60, minutesToday / 2)), true, cash, 0, null);

        var area = await db.KhuVuc.SingleOrDefaultAsync(x => x.TenKhuVuc == AreaName);
        if (area is null) { area = new KhuVuc { TenKhuVuc = AreaName, Tang = 1 }; db.KhuVuc.Add(area); await db.SaveChangesAsync(); }
        var tables = new List<BanAn>();
        for (var i = 1; i <= 6; i++)
        {
            var code = $"WEB-{i:00}";
            var table = await db.BanAn.SingleOrDefaultAsync(x => x.MaBan == code);
            if (table is null) { table = new BanAn { MaBan = code, KhuVuc = area, SoChoNgoi = i == 6 ? 8 : 4 }; db.BanAn.Add(table); }
            else if (table.KhuVucId != area.Id) throw new InvalidOperationException($"Bàn {code} đã thuộc khu vực khác.");
            tables.Add(table);
        }
        await db.SaveChangesAsync();

        for (var i = 0; i < 2; i++)
        {
            var code = $"{prefix}-BAN{i + 1}";
            var booking = await db.DatBan.SingleOrDefaultAsync(x => x.MaDatBan == code);
            if (booking is null)
            {
                // Never reclaim a table occupied by another party, including on a later daily demo run.
                if (tables[i].TrangThai != TrangThaiBan.SanSang) continue;
                booking = NewBooking(code, customer, now.AddMinutes(-25), reception.Id);
                booking.TrangThai = TrangThaiDatBan.DaNhanBan; booking.ThoiDiemNhanBan = now.AddMinutes(-20);
                booking.LaKhachTrucTiep = true; booking.Ban.Add(new ChiTietDatBan { BanAn = tables[i] });
                tables[i].TrangThai = TrangThaiBan.DangPhucVu;
                if (i == 1) { booking.TienCocYeuCau = booking.TienCocDaNop = 100000; booking.YeuCauCoc = true; booking.TrangThaiCoc = TrangThaiCoc.DaCoc; booking.ThoiDiemCoc = now.AddHours(-1); }
                db.DatBan.Add(booking); await db.SaveChangesAsync();
                if (i == 1) { AddDeposit(db, booking, cashierAccount, 100000, now.AddHours(-1)); await db.SaveChangesAsync(); }
            }
            if (booking.TrangThai == TrangThaiDatBan.DaNhanBan && booking.ThoiDiemKetThuc is null)
                await BillAsync(db, $"{prefix}-POS{i + 1}", customer, cashier.Id, sizes, now.AddMinutes(-15), false, null, i == 0 ? 3 : 4, booking);
        }
        await BillAsync(db, $"{prefix}-MANGDI", customer, cashier.Id, sizes, now, false, null, 3, null);
        await BillAsync(db, $"{prefix}-GIAOHANG", customers[1], cashier.Id, sizes, now, false, null, 5, null);

        for (var i = 0; i < 7; i++)
        {
            var code = $"{prefix}-LICH{i + 1}";
            if (await db.DatBan.AnyAsync(x => x.MaDatBan == code)) continue;
            var arrival = PromotionTime.FromLocal(today.AddDays(i == 4 ? 2 : 1).AddHours(i % 2 == 0 ? 11.5 : 18.5));
            var booking = NewBooking(code, customer, arrival, reception.Id);
            booking.KhuVucUuTien = area;
            if (i == 0) booking.TrangThai = TrangThaiDatBan.ChoXacNhan;
            if (i is 1 or 2)
            {
                booking.TrangThai = TrangThaiDatBan.ChoCoc; ReservationDepositPolicy.Apply(booking, 0);
                if (i == 2) { booking.ThoiDiemBaoChuyenKhoan = now; booking.SoTienKhachBao = booking.TienCocYeuCau; booking.MaGiaoDichKhachBao = $"DEMO-{today:MMdd}-COC"; }
            }
            if (i is 3 or 4)
            {
                booking.TrangThai = TrangThaiDatBan.DaXacNhan; booking.Ban.Add(new ChiTietDatBan { BanAn = tables[i] });
                foreach (var size in sizes.Take(2)) booking.MonDatTruoc.Add(new MonDatTruoc { MonAnId = size.MonAnId, MonAnSizeId = size.Id,
                    TenMonLucDat = size.MonAn.TenMon, TenSizeLucDat = size.TenSize, SoLuong = 2, DonGiaThoaThuan = size.GiaBan, YeuCauCheBien = "Ít cay, nước sốt để riêng" });
                booking.YeuCauCoc = booking.CocTuDong = true;
                booking.TienCocYeuCau = booking.TienCocDaNop = ReservationDepositPolicy.Calculate(booking.MonDatTruoc.Sum(x => x.SoLuong * x.DonGiaThoaThuan));
                booking.TrangThaiCoc = TrangThaiCoc.DaCoc; booking.ThoiDiemCoc = now;
            }
            if (i == 5) { booking.TrangThai = TrangThaiDatBan.DaHuy; booking.ThoiDiemHuy = now; booking.LyDoHuy = "Khách đổi kế hoạch (demo)"; booking.NhanVienHuyId = reception.Id; }
            if (i == 6) { booking.TrangThai = TrangThaiDatBan.KhongDen; booking.GioDen = PromotionTime.FromLocal(today.AddDays(-1).AddHours(18)); booking.GioKetThucDuKien = booking.GioDen.AddHours(3); }
            db.DatBan.Add(booking); await db.SaveChangesAsync();
            if (booking.TienCocDaNop > 0) { AddDeposit(db, booking, cashierAccount, booking.TienCocDaNop, now); await db.SaveChangesAsync(); }
        }
        if (!await db.MonAn.AnyAsync(x => x.TenMon == "Salad rau sốt mè (chờ duyệt demo)"))
            db.MonAn.Add(new MonAn { TenMon = "Salad rau sốt mè (chờ duyệt demo)", DanhMucId = dishes[0].DanhMucId,
                DaDuyet = false, Loai = LoaiMon.MonLe, TrangThai = TrangThaiMon.DangPhucVu, MoTa = Marker,
                Sizes = [new MonAnSize { TenSize = "Phần", GiaBan = 69000 }] });
        await db.SaveChangesAsync();
        Console.WriteLine($"Đã chuẩn bị {prefix}; chương trình {programName}.");
        Console.WriteLine("Tài khoản demo đã sẵn sàng. Xem DEMO_WEBSITE.md để chạy từng luồng.");
    }

    private static DatBan NewBooking(string code, KhachHang customer, DateTimeOffset arrival, int staffId) => new() {
        MaDatBan = code, KhachHangId = customer.Id, HoTenLienHe = customer.HoTen, SoDienThoaiLienHe = customer.SoDienThoai,
        ThoiDiemTao = DateTimeOffset.UtcNow, GioDen = arrival, GioKetThucDuKien = arrival.AddHours(3),
        SoNguoiLon = 4, NhanVienTiepNhanId = staffId, YeuCau = Marker
    };

    private static void AddDeposit(RestaurantDbContext db, DatBan booking, int actorId, decimal amount, DateTimeOffset at) =>
        db.GiaoDichCoc.Add(new GiaoDichCoc { DatBanId = booking.Id, TaiKhoanXuLyId = actorId, Loai = LoaiGiaoDichCoc.Thu,
            SoTien = amount, ThoiDiem = at, YeuCauId = Guid.NewGuid(), MaThamChieu = booking.MaDatBan, LyDo = Marker });

    private static async Task BillAsync(RestaurantDbContext db, string code, KhachHang customer, int staffId, List<MonAnSize> sizes,
        DateTimeOffset at, bool paid, Voucher? voucher, int variant, DatBan? booking)
    {
        if (await db.HoaDon.AnyAsync(x => x.MaHoaDon == code)) return;
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var bill = new HoaDon { MaHoaDon = code, ThoiDiemLap = at, KhachHangId = customer.Id, NhanVienId = staffId,
            DatBanId = booking?.Id, LoaiDonHang = booking is not null ? LoaiDonHang.TaiBan : variant == 5 ? LoaiDonHang.GiaoHang : LoaiDonHang.MangDi,
            TenNguoiNhan = customer.HoTen, SoDienThoaiNhan = customer.SoDienThoai,
            DiaChiGiaoHang = variant == 5 ? "Địa chỉ nhận hàng mẫu · Quận 1, TP.HCM" : null, GhiChuDonHang = Marker };
        for (var i = 0; i < sizes.Count; i++)
        {
            var size = sizes[i];
            bill.ChiTiet.Add(new ChiTietHoaDon { MonAnId = size.MonAnId, MonAnSizeId = size.Id, TenMonLucBan = size.MonAn.TenMon,
                TenSizeLucBan = size.TenSize, SoLuong = i == 0 ? 2 : 1, DonGia = size.GiaBan, ThoiDiemGoi = at,
                YeuCauCheBien = i == 0 ? "Ít cay, nước sốt để riêng" : null,
                TrangThai = paid || variant == 4 ? TrangThaiCheBien.DaPhucVu : i == 0 ? TrangThaiCheBien.DangCheBien : i == 1 ? TrangThaiCheBien.SanSang : TrangThaiCheBien.ChoCheBien });
        }
        bill.TongTienHang = bill.ChiTiet.Sum(x => x.SoLuong * x.DonGia);
        db.HoaDon.Add(bill); await db.SaveChangesAsync();
        var stock = await new InventoryService(db).DeductInventoryForOrderDishesAsync(bill.Id,
            bill.ChiTiet.Select(x => new Models.OrderItemInputModel { MonAnId = x.MonAnId, MonAnSizeId = x.MonAnSizeId!.Value, SoLuong = x.SoLuong }).ToList(), staffId);
        if (!stock.Success) throw new InvalidOperationException("Thiếu tồn kho cho dữ liệu demo: " + string.Join("; ", stock.Warnings));
        if (stock.PhieuXuatId.HasValue) {
            var output = await db.PhieuXuat.SingleAsync(x => x.Id == stock.PhieuXuatId.Value); output.ThoiDiem = at; output.GhiChu = Marker + " " + code;
        }
        var promotions = new PromotionService(db);
        await promotions.LockVoucherAsync(voucher?.Id);
        var evaluation = await promotions.EvaluatePromotionsAsync(bill.Id, voucher?.Ma);
        if (!evaluation.IsSuccess || (voucher is not null && evaluation.AppliedVoucherId != voucher.Id))
            throw new InvalidOperationException("Voucher demo không đủ điều kiện: " + evaluation.ErrorMessage + string.Join("; ", evaluation.IneligiblePromotions.Select(x => x.LyDoKhongDuDieuKien)));
        bill.TienGiam = evaluation.TongTienGiam; bill.VoucherId = evaluation.AppliedVoucherId;
        bill.UuDaiSnapshot = PromotionService.CaptureSnapshot(evaluation);
        if (paid)
        {
            var current = DateTimeOffset.UtcNow;
            bill.TrangThai = TrangThaiHoaDon.DaThanhToan; bill.ThoiDiemThanhToan = at.AddMinutes(45) > current ? current : at.AddMinutes(45);
            bill.PhuongThucThanhToan = (PhuongThucThanhToan)(variant % 3);
            bill.TienKhachDua = bill.TongTienHang - bill.TienGiam;
            if (bill.PhuongThucThanhToan != PhuongThucThanhToan.TienMat) bill.MaGiaoDich = "DEMO-" + code;
            CustomerHistoryService.EarnPoints(db, bill);
            db.DanhGia.Add(new DanhGia { HoaDon = bill, KhachHangId = customer.Id, Diem = variant == 0 ? 4 : 5,
                NoiDung = variant == 0 ? "Món ngon, phục vụ chu đáo (đánh giá mẫu)." : "Không gian dễ chịu, món ăn hợp khẩu vị (đánh giá mẫu).", ThoiDiem = bill.ThoiDiemThanhToan.Value.AddMinutes(5) > current ? current : bill.ThoiDiemThanhToan.Value.AddMinutes(5) });
        }
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }

    private static async Task StockAsync(RestaurantDbContext db, string prefix, int staffId, DateTime today, DateTimeOffset now)
    {
        var code = prefix + "-NHAP";
        if (await db.PhieuNhap.AnyAsync(x => x.MaPhieu == code)) return;
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var supplier = await db.NhaCungCap.Where(x => x.DangSuDung).OrderBy(x => x.Id).FirstAsync();
        var ingredients = await db.NguyenLieu.Where(x => x.DangSuDung).ToListAsync();
        var receipt = new PhieuNhap { MaPhieu = code, NhaCungCapId = supplier.Id, NhanVienId = staffId, ThoiDiem = PromotionTime.FromLocal(today.AddDays(-30).AddHours(8)),
            TrangThai = TrangThaiPhieu.DaGhiSo, LyDo = LyDoNhap.MuaHang, GhiChu = Marker };
        foreach (var ingredient in ingredients)
        {
            var unit = ingredient.DonViTinh.Trim().ToLowerInvariant();
            var quantity = unit is "g" or "ml" ? 100000m : unit is "kg" or "l" or "lít" ? 100m : 500m;
            var price = ingredient.DonGia > 0 ? ingredient.DonGia : unit is "g" or "ml" ? 50m : 5000m;
            receipt.ChiTiet.Add(new ChiTietPhieuNhap { NguyenLieuId = ingredient.Id, SoLuong = quantity, DonGia = price,
                ThanhTien = quantity * price, MaLo = $"DEMO-{today:yyyyMMdd}-{ingredient.Id}", HanSuDung = DateOnly.FromDateTime(today.AddMonths(2)) });
            ingredient.SoLuongTon += quantity;
            if (ingredient.DonGia <= 0) ingredient.DonGia = price;
        }
        receipt.TongTien = receipt.ChiTiet.Sum(x => x.ThanhTien); db.PhieuNhap.Add(receipt);
        var draft = new PhieuNhap { MaPhieu = prefix + "-NHAPNHAP", NhanVienId = staffId, NhaCungCapId = supplier.Id,
            ThoiDiem = now, TrangThai = TrangThaiPhieu.Nhap, LyDo = LyDoNhap.MuaHang, GhiChu = "Phiếu nháp để demo ghi sổ. " + Marker };
        var first = ingredients[0];
        draft.ChiTiet.Add(new ChiTietPhieuNhap { NguyenLieuId = first.Id, SoLuong = 100, DonGia = first.DonGia, ThanhTien = 100 * first.DonGia });
        draft.TongTien = draft.ChiTiet.Sum(x => x.ThanhTien); db.PhieuNhap.Add(draft);
        var disposal = new PhieuThanhLy { MaPhieu = prefix + "-THANHLY", NhanVienId = staffId, ThoiDiem = now,
            LyDoThanhLy = "Rau không đạt chất lượng (minh họa)", GhiChu = Marker };
        var waste = ingredients.FirstOrDefault(x => x.TenNguyenLieu.Contains("Rau")) ?? first;
        var loss = 10 * waste.DonGia;
        disposal.ChiTiet.Add(new ChiTietPhieuThanhLy { NguyenLieuId = waste.Id, SoLuong = 10, DonGiaVon = waste.DonGia, ThanhTien = loss, GhiChu = Marker });
        disposal.TongTienThietHai = loss; db.PhieuThanhLy.Add(disposal);
        db.PhieuXuat.Add(new PhieuXuat { MaPhieu = prefix + "-HUY", NhanVienId = staffId, ThoiDiem = now,
            LyDo = LyDoXuat.ThanhLy, TrangThai = TrangThaiPhieu.DaGhiSo, TongTien = loss, GhiChu = Marker + " " + disposal.MaPhieu,
            ChiTiet = [new ChiTietPhieuXuat { NguyenLieuId = waste.Id, SoLuong = 10, DonGiaXuat = waste.DonGia, ThanhTien = loss }] });
        waste.SoLuongTon -= 10;
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
}
