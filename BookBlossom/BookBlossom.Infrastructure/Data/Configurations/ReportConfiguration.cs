using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class ReportConfiguration : IEntityTypeConfiguration<Report>
    {
        public void Configure(EntityTypeBuilder<Report> builder)
        {
            builder.ToTable("Report", "Thread");
            builder.HasKey(r => r.ReportID);

            builder.Property(r => r.Description)
                .IsRequired()
                .HasMaxLength(2000);

            builder.HasOne(r => r.Post)
                .WithMany()
                .HasForeignKey(r => r.PostID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.CustomerID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
