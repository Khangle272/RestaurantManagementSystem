-- Bộ dữ liệu tự soạn cho thực hành; không phải giao dịch hoặc thông tin người thật.
-- Chạy sau PopulateCrudData.sql trên database đã có migrations.
-- Chỉ thêm bản ghi còn thiếu, không ghi đè dữ liệu đã có.
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
BEGIN TRANSACTION;

IF NOT EXISTS(SELECT 1 FROM NhanVien WHERE MaNhanVien = N'NV004')
    THROW 50001, N'Hãy chạy PopulateCrudData.sql trước.', 1;

DECLARE @Ingredients TABLE(Ten nvarchar(120), DonVi nvarchar(30), Nguong decimal(18,6));
INSERT @Ingredients VALUES
(N'Ức gà',N'g',1000),
(N'Tôm sú',N'g',1000),
(N'Mực ống',N'g',800),
(N'Thịt heo',N'g',1000),
(N'Nấm rơm',N'g',500),
(N'Đậu phụ',N'g',600),
(N'Mì trứng',N'g',500),
(N'Cà chua',N'g',500),
(N'Dưa leo',N'g',500),
(N'Cà phê rang xay',N'g',200),
(N'Sữa đặc',N'ml',300),
(N'Trà lài',N'g',100),
(N'Chanh',N'g',300),
(N'Đường',N'g',500),
(N'Sữa tươi',N'ml',500);
INSERT NguyenLieu(TenNguyenLieu,DonViTinh,NguongCanhBao,DangSuDung)
SELECT Ten,DonVi,Nguong,1 FROM @Ingredients v
WHERE NOT EXISTS(SELECT 1 FROM NguyenLieu WITH(UPDLOCK,HOLDLOCK) WHERE TenNguyenLieu=v.Ten);

DECLARE @Dishes TABLE(Ten nvarchar(150), DanhMuc nvarchar(100), MoTa nvarchar(1000), Loai nvarchar(40), TrangThai nvarchar(40), Gia decimal(18,2));
INSERT @Dishes VALUES
(N'Gà nướng mật ong',N'Món nướng',N'Đùi và ức gà nướng dùng kèm rau.',N'MonLe',N'DangPhucVu',129000),
(N'Tôm nướng muối ớt',N'Hải sản',N'Tôm nướng muối ớt, dùng kèm dưa leo.',N'MonLe',N'DangPhucVu',189000),
(N'Mực xào rau củ',N'Hải sản',N'Mực xào cùng cà chua và rau củ theo mùa.',N'MonLe',N'DangPhucVu',169000),
(N'Lẩu nấm chay',N'Món lẩu',N'Nấm, đậu phụ và rau trong nước dùng thanh nhẹ.',N'MonLe',N'DangPhucVu',199000),
(N'Đậu phụ sốt cà chua',N'Món chay',N'Đậu phụ mềm sốt cà chua dùng với cơm.',N'MonLe',N'DangPhucVu',69000),
(N'Cơm chiên hải sản',N'Cơm & Mì',N'Cơm chiên tôm, mực và trứng.',N'MonLe',N'DangPhucVu',89000),
(N'Mì xào bò',N'Cơm & Mì',N'Mì trứng xào thịt bò và rau củ.',N'MonLe',N'DangPhucVu',99000),
(N'Salad gà',N'Salad',N'Rau xà lách, ức gà và cà chua.',N'MonLe',N'DangPhucVu',79000),
(N'Cà phê sữa đá',N'Cà phê & Trà',N'Cà phê phin và sữa đặc.',N'ThucUong',N'DangPhucVu',35000),
(N'Trà chanh',N'Cà phê & Trà',N'Trà lài pha chanh tươi.',N'ThucUong',N'DangPhucVu',29000),
(N'Sữa tươi cà phê',N'Cà phê & Trà',N'Sữa tươi dùng cùng cà phê đậm vị.',N'ThucUong',N'DangPhucVu',39000),
(N'Lẩu hải sản',N'Món lẩu',N'Lẩu tôm, mực, nấm và rau.',N'MonLe',N'TamHet',299000),
(N'Heo nướng sả',N'Món nướng',N'Thịt heo nướng dùng kèm rau sống.',N'MonLe',N'DangPhucVu',119000),
(N'Cơm nấm chay',N'Món chay',N'Cơm ăn kèm nấm xào và đậu phụ.',N'MonLe',N'NgungKinhDoanh',65000),
(N'Combo sum vầy',N'Combo gia đình',N'Gà nướng, mì xào bò, salad gà và trà chanh cho nhóm bốn người.',N'Set',N'DangPhucVu',399000),
(N'Combo hải sản',N'Combo gia đình',N'Tôm nướng, mực xào, cơm chiên và trà chanh.',N'Set',N'DangPhucVu',549000);
IF EXISTS(SELECT TenMon FROM MonAn WHERE TenMon IN(SELECT Ten FROM @Dishes) GROUP BY TenMon HAVING COUNT(*)>1)
    THROW 50002, N'Trùng tên món, cần xác định món trước khi nạp dữ liệu.', 1;
