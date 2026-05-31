using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Core.DTOs.Notification;
using BookBlossom.Infrastructure.Data;
using BookBlossom.Infrastructure.Services;
using Xunit;

namespace BookBlossom.Tests.Services
{
    public class FakeNotificationService : INotificationService
    {
        public Task<IEnumerable<NotificationDTO>> GetUserNotificationsAsync(long userId, bool? isRead) => Task.FromResult<IEnumerable<NotificationDTO>>(new List<NotificationDTO>());
        public Task<bool> MarkAsReadAsync(long userId, long notificationId) => Task.FromResult(true);
        public Task<bool> MarkAllAsReadAsync(long userId) => Task.FromResult(true);
        public Task CreateAndSendNotificationAsync(long userId, string title, string content, NotificationType type, int? referenceId) => Task.CompletedTask;
        public Task<bool> SubscribeAsync(long customerId, long targetId, string targetType) => Task.FromResult(true);
        public Task<bool> UnsubscribeAsync(long customerId, long targetId, string targetType) => Task.FromResult(true);
        public Task<IEnumerable<SubscriptionDTO>> GetSubscriptionsAsync(long customerId) => Task.FromResult<IEnumerable<SubscriptionDTO>>(new List<SubscriptionDTO>());
    }

    public class GamificationServiceTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private async Task SeedBadgesAsync(ApplicationDbContext context)
        {
            var badges = new[]
            {
                new Badge { BadgeID = 1, BadgeName = "Chiến thần Review Cấp 1", Description = "Review Lvl 1" },
                new Badge { BadgeID = 2, BadgeName = "Chiến thần Review Cấp 2", Description = "Review Lvl 2" },
                new Badge { BadgeID = 5, BadgeName = "Trùm Blind Date Cấp 1", Description = "Blind Date Lvl 1" },
                new Badge { BadgeID = 8, BadgeName = "Mọt sách chính hiệu", Description = "Bookworm" },
                new Badge { BadgeID = 9, BadgeName = "Người dùng gương mẫu", Description = "Exemplary User" }
            };
            await context.Badges.AddRangeAsync(badges);
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task CheckAndGrantInteractionBadges_ShouldGrantLevel1_WhenCustomerHasFiveReviewsWithImages()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBadgesAsync(context);
            
            // Seed CustomerDetail
            var customer = new CustomerDetail
            {
                CustomerID = 1L,
                IsOnboardingCompleted = true
            };
            await context.CustomerDetails.AddAsync(customer);

            // Seed 5 reviews with images
            for (int i = 1; i <= 5; i++)
            {
                context.Reviews.Add(new Review
                {
                    ReviewID = i,
                    CustomerID = 1L,
                    Content = $"Review {i}",
                    ImageVideoPath = $"image_{i}.png",
                    CreatedAt = DateTime.UtcNow,
                    LikeCount = 0
                });
            }
            await context.SaveChangesAsync();

            var logger = NullLogger<GamificationService>.Instance;
            var notificationService = new FakeNotificationService();
            var service = new GamificationService(context, logger, notificationService);

            // Act
            await service.CheckAndGrantInteractionBadgesAsync(1L);

            // Assert
            var badgeCustomer = await context.BadgeCustomers
                .FirstOrDefaultAsync(bc => bc.CustomerID == 1L && bc.BadgeID == 1);
            
            Assert.NotNull(badgeCustomer);
        }

        [Fact]
        public async Task CheckAndGrantShoppingBadges_ShouldGrantLevel1AndBookworm_WhenConditionsMet()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBadgesAsync(context);

            var customer = new CustomerDetail
            {
                CustomerID = 1L,
                IsOnboardingCompleted = true
            };
            await context.CustomerDetails.AddAsync(customer);

            // Seed 5 different Categories
            for (long i = 1; i <= 5; i++)
            {
                context.Categories.Add(new Category { CategoryID = i, CategoryName = $"Cat {i}", Description = $"Cat {i}" });
                context.RealBooks.Add(new RealBook
                {
                    BookID = i,
                    CategoryID = i,
                    Title = $"Book {i}",
                    Publisher = "Pub",
                    ISBN = $"ISBN{i}",
                    Description = "Desc",
                    SampleFilePath = ""
                });
            }

            // Seed 3 completed orders with BlindBooks
            for (int o = 1; o <= 3; o++)
            {
                var order = new Order
                {
                    OrderID = o,
                    CustomerID = 1L,
                    OrderStatus = OrderStatus.Completed,
                    ShipReceiverName = "User",
                    ShipPhoneNumber = "123",
                    ShipDetailAddress = "Add"
                };

                // Add 1 blind book detail (maps Category o)
                order.OrderDetails.Add(new OrderDetail
                {
                    OrderID = o,
                    BookID = o, // RealBookID
                    BlindBookID = 100L + o, // BlindBookID
                    UnitPrice = 10000,
                    Quantity = 1
                });

                context.Orders.Add(order);
            }

            // Seed 2 more completed orders for regular books to cover 5 unique categories
            // Categories 4 and 5
            for (int o = 4; o <= 5; o++)
            {
                var order = new Order
                {
                    OrderID = o,
                    CustomerID = 1L,
                    OrderStatus = OrderStatus.Completed,
                    ShipReceiverName = "User",
                    ShipPhoneNumber = "123",
                    ShipDetailAddress = "Add"
                };

                order.OrderDetails.Add(new OrderDetail
                {
                    OrderID = o,
                    BookID = o, // CategoryID = o
                    UnitPrice = 10000,
                    Quantity = 1
                });

                context.Orders.Add(order);
            }

            await context.SaveChangesAsync();

            var logger = NullLogger<GamificationService>.Instance;
            var notificationService = new FakeNotificationService();
            var service = new GamificationService(context, logger, notificationService);

            // Act
            await service.CheckAndGrantShoppingBadgesAsync(1L);

            // Assert
            // 1. Trùm Blind Date Cấp 1 (BadgeID = 5) should be earned
            var blindDateBadge = await context.BadgeCustomers
                .FirstOrDefaultAsync(bc => bc.CustomerID == 1L && bc.BadgeID == 5);
            Assert.NotNull(blindDateBadge);

            // 2. Mọt sách chính hiệu (BadgeID = 8) should be earned because categoryCount = 5
            var bookwormBadge = await context.BadgeCustomers
                .FirstOrDefaultAsync(bc => bc.CustomerID == 1L && bc.BadgeID == 8);
            Assert.NotNull(bookwormBadge);
        }

        [Fact]
        public async Task CheckAndGrantReputationBadges_ShouldGrantExemplaryUser_WhenMaxReputationMaintainedFor90Days()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBadgesAsync(context);

            var customer = new CustomerDetail
            {
                CustomerID = 1L,
                IsOnboardingCompleted = true
            };
            await context.CustomerDetails.AddAsync(customer);

            var reputation = new CustomerReputation
            {
                CustomerID = 1L,
                ReputationPoint = 150,
                ReputationMaxStreakStartDate = DateTime.UtcNow.AddDays(-91) // More than 90 days
            };
            await context.Set<CustomerReputation>().AddAsync(reputation);
            await context.SaveChangesAsync();

            var logger = NullLogger<GamificationService>.Instance;
            var notificationService = new FakeNotificationService();
            var service = new GamificationService(context, logger, notificationService);

            // Act
            await service.CheckAndGrantReputationBadgesAsync(1L);

            // Assert
            var exemplaryBadge = await context.BadgeCustomers
                .FirstOrDefaultAsync(bc => bc.CustomerID == 1L && bc.BadgeID == 9);
            Assert.NotNull(exemplaryBadge);
        }
    }
}
