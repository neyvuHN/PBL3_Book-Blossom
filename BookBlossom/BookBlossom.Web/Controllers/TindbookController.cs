using Microsoft.AspNetCore.Mvc;
using BookBlossom.Core.DTOs.Tindbook;
using BookBlossom.Core.Interfaces;
using BookBlossom.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BookBlossom.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // Không dùng [Authorize] ở đây - mỗi endpoint sẽ tự kiểm soát quyền truy cập
    public class TindbookController : ControllerBase
    {
        private readonly ITindbookService _tindbookService;

        public TindbookController(ITindbookService tindbookService)
        {
            _tindbookService = tindbookService;
        }

        /// <summary>Lấy UserId từ JWT nếu đã đăng nhập. Trả về null nếu chưa.</summary>
        private long? GetUserId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(val, out var id) ? id : null;
        }

        /// <summary>Lấy GuestId từ HttpContext nếu là Guest đã có session hợp lệ.</summary>
        private Guid? GetGuestId()
        {
            return HttpContext.Items.TryGetValue("GuestID", out var val) && val is Guid gid ? gid : null;
        }

        // ─────────────────────────────────────────────────
        // 1. Lấy sách gợi ý
        // ─────────────────────────────────────────────────
        [HttpGet("recommendations")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRecommendations([FromQuery] int limit = 20)
        {
            var userId = GetUserId();
            var guestId = GetGuestId();

            if (userId.HasValue)
            {
                // Customer đã đăng nhập: gợi ý dựa trên CustomerPreference
                var books = await _tindbookService.GetRecommendedBooksForTindbookAsync(userId.Value, limit);
                return Ok(books);
            }
            else if (guestId.HasValue)
            {
                // Guest có session hợp lệ: gợi ý dựa trên GuestPreference
                var books = await _tindbookService.GetRecommendedBooksForGuestAsync(guestId.Value, limit);
                return Ok(books);
            }
            else
            {
                // Không có session nào -> yêu cầu tạo Guest Session
                return Unauthorized(new { message = "Vui lòng tạo phiên Guest hoặc đăng nhập để sử dụng Tindbook." });
            }
        }

        // ─────────────────────────────────────────────────
        // 2. Xử lý hành động quẹt
        // ─────────────────────────────────────────────────
        [HttpPost("swipe")]
        [AllowAnonymous]
        public async Task<IActionResult> SwipeAction([FromBody] SwipeActionDTO dto)
        {
            if (!Enum.IsDefined(typeof(SwipeIntent), dto.Intent))
                return BadRequest("Hành động quẹt không hợp lệ.");

            var userId = GetUserId();
            var guestId = GetGuestId();

            if (userId.HasValue)
            {
                // === Customer ===
                var result = await _tindbookService.RecordSwipeActionAsync(userId, dto);
                if (!result) return BadRequest("Không thể thực hiện hành động này.");
                return Ok(new { message = $"Đã thực hiện: {dto.Intent}", intent = dto.Intent.ToString() });
            }
            else if (guestId.HasValue)
            {
                // === Guest ===
                var (success, requiresLogin) = await _tindbookService.RecordGuestSwipeActionAsync(guestId.Value, dto);

                if (requiresLogin)
                {
                    // Nghiệp vụ: Guest không được AddToCart, bắt đăng nhập
                    return Unauthorized(new
                    {
                        requiresLogin = true,
                        message = "Vui lòng đăng nhập hoặc đăng ký tài khoản để thêm sách vào giỏ hàng!"
                    });
                }

                if (!success) return BadRequest("Không thể thực hiện hành động này.");
                return Ok(new { message = $"Đã thực hiện: {dto.Intent}", intent = dto.Intent.ToString() });
            }
            else
            {
                return Unauthorized(new { message = "Vui lòng tạo phiên Guest hoặc đăng nhập." });
            }
        }

        // ─────────────────────────────────────────────────
        // 3. Hoàn tác hành động cuối
        // ─────────────────────────────────────────────────
        [HttpPost("undo")]
        [AllowAnonymous]
        public async Task<IActionResult> UndoSwipe()
        {
            var userId = GetUserId();
            var guestId = GetGuestId();

            if (userId.HasValue)
            {
                var result = await _tindbookService.UndoLastSwipeAsync(userId.Value);
                if (!result) return NotFound("Không có hành động nào để hoàn tác.");
                return Ok(new { message = "Đã hoàn tác hành động gần nhất." });
            }
            else if (guestId.HasValue)
            {
                var result = await _tindbookService.UndoLastGuestSwipeAsync(guestId.Value);
                if (!result) return NotFound("Không có hành động nào để hoàn tác.");
                return Ok(new { message = "Đã hoàn tác hành động gần nhất." });
            }
            else
            {
                return Unauthorized(new { message = "Vui lòng tạo phiên Guest hoặc đăng nhập." });
            }
        }
    }
}