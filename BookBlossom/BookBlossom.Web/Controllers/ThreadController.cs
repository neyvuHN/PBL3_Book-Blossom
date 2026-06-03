using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using BookBlossom.Core.Enums;
using BookBlossom.Core.DTOs.Thread;
using BookBlossom.Core.Interfaces.Services;

namespace BookBlossom.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ThreadController : ControllerBase
    {
        private readonly IThreadService _service;
        private readonly IMemoryCache _cache;
        private readonly IReputationService _reputationService;

        public ThreadController(IThreadService service, IMemoryCache cache, IReputationService reputationService)
        {
            _service = service;
            _cache = cache;
            _reputationService = reputationService;
        }

        private long? GetCurrentCustomerId()
        {
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(customerIdStr, out var customerId) ? customerId : null;
        }

        // 1. GET ALL (Feed) - Cho phép xem công khai không cần đăng nhập
        [HttpGet("feed")]
        public async Task<IActionResult> GetFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
                if (!isAuthenticated)
                {
                    // Guest chỉ được xem tối đa 3 bài mới nhất (trang 1, size 3)
                    if (page > 1)
                    {
                        return Ok(Array.Empty<ThreadPostDTO>());
                    }
                    page = 1;
                    pageSize = 3;
                }
                else
                {
                    if (page < 1) page = 1;
                    if (pageSize < 1 || pageSize > 50) pageSize = 10;
                }

                var result = await _service.GetFeedAsync(page, pageSize, GetCurrentCustomerId());
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy danh sách bài viết.", detail = ex.Message });
            }
        }

        // 2. GET BY ID - Xem chi tiết bài viết và bình luận công khai
        [HttpGet("{postId}")]
        public async Task<IActionResult> GetPostById(long postId)
        {
            try
            {
                // Kiểm tra giới hạn xem 3 bài/ngày đối với Guest
                var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
                if (!isAuthenticated)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    var todayStr = DateTime.UtcNow.ToString("yyyyMMdd");
                    var cacheKey = $"Guest_ViewedThreads_{ipAddress}_{todayStr}";

                    var viewedThreadIds = _cache.Get<HashSet<long>>(cacheKey) ?? new HashSet<long>();

                    if (!viewedThreadIds.Contains(postId))
                    {
                        if (viewedThreadIds.Count >= 3)
                        {
                            return BadRequest(new { message = "Bạn đã đạt giới hạn xem 3 bài viết mỗi ngày dành cho Guest. Vui lòng đăng nhập để xem tiếp." });
                        }
                        viewedThreadIds.Add(postId);
                        _cache.Set(cacheKey, viewedThreadIds, TimeSpan.FromDays(1));
                    }
                }

                var post = await _service.GetPostByIdAsync(postId, GetCurrentCustomerId());
                if (post == null)
                {
                    return NotFound(new { message = "Không tìm thấy bài viết hoặc bài viết đã bị ẩn." });
                }

                if (post.IsHidden)
                {
                    var roleStr = User.FindFirst(ClaimTypes.Role)?.Value;
                    var isModeratorOrAdmin = !string.IsNullOrEmpty(roleStr) &&
                        (roleStr.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                         roleStr.Equals("3"));

                    if (!isModeratorOrAdmin)
                    {
                        return NotFound(new { message = "Không tìm thấy bài viết hoặc bài viết đã bị ẩn." });
                    }
                }

                return Ok(post);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy chi tiết bài viết.", detail = ex.Message });
            }
        }

        // 3. CREATE POST - Yêu cầu đăng nhập và chỉ có Customer (Buyer) mới được đăng
        [HttpPost]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> CreatePost([FromForm] CreateThreadPostDTO dto, [FromForm] List<IFormFile>? images)
        {
            if (dto == null) return BadRequest(new { message = "Dữ liệu bài viết trống." });

            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !long.TryParse(customerIdStr, out long customerId))
            {
                return Unauthorized(new { message = "Token không hợp lệ hoặc đã hết hạn." });
            }

            try
            {
                var reputation = await _reputationService.GetReputationWithRankAsync(customerId);
                if (reputation.ReputationPoint < 80)
                {
                    return BadRequest(new { message = "Hạn chế quyền lực: Điểm uy tín của bạn hiện tại dưới 80, bị cấm đăng bài viết mới trong cộng đồng." });
                }
                
                var result = await _service.CreatePostAsync(customerId, dto, images);
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
                return BadRequest(new { message = "Đã xảy ra lỗi khi tạo bài viết.", detail = ex.Message });
            }
        }

        // 4. UPDATE POST - Chỉ chủ bài viết mới được sửa
        [HttpPut("{postId}")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> UpdatePost(long postId, [FromBody] UpdateThreadPostDTO dto)
        {
            if (dto == null) return BadRequest(new { message = "Dữ liệu sửa bài trống." });

            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !long.TryParse(customerIdStr, out long customerId))
            {
                return Unauthorized(new { message = "Token không hợp lệ hoặc đã hết hạn." });
            }

            try
            {
                var result = await _service.UpdatePostAsync(customerId, postId, dto);
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
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi cập nhật bài viết.", detail = ex.Message });
            }
        }

        // 5. DELETE POST - Chủ bài viết hoặc Moderator/Admin có quyền xóa
        [HttpDelete("{postId}")]
        [Authorize]
        public async Task<IActionResult> DeletePost(long postId)
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
                var success = await _service.DeletePostAsync(userId, role, postId);
                if (!success)
                {
                    return NotFound(new { message = "Không tìm thấy bài viết cần xóa." });
                }
                return Ok(new { message = "Xóa bài viết thành công." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi xóa bài viết.", detail = ex.Message });
            }
        }

        // 6. HIDE/UNHIDE POST - Chỉ Moderator/Admin
        [HttpPost("{postId}/hide")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> HidePost(long postId, [FromQuery] bool isHidden = true)
        {
            try
            {
                var success = await _service.HidePostAsync(postId, isHidden);
                if (!success)
                {
                    return NotFound(new { message = "Không tìm thấy bài viết." });
                }
                return Ok(new { message = isHidden ? "Đã ẩn bài viết thành công." : "Đã bỏ ẩn bài viết thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi cập nhật trạng thái ẩn bài viết.", detail = ex.Message });
            }
        }

        // 7. REPORT POST - Bất kỳ khách hàng nào đăng nhập (CustomerOnly)
        [HttpPost("{postId}/report")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> ReportPost(long postId, [FromBody] CreateReportDTO dto)
        {
            if (dto == null) return BadRequest(new { message = "Dữ liệu báo cáo trống." });

            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !long.TryParse(customerIdStr, out long customerId))
            {
                return Unauthorized(new { message = "Token không hợp lệ hoặc đã hết hạn." });
            }

            try
            {
                var count = await _service.ReportPostAsync(customerId, postId, dto);
                return Ok(new { message = "Báo cáo bài viết thành công.", reportCount = count });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi báo cáo bài viết.", detail = ex.Message });
            }
        }

        // 8. ADD COMMENT - Chỉ khách hàng đăng nhập (CustomerOnly)
        [HttpPost("{postId}/like")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> ToggleLike(long postId)
        {
            var customerId = GetCurrentCustomerId();
            if (!customerId.HasValue)
            {
                return Unauthorized(new { message = "Token không hợp lệ hoặc đã hết hạn." });
            }

            try
            {
                var result = await _service.ToggleLikeAsync(customerId.Value, postId);
                return Ok(new
                {
                    message = result.IsLiked ? "Đã thích bài viết." : "Đã bỏ thích bài viết.",
                    isLiked = result.IsLiked,
                    likeCount = result.LikeCount
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi cập nhật lượt thích.", detail = ex.Message });
            }
        }

        [HttpPost("{postId}/share")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> SharePost(long postId, [FromBody] ShareThreadPostDTO dto)
        {
            var customerId = GetCurrentCustomerId();
            if (!customerId.HasValue)
            {
                return Unauthorized(new { message = "Token không hợp lệ hoặc đã hết hạn." });
            }

            try
            {
                var shareCount = await _service.SharePostAsync(customerId.Value, postId, dto ?? new ShareThreadPostDTO());
                return Ok(new
                {
                    message = "Ghi nhận chia sẻ bài viết thành công.",
                    shareCount
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi ghi nhận chia sẻ bài viết.", detail = ex.Message });
            }
        }

        [HttpPost("{postId}/comment")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> AddComment(long postId, [FromBody] CreateThreadCommentDTO dto)
        {
            if (dto == null) return BadRequest(new { message = "Dữ liệu bình luận trống." });

            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !long.TryParse(customerIdStr, out long customerId))
            {
                return Unauthorized(new { message = "Token không hợp lệ hoặc đã hết hạn." });
            }

            try
            {
                var reputation = await _reputationService.GetReputationWithRankAsync(customerId);
                if (reputation.ReputationPoint < 80)
                {
                    return BadRequest(new { message = "Hạn chế quyền lực: Điểm uy tín của bạn hiện tại dưới 80, bị cấm bình luận trong cộng đồng." });
                }
                
                var result = await _service.AddCommentAsync(customerId, postId, dto);
                return StatusCode(201, result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi tạo bình luận.", detail = ex.Message });
            }
        }

        // 9. DELETE COMMENT - Chủ bình luận hoặc Moderator/Admin
        [HttpDelete("comment/{commentId}")]
        [Authorize]
        public async Task<IActionResult> DeleteComment(long commentId)
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
                var success = await _service.DeleteCommentAsync(userId, role, commentId);
                if (!success)
                {
                    return NotFound(new { message = "Không tìm thấy bình luận cần xóa." });
                }
                return Ok(new { message = "Xóa bình luận thành công." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi xóa bình luận.", detail = ex.Message });
            }
        }
    }
}
