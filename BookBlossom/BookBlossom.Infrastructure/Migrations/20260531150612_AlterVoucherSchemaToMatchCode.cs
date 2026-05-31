using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBlossom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlterVoucherSchemaToMatchCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ─────────────────────────────────────────────────────────────────
            // 1. XỬ LÝ BẢNG Voucher.Voucher (đã tồn tại trong DB)
            //    - Xóa FK + cột MarketingManagerID (không cần trong hệ thống)
            //    - Xóa cột MinTindbookSwipes (TotalLimit đã đủ)
            // ─────────────────────────────────────────────────────────────────

            // Drop FK MarketingManagerID → StaffDetail (nếu còn tồn tại)
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = 'FK_Voucher_StaffDetail'
                    AND parent_object_id = OBJECT_ID('Voucher.Voucher')
                )
                BEGIN
                    ALTER TABLE [Voucher].[Voucher] DROP CONSTRAINT [FK_Voucher_StaffDetail];
                END");

            // Drop cột MarketingManagerID (nếu còn tồn tại)
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = 'Voucher' AND TABLE_NAME = 'Voucher'
                    AND COLUMN_NAME = 'MarketingManagerID'
                )
                BEGIN
                    ALTER TABLE [Voucher].[Voucher] DROP COLUMN [MarketingManagerID];
                END");

            // Drop cột MinTindbookSwipes (nếu còn tồn tại)
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = 'Voucher' AND TABLE_NAME = 'Voucher'
                    AND COLUMN_NAME = 'MinTindbookSwipes'
                )
                BEGIN
                    ALTER TABLE [Voucher].[Voucher] DROP COLUMN [MinTindbookSwipes];
                END");

            // ─────────────────────────────────────────────────────────────────
            // 2. Thêm cột VoucherID vào bảng OrderRequest.Orders (nếu chưa có)
            // ─────────────────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = 'OrderRequest' AND TABLE_NAME = 'Orders'
                    AND COLUMN_NAME = 'VoucherID'
                )
                BEGIN
                    ALTER TABLE [OrderRequest].[Orders] ADD [VoucherID] bigint NULL;
                END");

            // ─────────────────────────────────────────────────────────────────
            // 3. Thêm FK từ Orders sang Voucher.Voucher (nếu chưa có)
            // ─────────────────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = 'FK_Orders_Voucher_VoucherID'
                )
                BEGIN
                    ALTER TABLE [OrderRequest].[Orders]
                    ADD CONSTRAINT [FK_Orders_Voucher_VoucherID]
                    FOREIGN KEY ([VoucherID]) REFERENCES [Voucher].[Voucher]([VoucherID])
                    ON DELETE NO ACTION;
                END");

            // ─────────────────────────────────────────────────────────────────
            // 4. Thêm Index cho VoucherID trên Orders (nếu chưa có)
            // ─────────────────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_Orders_VoucherID'
                    AND object_id = OBJECT_ID('OrderRequest.Orders')
                )
                BEGIN
                    CREATE INDEX [IX_Orders_VoucherID]
                    ON [OrderRequest].[Orders] ([VoucherID]);
                END");

            // ─────────────────────────────────────────────────────────────────
            // 5. Đảm bảo unique index trên VoucherCode (nếu chưa có)
            // ─────────────────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'UQ_Voucher_VoucherCode'
                    AND object_id = OBJECT_ID('Voucher.Voucher')
                )
                BEGIN
                    CREATE UNIQUE INDEX [UQ_Voucher_VoucherCode]
                    ON [Voucher].[Voucher] ([VoucherCode]);
                END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Khôi phục cột VoucherID trên Orders
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = 'FK_Orders_Voucher_VoucherID'
                )
                BEGIN
                    ALTER TABLE [OrderRequest].[Orders] DROP CONSTRAINT [FK_Orders_Voucher_VoucherID];
                END");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_Orders_VoucherID'
                    AND object_id = OBJECT_ID('OrderRequest.Orders')
                )
                BEGIN
                    DROP INDEX [IX_Orders_VoucherID] ON [OrderRequest].[Orders];
                END");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = 'OrderRequest' AND TABLE_NAME = 'Orders'
                    AND COLUMN_NAME = 'VoucherID'
                )
                BEGIN
                    ALTER TABLE [OrderRequest].[Orders] DROP COLUMN [VoucherID];
                END");

            // Khôi phục cột MinTindbookSwipes
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = 'Voucher' AND TABLE_NAME = 'Voucher'
                    AND COLUMN_NAME = 'MinTindbookSwipes'
                )
                BEGIN
                    ALTER TABLE [Voucher].[Voucher] ADD [MinTindbookSwipes] int NOT NULL DEFAULT 0;
                END");
        }
    }
}
