using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Core.Entities;
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

        // ======================== PUBLIC ========================

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

        // ======================== STAFF ONLY - CRUD GÓI ========================

        [Authorize(Roles = "Staff")]
        [HttpPost("create")]
        public async Task<IActionResult> CreatePackage([FromBody] ServicePackage package)
        {
            if (package == null) return BadRequest("Dữ liệu không hợp lệ.");
            var result = await _packageService.CreatePackageAsync(package);
            if (result) return Ok("Tạo gói dịch vụ thành công.");
            return BadRequest("Tạo gói dịch vụ thất bại.");
        }

        [Authorize(Roles = "Staff")]
        [HttpPut("update/{packageId}")]
        public async Task<IActionResult> UpdatePackage(long packageId, [FromBody] ServicePackage package)
        {
            if (package == null) return BadRequest("Dữ liệu không hợp lệ.");
            package.PackageID = packageId;
            var result = await _packageService.UpdatePackageAsync(package);
            if (result) return Ok("Cập nhật gói dịch vụ thành công.");
            return NotFound("Không tìm thấy gói dịch vụ.");
        }

        [Authorize(Roles = "Staff")]
        [HttpDelete("delete/{packageId}")]
        public async Task<IActionResult> DeletePackage(long packageId)
        {
            var result = await _packageService.DeletePackageAsync(packageId);
            if (result) return Ok("Xóa gói dịch vụ thành công.");
            return NotFound("Không tìm thấy gói dịch vụ hoặc không thể xóa (gói đang được sử dụng).");
        }

        // ======================== SYSTEM / TEST ========================

        // Trigger thủ công cho test, production nên thêm [Authorize(Roles = "Staff")]
        [HttpPost("trigger-expiry-check")]
        public async Task<IActionResult> TriggerExpiryCheck()
        {
            await _packageService.CheckAndDowngradeExpiredSubscriptionsAsync();
            return Ok("Đã thực hiện kiểm tra và hạ cấp các gói hết hạn.");
        }
    }
}
