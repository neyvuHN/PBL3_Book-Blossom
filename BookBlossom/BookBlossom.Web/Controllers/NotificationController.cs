using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookBlossom.Core.DTOs.Notification;
using BookBlossom.Core.Enums;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;

namespace BookBlossom.Web.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ApplicationDbContext _context;

        public NotificationController(INotificationService notificationService, ApplicationDbContext context)
        {
            _notificationService = notificationService;
            _context = context;
        }

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out var uid))
            {
                return uid;
            }
            throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] bool? isRead)
        {
            try
            {
                var userId = GetCurrentUserId();
                var notifications = await _notificationService.GetUserNotificationsAsync(userId, isRead);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _notificationService.MarkAsReadAsync(userId, id);
                if (!result)
                {
                    return NotFound(new { message = "Không tìm thấy thông báo hoặc thông báo không thuộc quyền sở hữu của bạn." });
                }
                return Ok(new { message = "Đã đánh dấu đã đọc thông báo." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = GetCurrentUserId();
                await _notificationService.MarkAllAsReadAsync(userId);
                return Ok(new { message = "Đã đánh dấu đã đọc tất cả thông báo." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(long id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _notificationService.DeleteNotificationAsync(userId, id);
                if (!result)
                {
                    return NotFound(new { message = "Không tìm thấy thông báo hoặc thông báo không thuộc quyền sở hữu của bạn." });
                }
                return Ok(new { message = "Đã xóa thông báo." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeRequestDTO request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _notificationService.SubscribeAsync(userId, request.TargetID, request.TargetType);
                if (result)
                {
                    return Ok(new { message = "Đã đăng ký theo dõi thành công." });
                }
                return BadRequest(new { message = "Không thể đăng ký theo dõi." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromBody] SubscribeRequestDTO request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _notificationService.UnsubscribeAsync(userId, request.TargetID, request.TargetType);
                if (result)
                {
                    return Ok(new { message = "Đã hủy đăng ký theo dõi thành công." });
                }
                return NotFound(new { message = "Không tìm thấy đăng ký theo dõi tương ứng." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("subscriptions")]
        public async Task<IActionResult> GetSubscriptions()
        {
            try
            {
                var userId = GetCurrentUserId();
                var subscriptions = await _notificationService.GetSubscriptionsAsync(userId);
                return Ok(subscriptions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("mock-like")]
        public async Task<IActionResult> MockLike([FromQuery] long postId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var post = await _context.ThreadPosts
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.PostID == postId);

                if (post == null)
                {
                    return NotFound(new { message = "Không tìm thấy bài viết để Like." });
                }

                var user = await _context.Users.FindAsync(currentUserId);
                if (user == null)
                {
                    return NotFound(new { message = "Người dùng hiện tại không tồn tại." });
                }

                var currentUserName = $"{user.LastName} {user.FirstName}".Trim();
                if (string.IsNullOrEmpty(currentUserName)) currentUserName = user.UserName;

                // Send notification to the post owner (if they are not the one liking it)
                if (post.CustomerID != currentUserId)
                {
                    await _notificationService.CreateAndSendNotificationAsync(
                        post.CustomerID,
                        "Lượt thích mới",
                        $"Người dùng {currentUserName} đã thích bài viết của bạn.",
                        NotificationType.NewInteraction,
                        (int)postId
                    );
                }

                return Ok(new { message = $"Đã mock hành động Like bài viết ID {postId} thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("mock-follow")]
        public async Task<IActionResult> MockFollow([FromQuery] long targetUserId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var targetUser = await _context.Users.FindAsync(targetUserId);

                if (targetUser == null)
                {
                    return NotFound(new { message = "Không tìm thấy người dùng đích để Follow." });
                }

                var user = await _context.Users.FindAsync(currentUserId);
                if (user == null)
                {
                    return NotFound(new { message = "Người dùng hiện tại không tồn tại." });
                }

                var currentUserName = $"{user.LastName} {user.FirstName}".Trim();
                if (string.IsNullOrEmpty(currentUserName)) currentUserName = user.UserName;

                // Send notification to target user
                if (targetUserId != currentUserId)
                {
                    await _notificationService.CreateAndSendNotificationAsync(
                        targetUserId,
                        "Người theo dõi mới",
                        $"Người dùng {currentUserName} đã bắt đầu theo dõi bạn.",
                        NotificationType.NewInteraction,
                        (int)currentUserId
                    );
                }

                return Ok(new { message = $"Đã mock hành động theo dõi người dùng ID {targetUserId} thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
