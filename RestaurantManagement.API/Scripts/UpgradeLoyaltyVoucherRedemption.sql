BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026132015_LoyaltyVoucherRedemption'
)
BEGIN
    DROP INDEX [IX_LichSuTichDiem_HoaDonId] ON [LichSuTichDiem];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026132015_LoyaltyVoucherRedemption'
)
BEGIN
    ALTER TABLE [LichSuTichDiem] DROP CONSTRAINT [CK_LichSuTichDiem_Diem];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026132015_LoyaltyVoucherRedemption'
)
BEGIN
    ALTER TABLE [Voucher] ADD [DiemCanDoi] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026132015_LoyaltyVoucherRedemption'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[LichSuTichDiem]') AND [c].[name] = N'HoaDonId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [LichSuTichDiem] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [LichSuTichDiem] ALTER COLUMN [HoaDonId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026132015_LoyaltyVoucherRedemption'
)
BEGIN
    ALTER TABLE [LichSuTichDiem] ADD [VoucherId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026132015_LoyaltyVoucherRedemption'
)
BEGIN
    EXEC(N'ALTER TABLE [Voucher] ADD CONSTRAINT [CK_Voucher_DiemCanDoi] CHECK ([DiemCanDoi]>=0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026132015_LoyaltyVoucherRedemption'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_LichSuTichDiem_HoaDonId] ON [LichSuTichDiem] ([HoaDonId]) WHERE [HoaDonId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026132015_LoyaltyVoucherRedemption'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_LichSuTichDiem_KhachHangId_VoucherId] ON [LichSuTichDiem] ([KhachHangId], [VoucherId]) WHERE [VoucherId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026132015_LoyaltyVoucherRedemption'
)
BEGIN
    CREATE INDEX [IX_LichSuTichDiem_VoucherId] ON [LichSuTichDiem] ([VoucherId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026132015_LoyaltyVoucherRedemption'
)
BEGIN
    EXEC(N'ALTER TABLE [LichSuTichDiem] ADD CONSTRAINT [CK_LichSuTichDiem_Diem] CHECK (([HoaDonId] IS NOT NULL AND [VoucherId] IS NULL AND [SoDiem]>=0 AND [SoTienTinhDiem]>=0) OR ([HoaDonId] IS NULL AND [VoucherId] IS NOT NULL AND [SoDiem]<0 AND [SoTienTinhDiem]=0))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026132015_LoyaltyVoucherRedemption'
)
BEGIN
    ALTER TABLE [LichSuTichDiem] ADD CONSTRAINT [FK_LichSuTichDiem_Voucher_VoucherId] FOREIGN KEY ([VoucherId]) REFERENCES [Voucher] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026132015_LoyaltyVoucherRedemption'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261026132015_LoyaltyVoucherRedemption', N'10.0.12');
END;

COMMIT;
GO

