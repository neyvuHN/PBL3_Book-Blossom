using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class GuestDetailConfiguration : IEntityTypeConfiguration<GuestDetail>
    {
        public void Configure(EntityTypeBuilder<GuestDetail> builder)
        {
            builder.ToTable("GuestDetail");

            builder.HasKey(g => g.GuestID);
            builder.Property(g => g.GuestID).HasColumnName("GuestID");

            builder.Property(g => g.SessionToken)
                   .IsRequired()
                   .HasMaxLength(255)
                   .IsUnicode(false); // varchar

            builder.Property(g => g.IPAddress)
                   .HasMaxLength(45)
                   .IsUnicode(false); // varchar

            builder.Property(g => g.DeviceInfo)
                   .HasMaxLength(500); // nvarchar (mặc định là Unicode)

            builder.Property(g => g.CreatedAt).HasColumnType("datetime");
            builder.Property(g => g.LastActiveAt).HasColumnType("datetime");

            builder.HasOne(g => g.ConvertedUser)
                   .WithMany(u => u.GuestDetails) 
                   .HasForeignKey(g => g.ConvertedUserID)
                   .OnDelete(DeleteBehavior.Restrict); 
        }
    }
}
