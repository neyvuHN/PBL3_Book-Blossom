using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class CustomerBadgeConfiguration : IEntityTypeConfiguration<CustomerBadge>
    {
        public void Configure(EntityTypeBuilder<CustomerBadge> builder)
        {
            builder.ToTable("CustomerBadge", "Rank");

            builder.HasKey(cb => new { cb.CustomerID, cb.BadgeID });

            builder.HasOne(cb => cb.Customer)
                   .WithMany()
                   .HasForeignKey(cb => cb.CustomerID)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(cb => cb.Badge)
                   .WithMany()
                   .HasForeignKey(cb => cb.BadgeID)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
