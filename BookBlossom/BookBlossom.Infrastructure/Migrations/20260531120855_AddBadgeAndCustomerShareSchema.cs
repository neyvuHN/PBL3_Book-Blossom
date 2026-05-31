using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBlossom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBadgeAndCustomerShareSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.DropForeignKey(
            //     name: "FK_CustomerReputation_CustomerDetail_CustomerDetailCustomerID",
            //     schema: "Rank",
            //     table: "CustomerReputation");

            // migrationBuilder.DropIndex(
            //     name: "IX_CustomerReputation_CustomerDetailCustomerID",
            //     schema: "Rank",
            //     table: "CustomerReputation");

            // migrationBuilder.DropColumn(
            //     name: "CustomerDetailCustomerID",
            //     schema: "Rank",
            //     table: "CustomerReputation");

            migrationBuilder.EnsureSchema(
                name: "Badge");

            migrationBuilder.EnsureSchema(
                name: "Review");

            migrationBuilder.AddColumn<bool>(
                name: "IsAccurate",
                schema: "Thread",
                table: "Report",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReputationMaxStreakStartDate",
                schema: "Rank",
                table: "CustomerReputation",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShareCount",
                schema: "UserSystem",
                table: "CustomerDetail",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Badge",
                schema: "Badge",
                columns: table => new
                {
                    BadgeID = table.Column<long>(type: "bigint", nullable: false),
                    BadgeName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IconPath = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ConditionDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Badge", x => x.BadgeID);
                });

            // migrationBuilder.CreateTable(
            //     name: "Review",
            //     schema: "Review",
            //     columns: table => new
            //     {
            //         ReviewID = table.Column<long>(type: "bigint", nullable: false)
            //             .Annotation("SqlServer:Identity", "1, 1"),
            //         CustomerID = table.Column<long>(type: "bigint", nullable: false),
            //         BookID = table.Column<long>(type: "bigint", nullable: true),
            //         BlindBookID = table.Column<long>(type: "bigint", nullable: true),
            //         Rating = table.Column<int>(type: "int", nullable: false),
            //         Content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
            //         ImageVideoPath = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
            //         LikeCount = table.Column<int>(type: "int", nullable: false),
            //         CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
            //         IsHidden = table.Column<bool>(type: "bit", nullable: false),
            //         IsReputationAwarded = table.Column<bool>(type: "bit", nullable: false)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_Review", x => x.ReviewID);
            //         table.ForeignKey(
            //             name: "FK_Review_User_CustomerID",
            //             column: x => x.CustomerID,
            //             principalSchema: "UserSystem",
            //             principalTable: "User",
            //             principalColumn: "UserID",
            //             onDelete: ReferentialAction.Cascade);
            //     });

            migrationBuilder.CreateTable(
                name: "BadgeCustomer",
                schema: "Badge",
                columns: table => new
                {
                    CustomerID = table.Column<long>(type: "bigint", nullable: false),
                    BadgeID = table.Column<long>(type: "bigint", nullable: false),
                    EarnedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadgeCustomer", x => new { x.CustomerID, x.BadgeID });
                    table.ForeignKey(
                        name: "FK_BadgeCustomer_Badge_BadgeID",
                        column: x => x.BadgeID,
                        principalSchema: "Badge",
                        principalTable: "Badge",
                        principalColumn: "BadgeID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BadgeCustomer_CustomerDetail_CustomerID",
                        column: x => x.CustomerID,
                        principalSchema: "UserSystem",
                        principalTable: "CustomerDetail",
                        principalColumn: "CustomerID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BadgeCustomer_BadgeID",
                schema: "Badge",
                table: "BadgeCustomer",
                column: "BadgeID");

            // migrationBuilder.CreateIndex(
            //     name: "IX_Review_CustomerID",
            //     schema: "Review",
            //     table: "Review",
            //     column: "CustomerID");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerReputation_CustomerDetail_CustomerID",
                schema: "Rank",
                table: "CustomerReputation",
                column: "CustomerID",
                principalSchema: "UserSystem",
                principalTable: "CustomerDetail",
                principalColumn: "CustomerID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerReputation_CustomerDetail_CustomerID",
                schema: "Rank",
                table: "CustomerReputation");

            migrationBuilder.DropTable(
                name: "BadgeCustomer",
                schema: "Badge");

            // migrationBuilder.DropTable(
            //     name: "Review",
            //     schema: "Review");

            migrationBuilder.DropTable(
                name: "Badge",
                schema: "Badge");

            migrationBuilder.DropColumn(
                name: "IsAccurate",
                schema: "Thread",
                table: "Report");

            migrationBuilder.DropColumn(
                name: "ReputationMaxStreakStartDate",
                schema: "Rank",
                table: "CustomerReputation");

            migrationBuilder.DropColumn(
                name: "ShareCount",
                schema: "UserSystem",
                table: "CustomerDetail");

            // migrationBuilder.AddColumn<long>(
            //     name: "CustomerDetailCustomerID",
            //     schema: "Rank",
            //     table: "CustomerReputation",
            //     type: "bigint",
            //     nullable: true);

            // migrationBuilder.CreateIndex(
            //     name: "IX_CustomerReputation_CustomerDetailCustomerID",
            //     schema: "Rank",
            //     table: "CustomerReputation",
            //     column: "CustomerDetailCustomerID");

            // migrationBuilder.AddForeignKey(
            //     name: "FK_CustomerReputation_CustomerDetail_CustomerDetailCustomerID",
            //     schema: "Rank",
            //     table: "CustomerReputation",
            //     column: "CustomerDetailCustomerID",
            //     principalSchema: "UserSystem",
            //     principalTable: "CustomerDetail",
            //     principalColumn: "CustomerID");
        }
    }
}
