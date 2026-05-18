using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBlossom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImportTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bảng 'Import.Importing' và 'Import.ImportingDetail' đã tồn tại sẵn trên Database server.
            // Migration này được giữ rỗng để tránh cố gắng tạo lại các bảng đã có, 
            // đồng thời đảm bảo Snapshot đồng bộ và ghi nhận lịch sử Migration thành công.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down migration rỗng tương ứng.
        }
    }
}
