using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BookBlossom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookBlossom.Infrastructure.BackgroundJobs
{
    public class OtpCleanupJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OtpCleanupJob> _logger;

        public OtpCleanupJob(IServiceProvider serviceProvider, ILogger<OtpCleanupJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("OtpCleanupJob running at: {time}", DateTimeOffset.Now);

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        // Lấy các OTP đã hết hạn hoặc đã sử dụng
                        var expiredOrUsedOtps = await context.OTPLogs
                            .Where(o => o.ExpireAt < DateTime.UtcNow || o.IsUsed)
                            .ToListAsync(stoppingToken);

                        if (expiredOrUsedOtps.Any())
                        {
                            context.OTPLogs.RemoveRange(expiredOrUsedOtps);
                            await context.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation($"Đã xóa {expiredOrUsedOtps.Count} mã OTP hết hạn hoặc đã sử dụng.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Đã xảy ra lỗi khi chạy OtpCleanupJob.");
                }

                try
                {
                    // Chạy mỗi 24 giờ
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Clean shutdown during cancellation
                }
            }
        }
    }
}
