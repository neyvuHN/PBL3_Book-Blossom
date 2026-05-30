using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class CustomerPreferenceConfiguration : IEntityTypeConfiguration<CustomerPreference>
    {
        public void Configure(EntityTypeBuilder<CustomerPreference> builder)
        {
            builder.ToTable("CustomerPreference", "Preference"); // tên bảng, schema
            builder.HasKey(c => new {c.CustomerID, c.CategoryID});  
            builder.Property(c => c.CustomerID).ValueGeneratedNever();

            builder.Property(c => c.CategoryID).IsRequired();
            
            builder.Property(c => c.CreatedAt).IsRequired();

            //* CustomerDetail có quan hệ n-n với Category nên có bảng trung gian là CustomerPreference (n-n -> 1-n + 1-n)
            // Thiết lập quan hệ 1-n với CustomerDetail
            builder.HasOne(c => c.CustomerDetail)
                   .WithMany(cd => cd.CustomerPreferences)
                   .HasForeignKey(c => c.CustomerID)
                   .OnDelete(DeleteBehavior.Cascade);
            
            // Thiết lập quan hệ với Category 1-n
            builder.HasOne(c => c.Category)
                   .WithMany(cat => cat.CustomerPreferences)
                   .HasForeignKey(c => c.CategoryID)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
