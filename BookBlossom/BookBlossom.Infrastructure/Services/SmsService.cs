using System;
using System.Threading.Tasks;
using BookBlossom.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace BookBlossom.Infrastructure.Services
{
    public class MockSmsService : ISMSService
    {
        private readonly ILogger<MockSmsService> _logger;

        public MockSmsService(ILogger<MockSmsService> logger)
        {
            _logger = logger;
        }

        public Task SendSmsAsync(string phoneNumber, string message)
        {
            // Mô phỏng việc gửi SMS bằng cách log ra console/debug
            _logger.LogInformation($"[MOCK SMS] Gửi tới {phoneNumber}: {message}");
            
            // Ở đây bạn có thể tích hợp Twilio, eSMS, v.v.
            return Task.CompletedTask;
        }
    }
}
