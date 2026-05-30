using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class ReviewConfiguration : IEntityTypeConfiguration<Review>
    {
        public void Configure(EntityTypeBuilder<Review> builder)
        {
            builder.ToTable("Review", "Review");

            builder.HasKey(r => r.ReviewID);

            builder.Property(r => r.Rating).IsRequired();
            builder.Property(r => r.Content).HasMaxLength(1000);
            builder.Property(r => r.ImageVideoPath).HasMaxLength(2000);
            builder.Property(r => r.LikeCount).HasDefaultValue(0);
            builder.Property(r => r.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            builder.Property(r => r.IsHidden).HasDefaultValue(false);
            builder.Property(r => r.IsReputationAwarded).HasDefaultValue(false);

            // Indexes as requested
            builder.HasIndex(r => r.CreatedAt);
            builder.HasIndex(r => r.CustomerID);

            // Relationships
            builder.HasOne(r => r.Customer)
                   .WithMany()
                   .HasForeignKey(r => r.CustomerID)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.RealBook)
                   .WithMany()
                   .HasForeignKey(r => r.BookID)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(r => r.BlindBook)
                   .WithMany()
                   .HasForeignKey(r => r.BlindBookID)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
