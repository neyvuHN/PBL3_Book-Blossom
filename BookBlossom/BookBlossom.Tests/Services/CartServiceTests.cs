using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BookBlossom.Core.DTOs.Cart;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using BookBlossom.Infrastructure.Data;
using BookBlossom.Infrastructure.Services;
using Xunit;

namespace BookBlossom.Tests.Services
{
    public class CartServiceTests
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
            // Seed a Category
            var category = new Category
            {
                CategoryID = 1,
                CategoryName = "Literature",
                Description = "Literature books",
                Status = CategoryStatus.Active
            };
            await context.Categories.AddAsync(category);

            // Seed two RealBooks since BlindBook has a 1-to-1 relationship with RealBook
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

            var book2 = new RealBook
            {
                BookID = 12,
                CategoryID = 1,
                Title = "Real Book Test 2",
                Publisher = "Publisher B",
                ISBN = "2222222222",
                PublishYear = 2026,
                Description = "Description 2",
                Price = 60000,
                SampleFilePath = "",
                Weight = 0.5m,
                UnitsInStock = 5,
                IsContinued = true
            };
            await context.RealBooks.AddAsync(book2);

            // Seed a BlindBook
            var blindBook = new BlindBook
            {
                BlindBookID = 22,
                RealBookID = 11,
                Keywords = "mystery, horror",
                Quotes = "Some quote",

                Hashtags = "#horror",
                Price = 75000,
                StockQuantity = 3,
                RequestQuantity = 5,
                BlindBookRequestStatus = BlindBookRequestStatus.Approved,
                IsLocked = false
            };
            await context.BlindBooks.AddAsync(blindBook);

            // Seed an out of stock BlindBook (linked to book2)
            var outOfStockBlindBook = new BlindBook
            {
                BlindBookID = 33,
                RealBookID = 12,
                Keywords = "mystery",
                Quotes = "Quote",

                Hashtags = "#mystery",
                Price = 80000,
                StockQuantity = 0,
                RequestQuantity = 2,
                BlindBookRequestStatus = BlindBookRequestStatus.Approved,
                IsLocked = false
            };
            await context.BlindBooks.AddAsync(outOfStockBlindBook);

            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task AddToCart_RealBook_ShouldSucceed_WhenStockIsSufficient()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBaseDataAsync(context);
            var service = new CartService(context);

            var request = new AddCartRequestDTO
            {
                BookID = 11,
                Quantity = 2
            };

            // Act
            var result = await service.AddToCartAsync(101L, null, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(11, result.BookID);
            Assert.Null(result.BlindBookID);
            Assert.Equal(2, result.Quantity);
            Assert.Equal("Real Book Test", result.Title);
            Assert.Equal(50000, result.Price);

            var dbCart = await context.Carts.FirstOrDefaultAsync();
            Assert.NotNull(dbCart);
            Assert.Equal(101L, dbCart.UserID);
            Assert.Null(dbCart.GuestID);
            Assert.Equal(11, dbCart.BookID);
            Assert.Equal(2, dbCart.Quantity);
        }

        [Fact]
        public async Task AddToCart_RealBook_ShouldThrowException_WhenStockIsInsufficient()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBaseDataAsync(context);
            var service = new CartService(context);

