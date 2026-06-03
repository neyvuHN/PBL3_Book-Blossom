using Xunit;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BookBlossom.Infrastructure.Data;
using BookBlossom.Core.Enums;

namespace BookBlossom.Tests
{
    public class UnitTest1
    {
        [Fact]
        public async Task DeleteUser4Test()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer("Server=sql1004.site4now.net;Database=db_ac99f8_pbl3;User Id=db_ac99f8_pbl3_admin;Password=abc1234@;TrustServerCertificate=True;MultipleActiveResultSets=true;");

            using (var context = new ApplicationDbContext(optionsBuilder.Options))
            {
                context.Database.SetCommandTimeout(300);
                var userToDelete = await context.Users.FirstOrDefaultAsync(u => u.UserName == "user4");
                if (userToDelete != null)
                {
                    var userId = userToDelete.UserID;
                    Console.WriteLine($"Found user4 with ID {userId}. Starting deletion...");
                    
                    var sqls = new[]
                    {
                        // 1. Thread interactions
                        "DELETE FROM [Thread].[ThreadLike] WHERE [UserID] = " + userId,
                        "DELETE FROM [Thread].[ThreadSaved] WHERE [UserID] = " + userId,
                        "DELETE FROM [Thread].[ThreadComment] WHERE [CustomerID] = " + userId,
                        "DELETE FROM [Thread].[ThreadComment] WHERE [PostID] IN (SELECT [PostID] FROM [Thread].[ThreadPost] WHERE [CustomerID] = " + userId + ")",
                        "DELETE FROM [Thread].[Report] WHERE [CustomerID] = " + userId,
                        "DELETE FROM [Thread].[Report] WHERE [PostID] IN (SELECT [PostID] FROM [Thread].[ThreadPost] WHERE [CustomerID] = " + userId + ")",
                        "DELETE FROM [Thread].[ThreadImage] WHERE [PostID] IN (SELECT [PostID] FROM [Thread].[ThreadPost] WHERE [CustomerID] = " + userId + ")",
                        "DELETE FROM [Thread].[ThreadShare] WHERE [UserID] = " + userId,
                        "DELETE FROM [Thread].[ThreadPost] WHERE [CustomerID] = " + userId,

                        // 2. Rank, Badge, Reputation
                        "DELETE FROM [Rank].[ReputationHistory] WHERE [CustomerID] = " + userId,
                        "DELETE FROM [Rank].[CustomerBadge] WHERE [CustomerID] = " + userId,
                        "DELETE FROM [Rank].[CustomerReputation] WHERE [CustomerID] = " + userId,
                        "DELETE FROM [UserSystem].[BadgeCustomer] WHERE [CustomerID] = " + userId,
                        "DELETE FROM [Voucher].[CustomerVoucher] WHERE [CustomerID] = " + userId,

                        // 3. User related other logs and configurations
                        "DELETE FROM [Preference].[CustomerPreference] WHERE [CustomerID] = " + userId,
                        "DELETE FROM [Service].[CustomerService] WHERE [CustomerID] = " + userId,
                        "DELETE FROM [dbo].[Wishlist] WHERE [UserID] = " + userId,
                        "DELETE FROM [dbo].[Cart] WHERE [UserID] = " + userId,
                        "DELETE FROM [Notification].[UserFollow] WHERE [UserID] = " + userId,
                        "DELETE FROM [Notification].[Subscription] WHERE [CustomerID] = " + userId,
                        "DELETE FROM [Tindbook].[SwipeLog] WHERE [UserID] = " + userId,
                        "DELETE FROM [dbo].[SwipeLogs] WHERE [CustomerID] = " + userId,
                        "DELETE FROM [UserSystem].[OTPLogs] WHERE [UserID] = " + userId,

                        // 4. Review related
                        "DELETE FROM [Review].[ReviewLike] WHERE [CustomerID] = " + userId,
                        "DELETE FROM [Review].[ReviewLike] WHERE [ReviewID] IN (SELECT [ReviewID] FROM [Review].[Review] WHERE [CustomerID] = " + userId + ")",
                        "DELETE FROM [Review].[ReviewReport] WHERE [ReporterID] = " + userId,
                        "DELETE FROM [Review].[ReviewReport] WHERE [ReviewID] IN (SELECT [ReviewID] FROM [Review].[Review] WHERE [CustomerID] = " + userId + ")",
                        "DELETE FROM [Review].[ReviewMedia] WHERE [ReviewID] IN (SELECT [ReviewID] FROM [Review].[Review] WHERE [CustomerID] = " + userId + ")",
                        "DELETE FROM [Review].[Review] WHERE [CustomerID] = " + userId,

                        // 5. Orders related
                        "DELETE FROM [OrderRequest].[ReturnRequest] WHERE [OrderID] IN (SELECT [OrderID] FROM [OrderRequest].[Orders] WHERE [CustomerID] = " + userId + ")",
                        "DELETE FROM [OrderRequest].[OrderDetail] WHERE [OrderID] IN (SELECT [OrderID] FROM [OrderRequest].[Orders] WHERE [CustomerID] = " + userId + ")",
                        "DELETE FROM [OrderRequest].[Orders] WHERE [CustomerID] = " + userId,

                        // 6. Address, Details, Logs, and User base record
                        "DELETE FROM [UserSystem].[RecommendSys] WHERE [UserID] = " + userId,
                        "DELETE FROM [UserSystem].[DeliveryAddress] WHERE [CustomerID] = " + userId,
                        "DELETE FROM [UserSystem].[CustomerDetail] WHERE [CustomerID] = " + userId,
                        "DELETE FROM [UserSystem].[StaffDetail] WHERE [StaffID] = " + userId,
                        "DELETE FROM [UserSystem].[AuditLogs] WHERE [SystemAdminID] = " + userId + " OR [UserID] = " + userId,
                        "DELETE FROM [UserSystem].[User] WHERE [UserID] = " + userId
                    };

                    foreach (var sql in sqls)
                    {
                        try
                        {
                            await context.Database.ExecuteSqlRawAsync(sql);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"SQL Error on command: {sql}. Error: {ex.Message}");
                        }
                    }
                    Console.WriteLine("User 'user4' cleanup complete!");
                }
                else
                {
                    Console.WriteLine("User 'user4' not found in database.");
                }
            }
        }
    }
}
