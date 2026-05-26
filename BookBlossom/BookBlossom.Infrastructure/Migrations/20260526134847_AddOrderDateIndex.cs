using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBlossom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderDateIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderDate",
                schema: "OrderRequest",
                table: "Orders",
                column: "OrderDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderDate",
                schema: "OrderRequest",
                table: "Orders");
        }
    }
}
