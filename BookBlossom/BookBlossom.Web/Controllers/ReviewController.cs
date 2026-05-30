using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using BookBlossom.Core.Enums;
using BookBlossom.Core.DTOs.Review;
using BookBlossom.Core.Interfaces.Services;

namespace BookBlossom.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        // 1. CREATE REVIEW
        [HttpPost]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> CreateReview([FromForm] CreateReviewDTO dto, [FromForm] List<IFormFile>? mediaFiles)
        {
            if (dto == null) return BadRequest(new { message = "Dữ liệu đánh giá trống." });

            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !long.TryParse(customerIdStr, out long customerId))
            {
                return Unauthorized(new { message = "Token không hợp lệ hoặc đã hết hạn." });
            }

            try
            {
                var result = await _reviewService.CreateReviewAsync(customerId, dto, mediaFiles);
                return StatusCode(201, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi tạo đánh giá.", detail = ex.Message });
            }
        }

        // 2. GET REVIEW DETAILS BY ID
        [HttpGet("{reviewId}")]
        public async Task<IActionResult> GetReviewById(long reviewId)
        {
            try
            {
                var review = await _reviewService.GetReviewByIdAsync(reviewId);
                if (review == null)
                {
                    return NotFound(new { message = "Không tìm thấy đánh giá." });
                }

                if (review.IsHidden)
                {
                    // Chỉ cho phép Moderator hoặc SystemAdmin xem review bị ẩn
                    var roleStr = User.FindFirst(ClaimTypes.Role)?.Value;
                    var isModeratorOrAdmin = !string.IsNullOrEmpty(roleStr) &&
                        (roleStr.Equals("SystemAdmin", StringComparison.OrdinalIgnoreCase) ||
                         roleStr.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                         roleStr.Equals("3") ||
                         roleStr.Equals("Moderator", StringComparison.OrdinalIgnoreCase) ||
                         roleStr.Equals("4"));

                    if (!isModeratorOrAdmin)
                    {
                        return NotFound(new { message = "Không tìm thấy đánh giá hoặc đánh giá đã bị ẩn." });
                    }
                }

                return Ok(review);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy thông tin chi tiết đánh giá.", detail = ex.Message });
            }
        }

        // 3. GET REVIEWS FOR A BOOK (RealBook or BlindBook)
        [HttpGet("book")]
        public async Task<IActionResult> GetReviewsForBook([FromQuery] long? bookId, [FromQuery] long? blindBookId)
        {
            try
            {
                var result = await _reviewService.GetReviewsForBookAsync(bookId, blindBookId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy danh sách đánh giá của sách.", detail = ex.Message });
            }
        }

        // 4. UPDATE REVIEW
        [HttpPut("{reviewId}")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> UpdateReview(long reviewId, [FromBody] UpdateReviewDTO dto)
        {
            if (dto == null) return BadRequest(new { message = "Dữ liệu cập nhật trống." });

            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !long.TryParse(customerIdStr, out long customerId))
            {
                return Unauthorized(new { message = "Token không hợp lệ hoặc đã hết hạn." });
            }

            try
            {
                var result = await _reviewService.UpdateReviewAsync(customerId, reviewId, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi cập nhật đánh giá.", detail = ex.Message });
            }
        }

        // 5. DELETE REVIEW
        [HttpDelete("{reviewId}")]
        [Authorize]
        public async Task<IActionResult> DeleteReview(long reviewId)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleStr = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId) ||
                string.IsNullOrEmpty(roleStr) || !Enum.TryParse<UserRole>(roleStr, out var role))
            {
                return Unauthorized(new { message = "Token không hợp lệ hoặc đã hết hạn." });
            }

            try
            {
                var success = await _reviewService.DeleteReviewAsync(userId, role, reviewId);
                if (!success)
                {
                    return NotFound(new { message = "Không tìm thấy đánh giá cần xóa." });
                }
                return Ok(new { message = "Xóa đánh giá thành công." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi xóa đánh giá.", detail = ex.Message });
            }
        }

        // 6. HIDE/UNHIDE REVIEW (Moderator/Admin Only)
        [HttpPost("{reviewId}/hide")]
        [Authorize(Policy = "ModeratorOnly")]
        public async Task<IActionResult> HideReview(long reviewId, [FromQuery] bool isHidden = true)
        {
            try
            {
                var success = await _reviewService.HideReviewAsync(reviewId, isHidden);
                if (!success)
                {
                    return NotFound(new { message = "Không tìm thấy đánh giá." });
                }
                return Ok(new { message = isHidden ? "Đã ẩn đánh giá thành công." : "Đã bỏ ẩn đánh giá thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi cập nhật trạng thái ẩn đánh giá.", detail = ex.Message });
            }
        }

        // 7. LIKE REVIEW
        [HttpPost("{reviewId}/like")]
        public async Task<IActionResult> LikeReview(long reviewId)
        {
            try
            {
                var result = await _reviewService.LikeReviewAsync(reviewId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi thích đánh giá.", detail = ex.Message });
            }
        }

        // 8. GET CUSTOMER GAMIFICATION (Points and Badges)
        [HttpGet("gamification/{customerId}")]
        public async Task<IActionResult> GetCustomerGamification(long customerId)
        {
            try
            {
                var points = await _reviewService.GetCustomerReputationPointsAsync(customerId);
                var badges = await _reviewService.GetCustomerBadgesAsync(customerId);
                return Ok(new
                {
                    customerId = customerId,
                    reputationPoint = points,
                    badges = badges
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi lấy thông tin Gamification.", detail = ex.Message });
            }
        }
    }
}
