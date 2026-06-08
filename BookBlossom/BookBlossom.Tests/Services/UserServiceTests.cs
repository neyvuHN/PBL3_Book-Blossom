using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Moq;
using BookBlossom.Core.DTOs;
using BookBlossom.Core.Entities;
using BookBlossom.Infrastructure.Data;
using BookBlossom.Infrastructure.Services;
using Xunit;

namespace BookBlossom.Tests.Services
{
    public class UserServiceTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task UpdateProfileAsync_DeleteAvatar_ShouldResetAvatarToDefault()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            
            var user = new User
            {
                UserID = 1L,
                UserName = "testuser",
                FirstName = "Test",
                LastName = "User",
                Avatar = "/images/Avatar/custom-avatar.jpg",
                Password = BCrypt.Net.BCrypt.HashPassword("123456")
            };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(m => m.WebRootPath).Returns("wwwroot");

            var service = new UserService(context, mockEnv.Object);

            var dto = new UpdateProfileDTO
            {
                FirstName = "TestEdited",
                DeleteAvatar = true
            };

            // Act
            var result = await service.UpdateProfileAsync(1L, dto);

            // Assert
            Assert.True(result);
            var updatedUser = await context.Users.FindAsync(1L);
            Assert.NotNull(updatedUser);
            Assert.Equal("TestEdited", updatedUser.FirstName);
            Assert.Equal("/images/Avatar/avatar1.jpg", updatedUser.Avatar);
        }
    }
}