INSERT MonAn(DanhMucId,TenMon,MoTa,HinhAnh,Loai,TrangThai,LaMonMoi,LaMonNoiBat)
SELECT d.Id,v.Ten,v.MoTa,NULL,v.Loai,v.TrangThai,0,0 FROM @Dishes v JOIN DanhMuc d ON d.TenDanhMuc=v.DanhMuc
WHERE NOT EXISTS(SELECT 1 FROM MonAn WITH(UPDLOCK,HOLDLOCK) WHERE TenMon=v.Ten);
INSERT MonAnSize(MonAnId,TenSize,GiaBan,DangSuDung)
SELECT m.Id,N'Mặc định',v.Gia,1 FROM @Dishes v JOIN MonAn m ON m.TenMon=v.Ten
WHERE NOT EXISTS(SELECT 1 FROM MonAnSize WITH(UPDLOCK,HOLDLOCK) WHERE MonAnId=m.Id AND TenSize=N'Mặc định');
INSERT MonAnSize(MonAnId,TenSize,GiaBan,DangSuDung)
SELECT m.Id,N'Phần lớn',v.Gia*1.5,1 FROM @Dishes v JOIN MonAn m ON m.TenMon=v.Ten
WHERE v.Ten IN(N'Cơm chiên hải sản',N'Mì xào bò',N'Tôm nướng muối ớt')
AND NOT EXISTS(SELECT 1 FROM MonAnSize WITH(UPDLOCK,HOLDLOCK) WHERE MonAnId=m.Id AND TenSize=N'Phần lớn');

DECLARE @Recipes TABLE(Mon nvarchar(150), NguyenLieu nvarchar(120), SoLuong decimal(18,6));
INSERT @Recipes VALUES
(N'Gà nướng mật ong',N'Ức gà',300),
(N'Gà nướng mật ong',N'Dưa leo',100),
(N'Tôm nướng muối ớt',N'Tôm sú',300),
(N'Tôm nướng muối ớt',N'Dưa leo',100),
(N'Mực xào rau củ',N'Mực ống',250),
(N'Mực xào rau củ',N'Cà chua',100),
(N'Lẩu nấm chay',N'Nấm rơm',300),
(N'Lẩu nấm chay',N'Đậu phụ',250),
(N'Lẩu nấm chay',N'Rau xà lách',100),
(N'Đậu phụ sốt cà chua',N'Đậu phụ',200),
(N'Đậu phụ sốt cà chua',N'Cà chua',150),
(N'Cơm chiên hải sản',N'Gạo',150),
(N'Cơm chiên hải sản',N'Tôm sú',80),
(N'Cơm chiên hải sản',N'Mực ống',80),
(N'Cơm chiên hải sản',N'Trứng gà',1),
(N'Mì xào bò',N'Mì trứng',150),
(N'Mì xào bò',N'Thịt bò',120),
(N'Mì xào bò',N'Cà chua',80),
(N'Salad gà',N'Ức gà',120),
(N'Salad gà',N'Rau xà lách',150),
(N'Salad gà',N'Cà chua',80),
(N'Cà phê sữa đá',N'Cà phê rang xay',25),
(N'Cà phê sữa đá',N'Sữa đặc',30),
(N'Trà chanh',N'Trà lài',5),
(N'Trà chanh',N'Chanh',40),
(N'Trà chanh',N'Đường',20),
(N'Sữa tươi cà phê',N'Cà phê rang xay',20),
(N'Sữa tươi cà phê',N'Sữa tươi',150),
(N'Lẩu hải sản',N'Tôm sú',300),
(N'Lẩu hải sản',N'Mực ống',250),
(N'Lẩu hải sản',N'Nấm rơm',150),
(N'Heo nướng sả',N'Thịt heo',250),
(N'Heo nướng sả',N'Dưa leo',100),
(N'Cơm nấm chay',N'Gạo',150),
(N'Cơm nấm chay',N'Nấm rơm',100),
(N'Cơm nấm chay',N'Đậu phụ',100);
INSERT DinhMucMon(MonAnId,NguyenLieuId,SoLuong)
SELECT m.Id,n.Id,r.SoLuong FROM @Recipes r JOIN MonAn m ON m.TenMon=r.Mon JOIN NguyenLieu n ON n.TenNguyenLieu=r.NguyenLieu
WHERE NOT EXISTS(SELECT 1 FROM DinhMucMon WITH(UPDLOCK,HOLDLOCK) WHERE MonAnId=m.Id AND NguyenLieuId=n.Id);
INSERT ChiTietCombo(ComboId,MonAnId,SoLuong)
SELECT c.Id,m.Id,v.SoLuong FROM (VALUES
(N'Combo sum vầy',N'Gà nướng mật ong',1),(N'Combo sum vầy',N'Mì xào bò',1),(N'Combo sum vầy',N'Salad gà',1),(N'Combo sum vầy',N'Trà chanh',4),
(N'Combo hải sản',N'Tôm nướng muối ớt',1),(N'Combo hải sản',N'Mực xào rau củ',1),(N'Combo hải sản',N'Cơm chiên hải sản',2),(N'Combo hải sản',N'Trà chanh',4)
) v(Combo,Mon,SoLuong) JOIN MonAn c ON c.TenMon=v.Combo JOIN MonAn m ON m.TenMon=v.Mon
WHERE NOT EXISTS(SELECT 1 FROM ChiTietCombo WITH(UPDLOCK,HOLDLOCK) WHERE ComboId=c.Id AND MonAnId=m.Id);

