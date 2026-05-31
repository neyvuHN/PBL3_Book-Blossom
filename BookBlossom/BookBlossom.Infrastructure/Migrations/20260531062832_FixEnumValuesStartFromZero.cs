using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBlossom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixEnumValuesStartFromZero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // =====================================================================
            // Cập nhật User.RoleID từ giá trị cũ (Guest=1, Customer=2, Admin=3)
            // sang giá trị mới (Guest=0, Customer=1, Admin=2)
            //
            // Thứ tự đúng:
            //   1. Disable FK
            //   2. User → giá trị tạm (100,101,102) để tránh conflict với Role PK
            //   3. Cập nhật Role seed data (xóa 3, rename 1→Customer, 2→Admin, insert 0→Guest)
            //   4. User → giá trị mới (0,1,2) — lúc này Role đã có đủ PK 0,1,2
            //   5. Re-enable FK với WITH CHECK để validate
            // =====================================================================

            // Bước 1: Disable FK
            migrationBuilder.Sql("ALTER TABLE UserSystem.[User] NOCHECK CONSTRAINT ALL;");

            // Bước 2: User → giá trị tạm
            migrationBuilder.Sql("UPDATE UserSystem.[User] SET RoleID = 100 WHERE RoleID = 1;"); // Guest cũ → tạm
            migrationBuilder.Sql("UPDATE UserSystem.[User] SET RoleID = 101 WHERE RoleID = 2;"); // Customer cũ → tạm
            migrationBuilder.Sql("UPDATE UserSystem.[User] SET RoleID = 102 WHERE RoleID = 3;"); // Admin cũ → tạm

            // Bước 3a: Xóa Role cũ RoleID=3 (Admin cũ)
            migrationBuilder.DeleteData(
                schema: "UserSystem",
                table: "Role",
                keyColumn: "RoleID",
                keyValue: (byte)3);

            // Bước 3b: Đổi tên Role 1 → Customer, Role 2 → Admin
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

            // Bước 3c: Insert Role mới RoleID=0 (Guest)
            migrationBuilder.InsertData(
                schema: "UserSystem",
                table: "Role",
                columns: new[] { "RoleID", "Description", "RoleName" },
                values: new object[] { (byte)0, "Quyền Guest", "Guest" });

            // Bước 4: User → giá trị mới (Role đã có 0,1,2 nên FK sẽ hợp lệ)
            migrationBuilder.Sql("UPDATE UserSystem.[User] SET RoleID = 0 WHERE RoleID = 100;"); // Guest mới = 0
            migrationBuilder.Sql("UPDATE UserSystem.[User] SET RoleID = 1 WHERE RoleID = 101;"); // Customer mới = 1
            migrationBuilder.Sql("UPDATE UserSystem.[User] SET RoleID = 2 WHERE RoleID = 102;"); // Admin mới = 2

            // Bước 5: Re-enable FK và validate
            migrationBuilder.Sql("ALTER TABLE UserSystem.[User] WITH CHECK CHECK CONSTRAINT ALL;");

            // Cập nhật default value của OrderStatus (0 = Pending mới)
            migrationBuilder.AlterColumn<byte>(
                name: "OrderStatus",
                schema: "OrderRequest",
                table: "Orders",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0,
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldDefaultValue: (byte)1);
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "UserSystem",
                table: "Role",
                keyColumn: "RoleID",
                keyValue: (byte)0);

            migrationBuilder.AlterColumn<byte>(
                name: "OrderStatus",
                schema: "OrderRequest",
                table: "Orders",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1,
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldDefaultValue: (byte)0);

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
        }
    }
}
