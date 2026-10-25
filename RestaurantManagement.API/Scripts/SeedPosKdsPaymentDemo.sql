-- Dữ liệu minh họa POS/KDS/Thanh toán cho Development.
-- Chạy sau PopulateRestaurantData.sql trên database đã migrate. Chỉ thêm các mã demo cố định;
-- không cập nhật hay xóa hóa đơn, voucher hoặc dữ liệu nghiệp vụ khác.
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM NhanVien WHERE DangLamViec = 1)
        THROW 50101, N'Cần có ít nhất một nhân viên đang làm việc. Hãy nạp dữ liệu CRUD trước.', 1;
    IF NOT EXISTS (SELECT 1 FROM MonAn m JOIN MonAnSize s ON s.MonAnId = m.Id
        WHERE m.DaDuyet = 1 AND m.TrangThai = N'DangPhucVu' AND s.DangSuDung = 1 AND s.GiaBan > 0)
        THROW 50102, N'Không tìm thấy size món đang mở bán. Hãy chuẩn bị thực đơn trước.', 1;

    DECLARE @Now datetimeoffset(7) = SWITCHOFFSET(TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00'), '+07:00');
    DECLARE @StaffId int = (SELECT TOP (1) Id FROM NhanVien WHERE DangLamViec = 1 ORDER BY MaNhanVien);
    DECLARE @ProgramName nvarchar(150) = N'Ưu đãi POS/KDS Demo';
    DECLARE @VoucherCode nvarchar(40) = N'POSKDS-DEMO10';
    DECLARE @ProgramId int;
    DECLARE @VoucherId int;

    IF NOT EXISTS (SELECT 1 FROM KhuyenMai WITH (UPDLOCK, HOLDLOCK) WHERE TenChuongTrinh = @ProgramName)
    BEGIN
        INSERT KhuyenMai(TenChuongTrinh, BatDau, KetThuc, PhamVi, KieuGiam, GiaTri, MucGiamToiDa,
            GiaTriToiThieu, ChoPhepKetHop, ThuTuApDung, CanVoucher, DangSuDung)
        VALUES (@ProgramName, '2026-01-01T00:00:00+07:00', '2030-01-01T00:00:00+07:00',
            N'HoaDon', N'PhanTram', 10, 50000, 100000, 0, 1, 1, 1);
    END;
    SELECT TOP (1) @ProgramId = Id FROM KhuyenMai WHERE TenChuongTrinh = @ProgramName ORDER BY Id;
    IF EXISTS (SELECT 1 FROM KhuyenMai WHERE Id = @ProgramId AND
        (PhamVi <> N'HoaDon' OR KieuGiam <> N'PhanTram' OR GiaTri <> 10 OR MucGiamToiDa <> 50000
         OR GiaTriToiThieu <> 100000 OR CanVoucher <> 1 OR DangSuDung <> 1
         OR BatDau > @Now OR KetThuc <= @Now))
        THROW 50105, N'Chương trình POS/KDS demo đã tồn tại nhưng cấu hình không còn phù hợp. Không tự ghi đè dữ liệu.', 1;

    IF NOT EXISTS (SELECT 1 FROM Voucher WITH (UPDLOCK, HOLDLOCK) WHERE Ma = @VoucherCode)
        INSERT Voucher(Ma, KhuyenMaiId, KhachHangId, GioiHanTongLuot, DangSuDung)
        VALUES (@VoucherCode, @ProgramId, NULL, 100, 1);
    SELECT @VoucherId = Id FROM Voucher WHERE Ma = @VoucherCode;
    IF NOT EXISTS (SELECT 1 FROM Voucher WHERE Id = @VoucherId AND KhuyenMaiId = @ProgramId AND DangSuDung = 1)
        THROW 50106, N'Mã voucher POS/KDS demo đã được dùng cho cấu hình khác. Không tự ghi đè dữ liệu.', 1;

    DECLARE @DemoItems TABLE(Slot int PRIMARY KEY, MonAnId int, SizeId int, TenMon nvarchar(150), TenSize nvarchar(50), GiaBan decimal(18,2));
    INSERT @DemoItems(Slot, MonAnId, SizeId, TenMon, TenSize, GiaBan)
    SELECT Slot, MonAnId, SizeId, TenMon, TenSize, GiaBan
    FROM (
        SELECT CONVERT(int, ROW_NUMBER() OVER (ORDER BY s.GiaBan DESC, m.Id, s.Id)) AS Slot,
            m.Id AS MonAnId, s.Id AS SizeId, m.TenMon, s.TenSize, s.GiaBan
        FROM MonAn m JOIN MonAnSize s ON s.MonAnId = m.Id
        WHERE m.DaDuyet = 1 AND m.TrangThai = N'DangPhucVu' AND s.DangSuDung = 1 AND s.GiaBan > 0
    ) available
    WHERE Slot <= 2;
    DECLARE @BaseTotal decimal(18,2) = (SELECT SUM(GiaBan) FROM @DemoItems);
    IF @BaseTotal < 100000
        THROW 50103, N'Tổng giá món đang mở bán được chọn chưa đạt mức tối thiểu để minh họa voucher.', 1;

    DECLARE @NormalTotal decimal(18,2) = @BaseTotal;
    IF NOT EXISTS (SELECT 1 FROM HoaDon WITH (UPDLOCK, HOLDLOCK) WHERE MaHoaDon = N'HD-POSDEMO-01')
    BEGIN
        INSERT HoaDon(MaHoaDon, ThoiDiemLap, LoaiDonHang, TenNguoiNhan, SoDienThoaiNhan, NhanVienId,
            TrangThai, TongTienHang, TienGiam, TienCocDaTru, VoucherId, PhuongThucThanhToan)
        VALUES (N'HD-POSDEMO-01', @Now, N'MangDi', N'Khách demo không voucher', N'0900000991', @StaffId,
            N'ChuaThanhToan', @NormalTotal, 0, 0, NULL, N'TienMat');
        DECLARE @NormalInvoiceId int = SCOPE_IDENTITY();
        INSERT ChiTietHoaDon(HoaDonId, MonAnId, MonAnSizeId, TenMonLucBan, TenSizeLucBan, ThoiDiemGoi,
            SoLuong, DonGia, YeuCauCheBien, TrangThai)
        SELECT @NormalInvoiceId, MonAnId, SizeId, TenMon, TenSize, @Now, 1, GiaBan, NULL, N'DangCheBien'
        FROM @DemoItems;
    END;

    DECLARE @VoucherTotal decimal(18,2) = 2 * @BaseTotal;
    DECLARE @VoucherDiscount decimal(18,2) = CASE WHEN @VoucherTotal * 0.10 > 50000 THEN 50000 ELSE ROUND(@VoucherTotal * 0.10, 0) END;
    IF NOT EXISTS (SELECT 1 FROM HoaDon WITH (UPDLOCK, HOLDLOCK) WHERE MaHoaDon = N'HD-POSDEMO-02')
    BEGIN
        INSERT HoaDon(MaHoaDon, ThoiDiemLap, LoaiDonHang, TenNguoiNhan, SoDienThoaiNhan, NhanVienId,
            TrangThai, TongTienHang, TienGiam, TienCocDaTru, VoucherId, PhuongThucThanhToan)
        VALUES (N'HD-POSDEMO-02', @Now, N'MangDi', N'Khách demo dùng voucher', N'0900000992', @StaffId,
            N'ChuaThanhToan', @VoucherTotal, @VoucherDiscount, 0, @VoucherId, N'TienMat');
        DECLARE @VoucherInvoiceId int = SCOPE_IDENTITY();
        INSERT ChiTietHoaDon(HoaDonId, MonAnId, MonAnSizeId, TenMonLucBan, TenSizeLucBan, ThoiDiemGoi,
            SoLuong, DonGia, YeuCauCheBien, TrangThai)
        SELECT @VoucherInvoiceId, MonAnId, SizeId, TenMon, TenSize, @Now, 2, GiaBan, NULL, N'DangCheBien'
        FROM @DemoItems;
    END;

    IF EXISTS (
        SELECT 1 FROM HoaDon h
        WHERE h.MaHoaDon IN (N'HD-POSDEMO-01', N'HD-POSDEMO-02')
          AND h.TongTienHang <> (SELECT COALESCE(SUM(c.SoLuong * c.DonGia), 0) FROM ChiTietHoaDon c WHERE c.HoaDonId = h.Id))
        THROW 50104, N'Tổng hóa đơn demo không khớp chi tiết.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT MaHoaDon, TrangThai, TongTienHang, TienGiam, VoucherId
FROM HoaDon WHERE MaHoaDon IN (N'HD-POSDEMO-01', N'HD-POSDEMO-02') ORDER BY MaHoaDon;