INSERT KhachHang(HoTen,SoDienThoai,Email,NgayDangKy,DongYNhanUuDai,ThoiDiemDongYNhanUuDai,DangSuDung,TaiKhoanId)
SELECT v.HoTen,v.Sdt,v.Email,'2026-09-01T09:00:00+07:00',0,NULL,1,NULL
FROM (VALUES
(N'Nguyễn An Bình',N'0900000201',N'khach1@nhahang.example.test'),
(N'Trần Hải Yến',N'0900000202',N'khach2@nhahang.example.test'),
(N'Lê Phương Anh',N'0900000203',N'khach3@nhahang.example.test'),
(N'Phạm Quốc Minh',N'0900000204',N'khach4@nhahang.example.test'),
(N'Võ Thanh Tâm',N'0900000205',N'khach5@nhahang.example.test'),
(N'Bùi Ngọc Hân',N'0900000206',N'khach6@nhahang.example.test'),
(N'Đặng Tuấn Kiệt',N'0900000207',N'khach7@nhahang.example.test'),
(N'Hoàng Gia Linh',N'0900000208',N'khach8@nhahang.example.test')
) v(HoTen,Sdt,Email)
WHERE NOT EXISTS(SELECT 1 FROM KhachHang WITH(UPDLOCK,HOLDLOCK) WHERE SoDienThoai=v.Sdt);

INSERT NhaCungCap(TenNhaCungCap,SoDienThoai,DiaChi,MaSoThue,DangSuDung)
SELECT v.Ten,v.Sdt,N'TP. Hồ Chí Minh',NULL,1 FROM (VALUES
(N'Nông sản An Nhiên',N'0900000301'),(N'Hải sản Biển Xanh',N'0900000302'),(N'Đồ uống Hương Việt',N'0900000303')
) v(Ten,Sdt)
WHERE NOT EXISTS(SELECT 1 FROM NhaCungCap WITH(UPDLOCK,HOLDLOCK) WHERE TenNhaCungCap=v.Ten);
IF EXISTS(SELECT TenNhaCungCap FROM NhaCungCap WHERE TenNhaCungCap IN(N'Nông sản An Nhiên',N'Hải sản Biển Xanh',N'Đồ uống Hương Việt') GROUP BY TenNhaCungCap HAVING COUNT(*)>1)
    THROW 50003, N'Tên nhà cung cấp bị trùng.', 1;

