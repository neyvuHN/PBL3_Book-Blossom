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

            await SeedDashboardRequirementsAsync(context);
        }

        private static async Task SeedDashboardRequirementsAsync(ApplicationDbContext context)
        {
            // 1. Seed System Configurations nếu thiếu
            if (!await context.SystemConfigurations.AnyAsync(c => c.ConfigName == "CommunityReportThreshold"))
            {
                context.SystemConfigurations.Add(new SystemConfiguration
                {
                    SystemAdminID = 1,
                    ConfigName = "CommunityReportThreshold",
                    ConfigValue = "5",
                    Description = "Ngưỡng báo cáo vi phạm cộng đồng",
                    GroupType = GroupType.Community,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            if (!await context.SystemConfigurations.AnyAsync(c => c.ConfigName == "OrderConfirmTimeoutHours"))
            {
                context.SystemConfigurations.Add(new SystemConfiguration
                {
                    SystemAdminID = 1,
                    ConfigName = "OrderConfirmTimeoutHours",
                    ConfigValue = "48",
                    Description = "Số giờ quá hạn để xác nhận đơn hàng",
                    GroupType = GroupType.System,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            await context.SaveChangesAsync();

            // 2. Seed Membership Ranks nếu chưa có
            if (!await context.MembershipRanks.AnyAsync())
            {
                var ranks = new[]
                {
                    new MembershipRank { RankID = 1, RankType = 0, MinSpending = 0, DiscountRate = 0 }, // Đồng
                    new MembershipRank { RankID = 2, RankType = 1, MinSpending = 1000000, DiscountRate = 2 }, // Bạc
                    new MembershipRank { RankID = 3, RankType = 2, MinSpending = 5000000, DiscountRate = 5 }, // Vàng
                    new MembershipRank { RankID = 4, RankType = 3, MinSpending = 10000000, DiscountRate = 10 } // Kim Cương
                };
                await context.MembershipRanks.AddRangeAsync(ranks);
                await context.SaveChangesAsync();
            }

            // 3. Seed Categories nếu trống
            if (!await context.Categories.AnyAsync())
            {
                var categories = new[]
                {
                    new Category { CategoryName = "Fiction", Description = "Stories that are imaginary", Status = CategoryStatus.Active },
                    new Category { CategoryName = "Non-Fiction", Description = "Real-world based content", Status = CategoryStatus.Active },
                    new Category { CategoryName = "Science", Description = "Books related to scientific topics", Status = CategoryStatus.Active },
                    new Category { CategoryName = "Technology", Description = "Books about IT and modern tech", Status = CategoryStatus.Active },
                    new Category { CategoryName = "Self-Help", Description = "Personal development and growth", Status = CategoryStatus.Active }
                };
                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }

            // 4. Seed RealBooks nếu trống
            if (!await context.RealBooks.AnyAsync())
            {
                var catFiction = await context.Categories.FirstOrDefaultAsync(c => c.CategoryName == "Fiction");
                var catScience = await context.Categories.FirstOrDefaultAsync(c => c.CategoryName == "Science");
                var catSelfHelp = await context.Categories.FirstOrDefaultAsync(c => c.CategoryName == "Self-Help");

                var fictionId = catFiction?.CategoryID ?? 1;
                var scienceId = catScience?.CategoryID ?? 2;
                var selfHelpId = catSelfHelp?.CategoryID ?? 3;

                var realBooks = new[]
                {
                    new RealBook { CategoryID = fictionId, Title = "The Lost World", Publisher = "NXB Trẻ", ISBN = "9786041123451", PublishYear = 2024, Description = "Cuộc phiêu lưu thế giới bị mất", Price = 150000, Weight = 0.4m, UnitsInStock = 20, IsContinued = true },
                    new RealBook { CategoryID = scienceId, Title = "Science Basics", Publisher = "NXB Khoa Học", ISBN = "9786042123452", PublishYear = 2023, Description = "Kiến thức khoa học cơ bản", Price = 200000, Weight = 0.5m, UnitsInStock = 0, IsContinued = true },
                    new RealBook { CategoryID = selfHelpId, Title = "Life of Elon", Publisher = "NXB Thế Giới", ISBN = "9786043123453", PublishYear = 2024, Description = "Tiểu sử Elon Musk", Price = 190000, Weight = 0.6m, UnitsInStock = 5, IsContinued = true },
                    new RealBook { CategoryID = selfHelpId, Title = "Future AI", Publisher = "NXB Công Nghệ", ISBN = "9786044123454", PublishYear = 2025, Description = "Tương lai của Trí Tuệ Nhân Tạo", Price = 300000, Weight = 0.5m, UnitsInStock = 8, IsContinued = true }
                };
                await context.RealBooks.AddRangeAsync(realBooks);
                await context.SaveChangesAsync();
            }

            // 5. Seed BlindBooks nếu trống
            if (!await context.BlindBooks.AnyAsync())
            {
                var books = await context.RealBooks.ToListAsync();
                foreach (var b in books.Take(3))
                {
                    var blind = new BlindBook
                    {
                        RealBookID = b.BookID,
                        Keywords = "Bí ẩn, Trí tuệ, Thú vị",
                        Quotes = "Một cuốn sách sẽ làm thay đổi tư duy của bạn.",
                        Category = "Bí Ẩn",
                        Hashtags = "#blindbook #bookblossom",
                        Price = b.Price + 20000,
                        StockQuantity = 15,
                        Barcode = "BL" + b.BookID.ToString("D6"),
                        IsLocked = false,
                        RequestQuantity = 0,
                        BlindBookRequestStatus = BlindBookRequestStatus.Approved
                    };
                    context.BlindBooks.Add(blind);
                }
                await context.SaveChangesAsync();
            }

            // 6. Đảm bảo có ít nhất 15 Customers trong database để Lifetime Buyers đẹp mắt
            var customersCount = await context.Users.CountAsync(u => u.RoleID == UserRole.Customer);
            if (customersCount < 15)
            {
                int needToCreate = 15 - customersCount;
                var defaultRank = await context.MembershipRanks.OrderBy(r => r.MinSpending).FirstOrDefaultAsync();
                for (int i = 1; i <= needToCreate; i++)
                {
                    var username = $"customer_seed_{i}";
                    var user = new User
                    {
                        UserName = username,
                        Password = BCrypt.Net.BCrypt.HashPassword("customer123"),
                        Email = $"{username}@example.com",
                        PhoneNumber = $"09811122{i:D2}",
                        FirstName = $"Seed",
                        LastName = $"Customer {i}",
                        RoleID = UserRole.Customer,
                        AccountStatus = AccountStatus.Active,
                        IsActive = true
                    };
                    context.Users.Add(user);
                    await context.SaveChangesAsync();

                    // Tạo kèm CustomerDetail
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

                    // Tạo kèm CustomerReputation
                    var reputation = new CustomerReputation
                    {
                        CustomerID = user.UserID,
                        ReputationPoint = 100,
                        RankID = defaultRank?.RankID
                    };
                    context.CustomerReputations.Add(reputation);
                    await context.SaveChangesAsync();
                }
            }

            // 7. Seed Orders và OrderDetails trải rộng trong 30 ngày qua
            var dateLimit = DateTime.UtcNow.AddDays(-30);
            var recentOrdersCount = await context.Orders.CountAsync(o => o.OrderDate >= dateLimit);
            if (recentOrdersCount < 80)
            {
                var customerDetails = await context.CustomerDetails.ToListAsync();
                var realBooks = await context.RealBooks.ToListAsync();
                var blindBooks = await context.BlindBooks.ToListAsync();

                if (customerDetails.Any() && realBooks.Any())
                {
                    var rand = new Random();
                    // Tạo 35 đơn hàng ngẫu nhiên trải đều trong 30 ngày qua
                    for (int i = 0; i < 35; i++)
                    {
                        var detail = customerDetails[rand.Next(customerDetails.Count)];
                        var cust = await context.Users.FindAsync(detail.CustomerID);
                        if (cust == null) continue;

                        var daysAgo = rand.Next(1, 30);
                        var orderDate = DateTime.UtcNow.AddDays(-daysAgo).AddHours(rand.Next(24)).AddMinutes(rand.Next(60));

                        OrderStatus status = OrderStatus.Completed;
                        int prob = rand.Next(100);
                        if (prob < 60) status = OrderStatus.Completed;
                        else if (prob < 75) status = OrderStatus.Pending;
                        else if (prob < 85) status = OrderStatus.Returning;
                        else if (prob < 95) status = OrderStatus.Cancelled;
                        else status = OrderStatus.Shipping;

                        var order = new Order
                        {
                            CustomerID = cust.UserID,
                            OrderDate = orderDate,
                            OrderStatus = status,
                            PaymentMethod = rand.Next(2) == 0 ? PaymentMethod.COD : PaymentMethod.VNPay,
                            PaymentStatus = (byte)(status == OrderStatus.Completed ? 1 : 0),
                            ShippingFee = 30000,
                            DiscountAmount = 0,
                            ShipReceiverName = cust.FirstName + " " + cust.LastName,
                            ShipPhoneNumber = cust.PhoneNumber,
                            ShipDetailAddress = "Số " + rand.Next(1, 200) + " Đường Lê Lợi, TP. Đà Nẵng",
                            Note = "Đơn hàng thử nghiệm seed tự động"
                        };

                        context.Orders.Add(order);
                        await context.SaveChangesAsync();

                        int numDetails = rand.Next(1, 3);
                        decimal totalAmount = 30000;
                        var usedBooks = new HashSet<long>();

                        for (int j = 0; j < numDetails; j++)
                        {
                            OrderDetail orderDetailItem = null;
                            if (rand.Next(2) == 0 && blindBooks.Any())
                            {
                                var bb = blindBooks[rand.Next(blindBooks.Count)];
                                if (!usedBooks.Contains(bb.BlindBookID))
                                {
                                    usedBooks.Add(bb.BlindBookID);
                                    orderDetailItem = new OrderDetail
                                    {
                                        OrderID = order.OrderID,
                                        BookID = bb.RealBookID,
                                        BlindBookID = bb.BlindBookID,
                                        UnitPrice = bb.Price,
                                        Quantity = rand.Next(1, 3),
                                        Discount = 0
                                    };
                                    totalAmount += bb.Price * orderDetailItem.Quantity;
                                }
                            }
                            else
                            {
                                var rb = realBooks[rand.Next(realBooks.Count)];
                                if (!usedBooks.Contains(rb.BookID))
                                {
                                    usedBooks.Add(rb.BookID);
                                    orderDetailItem = new OrderDetail
                                    {
                                        OrderID = order.OrderID,
                                        BookID = rb.BookID,
                                        BlindBookID = null,
                                        UnitPrice = rb.Price,
                                        Quantity = rand.Next(1, 3),
                                        Discount = 0
                                    };
                                    totalAmount += rb.Price * orderDetailItem.Quantity;
                                }
                            }

                            if (orderDetailItem != null)
                            {
                                context.OrderDetails.Add(orderDetailItem);
                            }
                        }

                        order.TotalAmount = totalAmount;
                        await context.SaveChangesAsync();
                    }
                }
            }

            // 8. Đảm bảo có ít nhất 2 đơn hàng Pending bị quá hạn (Delayed Pending Orders) để kiểm tra dashboard cảnh báo
            var delayedPendingCount = await context.Orders.CountAsync(o => o.OrderStatus == OrderStatus.Pending && o.OrderDate <= DateTime.UtcNow.AddHours(-48));
            if (delayedPendingCount < 2)
            {
                var customerDetails = await context.CustomerDetails.ToListAsync();
                var realBooks = await context.RealBooks.ToListAsync();
                if (customerDetails.Any() && realBooks.Any())
                {
                    var rand = new Random();
                    for (int i = 0; i < 2; i++)
                    {
                        var detail = customerDetails[rand.Next(customerDetails.Count)];
                        var cust = await context.Users.FindAsync(detail.CustomerID);
                        if (cust == null) continue;

                        var orderDate = DateTime.UtcNow.AddHours(-rand.Next(50, 75));

                        var order = new Order
                        {
                            CustomerID = cust.UserID,
                            OrderDate = orderDate,
                            OrderStatus = OrderStatus.Pending,
                            PaymentMethod = PaymentMethod.COD,
                            PaymentStatus = 0,
                            ShippingFee = 30000,
                            DiscountAmount = 0,
                            ShipReceiverName = cust.FirstName + " " + cust.LastName,
                            ShipPhoneNumber = cust.PhoneNumber,
                            ShipDetailAddress = "Khu ký túc xá Đại học Seed, TP. Đà Nẵng",
                            Note = "Đơn hàng Pending trễ hạn để test cảnh báo"
                        };

                        context.Orders.Add(order);
                        await context.SaveChangesAsync();

                        var rb = realBooks[rand.Next(realBooks.Count)];
                        var detailItem = new OrderDetail
                        {
                            OrderID = order.OrderID,
                            BookID = rb.BookID,
                            BlindBookID = null,
                            UnitPrice = rb.Price,
                            Quantity = 1,
                            Discount = 0
                        };
                        context.OrderDetails.Add(detailItem);

                        order.TotalAmount = rb.Price + 30000;
                        await context.SaveChangesAsync();
                    }
                }
            }

            // 9. Đảm bảo có ít nhất 2 bài ThreadPost bị Report nhiều (vượt ngưỡng threshold = 5)
            var threshold = 5;
            var highReportPostsCount = await context.ThreadPosts.CountAsync(p => p.ReportCount >= threshold);
            if (highReportPostsCount < 2)
            {
                var customerDetails = await context.CustomerDetails.ToListAsync();
                if (customerDetails.Count >= 2)
                {
                    var authorDetail = customerDetails[0];
                    var reporterDetail = customerDetails[1];
                    var author = await context.Users.FindAsync(authorDetail.CustomerID);
                    var reporter = await context.Users.FindAsync(reporterDetail.CustomerID);

                    if (author != null && reporter != null)
                    {
                        var post1 = new ThreadPost
                        {
                            CustomerID = author.UserID,
                            Title = "Spam: Nhận thẻ cào điện thoại 500k miễn phí tại đây!!!",
                            Content = "Click ngay vào link rút gọn này để nhận quà tặng cực khủng từ nhà tài trợ bí ẩn. Chỉ áp dụng hôm nay thôi nhé anh em ơi! Nhanh tay nào!",
                            Hashtags = "#spam #quatang #free",
                            CreatedAt = DateTime.UtcNow.AddDays(-3),
                            IsHidden = false,
                            ReportCount = 6
                        };

                        var post2 = new ThreadPost
                        {
                            CustomerID = author.UserID,
                            Title = "Cá độ bóng đá tỉ lệ ăn cực cao uy tín 100%",
                            Content = "Tham gia sòng bạc trực tuyến, cá cược thể thao quốc tế. Đảm bảo rút tiền nhanh gọn trong vòng 3 phút, bảo mật danh tính tuyệt đối.",
                            Hashtags = "#cado #bongda #kiemtien",
                            CreatedAt = DateTime.UtcNow.AddDays(-2),
                            IsHidden = false,
                            ReportCount = 8
                        };

                        await context.ThreadPosts.AddRangeAsync(post1, post2);
                        await context.SaveChangesAsync();

                        var report1 = new Report
                        {
                            PostID = post1.PostID,
                            CustomerID = reporter.UserID,
                            Reason = ReportType.Spam,
                            Description = "Bài viết quảng cáo spam liên tục làm loãng diễn đàn, chứa link độc hại.",
                            CreatedAt = DateTime.UtcNow.AddHours(-12),
                            IsAccurate = null
                        };

                        var report2 = new Report
                        {
                            PostID = post2.PostID,
                            CustomerID = reporter.UserID,
                            Reason = ReportType.Fraud,
                            Description = "Nội dung quảng cáo cá độ bất hợp pháp, vi phạm thuần phong mỹ tục.",
                            CreatedAt = DateTime.UtcNow.AddHours(-6),
                            IsAccurate = null
                        };

                        await context.Reports.AddRangeAsync(report1, report2);
                        await context.SaveChangesAsync();
                    }
                }
            }

            // 10. Seed Vouchers mẫu đủ 5 trạng thái
            if (!await context.Vouchers.AnyAsync(v => v.VoucherCode == "WELCOMENEW"))
            {
                var vouchers = new[]
                {
                    new Voucher
                    {
                        VoucherName = "Mừng Khai Trương",
                        VoucherCode = "WELCOMENEW",
                        DiscountType = VoucherDiscountType.Percentage,
                        DiscountValue = 10,
                        MaxDiscountAmount = 50000,
                        MinOrderValue = 100000,
                        TotalLimit = 500,
                        UsedCount = 120,
                        StatusVoucher = VoucherStatus.Active,
                        StartDate = DateTime.UtcNow.AddDays(-10),
                        EndDate = DateTime.UtcNow.AddDays(20),
                        IsStackable = true,
                        IsAutoRefundable = true,
                        MinReputationRequired = 0,
                        MembershipRankRequired = 0
                    },
                    new Voucher
                    {
                        VoucherName = "Mùa Hè Rực Rỡ",
                        VoucherCode = "SUMMER2026",
                        DiscountType = VoucherDiscountType.Fixed,
                        DiscountValue = 30000,
                        MaxDiscountAmount = 30000,
                        MinOrderValue = 150000,
                        TotalLimit = 100,
                        UsedCount = 0,
                        StatusVoucher = VoucherStatus.Scheduled,
                        StartDate = DateTime.UtcNow.AddDays(5),
                        EndDate = DateTime.UtcNow.AddDays(25),
                        IsStackable = false,
                        IsAutoRefundable = true,
                        MinReputationRequired = 50,
                        MembershipRankRequired = 1
                    },
                    new Voucher
                    {
                        VoucherName = "Tri Ân Độc Giả Thân Thiết",
                        VoucherCode = "VIPMEMBERS",
                        DiscountType = VoucherDiscountType.Percentage,
                        DiscountValue = 25,
                        MaxDiscountAmount = 150000,
                        MinOrderValue = 300000,
                        TotalLimit = 50,
                        UsedCount = 5,
                        StatusVoucher = VoucherStatus.Active,
                        StartDate = DateTime.UtcNow.AddDays(-5),
                        EndDate = DateTime.UtcNow.AddDays(15),
                        IsStackable = true,
                        IsAutoRefundable = false,
                        MinReputationRequired = 100,
                        MembershipRankRequired = 3
                    },
                    new Voucher
                    {
                        VoucherName = "Nháp Sự Kiện Sắp Tới",
                        VoucherCode = "DRAFTVOUCH",
                        DiscountType = VoucherDiscountType.Fixed,
                        DiscountValue = 50000,
                        MaxDiscountAmount = 50000,
                        MinOrderValue = 200000,
                        TotalLimit = 1000,
                        UsedCount = 0,
                        StatusVoucher = VoucherStatus.Draft,
                        StartDate = DateTime.UtcNow.AddDays(30),
                        EndDate = DateTime.UtcNow.AddDays(60),
                        IsStackable = false,
                        IsAutoRefundable = false,
                        MinReputationRequired = 0,
                        MembershipRankRequired = 0
                    },
                    new Voucher
                    {
                        VoucherName = "Sự Kiện Sách Hay Tạm Dừng",
                        VoucherCode = "MIDYEARRUS",
                        DiscountType = VoucherDiscountType.Fixed,
                        DiscountValue = 20000,
                        MaxDiscountAmount = 20000,
                        MinOrderValue = 100000,
                        TotalLimit = 300,
                        UsedCount = 45,
                        StatusVoucher = VoucherStatus.Paused,
                        StartDate = DateTime.UtcNow.AddDays(-15),
                        EndDate = DateTime.UtcNow.AddDays(15),
                        IsStackable = false,
                        IsAutoRefundable = true,
                        MinReputationRequired = 20,
                        MembershipRankRequired = 0
                    },
                    new Voucher
                    {
                        VoucherName = "Giờ Vàng Tuần Trước",
                        VoucherCode = "FLASHPAST",
                        DiscountType = VoucherDiscountType.Percentage,
                        DiscountValue = 50,
                        MaxDiscountAmount = 200000,
                        MinOrderValue = 400000,
                        TotalLimit = 50,
                        UsedCount = 50,
                        StatusVoucher = VoucherStatus.Ended,
                        StartDate = DateTime.UtcNow.AddDays(-10),
                        EndDate = DateTime.UtcNow.AddDays(-3),
                        IsStackable = true,
                        IsAutoRefundable = true,
                        MinReputationRequired = 80,
                        MembershipRankRequired = 2
                    }
                };
                await context.Vouchers.AddRangeAsync(vouchers);
                await context.SaveChangesAsync();
            }

            // Cập nhật ngẫu nhiên các voucher đã seed vào các đơn hàng completed chưa có voucher
            var vouchersToAssign = await context.Vouchers
                .Where(v => v.VoucherCode == "WELCOMENEW" || v.VoucherCode == "VIPMEMBERS" || v.VoucherCode == "MIDYEARRUS" || v.VoucherCode == "FLASHPAST")
                .ToListAsync();

            if (vouchersToAssign.Any())
            {
                var unassignedCompletedOrders = await context.Orders
                    .Where(o => o.OrderStatus == OrderStatus.Completed && o.VoucherID == null)
                    .ToListAsync();
                
                if (unassignedCompletedOrders.Any())
                {
                    int index = 0;
                    foreach (var orderItem in unassignedCompletedOrders)
                    {
                        var v = vouchersToAssign[index % vouchersToAssign.Count];
                        index++;

                        decimal subtotal = orderItem.TotalAmount - orderItem.ShippingFee.GetValueOrDefault();
                        if (subtotal <= 0) subtotal = 100000;
                        
                        decimal discount = 0;
                        if (v.DiscountType == VoucherDiscountType.Fixed)
                        {
                            discount = v.DiscountValue;
                        }
                        else
                        {
                            discount = subtotal * v.DiscountValue / 100;
                            if (v.MaxDiscountAmount > 0 && discount > v.MaxDiscountAmount)
                                discount = v.MaxDiscountAmount;
                        }
                        
                        if (discount > subtotal) discount = subtotal * 0.5m;
                        
                        orderItem.VoucherID = v.VoucherID;
                        orderItem.DiscountAmount = discount;
                        orderItem.TotalAmount = subtotal + orderItem.ShippingFee.GetValueOrDefault() - discount;
                    }
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
