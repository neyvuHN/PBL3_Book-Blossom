using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBlossom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DeleteThreeBlindBooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Cập nhật các bảng tham chiếu có thể chứa BlindBookID về NULL
            migrationBuilder.Sql("UPDATE OrderRequest.OrderDetail SET BlindBookID = NULL WHERE BlindBookID IN (2, 9, 11);");
            migrationBuilder.Sql("UPDATE dbo.SwipeLogs SET BlindBookID = NULL WHERE BlindBookID IN (2, 9, 11);");

            // 2. Xóa các bản ghi liên quan trong giỏ hàng và danh sách yêu thích
            migrationBuilder.Sql("DELETE FROM dbo.Cart WHERE BlindBookID IN (2, 9, 11);");
            migrationBuilder.Sql("DELETE FROM dbo.Wishlist WHERE BlindBookID IN (2, 9, 11);");

            // 3. Xóa các hình ảnh của BlindBook
            migrationBuilder.Sql("DELETE FROM Book.BlindBookImages WHERE BlindBookID IN (2, 9, 11);");

            // 4. Xóa chính gói BlindBook
            migrationBuilder.Sql("DELETE FROM Book.BlindBook WHERE BlindBookID IN (2, 9, 11);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
