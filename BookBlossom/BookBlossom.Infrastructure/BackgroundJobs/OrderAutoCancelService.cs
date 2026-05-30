using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using BookBlossom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BookBlossom.Infrastructure.BackgroundJobs
{
    public class OrderAutoCancelService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrderAutoCancelService> _logger;

        public OrderAutoCancelService(IServiceProvider serviceProvider, ILogger<OrderAutoCancelService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Order Auto-Cancel Background Job is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        await AutoCancelOrdersAsync(context);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while executing order auto-cancel job.");
                }

                try
                {
                    // Chạy quét lại sau mỗi 10 phút
                    await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Clean shutdown during cancellation
                }
            }

            _logger.LogInformation("Order Auto-Cancel Background Job is stopping.");
        }

        private async Task AutoCancelOrdersAsync(ApplicationDbContext context)
        {
            var thresholdDate = DateTime.UtcNow.AddHours(-48);

            // Tìm các đơn hàng Pending được tạo cách đây hơn 48 giờ
            var expiredOrders = await context.Set<Order>()
                .Include(o => o.OrderDetails)
                .Where(o => o.OrderStatus == OrderStatus.Pending && o.OrderDate < thresholdDate)
                .ToListAsync();

            if (!expiredOrders.Any()) return;

            _logger.LogInformation("Found {count} expired pending orders to cancel.", expiredOrders.Count);

            foreach (var order in expiredOrders)
            {
                order.OrderStatus = OrderStatus.Cancelled;

                if (order.PaymentMethod != PaymentMethod.COD)
                {
                    order.Note = "Tự động hủy đơn hàng sau 48h chưa xác nhận. [Đã hoàn tiền trực tuyến]";
                }
                else
                {
                    order.Note = "Tự động hủy đơn hàng sau 48h do cửa hàng chưa xác nhận.";
                }

                // Hoàn kho
                foreach (var detail in order.OrderDetails)
                {
                    var realBook = await context.Set<RealBook>().FindAsync(detail.BookID);
                    if (realBook != null)
                    {
                        realBook.UnitsInStock += detail.Quantity;
                        realBook.ReservedQuantity -= detail.Quantity;
                        if (realBook.ReservedQuantity < 0) realBook.ReservedQuantity = 0;
                    }

                    if (detail.BlindBookID.HasValue)
                    {
                        var blindBook = await context.Set<BlindBook>().FindAsync(detail.BlindBookID.Value);
                        if (blindBook != null)
                        {
                            blindBook.StockQuantity += detail.Quantity;
                        }
                    }
                }

                _logger.LogInformation("Order #{id} has been auto-cancelled.", order.OrderID);
            }

            await context.SaveChangesAsync();
        }
    }
}
