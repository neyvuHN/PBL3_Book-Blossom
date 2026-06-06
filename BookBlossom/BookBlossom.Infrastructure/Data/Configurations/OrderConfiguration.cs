using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            // 1. Ánh xạ chính xác Tên bảng và Schema [OrderRequest]
            builder.ToTable("Orders", "OrderRequest");

            // 2. Cấu hình Khóa chính
            builder.HasKey(o => o.OrderID);

            // 3. Ép kiểu dữ liệu và cấu hình bắt buộc / tùy chọn
            builder.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(o => o.ShippingFee).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            builder.Property(o => o.DiscountAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            
            builder.Property(o => o.ShipReceiverName).HasMaxLength(100);
            builder.Property(o => o.ShipPhoneNumber).HasMaxLength(20).IsUnicode(false);
            builder.Property(o => o.ShipDetailAddress);
            builder.Property(o => o.Note).HasMaxLength(500);

            // 4. Ép kiểu Tinyint cho Enum và Trạng thái thanh toán theo đúng DB
            builder.Property(o => o.OrderStatus)
                   .HasColumnType("tinyint")
                   .HasDefaultValue(OrderStatus.Pending)
                   .IsRequired();

            builder.Property(o => o.PaymentMethod)
                   .HasColumnType("tinyint")
                   .IsRequired();

            builder.Property(o => o.PaymentStatus)
                   .HasColumnType("tinyint")
                   .HasDefaultValue(PaymentStatus.Pending)
                   .IsRequired();

            // 5. Cấu hình giá trị mặc định cho Ngày tạo đơn
            builder.Property(o => o.OrderDate)
                   .HasDefaultValueSql("getdate()");

            // 6. Tối ưu hiệu năng bằng Index (Dành cho chức năng quản lý của Staff)
            builder.HasIndex(o => o.OrderStatus)
                   .HasDatabaseName("IX_Orders_OrderStatus");

            builder.HasIndex(o => o.CustomerID)
                   .HasDatabaseName("IX_Orders_CustomerID");

            builder.HasIndex(o => o.OrderDate)
                   .HasDatabaseName("IX_Orders_OrderDate");

            // 7. (Removed VoucherID configuration as it uses OrderVoucher now)
        }
    }
}