using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            // Ánh xạ bảng UserSystem.AuditLog
            builder.ToTable("AuditLog", "UserSystem");

            builder.HasKey(a => a.LogID);

            builder.Property(a => a.LogID)
                   .UseIdentityColumn();

            builder.Property(a => a.TableName)
                   .HasMaxLength(100);

            builder.Property(a => a.IPAddress)
                   .HasMaxLength(50);

            // Cấu hình Enum ActionType (lưu dạng tinyint hoặc string tùy thiết kế - khuyên dùng tinyint/int)
            builder.Property(a => a.ActionType)
                   .HasColumnType("tinyint")
                   .IsRequired();

            // Liên kết Foreign Key tới bảng User (SystemAdminID)
            builder.HasOne<User>()
                   .WithMany()
                   .HasForeignKey(a => a.SystemAdminID)
                   .OnDelete(DeleteBehavior.Restrict);

            // Liên kết Foreign Key tới bảng User (UserID bị tác động - nullable)
            builder.HasOne<User>()
                   .WithMany()
                   .HasForeignKey(a => a.UserID)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
