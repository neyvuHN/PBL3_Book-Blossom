using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class ImportingDetailConfiguration : IEntityTypeConfiguration<ImportingDetail>
    {
        public void Configure(EntityTypeBuilder<ImportingDetail> builder)
        {
            builder.ToTable("ImportingDetail", "Import");

            // Composite key
            builder.HasKey(d => new { d.ImportingID, d.BookID });

            builder.Property(d => d.UnitPrice)
                .HasColumnType("decimal(18,2)");

            builder.Property(d => d.Quantity);

            // Computed column LineTotal = UnitPrice * Quantity
            builder.Property(d => d.LineTotal)
                .HasColumnType("decimal(18,2)")
                .HasComputedColumnSql("[UnitPrice] * [Quantity]", stored: true); // Computed stored for performance

            // Relationships
            builder.HasOne(d => d.Importing)
                .WithMany(i => i.ImportingDetails)
                .HasForeignKey(d => d.ImportingID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.Book)
                .WithMany()
                .HasForeignKey(d => d.BookID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
