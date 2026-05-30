using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBlossom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncBlindBookOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bảng 'Book.BlindBook' đã tồn tại sẵn trên Database server.
            // Migration này được giữ rỗng phần Up() để tránh lỗi CreateTable,
            // nhưng vẫn giữ trong Snapshot để EF Core tracking và làm việc bình thường.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rỗng vì Up() cũng rỗng
        }
    }
}
