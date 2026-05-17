using System.Security.Claims;
using BookBlossom.Core.DTOs.Onboarding;
using BookBlossom.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookBlossom.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Customer")] // Bảo mật chặt chẽ: Chỉ tài khoản Khách hàng được dùng
    public class OnboardingController : ControllerBase
    {
        private readonly IOnboardingService _onboardingService;

        public OnboardingController(IOnboardingService onboardingService)
        {
            _onboardingService = onboardingService;
        }

        /// <summary>
        /// API 1: Lấy danh sách sở thích ban đầu
        /// GET: /api/onboarding/tags
        /// </summary>
        [HttpGet("tags")]
        [AllowAnonymous] // Cho phép lấy danh sách bong bóng công khai (kể cả chưa đăng nhập)
        public async Task<IActionResult> GetTags()
        {
            var tags = await _onboardingService.GetOnboardingTagsAsync();
            return Ok(tags);
        }

        /// <summary>
        /// API 2: Lưu danh sách sở thích do người dùng chọn (Màn hình bong bóng)
        /// POST: /api/onboarding/preferences
        /// </summary>
        [HttpPost("preferences")]
        public async Task<IActionResult> SavePreferences([FromBody] SavePreferencesRequestDTO request)
        {
            try
            {
                long userId = GetCurrentUserId();
                await _onboardingService.SaveCustomerPreferencesAsync(userId, request);
                return Ok(new { message = "Lưu danh sách sở thích thành công! Chuyển sang màn hình hướng dẫn." });
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception) { return StatusCode(500, "Lỗi hệ thống khi lưu sở thích."); }
        }

        /// <summary>
        /// API 3: Xác nhận hoàn thành phần Hướng dẫn và mở khóa vào trang chính
        /// POST: /api/onboarding/finalize
        /// </summary>
        [HttpPost("finalize")]
        public async Task<IActionResult> FinalizeOnboarding()
        {
            try
            {
                long userId = GetCurrentUserId();
                var success = await _onboardingService.CompleteOnboardingTourAsync(userId);
                
                if (success)
                    return Ok(new { message = "Chúc mừng! Bạn đã hoàn thành toàn bộ Onboarding. Chào mừng tới Book Blossom!", isOnboardingCompleted = true });
                
                return BadRequest("Không thể hoàn tất Onboarding.");
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (Exception) { return StatusCode(500, "Lỗi hệ thống khi hoàn tất Onboarding."); }
        }

        // Hàm Helper: Trích xuất UserId an toàn từ chuỗi JWT Token của người dùng đang đăng nhập
        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long userId))
            {
                throw new UnauthorizedAccessException("Phiên đăng nhập không hợp lệ hoặc đã hết hạn.");
            }
            return userId;
        }
    }
}