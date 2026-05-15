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
        public DbSet<Role> Roles {get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // Seed Roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Guest" },
                new Role { Id = 2, Name = "Customer" },
                new Role { Id = 3, Name = "SystemAdmin" },
                new Role { Id = 4, Name = "Moderator" },
                new Role { Id = 5, Name = "MarketingManager" },
                new Role { Id = 6, Name = "StoreManager" }
            );
        }
    }
}