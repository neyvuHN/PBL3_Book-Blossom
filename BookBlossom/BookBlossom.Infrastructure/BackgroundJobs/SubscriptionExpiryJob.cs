using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BookBlossom.Core.Interfaces.Services;

namespace BookBlossom.Infrastructure.BackgroundJobs
{
    public class SubscriptionExpiryJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SubscriptionExpiryJob> _logger;

        public SubscriptionExpiryJob(IServiceProvider serviceProvider, ILogger<SubscriptionExpiryJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Subscription Expiry Job is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var servicePackageService = scope.ServiceProvider.GetRequiredService<IServicePackageService>();
                        await servicePackageService.CheckAndDowngradeExpiredSubscriptionsAsync();
                        _logger.LogInformation("Checked and downgraded expired subscriptions at: {time}", DateTimeOffset.Now);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing subscription expiry check.");
                }

                // Chạy mỗi ngày một lần (24 giờ)
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }

            _logger.LogInformation("Subscription Expiry Job is stopping.");
        }
    }
}
