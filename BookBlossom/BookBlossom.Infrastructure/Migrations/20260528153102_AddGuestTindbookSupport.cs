using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBlossom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestTindbookSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "CustomerID",
                table: "SwipeLogs",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<Guid>(
                name: "GuestID",
                table: "SwipeLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GuestPreference",
                schema: "Preference",
                columns: table => new
                {
                    GuestID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryID = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestPreference", x => new { x.GuestID, x.CategoryID });
                    table.ForeignKey(
                        name: "FK_GuestPreference_Category_CategoryID",
                        column: x => x.CategoryID,
                        principalSchema: "Preference",
                        principalTable: "Category",
                        principalColumn: "CategoryID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GuestPreference_GuestDetail_GuestID",
                        column: x => x.GuestID,
                        principalSchema: "UserSystem",
                        principalTable: "GuestDetail",
                        principalColumn: "GuestID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuestPreference_CategoryID",
                schema: "Preference",
                table: "GuestPreference",
                column: "CategoryID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuestPreference",
                schema: "Preference");

            migrationBuilder.DropColumn(
                name: "GuestID",
                table: "SwipeLogs");

            migrationBuilder.AlterColumn<long>(
                name: "CustomerID",
                table: "SwipeLogs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
