// File: Web/Controllers/ReviewController.cs
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using BookBlossom.Core.DTOs.Review;
using BookBlossom.Core.Interfaces.Services;

namespace BookBlossom.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _service;

        public ReviewController(IReviewService service)
        {
            _service = service;
        }

        // 1. LẤY DANH SÁCH REVIEW (Xem công khai không cần đăng nhập)
        [HttpGet]
        public async Task<IActionResult> GetReviews([FromQuery] long? bookId, [FromQuery] long? blindBookId)
        {
            try
            {
                var result = await _service.GetReviewsByBookAsync(bookId, blindBookId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy danh sách đánh giá.", detail = ex.Message });
            }
        }

        // 2. ĐĂNG BÀI REVIEW MỚI - Chỉ Customer đăng nhập mới có quyền
        [HttpPost]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> CreateReview([FromForm] CreateReviewDTO dto, [FromForm] List<IFormFile>? mediaFiles)
        {
            if (dto == null) return BadRequest(new { message = "Dữ liệu đánh giá trống." });

            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !long.TryParse(customerIdStr, out long customerId))
            {
                return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." });
            }

            try
            {
                var result = await _service.CreateReviewAsync(customerId, dto, mediaFiles);
                return StatusCode(201, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi hệ thống khi lưu review.", detail = ex.Message });
            }
        }

        // 3. THẢ LIKE BÀI REVIEW - Người dùng bất kỳ đã đăng nhập
        [HttpPost("{reviewId}/like")]
        [Authorize]
        public async Task<IActionResult> LikeReview(long reviewId)
        {
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !long.TryParse(customerIdStr, out long customerId))
            {
                return Unauthorized(new { message = "Vui lòng đăng nhập để thực hiện tính năng này." });
            }

            try
            {
                var updatedLikes = await _service.LikeReviewAsync(customerId, reviewId);
                return Ok(new { message = "Thích bài đánh giá thành công.", totalLikes = updatedLikes });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Không thể xử lý tương tác thích.", detail = ex.Message });
            }
        }

        // 4. ẨN/HIỆN REVIEW - Dành riêng cho Ban quản trị (Moderator/Admin)
        [HttpPost("{reviewId}/moderation")]
        [Authorize(Policy = "ModeratorOnly")]
        public async Task<IActionResult> ToggleHideReview(long reviewId, [FromQuery] bool isHidden = true)
        {
            try
            {
                var success = await _service.HideReviewAsync(reviewId, isHidden);
                if (!success)
                {
                    return NotFound(new { message = "Không tìm thấy dữ liệu bài đánh giá yêu cầu." });
                }
                return Ok(new { message = isHidden ? "Đã ẩn bài đánh giá thành công." : "Đã bỏ ẩn bài đánh giá." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi trong quá trình kiểm duyệt nội dung.", detail = ex.Message });
            }
        }
    }
}