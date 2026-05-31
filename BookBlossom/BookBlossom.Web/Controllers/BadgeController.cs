using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Core.DTOs;

namespace BookBlossom.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BadgeController : ControllerBase
    {
        private readonly IGamificationService _gamificationService;

        public BadgeController(IGamificationService gamificationService)
        {
            _gamificationService = gamificationService;
        }

        // 1. Lấy danh sách huy hiệu của bản thân (CustomerOnly)
        [HttpGet("my-collection")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> GetMyCollection()
        {
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !long.TryParse(customerIdStr, out long customerId))
            {
                return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });
            }

            try
            {
                var result = await _gamificationService.GetCustomerBadgesAsync(customerId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi lấy bộ sưu tập huy hiệu.", detail = ex.Message });
            }
        }

        // 2. Lấy danh sách toàn bộ huy hiệu trong hệ thống (Tất cả Staff/Admin/User xem điều kiện)
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllBadges()
        {
            try
            {
                var result = await _gamificationService.GetAllBadgesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi lấy danh sách huy hiệu hệ thống.", detail = ex.Message });
            }
        }

        // 3. Callback ghi nhận chia sẻ link sách hoặc bài viết (CustomerOnly)
        [HttpPost("share")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> RecordShare([FromBody] ShareRequestDTO dto)
        {
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !long.TryParse(customerIdStr, out long customerId))
            {
                return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });
            }

            try
            {
                await _gamificationService.RecordShareActionAsync(customerId);
                return Ok(new { message = "Ghi nhận chia sẻ thành công và đã cập nhật huy hiệu (nếu đủ điều kiện)." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi trong quá trình ghi nhận chia sẻ.", detail = ex.Message });
            }
        }
    }
}