INSERT KhuyenMai(TenChuongTrinh,BatDau,KetThuc,PhamVi,KieuGiam,GiaTri,MucGiamToiDa,GiaTriToiThieu,ChoPhepKetHop,ThuTuApDung,CanVoucher,DangSuDung)
SELECT v.Ten,'2026-09-01T00:00:00+07:00','2026-10-01T00:00:00+07:00',v.PhamVi,v.Kieu,v.GiaTri,v.ToiDa,v.ToiThieu,0,1,v.CanVoucher,1
FROM (VALUES
(N'Ưu đãi bữa tối tháng 9',N'HoaDon',N'PhanTram',10,50000,200000,1),
(N'Tiết kiệm cùng gia đình',N'HoaDon',N'SoTien',30000,NULL,300000,1),
(N'Ưu đãi cà phê buổi sáng',N'MonAn',N'PhanTram',10,10000,0,0)
) v(Ten,PhamVi,Kieu,GiaTri,ToiDa,ToiThieu,CanVoucher)
WHERE NOT EXISTS(SELECT 1 FROM KhuyenMai WITH(UPDLOCK,HOLDLOCK) WHERE TenChuongTrinh=v.Ten);
IF EXISTS(SELECT TenChuongTrinh FROM KhuyenMai WHERE TenChuongTrinh IN(N'Ưu đãi bữa tối tháng 9',N'Tiết kiệm cùng gia đình',N'Ưu đãi cà phê buổi sáng') GROUP BY TenChuongTrinh HAVING COUNT(*)>1)
    THROW 50004, N'Tên khuyến mãi bị trùng.', 1;
INSERT KhuyenMaiMon(KhuyenMaiId,MonAnId)
SELECT k.Id,m.Id FROM KhuyenMai k CROSS JOIN MonAn m
WHERE k.TenChuongTrinh=N'Ưu đãi cà phê buổi sáng' AND m.TenMon IN(N'Cà phê sữa đá',N'Sữa tươi cà phê')
AND NOT EXISTS(SELECT 1 FROM KhuyenMaiMon WITH(UPDLOCK,HOLDLOCK) WHERE KhuyenMaiId=k.Id AND MonAnId=m.Id);
INSERT Voucher(Ma,KhuyenMaiId,KhachHangId,GioiHanTongLuot,DangSuDung)
SELECT v.Ma,k.Id,NULL,100,1 FROM (VALUES
(N'THANG9-10',N'Ưu đãi bữa tối tháng 9'),(N'GIADINH-30',N'Tiết kiệm cùng gia đình')
) v(Ma,Ten) JOIN KhuyenMai k ON k.TenChuongTrinh=v.Ten
WHERE NOT EXISTS(SELECT 1 FROM Voucher WITH(UPDLOCK,HOLDLOCK) WHERE Ma=v.Ma);

