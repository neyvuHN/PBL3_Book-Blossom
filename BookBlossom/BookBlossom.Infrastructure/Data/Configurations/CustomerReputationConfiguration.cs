using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class CustomerReputationConfiguration : IEntityTypeConfiguration<CustomerReputation>
    {
        public void Configure(EntityTypeBuilder<CustomerReputation> builder)
        {
            // 1. Ánh xạ chính xác tên bảng và Schema [Rank]
            builder.ToTable("CustomerReputation", "Rank");

            // 2. Cấu hình Khóa chính (Không tự tăng IDENTITY vì nó lấy theo CustomerID của bảng User)
            builder.HasKey(cr => cr.CustomerID);
            builder.Property(cr => cr.CustomerID).ValueGeneratedNever();

            // 3. Cấu hình giá trị mặc định cho điểm uy tín
            builder.Property(cr => cr.ReputationPoint)
                   .HasDefaultValue(100);

            // 4. Cấu hình liên kết Foreign Key sang bảng RankType
            builder.HasOne(cr => cr.MembershipRank)
                   .WithMany()
                   .HasForeignKey(cr => cr.RankID)
                   .OnDelete(DeleteBehavior.SetNull);
            

            // Lưu ý: Mối quan hệ ngoại với [UserSystem].[CustomerDetail] nếu thực thể CustomerDetail 
            // chưa nằm trong DbContext này thì EF sẽ tự nhận biết qua trường CustomerID khi query.
        }
    }
}