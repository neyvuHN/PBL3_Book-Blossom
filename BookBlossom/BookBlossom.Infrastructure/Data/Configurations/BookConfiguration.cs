using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class BookConfiguration : IEntityTypeConfiguration<RealBook>
    {
        public void Configure(EntityTypeBuilder<RealBook> builder)
        {
            builder.ToTable("RealBook", "Book");

            builder.HasKey(b => b.BookID);
            builder.Property(b => b.BookID).HasColumnName("BookID");

            builder.Property(b => b.Title)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(b => b.Publisher)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(b => b.ISBN)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(b => b.Price)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(b => b.Weight)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            // Relationship with Category
            builder.HasOne(b => b.Category)
                .WithMany()
                .HasForeignKey(b => b.CategoryID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
