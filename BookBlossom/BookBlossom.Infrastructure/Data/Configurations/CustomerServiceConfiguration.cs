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

            builder.HasOne(c => c.User)
                .WithOne(u => u.CustomerService)
                .HasForeignKey<CustomerService>(c => c.CustomerID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.ServicePackage)
                .WithMany(s => s.CustomerServices)
                .HasForeignKey(c => c.CurrentPackageID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
