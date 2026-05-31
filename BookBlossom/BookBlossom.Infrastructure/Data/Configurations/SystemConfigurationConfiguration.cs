using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class SystemConfigurationConfiguration : IEntityTypeConfiguration<SystemConfiguration>
    {
        public void Configure(EntityTypeBuilder<SystemConfiguration> builder)
        {
            // Ánh xạ bảng UserSystem.SystemConfiguration
            builder.ToTable("SystemConfiguration", "UserSystem");

            builder.HasKey(c => c.ConfigID);

            builder.Property(c => c.ConfigID)
                   .UseIdentityColumn();

            builder.Property(c => c.ConfigName)
                   .HasMaxLength(255)
                   .IsRequired();

            builder.Property(c => c.ConfigValue)
                   .IsRequired();

            builder.Property(c => c.Description)
                   .HasMaxLength(500);

            // Cấu hình Enum GroupType
            builder.Property(c => c.GroupType)
                   .HasColumnType("tinyint")
                   .IsRequired();

            // Liên kết Foreign Key tới bảng User (SystemAdminID)
            builder.HasOne<User>()
                   .WithMany()
                   .HasForeignKey(c => c.SystemAdminID)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
