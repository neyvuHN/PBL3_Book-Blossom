using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("User");

            builder.HasKey(u => u.UserID);
            builder.Property(u => u.UserID).HasColumnName("UserID");
            builder.Property(u => u.RoleID)
                .IsRequired();

            // Mối quan hệ với Role
            builder.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleID)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Property(u => u.UserName).IsRequired().HasMaxLength(100);
            builder.Property(u => u.Password).IsRequired().HasMaxLength(255);
            builder.Property(u => u.AccountStatus).IsRequired().HasColumnType("tinyint");
            builder.Property(u => u.LastName).HasMaxLength(50);
            builder.Property(u => u.FirstName).HasMaxLength(50);
            builder.Property(u => u.Avatar).HasMaxLength(255);
            builder.Property(u => u.PhoneNumber).HasMaxLength(15);
            builder.Property(u => u.Email).HasMaxLength(100);
            builder.Property(u => u.Gender).HasMaxLength(10);
            builder.Property(u => u.Birthday).HasColumnType("date");
            builder.Property(u => u.IsActive).IsRequired(false);
        }
    }
}