using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Linq;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("Roles");

            builder.HasKey(r => r.RoleID);
            
            // Ép kiểu UserRole thành byte (tinyint trong SQL Server)
            builder.Property(r => r.RoleID)
                .HasConversion<byte>()
                .ValueGeneratedNever();

            builder.Property(r => r.RoleName)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(r => r.Description)
                .HasMaxLength(255);

            // Seed dữ liệu từ Enum UserRole
            var roles = Enum.GetValues(typeof(UserRole))
                .Cast<UserRole>()
                .Select(ur => new Role
                {
                    RoleID = ur,
                    RoleName = ur.ToString(),
                    Description = $"Quyền {ur}"
                });

            builder.HasData(roles);
        }
    }
}
