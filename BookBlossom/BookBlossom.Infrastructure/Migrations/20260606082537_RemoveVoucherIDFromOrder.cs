using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBlossom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveVoucherIDFromOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Voucher_VoucherID",
                schema: "OrderRequest",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_VoucherID",
                schema: "OrderRequest",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VoucherID",
                schema: "OrderRequest",
                table: "Orders");

            migrationBuilder.CreateTable(
                name: "OrderVouchers",
                columns: table => new
                {
                    OrderID = table.Column<long>(type: "bigint", nullable: false),
                    VoucherID = table.Column<long>(type: "bigint", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderVouchers", x => new { x.OrderID, x.VoucherID });
                    table.ForeignKey(
                        name: "FK_OrderVouchers_Orders_OrderID",
                        column: x => x.OrderID,
                        principalSchema: "OrderRequest",
                        principalTable: "Orders",
                        principalColumn: "OrderID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderVouchers_Voucher_VoucherID",
                        column: x => x.VoucherID,
                        principalSchema: "Voucher",
                        principalTable: "Voucher",
                        principalColumn: "VoucherID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderVouchers_VoucherID",
                table: "OrderVouchers",
                column: "VoucherID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderVouchers");

            migrationBuilder.AddColumn<long>(
                name: "VoucherID",
                schema: "OrderRequest",
                table: "Orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_VoucherID",
                schema: "OrderRequest",
                table: "Orders",
                column: "VoucherID");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Voucher_VoucherID",
                schema: "OrderRequest",
                table: "Orders",
                column: "VoucherID",
                principalSchema: "Voucher",
                principalTable: "Voucher",
                principalColumn: "VoucherID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
