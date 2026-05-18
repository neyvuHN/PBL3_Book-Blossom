using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class ImportingConfiguration : IEntityTypeConfiguration<Importing>
    {
        public void Configure(EntityTypeBuilder<Importing> builder)
        {
            builder.ToTable("Importing", "Import");

            builder.HasKey(i => i.ImportingID);
            builder.Property(i => i.ImportingID).HasColumnName("ImportingID");

            builder.Property(i => i.SupplierName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(i => i.TotalCost)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(i => i.ShipAddress)
                .HasMaxLength(500);

            // Relationship with StaffDetail
            builder.HasOne(i => i.Staff)
                .WithMany()
                .HasForeignKey(i => i.StaffID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
