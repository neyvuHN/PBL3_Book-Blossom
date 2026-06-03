using BookBlossom.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    public class ThreadPostConfiguration : IEntityTypeConfiguration<ThreadPost>
    {
        public void Configure(EntityTypeBuilder<ThreadPost> builder)
        {
            builder.ToTable("ThreadPost", "Thread");
            builder.HasKey(p => p.PostID);

            builder.Property(p => p.Title)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(p => p.Content)
                .IsRequired();

            builder.Property(p => p.Hashtags)
                .HasMaxLength(500);

            builder.Property(p => p.LikeCount)
                .HasDefaultValue(0);

            builder.Property(p => p.ShareCount)
                .HasDefaultValue(0);

            // Configure CustomerID FK referencing User
            builder.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.CustomerID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure BookID FK referencing Book
            builder.HasOne(p => p.Book)
                .WithMany()
                .HasForeignKey(p => p.BookID)
                .OnDelete(DeleteBehavior.SetNull);

            // Indices
            builder.HasIndex(p => p.ReportCount);
            builder.HasIndex(p => p.CreatedAt);
        }
    }

    public class ThreadCommentConfiguration : IEntityTypeConfiguration<ThreadComment>
    {
        public void Configure(EntityTypeBuilder<ThreadComment> builder)
        {
            builder.ToTable("ThreadComment", "Thread");
            builder.HasKey(c => c.CommentID);

            builder.Property(c => c.Content)
                .IsRequired();

            // Configure relationship with ThreadPost
            builder.HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostID)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure relationship with User (Customer)
            builder.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.CustomerID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class ThreadImageConfiguration : IEntityTypeConfiguration<ThreadImage>
    {
        public void Configure(EntityTypeBuilder<ThreadImage> builder)
        {
            builder.ToTable("ThreadImage", "Thread");
            builder.HasKey(i => i.ImageID);

            builder.Property(i => i.ImagePath)
                .IsRequired()
                .HasMaxLength(500);

            // Configure relationship with ThreadPost
            builder.HasOne(i => i.Post)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.PostID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class ThreadLikeConfiguration : IEntityTypeConfiguration<ThreadLike>
    {
        public void Configure(EntityTypeBuilder<ThreadLike> builder)
        {
            builder.ToTable("ThreadLike", "Thread");
            builder.HasKey(l => new { l.PostID, l.CustomerID });

            builder.Property(l => l.CreatedAt)
                .HasDefaultValueSql("SYSDATETIME()");

            builder.HasOne(l => l.Post)
                .WithMany(p => p.Likes)
                .HasForeignKey(l => l.PostID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.CustomerID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(l => l.CustomerID);
        }
    }

    public class ThreadShareConfiguration : IEntityTypeConfiguration<ThreadShare>
    {
        public void Configure(EntityTypeBuilder<ThreadShare> builder)
        {
            builder.ToTable("ThreadShare", "Thread");
            builder.HasKey(s => s.ShareID);

            builder.Property(s => s.ShareUrl)
                .HasMaxLength(1000);

            builder.Property(s => s.CreatedAt)
                .HasDefaultValueSql("SYSDATETIME()");

            builder.HasOne(s => s.Post)
                .WithMany(p => p.Shares)
                .HasForeignKey(s => s.PostID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.CustomerID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(s => s.PostID);
            builder.HasIndex(s => s.CustomerID);
            builder.HasIndex(s => s.CreatedAt);
        }
    }
}
