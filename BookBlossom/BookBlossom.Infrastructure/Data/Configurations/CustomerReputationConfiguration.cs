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
            builder.Property(cr => cr.CustomerID).ValueGeneratedNever();

            // 3. Cấu hình thuộc tính
            builder.Property(cr => cr.ReputationPoint).HasDefaultValue(100);

            // 4. CHỈNH SỬA: Liên kết với MembershipRank (Cần đồng bộ với Entity)
            builder.HasOne(cr => cr.MembershipRank)
                .WithOne(mr => mr.CustomerReputation) // Đảm bảo bên MembershipRank có thuộc tính này
                .HasForeignKey<CustomerReputation>(cr => cr.RankID) // Hoặc tùy logic khóa ngoại của bạn
                .OnDelete(DeleteBehavior.SetNull);

            // 5. CHỈNH SỬA: Liên kết với CustomerDetail (Để EF không tự sinh cột rác)
            builder.HasOne(cr => cr.CustomerDetail)
                .WithOne() // Hoặc .WithOne(cd => cd.CustomerReputation)
                .HasForeignKey<CustomerReputation>(cr => cr.CustomerID) // Ép cột CustomerID làm khóa ngoại
                .OnDelete(DeleteBehavior.Cascade);

            // 6. Liên kết với Histories
            builder.HasMany(cr => cr.Histories)
                .WithOne(h => h.CustomerReputation)
                .HasForeignKey(h => h.CustomerID)
                .OnDelete(DeleteBehavior.Restrict); 
        }
    }
}