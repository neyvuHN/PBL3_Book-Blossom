using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class OrderDetailConfiguration : IEntityTypeConfiguration<OrderDetail>
    {
        public void Configure(EntityTypeBuilder<OrderDetail> builder)
        {
            // 1. Ánh xạ Tên bảng và Schema [OrderRequest]
            builder.ToTable("OrderDetail", "OrderRequest");

            // 2. Cấu hình Khóa chính tổ hợp (Composite Key)
            builder.HasKey(od => new { od.OrderID, od.BookID });

            // 3. Cấu hình các trường số lượng và giá tiền
            builder.Property(od => od.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(od => od.Quantity).IsRequired();
            builder.Property(od => od.Discount).HasColumnType("decimal(18,2)").HasDefaultValue(0m);

            // 4. Cấu hình các mối quan hệ Foreign Key (Khóa ngoại)
            
            // Liên kết: OrderDetail -> Orders (Xóa Order tự động xóa sạch OrderDetail)
            builder.HasOne(od => od.Order)
                   .WithMany(o => o.OrderDetails)
                   .HasForeignKey(od => od.OrderID)
                   .OnDelete(DeleteBehavior.Cascade);

            // Liên kết: OrderDetail -> RealBook (Chặn xóa Sách nếu sách đó đã có trong đơn hàng)
            builder.HasOne(od => od.RealBook)
                   .WithMany()
                   .HasForeignKey(od => od.BookID)
                   .OnDelete(DeleteBehavior.Restrict);

            // Liên kết: OrderDetail -> BlindBook (Nếu gói sách mù bị xóa, giữ lại lịch sử đơn hàng và đặt BlindBookID = NULL)
            builder.HasOne(od => od.BlindBook)
                   .WithMany()
                   .HasForeignKey(od => od.BlindBookID)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}