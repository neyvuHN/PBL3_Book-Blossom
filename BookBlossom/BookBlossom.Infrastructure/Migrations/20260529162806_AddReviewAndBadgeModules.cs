using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBlossom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewAndBadgeModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Review");

            migrationBuilder.CreateTable(
                name: "Badge",
                schema: "Rank",
                columns: table => new
                {
                    BadgeID = table.Column<long>(type: "bigint", nullable: false),
                    BadgeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IconPath = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Badge", x => x.BadgeID);
                });

            migrationBuilder.CreateTable(
                name: "Review",
                schema: "Review",
                columns: table => new
                {
                    ReviewID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerID = table.Column<long>(type: "bigint", nullable: false),
                    BookID = table.Column<long>(type: "bigint", nullable: true),
                    BlindBookID = table.Column<long>(type: "bigint", nullable: true),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ImageVideoPath = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LikeCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsHidden = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsReputationAwarded = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Review", x => x.ReviewID);
                    table.ForeignKey(
                        name: "FK_Review_BlindBook_BlindBookID",
                        column: x => x.BlindBookID,
                        principalSchema: "Book",
                        principalTable: "BlindBook",
                        principalColumn: "BlindBookID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Review_RealBook_BookID",
                        column: x => x.BookID,
                        principalSchema: "Book",
                        principalTable: "RealBook",
                        principalColumn: "BookID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Review_User_CustomerID",
                        column: x => x.CustomerID,
                        principalSchema: "UserSystem",
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerBadge",
                schema: "Rank",
                columns: table => new
                {
                    CustomerID = table.Column<long>(type: "bigint", nullable: false),
                    BadgeID = table.Column<long>(type: "bigint", nullable: false),
                    EarnedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerBadge", x => new { x.CustomerID, x.BadgeID });
                    table.ForeignKey(
                        name: "FK_CustomerBadge_Badge_BadgeID",
                        column: x => x.BadgeID,
                        principalSchema: "Rank",
                        principalTable: "Badge",
                        principalColumn: "BadgeID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerBadge_User_CustomerID",
                        column: x => x.CustomerID,
                        principalSchema: "UserSystem",
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBadge_BadgeID",
                schema: "Rank",
                table: "CustomerBadge",
                column: "BadgeID");

            migrationBuilder.CreateIndex(
                name: "IX_Review_BlindBookID",
                schema: "Review",
                table: "Review",
                column: "BlindBookID");

            migrationBuilder.CreateIndex(
                name: "IX_Review_BookID",
                schema: "Review",
                table: "Review",
                column: "BookID");

            migrationBuilder.CreateIndex(
                name: "IX_Review_CreatedAt",
                schema: "Review",
                table: "Review",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Review_CustomerID",
                schema: "Review",
                table: "Review",
                column: "CustomerID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerBadge",
                schema: "Rank");

            migrationBuilder.DropTable(
                name: "Review",
                schema: "Review");

            migrationBuilder.DropTable(
                name: "Badge",
                schema: "Rank");
        }
    }
}
