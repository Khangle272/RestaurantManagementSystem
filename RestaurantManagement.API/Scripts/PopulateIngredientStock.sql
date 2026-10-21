-- Phiếu nhập minh họa bổ sung tồn kho ban đầu cho các nguyên liệu trong màn hình cảnh báo.
-- Chạy sau PopulateRestaurantData.sql. Chỉ áp dụng một lần theo mã phiếu cố định.
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Codes TABLE(MaPhieu nvarchar(30) PRIMARY KEY);
    INSERT @Codes VALUES
        (N'PN-20261021-01'),
        (N'PN-20261021-02'),
        (N'PN-20261021-03');

    DECLARE @Existing int = (SELECT COUNT(*) FROM PhieuNhap p JOIN @Codes c ON c.MaPhieu=p.MaPhieu);
    IF @Existing = 3
    BEGIN
        COMMIT TRANSACTION;
        PRINT N'Phiếu nhập nguyên liệu bổ sung đã tồn tại; không cộng tồn lần nữa.';
        RETURN;
    END;
    IF @Existing <> 0
        THROW 50010, N'Phát hiện bộ phiếu nhập bổ sung chưa đầy đủ; cần kiểm tra trước khi nạp lại.', 1;

    DECLARE @Items TABLE
    (
        MaPhieu nvarchar(30) NOT NULL,
        NhaCungCap nvarchar(150) NOT NULL,
        NguyenLieu nvarchar(120) NOT NULL,
        SoLuong decimal(18,6) NOT NULL,
        DonGia decimal(18,2) NOT NULL,
        HanSuDung date NULL,
        MaLo nvarchar(50) NOT NULL
    );
    INSERT @Items VALUES
        (N'PN-20261021-01',N'Nông sản An Nhiên',N'Cà chua',5000,30,'2026-10-27',N'LO-20261021-RAU'),
        (N'PN-20261021-01',N'Nông sản An Nhiên',N'Chanh',2000,25,'2026-10-28',N'LO-20261021-RAU'),
        (N'PN-20261021-01',N'Nông sản An Nhiên',N'Đậu phụ',3000,35,'2026-10-24',N'LO-20261021-RAU'),
        (N'PN-20261021-01',N'Nông sản An Nhiên',N'Dưa leo',3000,20,'2026-10-27',N'LO-20261021-RAU'),
        (N'PN-20261021-01',N'Nông sản An Nhiên',N'Đường',3000,22,'2027-04-21',N'LO-20261021-KHO'),
        (N'PN-20261021-01',N'Nông sản An Nhiên',N'Mì trứng',2500,55,'2026-11-20',N'LO-20261021-KHO'),
        (N'PN-20261021-01',N'Nông sản An Nhiên',N'Nấm rơm',2500,70,'2026-10-25',N'LO-20261021-RAU'),
        (N'PN-20261021-01',N'Nông sản An Nhiên',N'Thịt heo',3000,150,'2026-10-24',N'LO-20261021-THIT'),
        (N'PN-20261021-01',N'Nông sản An Nhiên',N'Ức gà',3000,130,'2026-10-24',N'LO-20261021-THIT'),
        (N'PN-20261021-02',N'Hải sản Biển Xanh',N'Mực ống',2000,220,'2026-10-24',N'LO-20261021-HAISAN'),
        (N'PN-20261021-02',N'Hải sản Biển Xanh',N'Tôm sú',2000,220,'2026-10-24',N'LO-20261021-HAISAN'),
        (N'PN-20261021-03',N'Đồ uống Hương Việt',N'Cà phê rang xay',1500,180,'2027-04-21',N'LO-20261021-DOUONG'),
        (N'PN-20261021-03',N'Đồ uống Hương Việt',N'Sữa đặc',3000,55,'2026-12-21',N'LO-20261021-DOUONG'),
        (N'PN-20261021-03',N'Đồ uống Hương Việt',N'Sữa tươi',5000,40,'2026-11-20',N'LO-20261021-DOUONG'),
        (N'PN-20261021-03',N'Đồ uống Hương Việt',N'Trà lài',500,120,'2027-04-21',N'LO-20261021-DOUONG');

    IF EXISTS
    (
        SELECT 1 FROM @Items i
        LEFT JOIN NguyenLieu nl ON nl.TenNguyenLieu=i.NguyenLieu
        LEFT JOIN NhaCungCap ncc ON ncc.TenNhaCungCap=i.NhaCungCap
        WHERE nl.Id IS NULL OR ncc.Id IS NULL
    )
        THROW 50011, N'Thiếu nguyên liệu hoặc nhà cung cấp; hãy nạp trước hai script dữ liệu chính.', 1;

    INSERT PhieuNhap(MaPhieu,NhaCungCapId,NhanVienId,ThoiDiem,TrangThai,LyDo,SoHoaDon,TongTien,GhiChu)
    SELECT c.MaPhieu,ncc.Id,nv.Id,'2026-10-21T09:00:00+07:00',N'DaGhiSo',N'MuaHang',c.MaPhieu,0,
           N'Nhập bổ sung tồn kho minh họa cho bộ dữ liệu phát triển.'
    FROM @Codes c
    JOIN @Items i ON i.MaPhieu=c.MaPhieu
    JOIN NhaCungCap ncc ON ncc.TenNhaCungCap=i.NhaCungCap
    CROSS JOIN NhanVien nv
    WHERE nv.MaNhanVien=N'NV012'
    GROUP BY c.MaPhieu,ncc.Id,nv.Id;

    INSERT ChiTietPhieuNhap(PhieuNhapId,NguyenLieuId,SoLuong,DonGia,ThanhTien,HanSuDung,MaLo)
    SELECT p.Id,nl.Id,i.SoLuong,i.DonGia,i.SoLuong*i.DonGia,i.HanSuDung,i.MaLo
    FROM @Items i
    JOIN PhieuNhap p ON p.MaPhieu=i.MaPhieu
    JOIN NguyenLieu nl ON nl.TenNguyenLieu=i.NguyenLieu;

    UPDATE p
    SET TongTien=t.TongTien
    FROM PhieuNhap p
    CROSS APPLY(SELECT SUM(ct.ThanhTien) TongTien FROM ChiTietPhieuNhap ct WHERE ct.PhieuNhapId=p.Id) t
    JOIN @Codes c ON c.MaPhieu=p.MaPhieu;

    UPDATE nl
    SET SoLuongTon=nl.SoLuongTon+t.SoLuong,
        DonGia=t.DonGia
    FROM NguyenLieu nl
    JOIN
    (
        SELECT i.NguyenLieu,SUM(i.SoLuong) SoLuong,MAX(i.DonGia) DonGia
        FROM @Items i
        GROUP BY i.NguyenLieu
    ) t ON t.NguyenLieu=nl.TenNguyenLieu;

    COMMIT TRANSACTION;
    PRINT N'Đã nhập tồn kho cho 15 nguyên liệu bằng 3 phiếu nhập.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
