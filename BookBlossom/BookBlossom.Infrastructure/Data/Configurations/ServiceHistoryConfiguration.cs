using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class ServiceHistoryConfiguration : IEntityTypeConfiguration<ServiceHistory>
    {
        public void Configure(EntityTypeBuilder<ServiceHistory> builder)
        {
            builder.ToTable("ServiceHistory", "Service");

            builder.HasKey(s => s.HistoryID);

            builder.Property(s => s.Price)
                .HasColumnType("decimal(18,2)");

            builder.Property(s => s.PaymentMethod)
                .HasColumnType("tinyint")
                .HasConversion<byte>();

            builder.Property(s => s.PaymentStatus)
                .HasColumnType("tinyint")
                .HasConversion<byte>();

            builder.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.CustomerID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
