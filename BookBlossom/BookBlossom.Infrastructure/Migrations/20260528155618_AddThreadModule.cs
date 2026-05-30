using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBlossom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddThreadModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Thread");

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

            migrationBuilder.CreateTable(
                name: "ThreadPost",
                schema: "Thread",
                columns: table => new
                {
                    PostID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerID = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Hashtags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsHidden = table.Column<bool>(type: "bit", nullable: false),
                    ReportCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThreadPost", x => x.PostID);
                    table.ForeignKey(
                        name: "FK_ThreadPost_User_CustomerID",
                        column: x => x.CustomerID,
                        principalSchema: "UserSystem",
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ThreadComment",
                schema: "Thread",
                columns: table => new
                {
                    CommentID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostID = table.Column<long>(type: "bigint", nullable: false),
                    CustomerID = table.Column<long>(type: "bigint", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThreadComment", x => x.CommentID);
                    table.ForeignKey(
                        name: "FK_ThreadComment_ThreadPost_PostID",
                        column: x => x.PostID,
                        principalSchema: "Thread",
                        principalTable: "ThreadPost",
                        principalColumn: "PostID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ThreadComment_User_CustomerID",
                        column: x => x.CustomerID,
                        principalSchema: "UserSystem",
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ThreadImage",
                schema: "Thread",
                columns: table => new
                {
                    ImageID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostID = table.Column<long>(type: "bigint", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThreadImage", x => x.ImageID);
                    table.ForeignKey(
                        name: "FK_ThreadImage_ThreadPost_PostID",
                        column: x => x.PostID,
                        principalSchema: "Thread",
                        principalTable: "ThreadPost",
                        principalColumn: "PostID",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateIndex(
                name: "IX_ThreadComment_CustomerID",
                schema: "Thread",
                table: "ThreadComment",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_ThreadComment_PostID",
                schema: "Thread",
                table: "ThreadComment",
                column: "PostID");

            migrationBuilder.CreateIndex(
                name: "IX_ThreadImage_PostID",
                schema: "Thread",
                table: "ThreadImage",
                column: "PostID");

            migrationBuilder.CreateIndex(
                name: "IX_ThreadPost_CreatedAt",
                schema: "Thread",
                table: "ThreadPost",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ThreadPost_CustomerID",
                schema: "Thread",
                table: "ThreadPost",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_ThreadPost_ReportCount",
                schema: "Thread",
                table: "ThreadPost",
                column: "ReportCount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReturnRequest",
                schema: "OrderRequest");

            migrationBuilder.DropTable(
                name: "ThreadComment",
                schema: "Thread");

            migrationBuilder.DropTable(
                name: "ThreadImage",
                schema: "Thread");

            migrationBuilder.DropTable(
                name: "ThreadPost",
                schema: "Thread");
        }
    }
}
