using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBlossom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixGuestRelationshipTo1N : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OTPLogs",
                columns: table => new
                {
                    LogID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<long>(type: "bigint", nullable: true),
                    GuestID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OTPCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpireAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OTPLogs", x => x.LogID);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleID = table.Column<int>(type: "int", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AccountStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Avatar = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Birthday = table.Column<DateTime>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefreshTokenExpiryTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.UserID);
                });

            migrationBuilder.CreateTable(
                name: "CustomerDetail",
                columns: table => new
                {
                    CustomerID = table.Column<int>(type: "int", nullable: false),
                    IsOnboardingCompleted = table.Column<bool>(type: "bit", nullable: false),
                    TotalSpending = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DailyUndoCount = table.Column<int>(type: "int", nullable: false),
                    LastUndoDate = table.Column<DateTime>(type: "date", nullable: true),
                    CurrentMonthThreadCount = table.Column<int>(type: "int", nullable: false),
                    LastThreadResetDate = table.Column<DateTime>(type: "date", nullable: true),
                    CurrentOrderStreak = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerDetail", x => x.CustomerID);
                    table.ForeignKey(
                        name: "FK_CustomerDetail_User_CustomerID",
                        column: x => x.CustomerID,
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuestDetail",
                columns: table => new
                {
                    GuestID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionToken = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    LastActiveAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    IPAddress = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    DeviceInfo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConvertedUserID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestDetail", x => x.GuestID);
                    table.ForeignKey(
                        name: "FK_GuestDetail_User_ConvertedUserID",
                        column: x => x.ConvertedUserID,
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffDetail",
                columns: table => new
                {
                    StaffID = table.Column<int>(type: "int", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsOnboardingCompleted = table.Column<bool>(type: "bit", nullable: false),
                    Department = table.Column<byte>(type: "tinyint", nullable: false),
                    Position = table.Column<byte>(type: "tinyint", nullable: false),
                    HireDate = table.Column<DateTime>(type: "date", nullable: false),
                    ContractType = table.Column<byte>(type: "tinyint", nullable: false),
                    Salary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BankAccount = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TaxCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Experience = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KPIScore = table.Column<decimal>(type: "decimal(3,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffDetail", x => x.StaffID);
                    table.ForeignKey(
                        name: "FK_StaffDetail_User_StaffID",
                        column: x => x.StaffID,
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuestDetail_ConvertedUserID",
                table: "GuestDetail",
                column: "ConvertedUserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerDetail");

            migrationBuilder.DropTable(
                name: "GuestDetail");

            migrationBuilder.DropTable(
                name: "OTPLogs");

            migrationBuilder.DropTable(
                name: "StaffDetail");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
