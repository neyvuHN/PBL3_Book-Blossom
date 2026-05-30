using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBlossom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipRankAndReputationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_User_Roles_RoleID",
                schema: "UserSystem",
                table: "User");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Roles",
                table: "Roles");

            migrationBuilder.EnsureSchema(
                name: "Rank");

            migrationBuilder.EnsureSchema(
                name: "OrderRequest");

            migrationBuilder.RenameTable(
                name: "Roles",
                newName: "Role",
                newSchema: "UserSystem");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Role",
                schema: "UserSystem",
                table: "Role",
                column: "RoleID");

            migrationBuilder.CreateTable(
                name: "MembershipRank",
                schema: "Rank",
                columns: table => new
                {
                    RankID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RankType = table.Column<byte>(type: "tinyint", nullable: true),
                    MinSpending = table.Column<decimal>(type: "decimal(18,2)", nullable: true, defaultValue: 0m),
                    DiscountRate = table.Column<decimal>(type: "decimal(18,2)", nullable: true, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipRank", x => x.RankID);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                schema: "OrderRequest",
                columns: table => new
                {
                    OrderID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerID = table.Column<long>(type: "bigint", nullable: false),
                    AddressID = table.Column<long>(type: "bigint", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "getdate()"),
                    ShippedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrderStatus = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    PaymentMethod = table.Column<byte>(type: "tinyint", nullable: false),
                    PaymentStatus = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                    ShippingFee = table.Column<decimal>(type: "decimal(18,2)", nullable: true, defaultValue: 0m),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true, defaultValue: 0m),
                    ShipReceiverName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ShipPhoneNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ShipDetailAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeliveredDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.OrderID);
                });

            migrationBuilder.CreateTable(
                name: "CustomerReputation",
                schema: "Rank",
                columns: table => new
                {
                    CustomerID = table.Column<long>(type: "bigint", nullable: false),
                    RankID = table.Column<long>(type: "bigint", nullable: true),
                    ReputationPoint = table.Column<int>(type: "int", nullable: true, defaultValue: 100)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerReputation", x => x.CustomerID);
                    table.ForeignKey(
                        name: "FK_CustomerReputation_MembershipRank_RankID",
                        column: x => x.RankID,
                        principalSchema: "Rank",
                        principalTable: "MembershipRank",
                        principalColumn: "RankID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OrderDetail",
                schema: "OrderRequest",
                columns: table => new
                {
                    OrderID = table.Column<long>(type: "bigint", nullable: false),
                    BookID = table.Column<long>(type: "bigint", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", nullable: true, defaultValue: 0m),
                    BlindBookID = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderDetail", x => new { x.OrderID, x.BookID });
                    table.ForeignKey(
                        name: "FK_OrderDetail_BlindBook_BlindBookID",
                        column: x => x.BlindBookID,
                        principalSchema: "Book",
                        principalTable: "BlindBook",
                        principalColumn: "BlindBookID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OrderDetail_Orders_OrderID",
                        column: x => x.OrderID,
                        principalSchema: "OrderRequest",
                        principalTable: "Orders",
                        principalColumn: "OrderID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderDetail_RealBook_BookID",
                        column: x => x.BookID,
                        principalSchema: "Book",
                        principalTable: "RealBook",
                        principalColumn: "BookID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReputation_RankID",
                schema: "Rank",
                table: "CustomerReputation",
                column: "RankID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetail_BlindBookID",
                schema: "OrderRequest",
                table: "OrderDetail",
                column: "BlindBookID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetail_BookID",
                schema: "OrderRequest",
                table: "OrderDetail",
                column: "BookID");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerID",
                schema: "OrderRequest",
                table: "Orders",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderStatus",
                schema: "OrderRequest",
                table: "Orders",
                column: "OrderStatus");

            migrationBuilder.AddForeignKey(
                name: "FK_User_Role_RoleID",
                schema: "UserSystem",
                table: "User",
                column: "RoleID",
                principalSchema: "UserSystem",
                principalTable: "Role",
                principalColumn: "RoleID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_User_Role_RoleID",
                schema: "UserSystem",
                table: "User");

            migrationBuilder.DropTable(
                name: "CustomerReputation",
                schema: "Rank");

            migrationBuilder.DropTable(
                name: "OrderDetail",
                schema: "OrderRequest");

            migrationBuilder.DropTable(
                name: "MembershipRank",
                schema: "Rank");

            migrationBuilder.DropTable(
                name: "Orders",
                schema: "OrderRequest");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Role",
                schema: "UserSystem",
                table: "Role");

            migrationBuilder.RenameTable(
                name: "Role",
                schema: "UserSystem",
                newName: "Roles");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Roles",
                table: "Roles",
                column: "RoleID");

            migrationBuilder.AddForeignKey(
                name: "FK_User_Roles_RoleID",
                schema: "UserSystem",
                table: "User",
                column: "RoleID",
                principalTable: "Roles",
                principalColumn: "RoleID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
