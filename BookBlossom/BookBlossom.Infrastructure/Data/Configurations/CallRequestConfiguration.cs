using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class CallRequestConfiguration : IEntityTypeConfiguration<CallRequest>
    {
        public void Configure(EntityTypeBuilder<CallRequest> builder)
        {
            builder.ToTable("CallRequests");
            builder.HasKey(c => c.RequestID);

            builder.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.CustomerID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
