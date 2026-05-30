using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class CustomerServiceConfiguration : IEntityTypeConfiguration<CustomerService>
    {
        public void Configure(EntityTypeBuilder<CustomerService> builder)
        {
            builder.ToTable("CustomerService", "Service");

            builder.HasKey(c => c.CustomerID);

            // 1. Thiết lập quan hệ 1-1 với User (Giữ nguyên)
            builder.HasOne(c => c.User)
                .WithOne(u => u.CustomerService)
                .HasForeignKey<CustomerService>(c => c.CustomerID)
                .OnDelete(DeleteBehavior.Cascade);

            // 2. Thiết lập quan hệ 1-1 với CustomerDetail để xóa bỏ hoàn toàn cột ẩn tự chế 'CustomerDetailCustomerID'
            builder.HasOne<CustomerDetail>()
                .WithOne(cd => cd.CustomerService)
                .HasForeignKey<CustomerService>(c => c.CustomerID)
                .OnDelete(DeleteBehavior.Restrict);

            // 3. Thiết lập quan hệ với ServicePackage (Giữ nguyên)
            builder.HasOne(c => c.ServicePackage)
                .WithMany(s => s.CustomerServices)
                .HasForeignKey(c => c.CurrentPackageID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}