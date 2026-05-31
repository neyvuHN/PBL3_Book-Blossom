using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class ReputationHistoryConfiguration : IEntityTypeConfiguration<ReputationHistory>
    {
        public void Configure(EntityTypeBuilder<ReputationHistory> builder)
        {
            builder.ToTable("ReputationHistory", "Rank");

            // ĐÂY LÀ DÒNG BẠN ĐANG THIẾU
            builder.HasKey(h => h.HistoryID); 

            builder.Property(h => h.HistoryID).ValueGeneratedOnAdd();
            
            builder.HasOne(h => h.CustomerReputation)
                   .WithMany(cr => cr.Histories)
                   .HasForeignKey(h => h.CustomerID)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}