DECLARE @Bookings TABLE(Ma nvarchar(30), Sdt nvarchar(20), Ban nvarchar(20), GioDen datetimeoffset, TrangThai nvarchar(40), SoKhach int, Coc decimal(18,2));
INSERT @Bookings VALUES
(N'DB-20260918-01',N'0900000201',N'S04','2026-09-18T18:00:00+07:00',N'DaNhanBan',4,100000),
(N'DB-20260918-02',N'0900000202',N'S05','2026-09-18T19:00:00+07:00',N'DaNhanBan',3,0),
(N'DB-20260919-01',N'0900000203',N'V03','2026-09-19T18:30:00+07:00',N'DaNhanBan',6,100000),
(N'DB-20260920-01',N'0900000204',N'S06','2026-09-20T11:30:00+07:00',N'DaNhanBan',4,0),
(N'DB-20260925-01',N'0900000205',N'V04','2026-09-25T18:00:00+07:00',N'DaXacNhan',8,200000),
(N'DB-20260926-01',N'0900000206',N'SV01','2026-09-26T19:00:00+07:00',N'ChoXacNhan',4,0),
(N'DB-20260919-02',N'0900000207',N'S07','2026-09-19T19:00:00+07:00',N'DaHuy',2,0),
(N'DB-20260920-02',N'0900000208',N'S08','2026-09-20T18:00:00+07:00',N'KhongDen',2,0);
INSERT DatBan(MaDatBan,KhachHangId,HoTenLienHe,SoDienThoaiLienHe,LaKhachTrucTiep,ThoiDiemTao,GioDen,GioKetThucDuKien,SoNguoiLon,SoTreEm,YeuCau,YeuCauTrangTri,YeuCauVip,TienCocYeuCau,DieuKienCocDaThoaThuan,TienCocDaNop,ThoiDiemCoc,TrangThaiCoc,TrangThai,ThoiDiemNhanBan,ThoiDiemHuy,LyDoHuy,NhanVienTiepNhanId,NhanVienHuyId)
SELECT v.Ma,k.Id,k.HoTen,k.SoDienThoai,0,DATEADD(day,-1,v.GioDen),v.GioDen,DATEADD(hour,2,v.GioDen),v.SoKhach,0,NULL,0,CASE WHEN v.Ban LIKE 'V%' THEN 1 ELSE 0 END,v.Coc,NULL,v.Coc,
CASE WHEN v.Coc>0 THEN DATEADD(day,-1,v.GioDen) END,
CASE WHEN v.Coc=0 THEN N'ChuaCoc' WHEN v.TrangThai=N'DaNhanBan' THEN N'DaDoiTru' ELSE N'DaCoc' END,v.TrangThai,
CASE WHEN v.TrangThai=N'DaNhanBan' THEN v.GioDen END,
CASE WHEN v.TrangThai=N'DaHuy' THEN DATEADD(hour,-3,v.GioDen) END,
CASE WHEN v.TrangThai=N'DaHuy' THEN N'Khách thay đổi kế hoạch' END,n.Id,
CASE WHEN v.TrangThai=N'DaHuy' THEN n.Id END
FROM @Bookings v JOIN KhachHang k ON k.SoDienThoai=v.Sdt CROSS JOIN NhanVien n
WHERE n.MaNhanVien=N'NV005' AND NOT EXISTS(SELECT 1 FROM DatBan WITH(UPDLOCK,HOLDLOCK) WHERE MaDatBan=v.Ma);
INSERT ChiTietDatBan(DatBanId,BanAnId)
SELECT d.Id,b.Id FROM @Bookings v JOIN DatBan d ON d.MaDatBan=v.Ma JOIN BanAn b ON b.MaBan=v.Ban
WHERE NOT EXISTS(SELECT 1 FROM ChiTietDatBan WITH(UPDLOCK,HOLDLOCK) WHERE DatBanId=d.Id AND BanAnId=b.Id);

DECLARE @Lines TABLE(MaDatBan nvarchar(30),Mon nvarchar(150),SoLuong int);
INSERT @Lines VALUES
(N'DB-20260918-01',N'Gà nướng mật ong',2),(N'DB-20260918-01',N'Trà chanh',4),
(N'DB-20260918-02',N'Mì xào bò',3),(N'DB-20260918-02',N'Cà phê sữa đá',3),
(N'DB-20260919-01',N'Tôm nướng muối ớt',2),(N'DB-20260919-01',N'Cơm chiên hải sản',3),
(N'DB-20260920-01',N'Đậu phụ sốt cà chua',2),(N'DB-20260920-01',N'Lẩu nấm chay',1),
(N'DB-20260925-01',N'Gà nướng mật ong',3),(N'DB-20260925-01',N'Trà chanh',8);
INSERT MonDatTruoc(DatBanId,MonAnId,MonAnSizeId,TenMonLucDat,SoLuong,DonGiaThoaThuan,YeuCauCheBien,ChiTietComboSnapshot)
SELECT d.Id,m.Id,s.Id,m.TenMon,v.SoLuong,s.GiaBan,NULL,NULL
FROM @Lines v JOIN DatBan d ON d.MaDatBan=v.MaDatBan JOIN MonAn m ON m.TenMon=v.Mon JOIN MonAnSize s ON s.MonAnId=m.Id AND s.TenSize=N'Mặc định'
WHERE NOT EXISTS(SELECT 1 FROM MonDatTruoc WITH(UPDLOCK,HOLDLOCK) WHERE DatBanId=d.Id AND MonAnId=m.Id);

