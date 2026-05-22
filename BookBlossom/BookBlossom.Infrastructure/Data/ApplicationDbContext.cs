using BookBlossom.Core.Entities;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

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
        public DbSet<Category> Categories {get; set; }
        public DbSet<RealBook> RealBooks {get; set; }
        public DbSet<Importing> Importings { get; set; }
        public DbSet<ImportingDetail> ImportingDetails { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<BlindBook> BlindBooks { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<MembershipRank> MembershipRanks { get; set; }
        public DbSet<CustomerReputation> CustomerReputations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. VẪN MỞ DÒNG QUÉT TỰ ĐỘNG NÀY ĐỂ GIỮ CHO USER, CUSTOMERDETAIL... KHÔNG BỊ LỖI
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // (Removed Ignore lines for Importing and ImportingDetail to solve EF Core schema sync issue)
        }
    }
}