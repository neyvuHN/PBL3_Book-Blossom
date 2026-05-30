using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class ServicePackageConfiguration : IEntityTypeConfiguration<ServicePackage>
    {
        public void Configure(EntityTypeBuilder<ServicePackage> builder)
        {
            builder.ToTable("ServicePackage", "Service");

            builder.HasKey(s => s.PackageID);
            builder.Property(s => s.PackageID).ValueGeneratedNever();

            builder.Property(s => s.PackageName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.Price)
                .HasColumnType("decimal(18,2)");

            builder.HasMany(s => s.CustomerServices)
                   .WithOne(sc => sc.ServicePackage)
                   .HasForeignKey(sc => sc.CurrentPackageID)
                   .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasMany(s => s.CustomerDetails)
                   .WithOne(cd => cd.ServicePackage)
                   .HasForeignKey("CurrentPackageID");

            builder.HasData(
                new ServicePackage 
                { 
                    PackageID = 1, 
                    PackageName = "Free", 
                    Price = 0, 
                    DurationDay = 0, 
                    ThreadLimit = 3, 
                    UndoLimit = 2, 
                    Description = "Gói miễn phí: 3 thread/tháng, 2 undo Tindbook." 
                },
                new ServicePackage 
                { 
                    PackageID = 2, 
                    PackageName = "Basic", 
                    Price = 99000, 
                    DurationDay = 30, 
                    ThreadLimit = 20, 
                    UndoLimit = 5, 
                    Description = "Gói Cơ bản: 20 thread/tháng, 5 undo Tindbook." 
                },
                new ServicePackage 
                { 
                    PackageID = 3, 
                    PackageName = "Pro", 
                    Price = 199000, 
                    DurationDay = 30, 
                    ThreadLimit = 999999, 
                    UndoLimit = 999999, 
                    Description = "Gói Chuyên nghiệp: Không giới hạn thread và undo." 
                }
            );
        }
    }
}
