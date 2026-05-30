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
                    existingUser = user;
                }

                // Phân loại tạo chi tiết thông tin cho từng vai trò nếu chưa có
                if (acc.Role == UserRole.Customer)
                {
                    var exists = await context.CustomerDetails.AnyAsync(c => c.CustomerID == existingUser.UserID);
                    if (!exists)
                    {
                        var customerDetail = new CustomerDetail
                        {
                            CustomerID = existingUser.UserID,
                            IsOnboardingCompleted = true,
                            TotalSpending = 0,
                            DailyUndoCount = 0,
                            CurrentMonthThreadCount = 0,
                            CurrentOrderStreak = 0
                        };
                        context.CustomerDetails.Add(customerDetail);
                    }
                }
                else
                {
                    // Vai trò Staff/Admin/Manager
                    var exists = await context.StaffDetails.AnyAsync(s => s.StaffID == existingUser.UserID);
                    if (!exists)
                    {
                        var staffDetail = new StaffDetail
                        {
                            StaffID = existingUser.UserID,
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

            // 3. Seed dữ liệu mẫu cho module Threads nếu chưa có
            var testCustomer = await context.Users.FirstOrDefaultAsync(u => u.UserName == "customer_test");
            if (testCustomer != null)
            {
                if (!await context.ThreadPosts.AnyAsync())
                {
                    var post1 = new ThreadPost
                    {
                        CustomerID = testCustomer.UserID,
                        Title = "Kinh nghiệm đọc sách hiệu quả mỗi ngày",
                        Content = "Xin chào mọi người! Mình muốn chia sẻ một vài kinh nghiệm nhỏ để duy trì thói quen đọc sách 30 phút mỗi ngày. Các bạn có tips nào hay hơn không?",
                        Hashtags = "#docsach #kienthuc #習慣",
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        IsHidden = false,
                        ReportCount = 0
                    };

                    var post2 = new ThreadPost
                    {
                        CustomerID = testCustomer.UserID,
                        Title = "Gợi ý sách tiểu thuyết trinh thám hay nhất 2026",
                        Content = "Mình vừa hoàn thành một vài cuốn trinh thám rất hồi hộp và muốn giới thiệu cho mọi người. Nội dung cực kỳ lôi cuốn, không thể rời mắt!",
                        Hashtags = "#trinhtham #review #sachhay",
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        IsHidden = false,
                        ReportCount = 0
                    };

                    await context.ThreadPosts.AddRangeAsync(post1, post2);
                    await context.SaveChangesAsync();

                    var image1 = new ThreadImage
                    {
                        PostID = post1.PostID,
                        ImagePath = "/uploads/threads/sample_book1.jpg"
                    };
                    var image2 = new ThreadImage
                    {
                        PostID = post2.PostID,
                        ImagePath = "/uploads/threads/sample_book2.jpg"
                    };
                    await context.ThreadImages.AddRangeAsync(image1, image2);

                    var comment1 = new ThreadComment
                    {
                        PostID = post1.PostID,
                        CustomerID = testCustomer.UserID,
                        Content = "Bài viết hữu ích quá, mình cũng đang áp dụng phương pháp quả cà chua Pomodoro để đọc sách tập trung hơn.",
                        CreatedAt = DateTime.UtcNow.AddHours(-12)
                    };
                    var comment2 = new ThreadComment
                    {
                        PostID = post2.PostID,
                        CustomerID = testCustomer.UserID,
                        Content = "Hóng tên các tựa sách bạn giới thiệu cụ thể nhé!",
                        CreatedAt = DateTime.UtcNow.AddHours(-6)
                    };
                    await context.ThreadComments.AddRangeAsync(comment1, comment2);
                    await context.SaveChangesAsync();
                }
            }

            // 4. Seed Membership Ranks nếu chưa có
            if (!await context.MembershipRanks.AnyAsync())
            {
                var ranks = new[]
                {
                    new MembershipRank { RankType = 1, MinSpending = 0m, DiscountRate = 0m },
                    new MembershipRank { RankType = 2, MinSpending = 1000000m, DiscountRate = 0.02m },
                    new MembershipRank { RankType = 3, MinSpending = 5000000m, DiscountRate = 0.05m },
                    new MembershipRank { RankType = 4, MinSpending = 10000000m, DiscountRate = 0.10m }
                };
                await context.MembershipRanks.AddRangeAsync(ranks);
                await context.SaveChangesAsync();
            }

            // 5. Seed Badges nếu chưa có
            if (!await context.Badges.AnyAsync())
            {
                var badges = new[]
                {
                    new Badge { BadgeID = 1, BadgeName = "Review Champion - Explorer", Description = "Viết bài đánh giá sách đầu tiên của bạn" },
                    new Badge { BadgeID = 2, BadgeName = "Review Champion - Critic", Description = "Viết 5 bài đánh giá sách" },
                    new Badge { BadgeID = 3, BadgeName = "Review Champion - Sage", Description = "Viết 15 bài đánh giá sách" },
                    new Badge { BadgeID = 4, BadgeName = "Knowledge Ambassador", Description = "Đạt 5 lượt thích cho một bài đánh giá sách" },
                    new Badge { BadgeID = 5, BadgeName = "Blind Date Adventurer - Curious", Description = "Trải nghiệm Sách Mù 1 lần" },
                    new Badge { BadgeID = 6, BadgeName = "Blind Date Adventurer - Seeker", Description = "Trải nghiệm Sách Mù 5 lần" },
                    new Badge { BadgeID = 7, BadgeName = "Blind Date Adventurer - Destiny", Description = "Trải nghiệm Sách Mù 10 lần" },
                    new Badge { BadgeID = 8, BadgeName = "True Bookworm", Description = "Đọc/mua tổng cộng 10 cuốn sách" },
                    new Badge { BadgeID = 9, BadgeName = "Exemplary User", Description = "Điểm uy tín đạt 100/100 tối đa" },
                    new Badge { BadgeID = 10, BadgeName = "Moderator Assistant", Description = "Báo cáo thành công 5 bài viết vi phạm hoặc điểm uy tín đạt 120" }
                };
                await context.Badges.AddRangeAsync(badges);
                await context.SaveChangesAsync();
            }
        }
    }
}
