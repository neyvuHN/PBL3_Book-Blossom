using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class CustomerDetailConfiguration : IEntityTypeConfiguration<CustomerDetail>
    {
        public void Configure(EntityTypeBuilder<CustomerDetail> builder)
{
    builder.ToTable("CustomerDetail", "UserSystem");
    builder.HasKey(c => c.CustomerID);
    builder.Property(c => c.CustomerID).ValueGeneratedNever();

    builder.Property(c => c.IsOnboardingCompleted).IsRequired();
    builder.Property(c => c.TotalSpending)
           .HasColumnType("decimal(18,2)")
           .IsRequired();
    builder.Property(c => c.DailyUndoCount).IsRequired();
    builder.Property(c => c.LastUndoDate).HasColumnType("date");
    builder.Property(c => c.CurrentMonthThreadCount).IsRequired();
    builder.Property(c => c.LastThreadResetDate).HasColumnType("date");
    builder.Property(c => c.CurrentOrderStreak).IsRequired();

    // === CHỈ THÊM RankID (không thêm CurrentPackageID) ===
    builder.Property<long?>("RankID")
           .HasColumnName("RankID");

    builder.HasOne(c => c.MembershipRank)
           .WithMany(m => m.CustomerDetails)
           .HasForeignKey("RankID")
           .OnDelete(DeleteBehavior.Restrict);

    builder.HasOne(c => c.User)
           .WithOne(u => u.CustomerDetail)
           .HasForeignKey<CustomerDetail>(c => c.CustomerID)
           .OnDelete(DeleteBehavior.Cascade);

    // Khai báo rõ quan hệ 1-N với CustomerReputation, FK là CustomerID
    // Tránh EF tự sinh shadow property 'CustomerDetailCustomerID'
    builder.HasMany(c => c.CustomerReputations)
           .WithOne()
           .HasForeignKey(cr => cr.CustomerID)
           .OnDelete(DeleteBehavior.Cascade);
}
    }
}
