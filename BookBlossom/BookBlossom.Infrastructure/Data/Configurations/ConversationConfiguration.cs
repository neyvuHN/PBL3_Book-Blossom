using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
    {
        public void Configure(EntityTypeBuilder<Conversation> builder)
        {
            builder.ToTable("Conversations");
            builder.HasKey(c => c.ConversationID);

            builder.HasOne(c => c.User)
                .WithOne()
                .HasForeignKey<Conversation>(c => c.CustomerID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