INSERT HoaDon(MaHoaDon,ThoiDiemLap,DatBanId,KhachHangId,NhanVienId,TrangThai,TongTienHang,TienGiam,TienCocDaTru,VoucherId,PhuongThucThanhToan,ThoiDiemThanhToan)
SELECT REPLACE(d.MaDatBan,N'DB-',N'HD-'),d.GioDen,d.Id,d.KhachHangId,n.Id,N'DaThanhToan',tot.Tong,
CASE WHEN d.MaDatBan=N'DB-20260918-01' THEN ROUND(tot.Tong*0.1,0) ELSE 0 END,
d.TienCocDaNop,CASE WHEN d.MaDatBan=N'DB-20260918-01' THEN vou.Id END,
CASE WHEN d.TienCocDaNop>0 THEN N'ChuyenKhoan' ELSE N'TienMat' END,DATEADD(minute,90,d.GioDen)
FROM DatBan d JOIN @Bookings b ON b.Ma=d.MaDatBan
CROSS APPLY(SELECT SUM(SoLuong*DonGiaThoaThuan) Tong FROM MonDatTruoc WHERE DatBanId=d.Id) tot
CROSS JOIN NhanVien n CROSS JOIN Voucher vou
WHERE b.TrangThai=N'DaNhanBan' AND n.MaNhanVien=N'NV004' AND vou.Ma=N'THANG9-10'
AND NOT EXISTS(SELECT 1 FROM HoaDon WITH(UPDLOCK,HOLDLOCK) WHERE MaHoaDon=REPLACE(d.MaDatBan,N'DB-',N'HD-'));
INSERT ChiTietHoaDon(HoaDonId,MonAnId,MonAnSizeId,TenMonLucBan,SoLuong,DonGia,YeuCauCheBien,ChiTietComboSnapshot,TrangThai,MonDatTruocId)
SELECT h.Id,p.MonAnId,p.MonAnSizeId,p.TenMonLucDat,p.SoLuong,p.DonGiaThoaThuan,p.YeuCauCheBien,p.ChiTietComboSnapshot,N'DaPhucVu',p.Id
FROM HoaDon h JOIN @Bookings b ON h.MaHoaDon=REPLACE(b.Ma,N'DB-',N'HD-') JOIN MonDatTruoc p ON p.DatBanId=h.DatBanId
WHERE NOT EXISTS(SELECT 1 FROM ChiTietHoaDon WITH(UPDLOCK,HOLDLOCK) WHERE MonDatTruocId=p.Id);

-- Nhập kho trước ngày phát sinh hóa đơn. Đơn giá theo đúng đơn vị g/ml/cái.
INSERT PhieuNhap(MaPhieu,NhaCungCapId,NhanVienId,ThoiDiem,TrangThai,LyDo,GhiChu)
SELECT v.Ma,c.Id,n.Id,'2026-09-17T07:00:00+07:00',N'DaGhiSo',N'MuaHang',N'Nhập hàng phục vụ thực đơn tháng 9'
FROM(VALUES(N'PN-20260917-01',N'Nông sản An Nhiên'),(N'PN-20260917-02',N'Hải sản Biển Xanh'),(N'PN-20260917-03',N'Đồ uống Hương Việt'))v(Ma,Ten)
JOIN NhaCungCap c ON c.TenNhaCungCap=v.Ten CROSS JOIN NhanVien n
WHERE n.MaNhanVien=N'NV012' AND NOT EXISTS(SELECT 1 FROM PhieuNhap WITH(UPDLOCK,HOLDLOCK) WHERE MaPhieu=v.Ma);
INSERT ChiTietPhieuNhap(PhieuNhapId,NguyenLieuId,SoLuong,DonGia,HanSuDung,MaLo)
SELECT p.Id,n.Id,CASE WHEN n.DonViTinh=N'cái' THEN 100 ELSE 20000 END,
CASE WHEN n.DonViTinh=N'cái' THEN 3000 WHEN n.TenNguyenLieu IN(N'Tôm sú',N'Mực ống',N'Thịt bò') THEN 220 ELSE 40 END,
'2026-10-01',N'LO-20260917'
FROM NguyenLieu n JOIN PhieuNhap p ON p.MaPhieu=
CASE WHEN n.TenNguyenLieu IN(N'Tôm sú',N'Mực ống') THEN N'PN-20260917-02'
WHEN n.TenNguyenLieu IN(N'Cà phê rang xay',N'Sữa đặc',N'Trà lài',N'Sữa tươi',N'Nước cam') THEN N'PN-20260917-03'
ELSE N'PN-20260917-01' END
WHERE n.TenNguyenLieu IN(SELECT Ten FROM @Ingredients UNION SELECT N'Thịt bò' UNION SELECT N'Rau xà lách' UNION SELECT N'Gạo' UNION SELECT N'Nước cam' UNION SELECT N'Trứng gà')
AND NOT EXISTS(SELECT 1 FROM ChiTietPhieuNhap WITH(UPDLOCK,HOLDLOCK) WHERE PhieuNhapId=p.Id AND NguyenLieuId=n.Id AND MaLo=N'LO-20260917');

