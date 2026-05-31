using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class CustomerReputationConfiguration : IEntityTypeConfiguration<CustomerReputation>
    {
        public void Configure(EntityTypeBuilder<CustomerReputation> builder)
        {
            // 1. Cấu hình bảng
            builder.ToTable("CustomerReputation", "Rank");

            // 2. Khóa chính
            builder.HasKey(cr => cr.CustomerID);
            builder.Property(cr => cr.CustomerID)
                   .ValueGeneratedNever();

            // 3. Giá trị mặc định
            builder.Property(cr => cr.ReputationPoint)
                   .HasDefaultValue(100);

            // 4. Liên kết với MembershipRank
            builder.HasOne(cr => cr.MembershipRank)
                   .WithOne(mr => mr.CustomerReputation)
                   .HasForeignKey<CustomerReputation>(cr => cr.RankID)
                   .OnDelete(DeleteBehavior.SetNull);

            // 5. Liên kết với CustomerDetail
            builder.HasOne(cr => cr.CustomerDetail)
                   .WithMany(cd => cd.CustomerReputations)
                   .HasForeignKey(cr => cr.CustomerID)
                   .OnDelete(DeleteBehavior.Cascade);

            // 6. Liên kết với Histories
            builder.HasMany(cr => cr.Histories)
                   .WithOne(h => h.CustomerReputation)
                   .HasForeignKey(h => h.CustomerID)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}