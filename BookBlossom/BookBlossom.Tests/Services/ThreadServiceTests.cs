using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using BookBlossom.Core.DTOs.Thread;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using BookBlossom.Infrastructure.Data;
using BookBlossom.Infrastructure.Services;
using Xunit;

namespace BookBlossom.Tests.Services
{
    public class ThreadServiceTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private Mock<ILogger<ThreadService>> GetMockLogger()
        {
            return new Mock<ILogger<ThreadService>>();
        }

        private async Task SeedBaseDataAsync(ApplicationDbContext context)
        {
            // Seed roles
            var customerRole = new Role { RoleID = UserRole.Customer, RoleName = "Customer", Description = "Customer" };
            await context.Roles.AddAsync(customerRole);

            // Seed user
            var user = new User
            {
                UserID = 101L,
                UserName = "test_customer",
                Email = "customer@test.com",
                PhoneNumber = "0987654321",
                FirstName = "John",
                LastName = "Doe",
                RoleID = UserRole.Customer,
                AccountStatus = AccountStatus.Active,
                IsActive = true
            };
            await context.Users.AddAsync(user);

            // Seed CustomerDetail
            var customerDetail = new CustomerDetail
            {
                CustomerID = 101L,
                IsOnboardingCompleted = true,
                CurrentMonthThreadCount = 0,
                LastThreadResetDate = DateTime.UtcNow
            };
            await context.CustomerDetails.AddAsync(customerDetail);

            // Seed Free Service Package
            var freePackage = new ServicePackage
            {
                PackageID = 1L,
                PackageName = "Free",
                Price = 0,
                DurationDay = 30,
                ThreadLimit = 3,
                Description = "Free Plan"
            };
            await context.ServicePackages.AddAsync(freePackage);

            // Seed CustomerService (Link package to customer)
            var customerService = new CustomerService
            {
                CustomerID = 101L,
                CurrentPackageID = 1L,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30)
            };
            await context.CustomerServices.AddAsync(customerService);

            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task CreatePostAsync_WithPhoneNumberInTitle_ShouldThrowArgumentException()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBaseDataAsync(context);
            var mockLogger = GetMockLogger();
            var service = new ThreadService(context, mockLogger.Object);

            var dto = new CreateThreadPostDTO
            {
                Title = "Liên hệ 0912345678 để mua hàng",
                Content = "Nội dung bình thường",
                Hashtags = "#sach"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreatePostAsync(101L, dto, null));
            Assert.Contains("tiêu đề chứa số điện thoại", ex.Message);
        }

        [Fact]
        public async Task CreatePostAsync_WithPhoneNumberInContent_ShouldThrowArgumentException()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBaseDataAsync(context);
            var mockLogger = GetMockLogger();
            var service = new ThreadService(context, mockLogger.Object);

            var dto = new CreateThreadPostDTO
            {
                Title = "Tiêu đề bình thường",
                Content = "Vui lòng gọi điện thoại số +84912345678 nhé.",
                Hashtags = "#sach"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreatePostAsync(101L, dto, null));
            Assert.Contains("nội dung chứa số điện thoại", ex.Message);
        }

        [Fact]
        public async Task CreatePostAsync_UnderLimit_ShouldSucceed()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBaseDataAsync(context);
            var mockLogger = GetMockLogger();
            var service = new ThreadService(context, mockLogger.Object);

            var dto = new CreateThreadPostDTO
            {
                Title = "Bài viết chia sẻ sách mới",
                Content = "Chào mọi người, đây là cuốn sách mình mới mua rất hay.",
                Hashtags = "#review #books"
            };

            // Act
            var result = await service.CreatePostAsync(101L, dto, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Bài viết chia sẻ sách mới", result.Title);
            Assert.Equal(101L, result.CustomerID);

            var customerDetail = await context.CustomerDetails.FirstAsync(c => c.CustomerID == 101L);
            Assert.Equal(1, customerDetail.CurrentMonthThreadCount);
        }

        [Fact]
        public async Task CreatePostAsync_ExceedingLimit_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBaseDataAsync(context);
            var mockLogger = GetMockLogger();
            var service = new ThreadService(context, mockLogger.Object);

            // Set CurrentMonthThreadCount to 3 (equal to limit of Free package)
            var customerDetail = await context.CustomerDetails.FirstAsync(c => c.CustomerID == 101L);
            customerDetail.CurrentMonthThreadCount = 3;
            await context.SaveChangesAsync();

            var dto = new CreateThreadPostDTO
            {
                Title = "Bài đăng thứ tư trong tháng",
                Content = "Nội dung bài viết thứ tư",
                Hashtags = "#limit"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreatePostAsync(101L, dto, null));
            Assert.Contains("vượt quá giới hạn đăng bài", ex.Message);
        }

        [Fact]
        public async Task ReportPostAsync_ShouldIncrementReportCount_AndAutoHide_WhenReportsReach5()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBaseDataAsync(context);
            var mockLogger = GetMockLogger();
            var service = new ThreadService(context, mockLogger.Object);

            var dto = new CreateThreadPostDTO
            {
                Title = "Bài viết nhạy cảm cần báo cáo",
                Content = "Nội dung bài viết",
                Hashtags = "#report"
            };
            var postDto = await service.CreatePostAsync(101L, dto, null);

            // Report the post 4 times
            for (int i = 1; i <= 4; i++)
            {
                var newReportCount = await service.ReportPostAsync(postDto.PostID);
                Assert.Equal(i, newReportCount);

                var tempPost = await context.ThreadPosts.FindAsync(postDto.PostID);
                Assert.NotNull(tempPost);
                Assert.False(tempPost.IsHidden);
            }

            // The 5th report should trigger auto-hiding
            var finalReportCount = await service.ReportPostAsync(postDto.PostID);
            Assert.Equal(5, finalReportCount);

            // Verify in DB
            var dbPost = await context.ThreadPosts.FindAsync(postDto.PostID);
            Assert.NotNull(dbPost);
            Assert.True(dbPost.IsHidden);
        }

        [Fact]
        public async Task AddCommentAsync_ShouldSucceed_AndAddComment()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBaseDataAsync(context);
            var mockLogger = GetMockLogger();
            var service = new ThreadService(context, mockLogger.Object);

            var postDto = await service.CreatePostAsync(101L, new CreateThreadPostDTO
            {
                Title = "Bài viết gốc",
                Content = "Nội dung bài viết gốc",
                Hashtags = "#test"
            }, null);

            var commentDto = new CreateThreadCommentDTO
            {
                Content = "Bình luận số 1"
            };

            // Act
            var result = await service.AddCommentAsync(101L, postDto.PostID, commentDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Bình luận số 1", result.Content);
            Assert.Equal(postDto.PostID, result.PostID);
            Assert.Equal(101L, result.CustomerID);

            var comments = await context.ThreadComments.Where(c => c.PostID == postDto.PostID).ToListAsync();
            Assert.Single(comments);
            Assert.Equal("Bình luận số 1", comments[0].Content);
        }
    }
}
