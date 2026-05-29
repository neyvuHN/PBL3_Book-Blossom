using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBlossom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateServicePackageData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReturnRequest",
                schema: "OrderRequest",
                columns: table => new
                {
                    ReturnRequestID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderID = table.Column<long>(type: "bigint", nullable: false),
                    BookID = table.Column<long>(type: "bigint", nullable: false),
                    StaffID = table.Column<long>(type: "bigint", nullable: true),
                    ReturnQuantity = table.Column<int>(type: "int", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReturnReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnboxVideoPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResolutionType = table.Column<byte>(type: "tinyint", nullable: false),
                    ReturnStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    RejectReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GatewayTransactionID = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RefundCompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnRequest", x => x.ReturnRequestID);
                    table.ForeignKey(
                        name: "FK_ReturnRequest_Orders_OrderID",
                        column: x => x.OrderID,
                        principalSchema: "OrderRequest",
                        principalTable: "Orders",
                        principalColumn: "OrderID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReturnRequest_RealBook_BookID",
                        column: x => x.BookID,
                        principalSchema: "Book",
                        principalTable: "RealBook",
                        principalColumn: "BookID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReturnRequest_StaffDetail_StaffID",
                        column: x => x.StaffID,
                        principalSchema: "UserSystem",
                        principalTable: "StaffDetail",
                        principalColumn: "StaffID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequest_BookID",
                schema: "OrderRequest",
                table: "ReturnRequest",
                column: "BookID");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequest_OrderID",
                schema: "OrderRequest",
                table: "ReturnRequest",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequest_StaffID",
                schema: "OrderRequest",
                table: "ReturnRequest",
                column: "StaffID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReturnRequest",
                schema: "OrderRequest");
        }
    }
}
