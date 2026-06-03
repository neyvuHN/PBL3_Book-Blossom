using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class MessageAttachmentConfiguration : IEntityTypeConfiguration<MessageAttachment>
    {
        public void Configure(EntityTypeBuilder<MessageAttachment> builder)
        {
            builder.ToTable("MessageAttachment", "Chat");
            builder.HasKey(a => a.AttachmentID);

            builder.HasOne(a => a.Message)
                .WithMany(m => m.Attachments)
                .HasForeignKey(a => a.MessageID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
