using Microsoft.AspNetCore.Mvc;
using BookBlossom.Core.DTOs.Tindbook;
using BookBlossom.Core.Interfaces;
using BookBlossom.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BookBlossom.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // Không dùng [Authorize] ở đây - mỗi endpoint sẽ tự kiểm soát quyền truy cập
    public class TindbookController : Controller
    {
        private readonly ITindbookService _tindbookService;

        public TindbookController(ITindbookService tindbookService)
        {
            _tindbookService = tindbookService;
        }

        // =========================
        // MVC VIEW ACTIONS - FRONTEND
        // =========================

        [HttpGet("/Tindbook")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Index()
        {
            return View();
        }

        // =========================
        // HELPER METHODS
        // =========================

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

        // =========================
        // API ACTIONS - BACKEND
        // =========================

        // ─────────────────────────────────────────────────
        // 1. Lấy sách gợi ý
        // GET /api/Tindbook/recommendations
        // ─────────────────────────────────────────────────
        [HttpGet("recommendations")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRecommendations()
        {
            var userId = GetUserId();
            var guestId = GetGuestId();

            if (userId.HasValue)
            {
                // Customer: Load liên tục không chặn, Service tự động bốc 10 cuốn chưa quẹt
                var books = await _tindbookService.GetRecommendedBooksForTindbookAsync(userId.Value);
                return Ok(books);
            }
            else if (guestId.HasValue)
            {
                // Guest: Service tự check nếu quẹt đủ 1 batch (10 cuốn) sẽ trả về mảng rỗng hoặc chặn
                var books = await _tindbookService.GetRecommendedBooksForGuestAsync(guestId.Value);
                return Ok(books);
            }
            else
            {
                return Unauthorized(new
                {
                    message = "Vui lòng tạo phiên Guest hoặc đăng nhập để sử dụng Tindbook."
                });
            }
        }

        // ─────────────────────────────────────────────────
        // 2. Xử lý hành động quẹt
        // POST /api/Tindbook/swipe
        // ─────────────────────────────────────────────────
        [HttpPost("swipe")]
        [AllowAnonymous]
        public async Task<IActionResult> SwipeAction([FromBody] SwipeActionDTO dto)
        {
            if (!Enum.IsDefined(typeof(SwipeIntent), dto.Intent))
            {
                return BadRequest("Hành động quẹt không hợp lệ.");
            }

            var userId = GetUserId();
            var guestId = GetGuestId();

            if (userId.HasValue)
            {
                // Customer
                var result = await _tindbookService.RecordSwipeActionAsync(userId, dto);

                if (!result)
                {
                    return BadRequest("Không thể thực hiện hành động này.");
                }

                return Ok(new
                {
                    message = $"Đã thực hiện: {dto.Intent}",
                    intent = dto.Intent.ToString()
                });
            }
            else if (guestId.HasValue)
            {
                // Guest
                var (success, requiresLogin) =
                    await _tindbookService.RecordGuestSwipeActionAsync(guestId.Value, dto);
                if (requiresLogin)
                {
                    // Guest quẹt AddToCart: Lưu tạm thành công, trả về Ok kèm cờ requiresLogin để Front-end hiển thị popup
                    return Ok(new
                    {
                        success = true,
                        requiresLogin = true,
                        message = "Sách đã được lưu tạm vào giỏ hàng! Vui lòng đăng ký hoặc đăng nhập tài khoản mới để giữ lại sách và thanh toán."
                    });
                }

                if (!success)
                {
                    return BadRequest("Không thể thực hiện hành động này.");
                }

                return Ok(new
                {
                    message = $"Đã thực hiện: {dto.Intent}",
                    intent = dto.Intent.ToString()
                });
            }
            else
            {
                return Unauthorized(new
                {
                    message = "Vui lòng tạo phiên Guest hoặc đăng nhập."
                });
            }
        }

        // ─────────────────────────────────────────────────
        // 3. Hoàn tác hành động cuối
        // POST /api/Tindbook/undo
        // ─────────────────────────────────────────────────
        [HttpPost("undo")]
        [AllowAnonymous]
        public async Task<IActionResult> UndoSwipe()
        {
            var userId = GetUserId();
            var guestId = GetGuestId();

            if (userId.HasValue)
            {
                var canUndo = await _tindbookService.CanUndoTindbookAsync(userId.Value);
                if (!canUndo)
                {
                    return BadRequest(new
                    {
                        message = "Bạn đã hết lượt Hoàn tác trong ngày! Vui lòng nâng cấp gói dịch vụ để nhận thêm đặc quyền."
                    });
                }

                var result = await _tindbookService.UndoLastSwipeAsync(userId.Value);

                if (!result)
                {
                    return NotFound(new { message = "Không có hành động nào để hoàn tác." });
                }

                return Ok(new
                {
                    message = "Đã hoàn tác hành động gần nhất."
                });
            }
            else if (guestId.HasValue)
            {
                var canGuestUndo = await _tindbookService.CanUndoGuestAsync(guestId.Value);

                if (!canGuestUndo)
                {
                    return BadRequest(new
                    {
                        message = "Bạn đã hết lượt Hoàn tác miễn phí trong ngày! Vui lòng đăng ký tài khoản để nhận thêm đặc quyền."
                    });
                }

                var result = await _tindbookService.UndoLastGuestSwipeAsync(guestId.Value);

                if (!result)
                {
                    return NotFound(new { message = "Không có hành động nào để hoàn tác." });
                }

                return Ok(new
                {
                    message = "Đã hoàn tác hành động gần nhất."
                });
            }
            else
            {
                return Unauthorized(new
                {
                    message = "Vui lòng tạo phiên Guest hoặc đăng nhập."
                });
            }
        }
    }
}