            var request = new AddCartRequestDTO
            {
                BookID = 11,
                Quantity = 10 // Only 5 in stock
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddToCartAsync(101L, null, request));
            Assert.Equal("Sách không đủ số lượng trong kho.", ex.Message);
        }

        [Fact]
        public async Task AddToCart_BlindBook_ShouldSucceed_WhenStockIsSufficient()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBaseDataAsync(context);
            var service = new CartService(context);

            var request = new AddCartRequestDTO
            {
                BlindBookID = 22,
                Quantity = 2
            };

            // Act
            var result = await service.AddToCartAsync(null, Guid.NewGuid(), request);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.BookID);
            Assert.Equal(22, result.BlindBookID);
            Assert.Equal(2, result.Quantity);
            Assert.Equal("Blind Book (Literature) - #horror", result.Title);
            Assert.Equal(75000, result.Price);
        }

        [Fact]
        public async Task AddToCart_BlindBook_ShouldThrowException_WhenOutOfStock()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBaseDataAsync(context);
            var service = new CartService(context);

            var request = new AddCartRequestDTO
            {
                BlindBookID = 33,
                Quantity = 1 // 0 in stock
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddToCartAsync(101L, null, request));
            Assert.Equal("Sách ẩn danh đã hết hàng.", ex.Message);
        }

        [Fact]
        public async Task AddToCart_CumulativeRealBookStockCheck_ShouldThrowException()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBaseDataAsync(context);
            var service = new CartService(context);

            var request1 = new AddCartRequestDTO { BookID = 11, Quantity = 3 };
            var request2 = new AddCartRequestDTO { BookID = 11, Quantity = 3 }; // Total 6, stock is 5

            await service.AddToCartAsync(101L, null, request1);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddToCartAsync(101L, null, request2));
            Assert.Equal("Sách không đủ số lượng trong kho khi cộng dồn.", ex.Message);
        }

        [Fact]
        public async Task AddToCart_CumulativeBlindBookStockCheck_ShouldThrowException()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBaseDataAsync(context);
            var service = new CartService(context);

            var request1 = new AddCartRequestDTO { BlindBookID = 22, Quantity = 2 };
            var request2 = new AddCartRequestDTO { BlindBookID = 22, Quantity = 2 }; // Total 4, stock is 3

            await service.AddToCartAsync(101L, null, request1);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddToCartAsync(101L, null, request2));
            Assert.Equal("Sách ẩn danh không đủ số lượng trong kho khi cộng dồn.", ex.Message);
        }

        [Fact]
        public async Task UpdateCartItemQuantity_RealBook_ShouldSucceed_AndVerifyStock()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBaseDataAsync(context);
            var service = new CartService(context);

            var request = new AddCartRequestDTO { BookID = 11, Quantity = 2 };
            var added = await service.AddToCartAsync(101L, null, request);

            // Act
            var result = await service.UpdateCartItemQuantityAsync(added.CartID, 101L, null, 4);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Quantity);

            // Update to 6 (exceeds stock of 5) should fail
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateCartItemQuantityAsync(added.CartID, 101L, null, 6));
            Assert.Equal("Sách không đủ số lượng trong kho.", ex.Message);
        }

        [Fact]
        public async Task UpdateCartItemQuantity_BlindBook_ShouldSucceed_AndVerifyStock()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBaseDataAsync(context);
            var service = new CartService(context);

            var request = new AddCartRequestDTO { BlindBookID = 22, Quantity = 1 };
            var added = await service.AddToCartAsync(101L, null, request);

            // Act
            var result = await service.UpdateCartItemQuantityAsync(added.CartID, 101L, null, 3);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Quantity);

            // Update to 4 (exceeds stock of 3) should fail
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateCartItemQuantityAsync(added.CartID, 101L, null, 4));
            Assert.Equal("Sách ẩn danh không đủ số lượng trong kho.", ex.Message);
        }

        [Fact]
        public async Task GetCartItems_ShouldReturnCorrectMappedList()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBaseDataAsync(context);
            var service = new CartService(context);

            await service.AddToCartAsync(101L, null, new AddCartRequestDTO { BookID = 11, Quantity = 1 });
            await service.AddToCartAsync(101L, null, new AddCartRequestDTO { BlindBookID = 22, Quantity = 1 });

            // Act
            var items = (await service.GetCartItemsAsync(101L, null)).ToList();

            // Assert
            Assert.Equal(2, items.Count);

            var realBookItem = items.First(i => i.BookID == 11);
            Assert.Equal("Real Book Test", realBookItem.Title);
            Assert.Equal(50000, realBookItem.Price);

            var blindBookItem = items.First(i => i.BlindBookID == 22);
            Assert.Equal("Blind Book (Literature) - #horror", blindBookItem.Title);
            Assert.Equal(75000, blindBookItem.Price);
        }
    }
}
