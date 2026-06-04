using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBlossom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBlindBookImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bảng [Book].[BlindBookImages] đã được tạo trực tiếp trong SQL Server.
            // Migration này chỉ cập nhật EF Core model snapshot để nhận biết bảng đó,
            // không chạy lệnh CREATE TABLE để tránh lỗi "table already exists".
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không DROP bảng [Book].[BlindBookImages] tự động vì bảng được quản lý thủ công.
        }
    }
}
