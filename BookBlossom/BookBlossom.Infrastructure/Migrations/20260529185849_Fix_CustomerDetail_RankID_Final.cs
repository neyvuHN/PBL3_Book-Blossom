using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBlossom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fix_CustomerDetail_RankID_Final : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "PackageID",
                schema: "Service",
                table: "ServicePackage",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<long>(
                name: "CustomerDetailCustomerID",
                schema: "Rank",
                table: "CustomerReputation",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CurrentPackageID",
                schema: "UserSystem",
                table: "CustomerDetail",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "RankID",
                schema: "UserSystem",
                table: "CustomerDetail",
                type: "bigint",
                nullable: true);

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
                name: "IX_CustomerReputation_CustomerDetailCustomerID",
                schema: "Rank",
                table: "CustomerReputation",
                column: "CustomerDetailCustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDetail_CurrentPackageID",
                schema: "UserSystem",
                table: "CustomerDetail",
                column: "CurrentPackageID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDetail_RankID",
                schema: "UserSystem",
                table: "CustomerDetail",
                column: "RankID");

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

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerDetail_MembershipRank_RankID",
                schema: "UserSystem",
                table: "CustomerDetail",
                column: "RankID",
                principalSchema: "Rank",
                principalTable: "MembershipRank",
                principalColumn: "RankID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerDetail_ServicePackage_CurrentPackageID",
                schema: "UserSystem",
                table: "CustomerDetail",
                column: "CurrentPackageID",
                principalSchema: "Service",
                principalTable: "ServicePackage",
                principalColumn: "PackageID");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerReputation_CustomerDetail_CustomerDetailCustomerID",
                schema: "Rank",
                table: "CustomerReputation",
                column: "CustomerDetailCustomerID",
                principalSchema: "UserSystem",
                principalTable: "CustomerDetail",
                principalColumn: "CustomerID");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerService_CustomerDetail_CustomerID",
                schema: "Service",
                table: "CustomerService",
                column: "CustomerID",
                principalSchema: "UserSystem",
                principalTable: "CustomerDetail",
                principalColumn: "CustomerID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerDetail_MembershipRank_RankID",
                schema: "UserSystem",
                table: "CustomerDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerDetail_ServicePackage_CurrentPackageID",
                schema: "UserSystem",
                table: "CustomerDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerReputation_CustomerDetail_CustomerDetailCustomerID",
                schema: "Rank",
                table: "CustomerReputation");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerService_CustomerDetail_CustomerID",
                schema: "Service",
                table: "CustomerService");

            migrationBuilder.DropTable(
                name: "ReturnRequest",
                schema: "OrderRequest");

            migrationBuilder.DropIndex(
                name: "IX_CustomerReputation_CustomerDetailCustomerID",
                schema: "Rank",
                table: "CustomerReputation");

            migrationBuilder.DropIndex(
                name: "IX_CustomerDetail_CurrentPackageID",
                schema: "UserSystem",
                table: "CustomerDetail");

            migrationBuilder.DropIndex(
                name: "IX_CustomerDetail_RankID",
                schema: "UserSystem",
                table: "CustomerDetail");

            migrationBuilder.DropColumn(
                name: "CustomerDetailCustomerID",
                schema: "Rank",
                table: "CustomerReputation");

            migrationBuilder.DropColumn(
                name: "CurrentPackageID",
                schema: "UserSystem",
                table: "CustomerDetail");

            migrationBuilder.DropColumn(
                name: "RankID",
                schema: "UserSystem",
                table: "CustomerDetail");

            migrationBuilder.AlterColumn<long>(
                name: "PackageID",
                schema: "Service",
                table: "ServicePackage",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("SqlServer:Identity", "1, 1");
        }
    }
}
