using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class StaffDetailConfiguration : IEntityTypeConfiguration<StaffDetail>
    {
        public void Configure(EntityTypeBuilder<StaffDetail> builder)
        {
            builder.ToTable("StaffDetail", "UserSystem");

            // Thiết lập StaffID là Khóa chính
            builder.HasKey(s => s.StaffID);
            builder.Property(s => s.StaffID).ValueGeneratedNever(); // Vì là FK nên không tự tăng

            builder.Property(s => s.Address).HasMaxLength(255);
            builder.Property(s => s.IsOnboardingCompleted).IsRequired();
            
            // Cấu hình các trường Enum (tinyint)
            builder.Property(s => s.Department)
                   .HasColumnType("tinyint")
                   .IsRequired();
            
            builder.Property(s => s.Position)
                   .HasColumnType("tinyint")
                   .IsRequired();

            builder.Property(s => s.ContractType)
                   .HasColumnType("tinyint")
                   .IsRequired();

            builder.Property(s => s.HireDate).HasColumnType("date").IsRequired();
            
            builder.Property(s => s.Salary).HasColumnType("decimal(18,2)").IsRequired();
            
            builder.Property(s => s.BankAccount).HasMaxLength(20);
            builder.Property(s => s.TaxCode).HasMaxLength(20);
            
            builder.Property(s => s.KPIScore).HasColumnType("decimal(3,2)");

            // THIẾT LẬP QUAN HỆ 1-1 VÀ KHÓA NGOẠI
            builder.HasOne(s => s.User)
                   .WithOne(u => u.StaffDetail)
                   .HasForeignKey<StaffDetail>(s => s.StaffID)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}