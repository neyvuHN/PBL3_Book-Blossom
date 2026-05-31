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
            
            builder.Property(b => b.BadgeID)
                .ValueGeneratedNever();

            builder.Property(b => b.BadgeName)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(b => b.IconPath)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(b => b.ConditionDescription)
                .IsRequired()
                .HasMaxLength(500);
        }
    }
}
