using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class CartConfiguration : IEntityTypeConfiguration<Cart>
    {
        public void Configure(EntityTypeBuilder<Cart> builder)
        {
            builder.ToTable("Cart", "dbo");

            builder.HasKey(c => c.CartID);

            builder.Property(c => c.Quantity).IsRequired();

            // Index trên (UserID + BookID) và (GuestID + BookID)
            builder.HasIndex(c => new { c.UserID, c.BookID }).IsUnique().HasFilter("[UserID] IS NOT NULL AND [BookID] IS NOT NULL");
            builder.HasIndex(c => new { c.GuestID, c.BookID }).IsUnique().HasFilter("[GuestID] IS NOT NULL AND [BookID] IS NOT NULL");

            // Index trên (UserID + BlindBookID) và (GuestID + BlindBookID)
            builder.HasIndex(c => new { c.UserID, c.BlindBookID }).IsUnique().HasFilter("[UserID] IS NOT NULL AND [BlindBookID] IS NOT NULL");
            builder.HasIndex(c => new { c.GuestID, c.BlindBookID }).IsUnique().HasFilter("[GuestID] IS NOT NULL AND [BlindBookID] IS NOT NULL");

            // Relationships
            builder.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserID)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(c => c.Book)
                .WithMany()
                .HasForeignKey(c => c.BookID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.BlindBook)
                .WithMany()
                .HasForeignKey(c => c.BlindBookID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
