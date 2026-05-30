using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("Category", "Preference");

            builder.HasKey(c => c.CategoryID);
            builder.Property(c => c.CategoryID).HasColumnName("CategoryID");

            builder.Property(c => c.CategoryName)
                .IsRequired()
                .HasMaxLength(100);

            // unique index
            builder.HasIndex(c => c.CategoryName)
                .IsUnique();

            builder.Property(c => c.Description)
                .HasMaxLength(500);

            builder.Property(c => c.Status)
                .HasColumnType("tinyint")
                .HasConversion<byte>()
                .IsRequired();

            // Seed sample categories as requested
            builder.HasData(
                new Category 
                { 
                    CategoryID = 1, 
                    CategoryName = "Văn học", 
                    Description = "Sách Văn học nghệ thuật, tiểu thuyết, thơ ca", 
                    Status = CategoryStatus.Active 
                },
                new Category 
                { 
                    CategoryID = 2, 
                    CategoryName = "Kỹ năng", 
                    Description = "Sách phát triển bản thân, kỹ năng sống và làm việc", 
                    Status = CategoryStatus.Active 
                },
                new Category 
                { 
                    CategoryID = 3, 
                    CategoryName = "Tâm lý", 
                    Description = "Sách tâm lý học, hành vi và nhận thức", 
                    Status = CategoryStatus.Active 
                },
                new Category 
                { 
                    CategoryID = 4, 
                    CategoryName = "Kinh dị", 
                    Description = "Sách thuộc thể loại kinh dị, giật gân và bí ẩn", 
                    Status = CategoryStatus.Active 
                }
            );
        }
    }
}
