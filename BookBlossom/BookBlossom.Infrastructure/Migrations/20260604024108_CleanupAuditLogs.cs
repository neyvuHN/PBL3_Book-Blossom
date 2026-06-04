using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBlossom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CleanupAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Chuẩn hóa các log hợp lệ cũ về đúng số enum hiện tại (tạo trước 01/06/2026)
            migrationBuilder.Sql("UPDATE UserSystem.AuditLogs SET ActionType = 3 WHERE ActionType = 6 AND TableName = 'Users' AND CreatedAt < '2026-06-01 00:00:00';");
            migrationBuilder.Sql("UPDATE UserSystem.AuditLogs SET ActionType = 4 WHERE ActionType = 7 AND TableName = 'Users' AND CreatedAt < '2026-06-01 00:00:00';");
            migrationBuilder.Sql("UPDATE UserSystem.AuditLogs SET ActionType = 5 WHERE ActionType = 8 AND TableName = 'Orders' AND CreatedAt < '2026-06-01 00:00:00';");

            // 2. Xóa các log không thuộc 8 loại hành động hệ thống hợp lệ
            // Xóa log cập nhật vai trò, thêm admin, sửa admin (đang mang ActionType = 3 nhưng có dữ liệu khác)
            migrationBuilder.Sql(@"
                DELETE FROM UserSystem.AuditLogs 
                WHERE ActionType = 3 
                  AND TableName = 'Users' 
                  AND (
                      OldData LIKE '%""Role""%' 
                      OR NewData LIKE '%""Role""%' 
                      OR NewData LIKE '%""Action"":""AddAdmin""%' 
                      OR (NewData LIKE '%""UserName""%' AND NewData LIKE '%""Email""%')
                  );
            ");

            // Xóa bất kỳ log nào có ActionType > 8
            migrationBuilder.Sql("DELETE FROM UserSystem.AuditLogs WHERE ActionType > 8;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
