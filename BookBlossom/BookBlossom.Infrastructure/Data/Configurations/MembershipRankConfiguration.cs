using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class MembershipRankConfiguration : IEntityTypeConfiguration<MembershipRank>
    {
        public void Configure(EntityTypeBuilder<MembershipRank> builder)
        {
            builder.ToTable("MembershipRank", "Rank");

            builder.HasKey(m => m.RankID);

            builder.Property(m => m.RankType)
                   .HasColumnType("tinyint");

            builder.Property(m => m.MinSpending)
                   .HasColumnType("decimal(18,2)")
                   .HasDefaultValue(0m);

            builder.Property(m => m.DiscountRate)
                   .HasColumnType("decimal(18,2)")
                   .HasDefaultValue(0m);
        }
    }
}
