using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class BadgeConfiguration : IEntityTypeConfiguration<Badge>
    {
        public void Configure(EntityTypeBuilder<Badge> builder)
        {
            builder.ToTable("Badge", "Rank");

            builder.HasKey(b => b.BadgeID);
            builder.Property(b => b.BadgeID).ValueGeneratedNever(); // Badge IDs are assigned explicitly based on BadgeType enum

            builder.Property(b => b.BadgeName).IsRequired().HasMaxLength(100);
            builder.Property(b => b.Description).HasMaxLength(500);
            builder.Property(b => b.IconPath).HasMaxLength(255);
        }
    }
}
