using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BookBlossom.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BookBlossom.Infrastructure.Services
{
    public class EsmsSmsService : ISMSService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<EsmsSmsService> _logger;

        public EsmsSmsService(IConfiguration configuration, HttpClient httpClient, ILogger<EsmsSmsService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            var apiKey = _configuration["SMS:EsmsApiKey"];
            var secretKey = _configuration["SMS:EsmsSecretKey"];
            var brandname = _configuration["SMS:EsmsBrandname"] ?? "Qcao";
            var smsType = _configuration["SMS:EsmsSmsType"] ?? "2";

            var url = "https://rest.esms.vn/MainService.svc/json/SendMultipleMessage_V4_post_json/";

            var payload = new
            {
                ApiKey = apiKey,
                SecretKey = secretKey,
                Content = message,
                Phone = phoneNumber,
                Brandname = brandname,
                SmsType = smsType,
                IsUnicode = "0",
                Sandbox = "0"
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                _logger.LogInformation("Sending SMS via eSMS to {Phone}...", phoneNumber);
                var response = await _httpClient.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("eSMS Response: {Response}", responseString);
                }
                else
                {
                    _logger.LogError("eSMS failed with status code {StatusCode}. Response: {Response}", response.StatusCode, responseString);
                    throw new Exception($"Gửi SMS thất bại với mã lỗi HTTP: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi gửi tin nhắn qua eSMS.");
                throw new Exception("Không thể kết nối đến hệ thống gửi tin nhắn eSMS.", ex);
            }
        }
    }
}
