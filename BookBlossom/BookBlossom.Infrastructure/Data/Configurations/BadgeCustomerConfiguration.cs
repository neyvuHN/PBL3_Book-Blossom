using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class BadgeCustomerConfiguration : IEntityTypeConfiguration<BadgeCustomer>
    {
        public void Configure(EntityTypeBuilder<BadgeCustomer> builder)
        {
            builder.ToTable("CustomerBadge", "Rank");

            builder.HasKey(bc => new { bc.CustomerID, bc.BadgeID });

            builder.Property(bc => bc.EarnedAt)
                .IsRequired();

            builder.HasOne(bc => bc.CustomerDetail)
                .WithMany(c => c.BadgeCustomers)
                .HasForeignKey(bc => bc.CustomerID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(bc => bc.Badge)
                .WithMany(b => b.BadgeCustomers)
                .HasForeignKey(bc => bc.BadgeID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
