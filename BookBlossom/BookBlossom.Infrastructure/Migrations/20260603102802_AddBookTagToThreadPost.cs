using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBlossom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookTagToThreadPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BookID",
                schema: "Thread",
                table: "ThreadPost",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThreadPost_BookID",
                schema: "Thread",
                table: "ThreadPost",
                column: "BookID");

            migrationBuilder.AddForeignKey(
                name: "FK_ThreadPost_RealBook_BookID",
                schema: "Thread",
                table: "ThreadPost",
                column: "BookID",
                principalSchema: "Book",
                principalTable: "RealBook",
                principalColumn: "BookID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ThreadPost_RealBook_BookID",
                schema: "Thread",
                table: "ThreadPost");

            migrationBuilder.DropIndex(
                name: "IX_ThreadPost_BookID",
                schema: "Thread",
                table: "ThreadPost");

            migrationBuilder.DropColumn(
                name: "BookID",
                schema: "Thread",
                table: "ThreadPost");
        }
    }
}
