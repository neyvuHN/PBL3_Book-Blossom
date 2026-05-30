using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBlossom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUndoFieldsToGuestDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DailyUndoCount",
                schema: "UserSystem",
                table: "GuestDetail",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUndoDate",
                schema: "UserSystem",
                table: "GuestDetail",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyUndoCount",
                schema: "UserSystem",
                table: "GuestDetail");

            migrationBuilder.DropColumn(
                name: "LastUndoDate",
                schema: "UserSystem",
                table: "GuestDetail");
        }
    }
}
