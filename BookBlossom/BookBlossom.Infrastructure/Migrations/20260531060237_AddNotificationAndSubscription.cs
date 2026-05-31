using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBlossom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationAndSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Notification");

            migrationBuilder.Sql(@"
                IF OBJECT_ID('Notification.Subscription', 'U') IS NULL
                BEGIN
                    CREATE TABLE [Notification].[Subscription] (
                        [FollowID] bigint IDENTITY(1,1) NOT NULL,
                        [CustomerID] bigint NOT NULL,
                        [TargetID] bigint NOT NULL,
                        [TargetType] tinyint NOT NULL,
                        [CreatedDate] datetime2 NOT NULL,
                        CONSTRAINT [PK_Subscription] PRIMARY KEY ([FollowID]),
                        CONSTRAINT [FK_Subscription_User_CustomerID] FOREIGN KEY ([CustomerID]) REFERENCES [UserSystem].[User] ([UserID]) ON DELETE CASCADE
                    );
                END
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID('Notification.UserFollow', 'U') IS NULL
                BEGIN
                    CREATE TABLE [Notification].[UserFollow] (
                        [NotificationID] bigint IDENTITY(1,1) NOT NULL,
                        [UserID] bigint NOT NULL,
                        [Title] nvarchar(255) NOT NULL,
                        [Content] nvarchar(max) NOT NULL,
                        [NotificationType] tinyint NOT NULL,
                        [ReferenceID] int NULL,
                        [ReferenceType] tinyint NULL,
                        [IsRead] bit NOT NULL,
                        [CreatedDate] datetime2 NOT NULL,
                        CONSTRAINT [PK_UserFollow] PRIMARY KEY ([NotificationID]),
                        CONSTRAINT [FK_UserFollow_User_UserID] FOREIGN KEY ([UserID]) REFERENCES [UserSystem].[User] ([UserID]) ON DELETE CASCADE
                    );
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Subscription_CustomerID_TargetID_TargetType' AND object_id = OBJECT_ID('Notification.Subscription'))
                BEGIN
                    CREATE INDEX [IX_Subscription_CustomerID_TargetID_TargetType] ON [Notification].[Subscription] ([CustomerID], [TargetID], [TargetType]);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserFollow_UserID_IsRead' AND object_id = OBJECT_ID('Notification.UserFollow'))
                BEGIN
                    CREATE INDEX [IX_UserFollow_UserID_IsRead] ON [Notification].[UserFollow] ([UserID], [IsRead]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF OBJECT_ID('Notification.Subscription', 'U') IS NOT NULL
                BEGIN
                    DROP TABLE [Notification].[Subscription];
                END
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID('Notification.UserFollow', 'U') IS NOT NULL
                BEGIN
                    DROP TABLE [Notification].[UserFollow];
                END
            ");
        }
    }
}