INSERT PhieuXuat(MaPhieu,NhanVienId,HoaDonId,ThoiDiem,TrangThai,LyDo,GhiChu)
SELECT REPLACE(h.MaHoaDon,N'HD-',N'PX-'),n.Id,h.Id,DATEADD(minute,15,h.ThoiDiemLap),N'DaGhiSo',N'CheBien',N'Xuất theo công thức cho hóa đơn'
FROM HoaDon h JOIN @Bookings b ON h.MaHoaDon=REPLACE(b.Ma,N'DB-',N'HD-') CROSS JOIN NhanVien n
WHERE n.MaNhanVien=N'NV012' AND NOT EXISTS(SELECT 1 FROM PhieuXuat WITH(UPDLOCK,HOLDLOCK) WHERE MaPhieu=REPLACE(h.MaHoaDon,N'HD-',N'PX-'));
INSERT ChiTietPhieuXuat(PhieuXuatId,NguyenLieuId,SoLuong,ChiTietPhieuNhapId)
SELECT p.Id,r.NguyenLieuId,SUM(c.SoLuong*r.SoLuong),MIN(l.Id)
FROM PhieuXuat p JOIN ChiTietHoaDon c ON c.HoaDonId=p.HoaDonId JOIN DinhMucMon r ON r.MonAnId=c.MonAnId
JOIN ChiTietPhieuNhap l ON l.NguyenLieuId=r.NguyenLieuId AND l.MaLo=N'LO-20260917'
WHERE p.MaPhieu IN(SELECT REPLACE(Ma,N'DB-',N'PX-') FROM @Bookings)
AND NOT EXISTS(SELECT 1 FROM ChiTietPhieuXuat WITH(UPDLOCK,HOLDLOCK) WHERE PhieuXuatId=p.Id AND NguyenLieuId=r.NguyenLieuId)
GROUP BY p.Id,r.NguyenLieuId;

INSERT DanhGia(HoaDonId,ChiTietHoaDonId,KhachHangId,Diem,NoiDung,ThoiDiem)
SELECT h.Id,NULL,h.KhachHangId,5,N'Món ăn ngon, phục vụ chu đáo, không gian thoải mái.',DATEADD(hour,1,h.ThoiDiemThanhToan)
FROM HoaDon h JOIN @Bookings b ON h.MaHoaDon=REPLACE(b.Ma,N'DB-',N'HD-')
WHERE NOT EXISTS(SELECT 1 FROM DanhGia WITH(UPDLOCK,HOLDLOCK) WHERE HoaDonId=h.Id AND ChiTietHoaDonId IS NULL);
INSERT DanhGia(HoaDonId,ChiTietHoaDonId,KhachHangId,Diem,NoiDung,ThoiDiem)
SELECT h.Id,c.Id,h.KhachHangId,4,N'Món vừa khẩu vị, phần ăn phù hợp.',DATEADD(hour,1,h.ThoiDiemThanhToan)
FROM HoaDon h JOIN @Bookings b ON h.MaHoaDon=REPLACE(b.Ma,N'DB-',N'HD-')
CROSS APPLY(SELECT TOP(1) Id FROM ChiTietHoaDon WHERE HoaDonId=h.Id ORDER BY Id)c
WHERE NOT EXISTS(SELECT 1 FROM DanhGia WITH(UPDLOCK,HOLDLOCK) WHERE HoaDonId=h.Id AND ChiTietHoaDonId=c.Id);

