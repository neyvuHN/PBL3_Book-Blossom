using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBlossom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddThreadLikeShare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'Thread.ThreadPost', N'LikeCount') IS NULL
    ALTER TABLE [Thread].[ThreadPost] ADD [LikeCount] int NOT NULL CONSTRAINT [DF_ThreadPost_LikeCount] DEFAULT 0;

IF COL_LENGTH(N'Thread.ThreadPost', N'ShareCount') IS NULL
    ALTER TABLE [Thread].[ThreadPost] ADD [ShareCount] int NOT NULL CONSTRAINT [DF_ThreadPost_ShareCount] DEFAULT 0;

IF OBJECT_ID(N'[Thread].[ThreadLike]', N'U') IS NULL
BEGIN
    CREATE TABLE [Thread].[ThreadLike] (
        [PostID] bigint NOT NULL,
        [CustomerID] bigint NOT NULL,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_ThreadLike_CreatedAt] DEFAULT (SYSDATETIME()),
        CONSTRAINT [PK_ThreadLike] PRIMARY KEY ([PostID], [CustomerID]),
        CONSTRAINT [FK_ThreadLike_ThreadPost_PostID] FOREIGN KEY ([PostID]) REFERENCES [Thread].[ThreadPost] ([PostID]) ON DELETE CASCADE,
        CONSTRAINT [FK_ThreadLike_User_CustomerID] FOREIGN KEY ([CustomerID]) REFERENCES [UserSystem].[User] ([UserID]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ThreadLike_CustomerID'
      AND object_id = OBJECT_ID(N'[Thread].[ThreadLike]')
)
    CREATE INDEX [IX_ThreadLike_CustomerID] ON [Thread].[ThreadLike] ([CustomerID]);

IF OBJECT_ID(N'[Thread].[ThreadShare]', N'U') IS NULL
BEGIN
    CREATE TABLE [Thread].[ThreadShare] (
        [ShareID] bigint IDENTITY(1,1) NOT NULL,
        [PostID] bigint NOT NULL,
        [CustomerID] bigint NOT NULL,
        [ShareUrl] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_ThreadShare_CreatedAt] DEFAULT (SYSDATETIME()),
        CONSTRAINT [PK_ThreadShare] PRIMARY KEY ([ShareID]),
        CONSTRAINT [FK_ThreadShare_ThreadPost_PostID] FOREIGN KEY ([PostID]) REFERENCES [Thread].[ThreadPost] ([PostID]) ON DELETE CASCADE,
        CONSTRAINT [FK_ThreadShare_User_CustomerID] FOREIGN KEY ([CustomerID]) REFERENCES [UserSystem].[User] ([UserID]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ThreadShare_CreatedAt'
      AND object_id = OBJECT_ID(N'[Thread].[ThreadShare]')
)
    CREATE INDEX [IX_ThreadShare_CreatedAt] ON [Thread].[ThreadShare] ([CreatedAt]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ThreadShare_CustomerID'
      AND object_id = OBJECT_ID(N'[Thread].[ThreadShare]')
)
    CREATE INDEX [IX_ThreadShare_CustomerID] ON [Thread].[ThreadShare] ([CustomerID]);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ThreadShare_PostID'
      AND object_id = OBJECT_ID(N'[Thread].[ThreadShare]')
)
    CREATE INDEX [IX_ThreadShare_PostID] ON [Thread].[ThreadShare] ([PostID]);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[Thread].[ThreadLike]', N'U') IS NOT NULL
    DROP TABLE [Thread].[ThreadLike];

IF OBJECT_ID(N'[Thread].[ThreadShare]', N'U') IS NOT NULL
    DROP TABLE [Thread].[ThreadShare];

IF COL_LENGTH(N'Thread.ThreadPost', N'LikeCount') IS NOT NULL
BEGIN
    DECLARE @LikeConstraintName sysname;
    SELECT @LikeConstraintName = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    INNER JOIN sys.tables t ON t.object_id = c.object_id
    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = N'Thread' AND t.name = N'ThreadPost' AND c.name = N'LikeCount';

    IF @LikeConstraintName IS NOT NULL
        EXEC(N'ALTER TABLE [Thread].[ThreadPost] DROP CONSTRAINT [' + @LikeConstraintName + N']');

    ALTER TABLE [Thread].[ThreadPost] DROP COLUMN [LikeCount];
END;

IF COL_LENGTH(N'Thread.ThreadPost', N'ShareCount') IS NOT NULL
BEGIN
    DECLARE @ShareConstraintName sysname;
    SELECT @ShareConstraintName = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    INNER JOIN sys.tables t ON t.object_id = c.object_id
    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = N'Thread' AND t.name = N'ThreadPost' AND c.name = N'ShareCount';

    IF @ShareConstraintName IS NOT NULL
        EXEC(N'ALTER TABLE [Thread].[ThreadPost] DROP CONSTRAINT [' + @ShareConstraintName + N']');

    ALTER TABLE [Thread].[ThreadPost] DROP COLUMN [ShareCount];
END;
");
        }
    }
}
