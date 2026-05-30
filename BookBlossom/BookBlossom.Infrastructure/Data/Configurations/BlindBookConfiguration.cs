using BookBlossom.Core.Entities; 
using BookBlossom.Core.Enums;  
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class BlindBookConfiguration : IEntityTypeConfiguration<BlindBook>
    {
        public void Configure(EntityTypeBuilder<BlindBook> builder)
        {
            builder.ToTable("BlindBook", "Book");

            builder.HasKey(bb => bb.BlindBookID);
            builder.Property(bb => bb.BlindBookID)
                   .UseIdentityColumn();

            builder.Property(bb => bb.Keywords).IsRequired().HasMaxLength(500);
            builder.Property(bb => bb.Quotes).IsRequired().HasMaxLength(1000);
            builder.Property(bb => bb.Category).IsRequired().HasMaxLength(100);
            builder.Property(bb => bb.Hashtags).HasMaxLength(200);

            builder.Property(bb => bb.Price)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(bb => bb.Barcode).HasMaxLength(50).IsRequired(false);
            builder.Property(bb => bb.RejectReason).HasMaxLength(500).IsRequired(false);

            // Ép kiểu Enum Status thành số int dưới SQL Server
            builder.Property(bb => bb.BlindBookRequestStatus)
                   .HasConversion<int>()
                   .IsRequired();

            // ==========================================
            // THIẾT LẬP MỐI QUAN HỆ 1-1 (CHỈ ĐẶT TẠI BẢNG CON)
            // ==========================================
            builder.HasOne(bb => bb.RealBook)             // BlindBook (con) chỉ thuộc về 1 RealBook (cha)
                   .WithOne(b => b.BlindBook)             // Ngược lại, RealBook (cha) chỉ có 1 BlindBook (con) đại diện
                   .HasForeignKey<BlindBook>(bb => bb.RealBookID) // Khóa ngoại RealBookID bắt buộc nằm ở bảng con BlindBook
                   .OnDelete(DeleteBehavior.Restrict);    // Không cho xóa sách thật nếu đang có gói BlindBook liên kết
        }
    }
}