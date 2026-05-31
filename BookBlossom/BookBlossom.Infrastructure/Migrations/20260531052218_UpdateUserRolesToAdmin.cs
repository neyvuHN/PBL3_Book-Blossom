using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BookBlossom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserRolesToAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE UserSystem.[User] SET RoleID = 3 WHERE RoleID IN (4, 5, 6)");

            migrationBuilder.DeleteData(
                schema: "UserSystem",
                table: "Role",
                keyColumn: "RoleID",
                keyValue: (byte)4);

            migrationBuilder.DeleteData(
                schema: "UserSystem",
                table: "Role",
                keyColumn: "RoleID",
                keyValue: (byte)5);

            migrationBuilder.DeleteData(
                schema: "UserSystem",
                table: "Role",
                keyColumn: "RoleID",
                keyValue: (byte)6);

            migrationBuilder.UpdateData(
                schema: "UserSystem",
                table: "Role",
                keyColumn: "RoleID",
                keyValue: (byte)3,
                columns: new[] { "Description", "RoleName" },
                values: new object[] { "Quyền Admin", "Admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "UserSystem",
                table: "Role",
                keyColumn: "RoleID",
                keyValue: (byte)3,
                columns: new[] { "Description", "RoleName" },
                values: new object[] { "Quyền SystemAdmin", "SystemAdmin" });

            migrationBuilder.InsertData(
                schema: "UserSystem",
                table: "Role",
                columns: new[] { "RoleID", "Description", "RoleName" },
                values: new object[,]
                {
                    { (byte)4, "Quyền Moderator", "Moderator" },
                    { (byte)5, "Quyền MarketingManager", "MarketingManager" },
                    { (byte)6, "Quyền StoreManager", "StoreManager" }
                });
        }
    }
}