-- Tài khoản minh họa không có mật khẩu và đang khóa; không cấp tài khoản đăng nhập sử dụng được.
INSERT VaiTro(Name,NormalizedName,ConcurrencyStamp)
SELECT v.Name,UPPER(v.Name),CONVERT(nvarchar(36),NEWID())
FROM(VALUES(N'Manager'),(N'Hostess'),(N'Waiter'),(N'Cashier'),(N'Kitchen'),(N'Stockkeeper'))v(Name)
WHERE NOT EXISTS(SELECT 1 FROM VaiTro WITH(UPDLOCK,HOLDLOCK) WHERE NormalizedName=UPPER(v.Name));
DECLARE @Accounts TABLE(MaNV nvarchar(20), Username nvarchar(256), RoleName nvarchar(256));
INSERT @Accounts VALUES(N'NV004',N'thungan.minhhoa',N'Cashier'),(N'NV005',N'letan.minhhoa',N'Hostess'),(N'NV006',N'phucvu.minhhoa',N'Waiter'),(N'NV008',N'bep.minhhoa',N'Kitchen'),(N'NV012',N'kho.minhhoa',N'Stockkeeper');
INSERT TaiKhoan(UserName,NormalizedUserName,Email,NormalizedEmail,EmailConfirmed,PasswordHash,SecurityStamp,ConcurrencyStamp,PhoneNumber,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEnd,LockoutEnabled,AccessFailedCount)
SELECT v.Username,UPPER(v.Username),v.Username+N'@nhahang.example.test',UPPER(v.Username+N'@nhahang.example.test'),0,NULL,CONVERT(nvarchar(36),NEWID()),CONVERT(nvarchar(36),NEWID()),NULL,0,0,'2099-01-01T00:00:00+07:00',1,0
FROM @Accounts v WHERE NOT EXISTS(SELECT 1 FROM TaiKhoan WITH(UPDLOCK,HOLDLOCK) WHERE NormalizedUserName=UPPER(v.Username));
-- Chỉ gắn tài khoản do script tạo, không sửa liên kết tài khoản có sẵn.
UPDATE n SET TaiKhoanId=u.Id FROM NhanVien n JOIN @Accounts v ON v.MaNV=n.MaNhanVien
JOIN TaiKhoan u ON u.NormalizedUserName=UPPER(v.Username)
WHERE n.TaiKhoanId IS NULL AND u.Email=v.Username+N'@nhahang.example.test' AND u.PasswordHash IS NULL AND u.LockoutEnd>SYSDATETIMEOFFSET()
AND NOT EXISTS(SELECT 1 FROM NhanVien other WHERE other.TaiKhoanId=u.Id);
INSERT TaiKhoanVaiTro(UserId,RoleId)
SELECT u.Id,r.Id FROM @Accounts v JOIN TaiKhoan u ON u.NormalizedUserName=UPPER(v.Username) JOIN VaiTro r ON r.NormalizedName=UPPER(v.RoleName)
WHERE u.Email=v.Username+N'@nhahang.example.test' AND u.PasswordHash IS NULL AND u.LockoutEnd>SYSDATETIMEOFFSET()
AND NOT EXISTS(SELECT 1 FROM TaiKhoanVaiTro WITH(UPDLOCK,HOLDLOCK) WHERE UserId=u.Id AND RoleId=r.Id);

-- Kiểm tra tính nhất quán trước khi commit.
IF EXISTS(SELECT 1 FROM HoaDon h JOIN @Bookings b ON h.MaHoaDon=REPLACE(b.Ma,N'DB-',N'HD-')
WHERE h.TongTienHang<>(SELECT COALESCE(SUM(c.SoLuong*c.DonGia),0) FROM ChiTietHoaDon c WHERE c.HoaDonId=h.Id))
    THROW 50005,N'Tổng hóa đơn không khớp chi tiết.',1;
IF EXISTS(SELECT 1 FROM ChiTietPhieuNhap l CROSS APPLY(SELECT COALESCE(SUM(x.SoLuong),0) Qty FROM ChiTietPhieuXuat x JOIN PhieuXuat p ON p.Id=x.PhieuXuatId WHERE x.ChiTietPhieuNhapId=l.Id AND p.TrangThai=N'DaGhiSo')s WHERE l.MaLo=N'LO-20260917' AND s.Qty>l.SoLuong)
    THROW 50006,N'Xuất kho vượt lượng nhập của lô.',1;
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT t.name AS Bang,SUM(p.rows) AS SoLuong
FROM sys.tables t JOIN sys.partitions p ON p.object_id=t.object_id AND p.index_id IN(0,1)
GROUP BY t.name ORDER BY t.name;
