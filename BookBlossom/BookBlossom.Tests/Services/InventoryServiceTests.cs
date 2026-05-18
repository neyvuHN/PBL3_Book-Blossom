using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BookBlossom.Core.DTOs.Importing;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using BookBlossom.Infrastructure.Data;
using BookBlossom.Infrastructure.Services;
using Xunit;

namespace BookBlossom.Tests.Services
{
    public class InventoryServiceTests
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
            // Seed a StoreManager Staff User
            var role = new Role { RoleID = UserRole.StoreManager, RoleName = "StoreManager" };
            await context.Roles.AddAsync(role);

            var user = new User
            {
                UserID = 101,
                RoleID = UserRole.StoreManager,
                UserName = "storemanager1",
                Password = "hashedpassword",
                PhoneNumber = "0123456789",
                Email = "manager@bookblossom.com",
                AccountStatus = AccountStatus.Active
            };
            await context.Users.AddAsync(user);

            var staff = new StaffDetail
            {
                StaffID = 101,
                Address = "123 Main St",
                IsOnboardingCompleted = true,
                Department = Department.StoreManager,
                Position = StaffPosition.Leader,
                HireDate = DateTime.Today,
                ContractType = ContractType.FullTime,
                Salary = 20000000
            };
            await context.StaffDetails.AddAsync(staff);

            // Seed 3 books
            var category = new Category
            {
                CategoryID = 1,
                CategoryName = "Literature",
                Description = "Literature books",
                Status = CategoryStatus.Active
            };
            await context.Categories.AddAsync(category);

            var book1 = new RealBook
            {
                BookID = 11,
                CategoryID = 1,
                Title = "Book One",
                Publisher = "Publisher A",
                ISBN = "1111111111",
                PublishYear = DateTime.Today,
                Description = "Book 1 Desc",
                Price = 50000,
                SampleFilePath = "",
                Weight = 0.5m,
                UnitsInStock = 10, // Base stock
                IsContinued = true
            };

            var book2 = new RealBook
            {
                BookID = 12,
                CategoryID = 1,
                Title = "Book Two",
                Publisher = "Publisher B",
                ISBN = "2222222222",
                PublishYear = DateTime.Today,
                Description = "Book 2 Desc",
                Price = 60000,
                SampleFilePath = "",
                Weight = 0.6m,
                UnitsInStock = 20, // Base stock
                IsContinued = true
            };

            var book3 = new RealBook
            {
                BookID = 13,
                CategoryID = 1,
                Title = "Book Three",
                Publisher = "Publisher C",
                ISBN = "3333333333",
                PublishYear = DateTime.Today,
                Description = "Book 3 Desc",
                Price = 70000,
                SampleFilePath = "",
                Weight = 0.7m,
                UnitsInStock = 30, // Base stock
                IsContinued = true
            };

            await context.RealBooks.AddRangeAsync(book1, book2, book3);
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task ImportVoucher_With3Books_ShouldCalculateTotalCostAndIncreaseUnitsInStock()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBaseDataAsync(context);
            var service = new InventoryService(context);

            var request = new CreateImportingRequestDTO
            {
                SupplierName = "Nha Sach Phuong Nam",
                RequiredDate = DateTime.Today.AddDays(5),
                ShipAddress = "99 Nguyen Hue, District 1",
                Details = new List<CreateImportingDetailRequestDTO>
                {
                    new CreateImportingDetailRequestDTO { BookID = 11, UnitPrice = 40000, Quantity = 5 },   // LineTotal: 200,000
                    new CreateImportingDetailRequestDTO { BookID = 12, UnitPrice = 50000, Quantity = 10 },  // LineTotal: 500,000
                    new CreateImportingDetailRequestDTO { BookID = 13, UnitPrice = 55000, Quantity = 20 }   // LineTotal: 1,100,000
                }
            };

            // Expected calculations:
            // TotalCost = (40000 * 5) + (50000 * 10) + (55000 * 20) = 200,000 + 500,000 + 1,100,000 = 1,800,000
            // Book 11 UnitsInStock = 10 + 5 = 15
            // Book 12 UnitsInStock = 20 + 10 = 30
            // Book 13 UnitsInStock = 30 + 20 = 50

            // Act
            var result = await service.CreateImportingAsync(101, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("storemanager1", result.StaffName);
            Assert.Equal("Nha Sach Phuong Nam", result.SupplierName);
            Assert.Equal(1800000, result.TotalCost);
            Assert.Equal(3, result.Details.Count);

            // Verify stocks
            var dbBook11 = await context.RealBooks.FindAsync(11L);
            var dbBook12 = await context.RealBooks.FindAsync(12L);
            var dbBook13 = await context.RealBooks.FindAsync(13L);

            Assert.Equal(15, dbBook11!.UnitsInStock);
            Assert.Equal(30, dbBook12!.UnitsInStock);
            Assert.Equal(50, dbBook13!.UnitsInStock);
        }

        [Fact]
        public async Task DeleteImportingDetail_ShouldRecalculateTotalCostAndRevertUnitsInStock()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBaseDataAsync(context);
            var service = new InventoryService(context);

            var request = new CreateImportingRequestDTO
            {
                SupplierName = "Fahasa",
                Details = new List<CreateImportingDetailRequestDTO>
                {
                    new CreateImportingDetailRequestDTO { BookID = 11, UnitPrice = 40000, Quantity = 5 },   // LineTotal: 200,000
                    new CreateImportingDetailRequestDTO { BookID = 12, UnitPrice = 50000, Quantity = 10 }   // LineTotal: 500,000
                }
            };

            var importDto = await service.CreateImportingAsync(101, request);
            Assert.Equal(700000, importDto.TotalCost);

            // Verify book 12 stock has increased
            var book12Before = await context.RealBooks.FindAsync(12L);
            Assert.Equal(30, book12Before!.UnitsInStock); // 20 base + 10 imported

            // Act - Delete detail of Book 12
            var deleteResult = await service.DeleteImportingDetailAsync(importDto.ImportingID, 12L);

            // Assert
            Assert.True(deleteResult);

            // Verify Book 12 stock reverted to 20
            var book12After = await context.RealBooks.FindAsync(12L);
            Assert.Equal(20, book12After!.UnitsInStock);

            // Verify Importing TotalCost updated to 200,000 (only Book 11 left)
            var updatedImport = await service.GetImportingByIdAsync(importDto.ImportingID);
            Assert.NotNull(updatedImport);
            Assert.Equal(200000, updatedImport.TotalCost);
            Assert.Single(updatedImport.Details);
            Assert.Equal(11L, updatedImport.Details.First().BookID);
        }
    }
}
