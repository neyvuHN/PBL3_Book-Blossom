using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class OTPLogConfiguration : IEntityTypeConfiguration<OTPLog>
    {
        public void Configure(EntityTypeBuilder<OTPLog> builder)
        {
            builder.ToTable("OTPLogs", "UserSystem");

            builder.HasKey(o => o.LogID);
            
            builder.Property(o => o.OTPCode).IsRequired().HasMaxLength(255);
            builder.Property(o => o.PhoneNumber).IsRequired().HasMaxLength(15);
            builder.Property(o => o.IpAddress).HasMaxLength(45);
        }
    }
}
