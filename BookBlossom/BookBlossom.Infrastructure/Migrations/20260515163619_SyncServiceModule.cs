using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BookBlossom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncServiceModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "UserSystem");

            migrationBuilder.EnsureSchema(
                name: "Service");

            migrationBuilder.RenameTable(
                name: "User",
                newName: "User",
                newSchema: "UserSystem");

            migrationBuilder.RenameTable(
                name: "StaffDetail",
                newName: "StaffDetail",
                newSchema: "UserSystem");

            migrationBuilder.RenameTable(
                name: "OTPLogs",
                newName: "OTPLogs",
                newSchema: "UserSystem");

            migrationBuilder.RenameTable(
                name: "GuestDetail",
                newName: "GuestDetail",
                newSchema: "UserSystem");

            migrationBuilder.RenameTable(
                name: "CustomerDetail",
                newName: "CustomerDetail",
                newSchema: "UserSystem");

            migrationBuilder.AlterColumn<long>(
                name: "UserID",
                schema: "UserSystem",
                table: "User",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<long>(
                name: "StaffID",
                schema: "UserSystem",
                table: "StaffDetail",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                schema: "UserSystem",
                table: "OTPLogs",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "OTPCode",
                schema: "UserSystem",
                table: "OTPLogs",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                schema: "UserSystem",
                table: "OTPLogs",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "ConvertedUserID",
                schema: "UserSystem",
                table: "GuestDetail",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "CustomerID",
                schema: "UserSystem",
                table: "CustomerDetail",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "ServiceHistory",
                schema: "Service",
                columns: table => new
                {
                    HistoryID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerID = table.Column<long>(type: "bigint", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<byte>(type: "tinyint", nullable: false),
                    PaymentStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAutoRenew = table.Column<bool>(type: "bit", nullable: true),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceHistory", x => x.HistoryID);
                    table.ForeignKey(
                        name: "FK_ServiceHistory_User_CustomerID",
                        column: x => x.CustomerID,
                        principalSchema: "UserSystem",
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServicePackage",
                schema: "Service",
                columns: table => new
                {
                    PackageID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DurationDay = table.Column<int>(type: "int", nullable: false),
                    ThreadLimit = table.Column<int>(type: "int", nullable: false),
                    UndoLimit = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePackage", x => x.PackageID);
                });

            migrationBuilder.CreateTable(
                name: "CustomerService",
                schema: "Service",
                columns: table => new
                {
                    CustomerID = table.Column<long>(type: "bigint", nullable: false),
                    CurrentPackageID = table.Column<long>(type: "bigint", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerService", x => x.CustomerID);
                    table.ForeignKey(
                        name: "FK_CustomerService_ServicePackage_CurrentPackageID",
                        column: x => x.CurrentPackageID,
                        principalSchema: "Service",
                        principalTable: "ServicePackage",
                        principalColumn: "PackageID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerService_User_CustomerID",
                        column: x => x.CustomerID,
                        principalSchema: "UserSystem",
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "ServicePackage",
                columns: new[] { "PackageID", "Description", "DurationDay", "PackageName", "Price", "ThreadLimit", "UndoLimit" },
                values: new object[,]
                {
                    { 1L, "Gói miễn phí: 3 thread/tháng, 2 undo Tindbook.", 0, "Free", 0m, 3, 2 },
                    { 2L, "Gói Cơ bản: 20 thread/tháng, 5 undo Tindbook.", 30, "Basic", 99000m, 20, 5 },
                    { 3L, "Gói Chuyên nghiệp: Không giới hạn thread và undo.", 30, "Pro", 199000m, 999999, 999999 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerService_CurrentPackageID",
                schema: "Service",
                table: "CustomerService",
                column: "CurrentPackageID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceHistory_CustomerID",
                schema: "Service",
                table: "ServiceHistory",
                column: "CustomerID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerService",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "ServiceHistory",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "ServicePackage",
                schema: "Service");

            migrationBuilder.RenameTable(
                name: "User",
                schema: "UserSystem",
                newName: "User");

            migrationBuilder.RenameTable(
                name: "StaffDetail",
                schema: "UserSystem",
                newName: "StaffDetail");

            migrationBuilder.RenameTable(
                name: "OTPLogs",
                schema: "UserSystem",
                newName: "OTPLogs");

            migrationBuilder.RenameTable(
                name: "GuestDetail",
                schema: "UserSystem",
                newName: "GuestDetail");

            migrationBuilder.RenameTable(
                name: "CustomerDetail",
                schema: "UserSystem",
                newName: "CustomerDetail");

            migrationBuilder.AlterColumn<int>(
                name: "UserID",
                table: "User",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "StaffID",
                table: "StaffDetail",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "OTPLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(15)",
                oldMaxLength: 15);

            migrationBuilder.AlterColumn<string>(
                name: "OTPCode",
                table: "OTPLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "OTPLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(45)",
                oldMaxLength: 45,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ConvertedUserID",
                table: "GuestDetail",
                type: "int",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CustomerID",
                table: "CustomerDetail",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");
        }
    }
}
