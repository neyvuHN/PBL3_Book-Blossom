using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class RealBookConfiguration : IEntityTypeConfiguration<RealBook>
    {
        public void Configure(EntityTypeBuilder<RealBook> builder)
        {
            // 1. Cấu hình bảng và Schema
            builder.ToTable("RealBook", "Book");

            // 2. Cấu hình Khóa chính
            builder.HasKey(b => b.BookID);
            builder.Property(b => b.BookID).HasColumnName("BookID");

            // 3. Cấu hình các thuộc tính thông thường
            builder.Property(b => b.Title)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(b => b.Publisher)
                .IsRequired()
                .HasMaxLength(255);

            // Cấu hình Unique Index cho ISBN để tránh trùng sách
            builder.Property(b => b.ISBN)
                .IsRequired()
                .HasMaxLength(50);
            
            builder.HasIndex(b => b.ISBN)
                .IsUnique();

            // Đồng bộ kiểu dữ liệu DateTime với DB cũ của bạn
            builder.Property(b => b.PublishYear)
                .HasColumnType("int")
                .IsRequired();

            builder.Property(b => b.Description)
                .HasMaxLength(2000);

            builder.Property(b => b.Price)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(b => b.Weight)
                .HasColumnType("double precision");

            builder.Property(b => b.UnitsInStock)
                .IsRequired();

            // 4. 🚨 CẤU HÌNH MỐI QUAN HỆ KHÓA NGOẠI (Xóa sổ hoàn toàn lỗi CategoryID1)
            builder.HasOne(b => b.Category)             // Một cuốn sách có một danh mục
                .WithMany(c => c.RealBooks)             // Một danh mục có nhiều cuốn sách
                .HasForeignKey(b => b.CategoryID)       // 🎯 Ép dùng cột CategoryID này làm khóa ngoại
                .OnDelete(DeleteBehavior.Restrict);     // Tránh xóa dây chuyền (Cascade) gây mất dữ liệu
        }
    }
}