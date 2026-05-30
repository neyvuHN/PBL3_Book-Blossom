using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class GuestPreferenceConfiguration : IEntityTypeConfiguration<GuestPreference>
    {
        public void Configure(EntityTypeBuilder<GuestPreference> builder)
        {
            builder.ToTable("GuestPreference", "Preference");
            builder.HasKey(g => new { g.GuestID, g.CategoryID });

            builder.Property(g => g.GuestID).IsRequired();
            builder.Property(g => g.CategoryID).IsRequired();
            builder.Property(g => g.CreatedAt).IsRequired();

            // Setup relation to GuestDetail
            builder.HasOne(g => g.GuestDetail)
                   .WithMany() // GuestDetail may not have a collection of GuestPreferences to keep it simple
                   .HasForeignKey(g => g.GuestID)
                   .OnDelete(DeleteBehavior.Cascade);

            // Setup relation to Category
            builder.HasOne(g => g.Category)
                   .WithMany()
                   .HasForeignKey(g => g.CategoryID)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
