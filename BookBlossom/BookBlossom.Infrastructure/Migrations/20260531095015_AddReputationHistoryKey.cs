using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBlossom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReputationHistoryKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerReputation_RankID",
                schema: "Rank",
                table: "CustomerReputation");

            migrationBuilder.DeleteData(
                schema: "UserSystem",
                table: "Role",
                keyColumn: "RoleID",
                keyValue: (byte)3);

            migrationBuilder.EnsureSchema(
                name: "Review");

            migrationBuilder.CreateTable(
                name: "ReputationHistory",
                schema: "Rank",
                columns: table => new
                {
                    HistoryID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerID = table.Column<long>(type: "bigint", nullable: false),
                    ChangeAmount = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferenceType = table.Column<byte>(type: "tinyint", nullable: false),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReputationHistory", x => x.HistoryID);
                    table.ForeignKey(
                        name: "FK_ReputationHistory_CustomerReputation_CustomerID",
                        column: x => x.CustomerID,
                        principalSchema: "Rank",
                        principalTable: "CustomerReputation",
                        principalColumn: "CustomerID",
                        onDelete: ReferentialAction.Restrict);
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
                    LikeCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsHidden = table.Column<bool>(type: "bit", nullable: false),
                    IsReputationAwarded = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Review", x => x.ReviewID);
                    table.ForeignKey(
                        name: "FK_Review_User_CustomerID",
                        column: x => x.CustomerID,
                        principalSchema: "UserSystem",
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                schema: "UserSystem",
                table: "Role",
                keyColumn: "RoleID",
                keyValue: (byte)1,
                columns: new[] { "Description", "RoleName" },
                values: new object[] { "Quyền Customer", "Customer" });

            migrationBuilder.UpdateData(
                schema: "UserSystem",
                table: "Role",
                keyColumn: "RoleID",
                keyValue: (byte)2,
                columns: new[] { "Description", "RoleName" },
                values: new object[] { "Quyền Admin", "Admin" });

            migrationBuilder.InsertData(
                schema: "UserSystem",
                table: "Role",
                columns: new[] { "RoleID", "Description", "RoleName" },
                values: new object[] { (byte)0, "Quyền Guest", "Guest" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReputation_RankID",
                schema: "Rank",
                table: "CustomerReputation",
                column: "RankID",
                unique: true,
                filter: "[RankID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationHistory_CustomerID",
                schema: "Rank",
                table: "ReputationHistory",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_Review_CustomerID",
                schema: "Review",
                table: "Review",
                column: "CustomerID");

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
                name: "ReputationHistory",
                schema: "Rank");

            migrationBuilder.DropTable(
                name: "Review",
                schema: "Review");

            migrationBuilder.DropIndex(
                name: "IX_CustomerReputation_RankID",
                schema: "Rank",
                table: "CustomerReputation");

            migrationBuilder.DeleteData(
                schema: "UserSystem",
                table: "Role",
                keyColumn: "RoleID",
                keyValue: (byte)0);

            migrationBuilder.UpdateData(
                schema: "UserSystem",
                table: "Role",
                keyColumn: "RoleID",
                keyValue: (byte)1,
                columns: new[] { "Description", "RoleName" },
                values: new object[] { "Quyền Guest", "Guest" });

            migrationBuilder.UpdateData(
                schema: "UserSystem",
                table: "Role",
                keyColumn: "RoleID",
                keyValue: (byte)2,
                columns: new[] { "Description", "RoleName" },
                values: new object[] { "Quyền Customer", "Customer" });

            migrationBuilder.InsertData(
                schema: "UserSystem",
                table: "Role",
                columns: new[] { "RoleID", "Description", "RoleName" },
                values: new object[] { (byte)3, "Quyền Admin", "Admin" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReputation_RankID",
                schema: "Rank",
                table: "CustomerReputation",
                column: "RankID");
        }
    }
}
