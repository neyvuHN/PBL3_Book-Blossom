using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
    {
        public void Configure(EntityTypeBuilder<UserNotification> builder)
        {
            builder.ToTable("UserFollow", "Notification");
            builder.HasKey(n => n.NotificationID);

            builder.Property(n => n.Title)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(n => n.Content)
                .IsRequired();

            builder.Property(n => n.NotificationType)
                .HasColumnType("tinyint")
                .HasConversion<byte>()
                .IsRequired();

            builder.Property(n => n.ReferenceType)
                .HasColumnType("tinyint")
                .HasConversion<byte>();

            builder.Property(n => n.CreatedDate)
                .HasColumnName("CreatedDate");

            // Configure User relationship
            builder.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            // Index on UserID + IsRead for query optimization
            builder.HasIndex(n => new { n.UserID, n.IsRead });
        }
    }
}
