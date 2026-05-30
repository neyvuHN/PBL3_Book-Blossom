using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BookBlossom.Core.DTOs.Wishlist;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using BookBlossom.Infrastructure.Data;
using BookBlossom.Infrastructure.Services;
using Xunit;

namespace BookBlossom.Tests.Services
{
    public class WishlistServiceTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private async Task SeedBaseDataAsync(ApplicationDbContext context)
        {
            // Seed a RealBook
            var category = new Category
            {
                CategoryID = 1,
                CategoryName = "Literature",
                Description = "Literature books",
                Status = CategoryStatus.Active
            };
            await context.Categories.AddAsync(category);

            var book = new RealBook
            {
                BookID = 11,
                CategoryID = 1,
                Title = "Real Book Test",
                Publisher = "Publisher A",
                ISBN = "1111111111",
                PublishYear = 2026,
                Description = "Description",
                Price = 50000,
                SampleFilePath = "",
                Weight = 0.5m,
                UnitsInStock = 5,
                IsContinued = true
            };
            await context.RealBooks.AddAsync(book);

            // Seed a BlindBook
            var blindBook = new BlindBook
            {
                BlindBookID = 22,
                RealBookID = 11,
                Keywords = "mystery, horror",
                Quotes = "Some quote",
                Category = "Horror",
                Hashtags = "#horror",
                Price = 75000,
                StockQuantity = 3,
                RequestQuantity = 5,
                BlindBookRequestStatus = BlindBookRequestStatus.Approved,
                IsLocked = false
            };
            await context.BlindBooks.AddAsync(blindBook);

            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task AddToWishlist_RealBook_ShouldSucceed()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBaseDataAsync(context);
            var service = new WishlistService(context);

            var request = new AddWishlistRequestDTO
            {
                BookID = 11
            };

            // Act
            var result = await service.AddToWishlistAsync(101L, null, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(11, result.BookID);
            Assert.Null(result.BlindBookID);
            Assert.Equal("Real Book Test", result.Title);
            Assert.Equal(50000, result.Price);

            var dbWishlist = await context.Wishlists.FirstOrDefaultAsync();
            Assert.NotNull(dbWishlist);
            Assert.Equal(101L, dbWishlist.UserID);
            Assert.Null(dbWishlist.GuestID);
            Assert.Equal(11, dbWishlist.BookID);
        }

        [Fact]
        public async Task AddToWishlist_BlindBook_ShouldSucceed()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBaseDataAsync(context);
            var service = new WishlistService(context);

            var request = new AddWishlistRequestDTO
            {
                BlindBookID = 22
            };

            // Act
            var result = await service.AddToWishlistAsync(null, Guid.NewGuid(), request);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.BookID);
            Assert.Equal(22, result.BlindBookID);
            Assert.Equal("Blind Book (Horror)", result.Title);
            Assert.Equal(75000, result.Price);
        }

        [Fact]
        public async Task AddToWishlist_DuplicateItem_ShouldThrowException()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBaseDataAsync(context);
            var service = new WishlistService(context);

            var request = new AddWishlistRequestDTO { BookID = 11 };
            await service.AddToWishlistAsync(101L, null, request);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddToWishlistAsync(101L, null, request));
            Assert.Equal("Sản phẩm đã có trong danh sách yêu thích.", ex.Message);
        }

        [Fact]
        public async Task AddToWishlist_NonExistentBook_ShouldThrowException()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBaseDataAsync(context);
            var service = new WishlistService(context);

            var request = new AddWishlistRequestDTO { BookID = 999 };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddToWishlistAsync(101L, null, request));
            Assert.Equal("Không tìm thấy sách.", ex.Message);
        }

        [Fact]
        public async Task AddToWishlist_NonExistentBlindBook_ShouldThrowException()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBaseDataAsync(context);
            var service = new WishlistService(context);

            var request = new AddWishlistRequestDTO { BlindBookID = 999 };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddToWishlistAsync(101L, null, request));
            Assert.Equal("Không tìm thấy sách ẩn danh.", ex.Message);
        }

        [Fact]
        public async Task RemoveFromWishlist_ShouldSucceed()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBaseDataAsync(context);
            var service = new WishlistService(context);

            var request = new AddWishlistRequestDTO { BookID = 11 };
            var added = await service.AddToWishlistAsync(101L, null, request);

            // Act
            var result = await service.RemoveFromWishlistAsync(added.WishlistID, 101L, null);

            // Assert
            Assert.True(result);
            var exists = await context.Wishlists.AnyAsync(w => w.WishlistID == added.WishlistID);
            Assert.False(exists);
        }

        [Fact]
        public async Task RemoveFromWishlist_Unauthorized_ShouldThrowException()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBaseDataAsync(context);
            var service = new WishlistService(context);

            var request = new AddWishlistRequestDTO { BookID = 11 };
            var added = await service.AddToWishlistAsync(101L, null, request);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RemoveFromWishlistAsync(added.WishlistID, 202L, null));
            Assert.Equal("Bạn không có quyền xóa danh sách yêu thích này.", ex.Message);
        }

        [Fact]
        public async Task GetWishlistItems_ShouldReturnCorrectMappedList()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBaseDataAsync(context);
            var service = new WishlistService(context);

            await service.AddToWishlistAsync(101L, null, new AddWishlistRequestDTO { BookID = 11 });
            await service.AddToWishlistAsync(101L, null, new AddWishlistRequestDTO { BlindBookID = 22 });

            // Act
            var items = (await service.GetWishlistItemsAsync(101L, null)).ToList();

            // Assert
            Assert.Equal(2, items.Count);

            var realBookItem = items.First(i => i.BookID == 11);
            Assert.Equal("Real Book Test", realBookItem.Title);
            Assert.Equal(50000, realBookItem.Price);

            var blindBookItem = items.First(i => i.BlindBookID == 22);
            Assert.Equal("Blind Book (Horror)", blindBookItem.Title);
            Assert.Equal(75000, blindBookItem.Price);
        }
    }
}
