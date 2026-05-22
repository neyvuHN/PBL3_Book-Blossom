using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class DeliveryAddressConfiguration : IEntityTypeConfiguration<DeliveryAddress>
    {
        public void Configure(EntityTypeBuilder<DeliveryAddress> builder)
        {
            builder.ToTable("DeliveryAddress", "UserSystem");

            builder.HasKey(da => da.AddressID);

            builder.Property(da => da.ReceiverName).HasMaxLength(100).IsRequired();
            builder.Property(da => da.PhoneNumber).HasMaxLength(15).IsUnicode(false).IsRequired();
            builder.Property(da => da.DetailAddress).HasMaxLength(500).IsRequired();
            builder.Property(da => da.IsDefault).HasDefaultValue(false);
        }
    }
}