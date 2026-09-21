-- Dữ liệu tự soạn để thực hành CRUD, được lưu trực tiếp trong SQL Server.
-- Tên và thông tin liên hệ là giả lập; không đại diện cho nhân sự thực tế.
-- Chạy trên database ứng dụng đã áp dụng migrations. Không xóa/ghi đè bản ghi có sẵn.
-- Chạy lại chỉ bổ sung những mã/tên chưa có. Không chạy tự động khi khởi động Web.
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @NhanVienAdded int, @KhuVucAdded int, @BanAnAdded int, @DanhMucAdded int;

    INSERT INTO dbo.NhanVien
        (MaNhanVien, HoTen, SoDienThoai, Email, DiaChi, ChucVu, NgayVaoLam, DangLamViec)
    SELECT v.MaNhanVien, v.HoTen, v.SoDienThoai, v.Email, v.DiaChi, v.ChucVu, v.NgayVaoLam, v.DangLamViec
    FROM (VALUES
    (N'NV004', N'Nguyễn Hoàng Nam', N'0900000104', N'nv004@nhahang.example.test', N'TP. Hồ Chí Minh', N'Thu ngân', '2025-06-02', 1),
    (N'NV005', N'Trần Ngọc Mai', N'0900000105', N'nv005@nhahang.example.test', N'TP. Hồ Chí Minh', N'Lễ tân', '2025-07-15', 1),
    (N'NV006', N'Lê Minh Tuấn', N'0900000106', N'nv006@nhahang.example.test', N'TP. Hồ Chí Minh', N'Phục vụ', '2025-09-01', 1),
    (N'NV007', N'Phạm Thảo Vy', N'0900000107', N'nv007@nhahang.example.test', N'TP. Hồ Chí Minh', N'Phục vụ', '2025-10-10', 1),
    (N'NV008', N'Võ Quốc Huy', N'0900000108', N'nv008@nhahang.example.test', N'TP. Hồ Chí Minh', N'Bếp trưởng', '2024-12-01', 1),
    (N'NV009', N'Đặng Thu Hà', N'0900000109', N'nv009@nhahang.example.test', N'TP. Hồ Chí Minh', N'Phụ bếp', '2026-01-05', 1),
    (N'NV010', N'Bùi Gia Bảo', N'0900000110', N'nv010@nhahang.example.test', N'TP. Hồ Chí Minh', N'Pha chế', '2026-02-12', 1),
    (N'NV011', N'Đỗ Khánh Linh', N'0900000111', N'nv011@nhahang.example.test', N'TP. Hồ Chí Minh', N'Thu ngân', '2026-03-01', 1),
    (N'NV012', N'Huỳnh Đức Anh', N'0900000112', N'nv012@nhahang.example.test', N'TP. Hồ Chí Minh', N'Nhân viên kho', '2026-04-08', 1),
    (N'NV013', N'Ngô Thanh Trúc', N'0900000113', N'nv013@nhahang.example.test', N'TP. Hồ Chí Minh', N'Phục vụ', '2026-05-15', 1),
    (N'NV014', N'Phan Minh Khang', N'0900000114', N'nv014@nhahang.example.test', N'TP. Hồ Chí Minh', N'Phụ bếp', '2025-04-21', 0),
    (N'NV015', N'Dương Bảo Ngọc', N'0900000115', N'nv015@nhahang.example.test', N'TP. Hồ Chí Minh', N'Phục vụ', '2025-08-18', 0)
    ) v(MaNhanVien, HoTen, SoDienThoai, Email, DiaChi, ChucVu, NgayVaoLam, DangLamViec)
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.NhanVien WITH (UPDLOCK, HOLDLOCK) WHERE MaNhanVien = v.MaNhanVien
    );
    SET @NhanVienAdded = @@ROWCOUNT;

    INSERT INTO dbo.KhuVuc (TenKhuVuc, LaPhongVip, DangSuDung)
    SELECT v.TenKhuVuc, v.LaPhongVip, 1
    FROM (VALUES (N'Sảnh chung', 0), (N'Phòng VIP', 1), (N'Sân vườn', 0)) v(TenKhuVuc, LaPhongVip)
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.KhuVuc WITH (UPDLOCK, HOLDLOCK) WHERE TenKhuVuc = v.TenKhuVuc
    );
    SET @KhuVucAdded = @@ROWCOUNT;

    IF EXISTS (
        SELECT TenKhuVuc FROM dbo.KhuVuc
        WHERE TenKhuVuc IN (N'Sảnh chung', N'Phòng VIP', N'Sân vườn')
        GROUP BY TenKhuVuc HAVING COUNT(*) <> 1
    )
        THROW 50001, N'Tên khu vực bị trùng. Cần xác định khu vực trước khi thêm bàn.', 1;

    IF EXISTS (
        SELECT 1 FROM dbo.KhuVuc
        WHERE TenKhuVuc IN (N'Sảnh chung', N'Phòng VIP', N'Sân vườn') AND DangSuDung = 0
    )
        THROW 50002, N'Khu vực đã ngừng sử dụng. Không thể thêm bàn vào khu vực này.', 1;

    INSERT INTO dbo.BanAn (MaBan, KhuVucId, SoChoNgoi, TrangThai)
    SELECT v.MaBan, k.Id, v.SoChoNgoi, v.TrangThai
    FROM (VALUES
    (N'S04', N'Sảnh chung', 4, N'SanSang'),
    (N'S05', N'Sảnh chung', 4, N'SanSang'),
    (N'S06', N'Sảnh chung', 6, N'SanSang'),
    (N'S07', N'Sảnh chung', 4, N'SanSang'),
    (N'S08', N'Sảnh chung', 4, N'SanSang'),
    (N'S09', N'Sảnh chung', 6, N'SanSang'),
    (N'S10', N'Sảnh chung', 4, N'SanSang'),
    (N'S11', N'Sảnh chung', 4, N'SanSang'),
    (N'S12', N'Sảnh chung', 6, N'NgungSuDung'),
    (N'V03', N'Phòng VIP', 8, N'SanSang'),
    (N'V04', N'Phòng VIP', 10, N'SanSang'),
    (N'V05', N'Phòng VIP', 8, N'SanSang'),
    (N'V06', N'Phòng VIP', 10, N'SanSang'),
    (N'SV01', N'Sân vườn', 4, N'SanSang'),
    (N'SV02', N'Sân vườn', 4, N'SanSang'),
    (N'SV03', N'Sân vườn', 4, N'SanSang'),
    (N'SV04', N'Sân vườn', 6, N'SanSang'),
    (N'SV05', N'Sân vườn', 6, N'NgungSuDung')
    ) v(MaBan, TenKhuVuc, SoChoNgoi, TrangThai)
    JOIN dbo.KhuVuc k ON k.TenKhuVuc = v.TenKhuVuc
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.BanAn WITH (UPDLOCK, HOLDLOCK) WHERE MaBan = v.MaBan
    );
    SET @BanAnAdded = @@ROWCOUNT;

    INSERT INTO dbo.DanhMuc (TenDanhMuc, MoTa, DangSuDung)
    SELECT v.TenDanhMuc, v.MoTa, 1
    FROM (VALUES
    (N'Món nướng', N'Các món thịt, cá và rau củ chế biến trên bếp nướng.'),
    (N'Món lẩu', N'Lẩu dùng chung cho nhóm khách, kèm rau và món nhúng.'),
    (N'Hải sản', N'Các món tôm, mực, cá và nghêu chế biến theo yêu cầu.'),
    (N'Món chay', N'Các món từ rau củ, nấm và đậu phụ.'),
    (N'Cơm & Mì', N'Cơm chiên, mì xào và các món tinh bột ăn kèm.'),
    (N'Salad', N'Salad rau xanh và các món trộn dùng làm món khai vị.'),
    (N'Tráng miệng', N'Trái cây, chè, bánh ngọt và kem sau bữa ăn.'),
    (N'Nước ép', N'Nước ép trái cây và sinh tố pha theo phần.'),
    (N'Cà phê & Trà', N'Cà phê pha máy, cà phê phin và các loại trà.'),
    (N'Combo gia đình', N'Thực đơn kết hợp dành cho gia đình từ 4 đến 6 người.')
    ) v(TenDanhMuc, MoTa)
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.DanhMuc WITH (UPDLOCK, HOLDLOCK) WHERE TenDanhMuc = v.TenDanhMuc
    );
    SET @DanhMucAdded = @@ROWCOUNT;

    COMMIT TRANSACTION;

    SELECT N'Nhân viên' AS LoaiDuLieu, @NhanVienAdded AS DaThem, COUNT(*) AS TongSo FROM dbo.NhanVien
    UNION ALL SELECT N'Bàn ăn', @BanAnAdded, COUNT(*) FROM dbo.BanAn
    UNION ALL SELECT N'Danh mục', @DanhMucAdded, COUNT(*) FROM dbo.DanhMuc
    UNION ALL SELECT N'Khu vực', @KhuVucAdded, COUNT(*) FROM dbo.KhuVuc;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

