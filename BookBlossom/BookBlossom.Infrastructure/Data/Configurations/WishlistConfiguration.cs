using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
    {
        public void Configure(EntityTypeBuilder<Wishlist> builder)
        {
            builder.ToTable("Wishlist", "dbo");

            builder.HasKey(w => w.WishlistID);

            // Index trên (UserID + BookID) và (GuestID + BookID)
            builder.HasIndex(w => new { w.UserID, w.BookID }).IsUnique().HasFilter("[UserID] IS NOT NULL AND [BookID] IS NOT NULL");
            builder.HasIndex(w => new { w.GuestID, w.BookID }).IsUnique().HasFilter("[GuestID] IS NOT NULL AND [BookID] IS NOT NULL");

            // Index trên (UserID + BlindBookID) và (GuestID + BlindBookID)
            builder.HasIndex(w => new { w.UserID, w.BlindBookID }).IsUnique().HasFilter("[UserID] IS NOT NULL AND [BlindBookID] IS NOT NULL");
            builder.HasIndex(w => new { w.GuestID, w.BlindBookID }).IsUnique().HasFilter("[GuestID] IS NOT NULL AND [BlindBookID] IS NOT NULL");

            // Relationships
            builder.HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserID)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(w => w.Book)
                .WithMany()
                .HasForeignKey(w => w.BookID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(w => w.BlindBook)
                .WithMany()
                .HasForeignKey(w => w.BlindBookID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
