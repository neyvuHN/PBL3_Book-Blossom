using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookBlossom.Core.Interfaces.Services;
using System.Security.Claims;

namespace BookBlossom.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServicePackageController : ControllerBase
    {
        private readonly IServicePackageService _packageService;

        public ServicePackageController(IServicePackageService packageService)
        {
            _packageService = packageService;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllPackages()
        {
            var packages = await _packageService.GetAllPackagesAsync();
            return Ok(packages);
        }

        [Authorize]
        [HttpGet("my-service")]
        public async Task<IActionResult> GetMyService()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdStr, out long userId)) return Unauthorized();

            var service = await _packageService.GetUserCurrentServiceAsync(userId);
            if (service == null) return NotFound("Bạn chưa đăng ký gói dịch vụ nào.");

            return Ok(new
            {
                PackageName = service.ServicePackage.PackageName,
                StartDate = service.StartDate,
                EndDate = service.EndDate,
                ThreadLimit = service.ServicePackage.ThreadLimit,
                UndoLimit = service.ServicePackage.UndoLimit
            });
        }

        [Authorize]
        [HttpPost("subscribe/{packageId}")]
        public async Task<IActionResult> Subscribe(long packageId, [FromQuery] byte paymentMethod = 1)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdStr, out long userId)) return Unauthorized();

            var result = await _packageService.SubscribeToPackageAsync(userId, packageId, paymentMethod);
            if (result) return Ok("Đăng ký gói thành công!");
            
            return BadRequest("Đăng ký gói thất bại.");
        }

        // Endpoint dành cho Admin/System để trigger background job thủ công (phục vụ test)
        [HttpPost("trigger-expiry-check")]
        public async Task<IActionResult> TriggerExpiryCheck()
        {
            await _packageService.CheckAndDowngradeExpiredSubscriptionsAsync();
            return Ok("Đã thực hiện kiểm tra và hạ cấp các gói hết hạn.");
        }
    }
}
