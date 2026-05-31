using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
    {
        public void Configure(EntityTypeBuilder<Subscription> builder)
        {
            builder.ToTable("Subscription", "Notification");
            builder.HasKey(s => s.FollowID);

            builder.Property(s => s.TargetType)
                .HasColumnType("tinyint")
                .HasConversion<byte>()
                .IsRequired();

            builder.Property(s => s.CreatedDate)
                .HasColumnName("CreatedDate");

            // Configure Customer relationship
            builder.HasOne(s => s.Customer)
                .WithMany()
                .HasForeignKey(s => s.CustomerID)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique/Non-unique index for fast lookup
            builder.HasIndex(s => new { s.CustomerID, s.TargetID, s.TargetType });
        }
    }
}
