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
                    new Role { RoleID = UserRole.Admin, RoleName = "Admin", Description = "Quyền Admin" }
                };
                await context.Roles.AddRangeAsync(roles);
                await context.SaveChangesAsync();
            }

            // 1.5. Seed Badges nếu chưa có
            if (!await context.Badges.AnyAsync())
            {
                var badges = new[]
                {
                    new Badge { BadgeID = 1, BadgeName = "Chiến thần Review Cấp 1 (Người trải nghiệm)", Description = "Viết đủ 5 review có ảnh." },
                    new Badge { BadgeID = 2, BadgeName = "Chiến thần Review Cấp 2 (Chuyên gia phê bình)", Description = "Viết đủ 20 bài review chất lượng (đạt >= 5 like)." },
                    new Badge { BadgeID = 3, BadgeName = "Chiến thần Review Cấp 3 (Độc giả thông thái)", Description = "Viết đủ 50 bài review chất lượng + lọt Top \"Review của tháng\"." },
                    new Badge { BadgeID = 4, BadgeName = "Sứ giả Tri thức (Sharing)", Description = "Chia sẻ tích cực link sách hoặc bài viết lên mạng xã hội (Facebook/Instagram)." },
                    new Badge { BadgeID = 5, BadgeName = "Trùm Blind Date Cấp 1 (Tò mò)", Description = "Mua đủ 3 đơn Blind Date." },
                    new Badge { BadgeID = 6, BadgeName = "Trùm Blind Date Cấp 2 (Kẻ săn tin)", Description = "Mua đủ 10 đơn Blind Date." },
                    new Badge { BadgeID = 7, BadgeName = "Trùm Blind Date Cấp 3 (Định mệnh)", Description = "Mua đủ 25 đơn Blind Date (hiệu ứng màu tím huyền bí xung quanh Avatar)." },
                    new Badge { BadgeID = 8, BadgeName = "Mọt sách chính hiệu (Bookworm)", Description = "Mua đủ 5 thể loại sách khác nhau (Tâm lý, Kỹ năng, Tiểu thuyết, Kinh dị, Khoa học...)." },
                    new Badge { BadgeID = 9, BadgeName = "Người dùng gương mẫu", Description = "Duy trì điểm Uy tín ở mức tối đa (150 điểm) trong vòng 3 tháng liên tiếp." },
                    new Badge { BadgeID = 10, BadgeName = "Cánh tay đắc lực (Moderator Assistant)", Description = "Có > 10 lượt report bài viết vi phạm chính xác." }
                };
                await context.Badges.AddRangeAsync(badges);
                await context.SaveChangesAsync();
            }

            // 2. Định nghĩa danh sách tài khoản test
            var testAccounts = new[]
            {
                new { UserName = "admin_test", Password = "admin", Role = UserRole.Admin, Email = "admin@bookblossom.com", Phone = "0900000001", FirstName = "Hệ thống", LastName = "Admin", Department = Department.SystemAdmin },
                new { UserName = "storemanager_test", Password = "storemanager", Role = UserRole.Admin, Email = "manager@bookblossom.com", Phone = "0900000002", FirstName = "Kho", LastName = "Quản lý", Department = Department.StoreManager },
                new { UserName = "moderator_test", Password = "moderator", Role = UserRole.Admin, Email = "moderator@bookblossom.com", Phone = "0900000003", FirstName = "Duyệt", LastName = "Kiểm duyệt viên", Department = Department.Moderator },
                new { UserName = "marketing_test", Password = "marketing", Role = UserRole.Admin, Email = "marketing@bookblossom.com", Phone = "0900000004", FirstName = "MKT", LastName = "Marketing", Department = Department.MarketingManager },
                new { UserName = "customer_test", Password = "customer", Role = UserRole.Customer, Email = "customer@bookblossom.com", Phone = "0900000005", FirstName = "Khách", LastName = "Khách hàng", Department = (Department)0 }
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
                            Department = acc.Department,
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
        }
    }
}
