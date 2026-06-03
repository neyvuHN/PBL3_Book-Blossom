using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class CallRequestConfiguration : IEntityTypeConfiguration<CallRequest>
    {
        public void Configure(EntityTypeBuilder<CallRequest> builder)
        {
            builder.ToTable("CallRequest", "Chat");
            builder.HasKey(c => c.CallRequestID);

            builder.Property(c => c.CallRequestID)
                .HasColumnName("CallRequestID");

            builder.Property(c => c.BuyerID)
                .HasColumnName("BuyerID");

            builder.Property(c => c.ConversationID)
                .HasColumnName("ConversationID");

            builder.Property(c => c.PhoneNumber)
                .HasColumnName("PhoneNumber")
                .HasMaxLength(20)
                .IsUnicode(false);

            builder.Property(c => c.Category)
                .HasColumnName("Category")
                .HasConversion<byte>()
                .HasColumnType("tinyint");

            builder.Property(c => c.Note)
                .HasColumnName("Note")
                .HasMaxLength(1000)
                .IsRequired(false);

            builder.Property(c => c.Status)
                .HasColumnName("Status")
                .HasConversion<byte>()
                .HasColumnType("tinyint");

            builder.Property(c => c.ResolvedBy)
                .HasColumnName("ResolvedBy")
                .IsRequired(false);

            builder.Property(c => c.CreatedAt)
                .HasColumnName("CreatedAt");

            builder.Property(c => c.ResolvedAt)
                .HasColumnName("ResolvedAt")
                .IsRequired(false);

            // Relationships
            builder.HasOne(c => c.Buyer)
                .WithMany()
                .HasForeignKey(c => c.BuyerID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(c => c.Conversation)
                .WithMany()
                .HasForeignKey(c => c.ConversationID)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(c => c.Resolver)
                .WithMany()
                .HasForeignKey(c => c.ResolvedBy)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
