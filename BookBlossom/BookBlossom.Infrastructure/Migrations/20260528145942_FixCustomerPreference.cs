using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBlossom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixCustomerPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Preference')
                BEGIN
                    EXEC('CREATE SCHEMA [Preference]');
                END

                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Preference].[CustomerPreference]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [Preference].[CustomerPreference] (
                        [CustomerID] bigint NOT NULL,
                        [CategoryID] bigint NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_CustomerPreference] PRIMARY KEY ([CustomerID], [CategoryID]),
                        CONSTRAINT [FK_CustomerPreference_Categories_CategoryID] FOREIGN KEY ([CategoryID]) REFERENCES [Preference].[Category] ([CategoryID]) ON DELETE CASCADE,
                        CONSTRAINT [FK_CustomerPreference_CustomerDetail_CustomerID] FOREIGN KEY ([CustomerID]) REFERENCES [UserSystem].[CustomerDetail] ([CustomerID]) ON DELETE CASCADE
                    );
                    
                    CREATE INDEX [IX_CustomerPreference_CategoryID] ON [Preference].[CustomerPreference] ([CategoryID]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
