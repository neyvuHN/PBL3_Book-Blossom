using BookBlossom.Core.Entities;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using BookBlossom.Core.Enums;

namespace BookBlossom.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<StaffDetail> StaffDetails { get; set; }
        public DbSet<CustomerDetail> CustomerDetails { get; set; }
        public DbSet<GuestDetail> GuestDetails { get; set; }
        public DbSet<OTPLog> OTPLogs { get; set; }
        public DbSet<ServicePackage> ServicePackages { get; set; }
        public DbSet<CustomerService> CustomerServices { get; set; }
        public DbSet<ServiceHistory> ServiceHistories { get; set; }
        public DbSet<Role> Roles {get; set; }
        public DbSet<CustomerPreference> CustomerPreferences {get; set; }
        public DbSet<GuestPreference> GuestPreferences {get; set; }
        public DbSet<Category> Categories {get; set; }
        public DbSet<RealBook> RealBooks {get; set; }
        public DbSet<BookImage> BookImages { get; set; }
        public DbSet<Importing> Importings { get; set; }
        public DbSet<ImportingDetail> ImportingDetails { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<BlindBook> BlindBooks { get; set; }
        public DbSet<BlindBookImage> BlindBookImages { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<MembershipRank> MembershipRanks { get; set; }
        public DbSet<CustomerReputation> CustomerReputations { get; set; }
        public DbSet<SwipeLog> SwipeLogs { get; set; } 
        public DbSet<ReturnRequest> ReturnRequests { get; set; }
        public DbSet<ThreadPost> ThreadPosts { get; set; }
        public DbSet<ThreadComment> ThreadComments { get; set; }
        public DbSet<ThreadImage> ThreadImages { get; set; }
        public DbSet<ThreadLike> ThreadLikes { get; set; }
        public DbSet<ThreadShare> ThreadShares { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<UserNotification> UserNotifications { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ReviewMedia> ReviewMedias { get; set; }
        public DbSet<ReviewLike> ReviewLikes { get; set; }
        public DbSet<ReviewReport> ReviewReports { get; set; }
        public DbSet<ReputationHistory> ReputationHistories { get; set; }
        public DbSet<Badge> Badges { get; set; }
        public DbSet<BadgeCustomer> BadgeCustomers { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageAttachment> MessageAttachments { get; set; }
        public DbSet<CallRequest> CallRequests { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<CustomerVoucher> CustomerVouchers { get; set; }
        public DbSet<VoucherCategory> VoucherCategories { get; set; }
        public DbSet<VoucherBook> VoucherBooks { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<SystemConfiguration> SystemConfigurations { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<BookAuthor> BookAuthors { get; set; }
        public DbSet<DeliveryAddress> DeliveryAddresses { get; set; }
        public DbSet<OrderVoucher> OrderVouchers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BookAuthor>(entity =>
            {
                entity.HasKey(ba => new { ba.BookID, ba.AuthorID });

                entity.HasOne(ba => ba.Book)
                      .WithMany(b => b.BookAuthors)
                      .HasForeignKey(ba => ba.BookID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ba => ba.Author)
                      .WithMany(a => a.BookAuthors)
                      .HasForeignKey(ba => ba.AuthorID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OrderVoucher>(entity =>
            {
                entity.HasKey(ov => new { ov.OrderID, ov.VoucherID });

                entity.HasOne(ov => ov.Order)
                      .WithMany(o => o.OrderVouchers)
                      .HasForeignKey(ov => ov.OrderID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ov => ov.Voucher)
                      .WithMany(v => v.OrderVouchers)
                      .HasForeignKey(ov => ov.VoucherID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 1. VẪN MỞ DÒNG QUÉT TỰ ĐỘNG NÀY ĐỂ GIỮ CHO USER, CUSTOMERDETAIL... KHÔNG BỊ LỖI
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // Review Entity configuration
            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasOne(r => r.Orders)
                    .WithMany(o => o.Reviews)
                    .HasForeignKey(r => r.OrderID)
                    .OnDelete(DeleteBehavior.NoAction);

                // Default values matching SQL constraints
                entity.Property(r => r.LikeCount).HasDefaultValue(0);
                entity.Property(r => r.CreatedAt).HasDefaultValueSql("getutcdate()");
                entity.Property(r => r.IsHidden).HasDefaultValue(false);
                entity.Property(r => r.IsReputationAwarded).HasDefaultValue(false);
                entity.Property(r => r.ReportsCount).HasDefaultValue(0);
                entity.Property(r => r.IsTransferredToStore).HasDefaultValue(false);
            });

            // ReviewMedia Configuration
            modelBuilder.Entity<ReviewMedia>(entity =>
            {
                entity.Property(e => e.MediaType)
                      .HasConversion<byte>()
                      .HasColumnType("tinyint");
                
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            });

            // ReviewLike Configuration
            modelBuilder.Entity<ReviewLike>(entity =>
            {
                entity.HasKey(e => new { e.ReviewID, e.CustomerID });
                
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
                
                entity.HasOne(rl => rl.CustomerDetail)
                      .WithMany()
                      .HasForeignKey(rl => rl.CustomerID)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            // ReviewReport Configuration
            modelBuilder.Entity<ReviewReport>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
                entity.Property(e => e.Status)
                      .HasConversion<byte>()
                      .HasColumnType("tinyint")
                      .HasDefaultValue(ReportStatus.Pending);

                entity.HasOne(rr => rr.Reporter)
                      .WithMany()
                      .HasForeignKey(rr => rr.ReporterID)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            // (Removed Ignore lines for Importing and ImportingDetail to solve EF Core schema sync issue)
        }
    }
}
