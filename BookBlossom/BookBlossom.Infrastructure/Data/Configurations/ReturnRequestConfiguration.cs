using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class ReturnRequestConfiguration : IEntityTypeConfiguration<ReturnRequest>
    {
        public void Configure(EntityTypeBuilder<ReturnRequest> builder)
        {
            // 1. Ánh xạ Tên bảng và Schema [OrderRequest]
            builder.ToTable("ReturnRequest", "OrderRequest");

            // 2. Cấu hình Khóa chính
            builder.HasKey(r => r.ReturnRequestID);

            // 3. Ép kiểu dữ liệu và bắt buộc / tùy chọn
            builder.Property(r => r.ReturnReason)
                   .IsRequired();

            builder.Property(r => r.UnboxVideoPath)
                   .IsRequired();

            builder.Property(r => r.ResolutionType)
                   .HasColumnType("tinyint")
                   .IsRequired();

            builder.Property(r => r.ReturnStatus)
                   .HasColumnType("tinyint")
                   .IsRequired();

            builder.Property(r => r.GatewayTransactionID)
                   .HasMaxLength(255)
                   .IsUnicode(false);

            builder.Property(r => r.RefundAmount)
                   .HasColumnType("decimal(18,2)");

            // 4. Cấu hình các Quan hệ khóa ngoại (Foreign Keys)
            builder.HasOne(r => r.Order)
                   .WithMany()
                   .HasForeignKey(r => r.OrderID)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.RealBook)
                   .WithMany()
                   .HasForeignKey(r => r.BookID)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.StaffDetail)
                   .WithMany()
                   .HasForeignKey(r => r.StaffID)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
