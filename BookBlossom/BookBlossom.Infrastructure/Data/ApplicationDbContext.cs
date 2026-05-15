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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}