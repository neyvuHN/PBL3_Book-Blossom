using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookBlossom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCartAndWishlistModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The 'Cart' and 'Wishlist' tables already exist in the database.
            // This migration is kept empty to avoid trying to recreate them,
            // while EF Core's snapshot tracks them correctly.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Empty because Up() is empty
        }
    }
}
