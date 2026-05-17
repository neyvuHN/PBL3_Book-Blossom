using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BookBlossom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Preference");

            migrationBuilder.AlterColumn<byte>(
                name: "RoleID",
                schema: "UserSystem",
                table: "User",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryID);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleID = table.Column<byte>(type: "tinyint", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleID);
                });

            migrationBuilder.CreateTable(
                name: "CustomerPreference",
                schema: "Preference",
                columns: table => new
                {
                    CustomerID = table.Column<long>(type: "bigint", nullable: false),
                    CategoryID = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPreference", x => new { x.CustomerID, x.CategoryID });
                    table.ForeignKey(
                        name: "FK_CustomerPreference_Categories_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "Categories",
                        principalColumn: "CategoryID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerPreference_CustomerDetail_CustomerID",
                        column: x => x.CustomerID,
                        principalSchema: "UserSystem",
                        principalTable: "CustomerDetail",
                        principalColumn: "CustomerID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleID", "Description", "RoleName" },
                values: new object[,]
                {
                    { (byte)1, "Quyền Guest", "Guest" },
                    { (byte)2, "Quyền Customer", "Customer" },
                    { (byte)3, "Quyền SystemAdmin", "SystemAdmin" },
                    { (byte)4, "Quyền Moderator", "Moderator" },
                    { (byte)5, "Quyền MarketingManager", "MarketingManager" },
                    { (byte)6, "Quyền StoreManager", "StoreManager" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_User_RoleID",
                schema: "UserSystem",
                table: "User",
                column: "RoleID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPreference_CategoryID",
                schema: "Preference",
                table: "CustomerPreference",
                column: "CategoryID");

            migrationBuilder.AddForeignKey(
                name: "FK_User_Roles_RoleID",
                schema: "UserSystem",
                table: "User",
                column: "RoleID",
                principalTable: "Roles",
                principalColumn: "RoleID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_User_Roles_RoleID",
                schema: "UserSystem",
                table: "User");

            migrationBuilder.DropTable(
                name: "CustomerPreference",
                schema: "Preference");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_User_RoleID",
                schema: "UserSystem",
                table: "User");

            migrationBuilder.AlterColumn<int>(
                name: "RoleID",
                schema: "UserSystem",
                table: "User",
                type: "int",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");
        }
    }
}
