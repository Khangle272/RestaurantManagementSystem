BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025134614_CampaignVoucherWalletAndCustomerPoints'
)
BEGIN
    ALTER TABLE [Voucher] DROP CONSTRAINT [CK_Voucher_Luot];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025134614_CampaignVoucherWalletAndCustomerPoints'
)
BEGIN
    DROP INDEX [IX_HoaDon_KhachHangId] ON [HoaDon];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025134614_CampaignVoucherWalletAndCustomerPoints'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Voucher]') AND [c].[name] = N'GioiHanTongLuot');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Voucher] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Voucher] ALTER COLUMN [GioiHanTongLuot] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025134614_CampaignVoucherWalletAndCustomerPoints'
)
BEGIN
    ALTER TABLE [Voucher] ADD [BatDau] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025134614_CampaignVoucherWalletAndCustomerPoints'
)
BEGIN
    ALTER TABLE [Voucher] ADD [GiaTri] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025134614_CampaignVoucherWalletAndCustomerPoints'
)
BEGIN
    ALTER TABLE [Voucher] ADD [GiaTriToiThieu] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025134614_CampaignVoucherWalletAndCustomerPoints'
)
BEGIN
    ALTER TABLE [Voucher] ADD [GioiHanPhatHanh] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025134614_CampaignVoucherWalletAndCustomerPoints'
)
BEGIN
    ALTER TABLE [Voucher] ADD [KetThuc] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025134614_CampaignVoucherWalletAndCustomerPoints'
)
BEGIN
    ALTER TABLE [Voucher] ADD [KieuGiam] nvarchar(40) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025134614_CampaignVoucherWalletAndCustomerPoints'
)
BEGIN
    ALTER TABLE [Voucher] ADD [MucGiamToiDa] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025134614_CampaignVoucherWalletAndCustomerPoints'
)
BEGIN
    ALTER TABLE [Voucher] ADD [PhamVi] nvarchar(40) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025134614_CampaignVoucherWalletAndCustomerPoints'
)
BEGIN
    ALTER TABLE [KhuyenMai] ADD [GioiHanPhatHanh] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025134614_CampaignVoucherWalletAndCustomerPoints'
)
BEGIN
    CREATE TABLE [LichSuTichDiem] (
        [Id] int NOT NULL IDENTITY,
        [KhachHangId] int NOT NULL,
        [HoaDonId] int NOT NULL,
        [SoDiem] int NOT NULL,
        [SoTienTinhDiem] decimal(18,2) NOT NULL,
        [ThoiDiem] datetimeoffset NOT NULL,
        [NoiDung] nvarchar(200) NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_LichSuTichDiem] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_LichSuTichDiem_Diem] CHECK ([SoDiem]>=0 AND [SoTienTinhDiem]>=0),
        CONSTRAINT [FK_LichSuTichDiem_HoaDon_HoaDonId] FOREIGN KEY ([HoaDonId]) REFERENCES [HoaDon] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LichSuTichDiem_KhachHang_KhachHangId] FOREIGN KEY ([KhachHangId]) REFERENCES [KhachHang] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025134614_CampaignVoucherWalletAndCustomerPoints'
)
BEGIN
    CREATE TABLE [VoucherKhachHang] (
        [KhachHangId] int NOT NULL,
        [VoucherId] int NOT NULL,
        [ThoiDiemThuThap] datetimeoffset NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_VoucherKhachHang] PRIMARY KEY ([KhachHangId], [VoucherId]),
        CONSTRAINT [FK_VoucherKhachHang_KhachHang_KhachHangId] FOREIGN KEY ([KhachHangId]) REFERENCES [KhachHang] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VoucherKhachHang_Voucher_VoucherId] FOREIGN KEY ([VoucherId]) REFERENCES [Voucher] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025134614_CampaignVoucherWalletAndCustomerPoints'
)
BEGIN
    CREATE TABLE [VoucherMon] (
        [VoucherId] int NOT NULL,
        [MonAnId] int NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_VoucherMon] PRIMARY KEY ([VoucherId], [MonAnId]),
        CONSTRAINT [FK_VoucherMon_MonAn_MonAnId] FOREIGN KEY ([MonAnId]) REFERENCES [MonAn] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VoucherMon_Voucher_VoucherId] FOREIGN KEY ([VoucherId]) REFERENCES [Voucher] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025134614_CampaignVoucherWalletAndCustomerPoints'
)
BEGIN
    EXEC(N'ALTER TABLE [Voucher] ADD CONSTRAINT [CK_Voucher_DieuKien] CHECK (([BatDau] IS NULL OR [KetThuc] IS NULL OR [KetThuc]>=[BatDau]) AND ([GiaTri] IS NULL OR ([GiaTri]>0 AND ([KieuGiam]<>''PhanTram'' OR [GiaTri]<=100))) AND [GiaTriToiThieu]>=0 AND ([MucGiamToiDa] IS NULL OR [MucGiamToiDa]>0) AND ([GioiHanPhatHanh] IS NULL OR [GioiHanPhatHanh]>0))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025134614_CampaignVoucherWalletAndCustomerPoints'
)
BEGIN
    EXEC(N'ALTER TABLE [Voucher] ADD CONSTRAINT [CK_Voucher_KieuGiam_Enum] CHECK ([KieuGiam] IN (''PhanTram'',''SoTien''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025134614_CampaignVoucherWalletAndCustomerPoints'
)
BEGIN
    EXEC(N'ALTER TABLE [Voucher] ADD CONSTRAINT [CK_Voucher_Luot] CHECK ([GioiHanTongLuot] IS NULL OR [GioiHanTongLuot]>0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025134614_CampaignVoucherWalletAndCustomerPoints'
)
BEGIN
    EXEC(N'ALTER TABLE [Voucher] ADD CONSTRAINT [CK_Voucher_PhamVi_Enum] CHECK ([PhamVi] IN (''MonAn'',''HoaDon''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025134614_CampaignVoucherWalletAndCustomerPoints'
)
BEGIN
    EXEC(N'ALTER TABLE [KhuyenMai] ADD CONSTRAINT [CK_KhuyenMai_PhatHanh] CHECK ([GioiHanPhatHanh] IS NULL OR [GioiHanPhatHanh]>0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025134614_CampaignVoucherWalletAndCustomerPoints'
)
BEGIN
    CREATE INDEX [IX_HoaDon_KhachHangId_ThoiDiemLap] ON [HoaDon] ([KhachHangId], [ThoiDiemLap]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025134614_CampaignVoucherWalletAndCustomerPoints'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LichSuTichDiem_HoaDonId] ON [LichSuTichDiem] ([HoaDonId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025134614_CampaignVoucherWalletAndCustomerPoints'
)
BEGIN
    CREATE INDEX [IX_LichSuTichDiem_KhachHangId_ThoiDiem] ON [LichSuTichDiem] ([KhachHangId], [ThoiDiem]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025134614_CampaignVoucherWalletAndCustomerPoints'
)
BEGIN
    CREATE INDEX [IX_VoucherKhachHang_VoucherId] ON [VoucherKhachHang] ([VoucherId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025134614_CampaignVoucherWalletAndCustomerPoints'
)
BEGIN
    CREATE INDEX [IX_VoucherMon_MonAnId] ON [VoucherMon] ([MonAnId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025134614_CampaignVoucherWalletAndCustomerPoints'
)
BEGIN
    EXEC(N'UPDATE v SET v.BatDau=p.BatDau, v.KetThuc=p.KetThuc,
        v.PhamVi=p.PhamVi, v.KieuGiam=p.KieuGiam, v.GiaTri=p.GiaTri,
        v.MucGiamToiDa=p.MucGiamToiDa, v.GiaTriToiThieu=p.GiaTriToiThieu
    FROM Voucher v INNER JOIN KhuyenMai p ON p.Id=v.KhuyenMaiId;
    INSERT INTO VoucherMon (VoucherId, MonAnId)
    SELECT v.Id, m.MonAnId FROM Voucher v
    INNER JOIN KhuyenMaiMon m ON m.KhuyenMaiId=v.KhuyenMaiId;');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025134614_CampaignVoucherWalletAndCustomerPoints'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261025134614_CampaignVoucherWalletAndCustomerPoints', N'10.0.12');
END;

COMMIT;
GO

