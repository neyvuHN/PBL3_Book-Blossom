using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;

namespace BookBlossom.Infrastructure.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(ApplicationDbContext context)
        {
            // Đảm bảo cơ sở dữ liệu đã được khởi tạo
            await context.Database.EnsureCreatedAsync();

            // 1. Seed Roles nếu chưa có
            if (!await context.Roles.AnyAsync())
            {
                var roles = new[]
                {
                    new Role { RoleID = UserRole.Guest, RoleName = "Guest", Description = "Quyền Guest" },
                    new Role { RoleID = UserRole.Customer, RoleName = "Customer", Description = "Quyền Customer" },
                    new Role { RoleID = UserRole.SystemAdmin, RoleName = "SystemAdmin", Description = "Quyền SystemAdmin" },
                    new Role { RoleID = UserRole.Moderator, RoleName = "Moderator", Description = "Quyền Moderator" },
                    new Role { RoleID = UserRole.MarketingManager, RoleName = "MarketingManager", Description = "Quyền MarketingManager" },
                    new Role { RoleID = UserRole.StoreManager, RoleName = "StoreManager", Description = "Quyền StoreManager" }
                };
                await context.Roles.AddRangeAsync(roles);
                await context.SaveChangesAsync();
            }

            // 2. Định nghĩa danh sách tài khoản test
            var testAccounts = new[]
            {
                new { UserName = "admin_test", Password = "admin", Role = UserRole.SystemAdmin, Email = "admin@bookblossom.com", Phone = "0900000001", FirstName = "Hệ thống", LastName = "Admin" },
                new { UserName = "storemanager_test", Password = "storemanager", Role = UserRole.StoreManager, Email = "manager@bookblossom.com", Phone = "0900000002", FirstName = "Kho", LastName = "Quản lý" },
                new { UserName = "moderator_test", Password = "moderator", Role = UserRole.Moderator, Email = "moderator@bookblossom.com", Phone = "0900000003", FirstName = "Duyệt", LastName = "Kiểm duyệt viên" },
                new { UserName = "marketing_test", Password = "marketing", Role = UserRole.MarketingManager, Email = "marketing@bookblossom.com", Phone = "0900000004", FirstName = "MKT", LastName = "Marketing" },
                new { UserName = "customer_test", Password = "customer", Role = UserRole.Customer, Email = "customer@bookblossom.com", Phone = "0900000005", FirstName = "Khách", LastName = "Khách hàng" }
            };

            foreach (var acc in testAccounts)
            {
                // Kiểm tra xem user đã tồn tại chưa
                var existingUser = await context.Users.FirstOrDefaultAsync(u => u.UserName == acc.UserName);
                if (existingUser == null)
                {
                    // Tạo mới User
                    var user = new User
                    {
                        UserName = acc.UserName,
                        Password = BCrypt.Net.BCrypt.HashPassword(acc.Password),
                        Email = acc.Email,
                        PhoneNumber = acc.Phone,
                        FirstName = acc.FirstName,
                        LastName = acc.LastName,
                        RoleID = acc.Role,
                        AccountStatus = AccountStatus.Active,
                        IsActive = true
                    };

                    context.Users.Add(user);
                    await context.SaveChangesAsync(); // Lưu để có UserID

                    // Phân loại tạo chi tiết thông tin cho từng vai trò
                    if (acc.Role == UserRole.Customer)
                    {
                        var customerDetail = new CustomerDetail
                        {
                            CustomerID = user.UserID,
                            IsOnboardingCompleted = true,
                            TotalSpending = 0,
                            DailyUndoCount = 0,
                            CurrentMonthThreadCount = 0,
                            CurrentOrderStreak = 0
                        };
                        context.CustomerDetails.Add(customerDetail);
                    }
                    else
                    {
                        // Vai trò Staff/Admin/Manager
                        var staffDetail = new StaffDetail
                        {
                            StaffID = user.UserID,
                            Address = "Khu Công Nghệ Phần Mềm, Thủ Đức, TP.HCM",
                            IsOnboardingCompleted = true,
                            Department = acc.Role == UserRole.SystemAdmin ? Department.SystemAdmin :
                                         acc.Role == UserRole.StoreManager ? Department.StoreManager :
                                         acc.Role == UserRole.Moderator ? Department.Moderator :
                                         Department.MarketingManager,
                            Position = StaffPosition.Leader,
                            HireDate = DateTime.Today,
                            ContractType = ContractType.FullTime,
                            Salary = 15000000
                        };
                        context.StaffDetails.Add(staffDetail);
                    }
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
