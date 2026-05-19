using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BookBlossom.Core.DTOs.Wishlist;
using BookBlossom.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookBlossom.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService _wishlistService;

        public WishlistController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        private (long? userId, Guid? guestId) GetUserOrGuestId()
        {
            long? userId = null;
            Guid? guestId = null;

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (long.TryParse(userIdClaim, out var uid))
                {
                    userId = uid;
                }
            }
            else
            {
                // Try to get GuestID from Header or Cookie
                if (Request.Headers.TryGetValue("X-Guest-ID", out var guestIdHeader) && Guid.TryParse(guestIdHeader.ToString(), out var parsedGuestId))
                {
                    guestId = parsedGuestId;
                }
            }

            return (userId, guestId);
        }

        [HttpGet]
        public async Task<IActionResult> GetWishlistItems(
            [FromHeader(Name = "X-Guest-Id")] string? guestIdHeader = null,
            [FromHeader(Name = "X-Guest-Token")] string? guestTokenHeader = null)
        {
            var (userId, guestId) = GetUserOrGuestId();

            if (!userId.HasValue && !guestId.HasValue)
            {
                return BadRequest(new { message = "Yêu cầu phải có xác thực người dùng hoặc GuestID trong header (X-Guest-ID)." });
            }

            var items = await _wishlistService.GetWishlistItemsAsync(userId, guestId);
            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> AddToWishlist(
            [FromBody] AddWishlistRequestDTO request,
            [FromHeader(Name = "X-Guest-Id")] string? guestIdHeader = null,
            [FromHeader(Name = "X-Guest-Token")] string? guestTokenHeader = null)
        {
            var (userId, guestId) = GetUserOrGuestId();

            if (!userId.HasValue && !guestId.HasValue)
            {
                return BadRequest(new { message = "Yêu cầu phải có xác thực người dùng hoặc GuestID trong header (X-Guest-ID)." });
            }

            try
            {
                var wishlistItem = await _wishlistService.AddToWishlistAsync(userId, guestId, request);
                return Ok(wishlistItem);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi hệ thống.", details = ex.Message });
            }
        }

        [HttpDelete("{wishlistId}")]
        public async Task<IActionResult> RemoveFromWishlist(
            long wishlistId,
            [FromHeader(Name = "X-Guest-Id")] string? guestIdHeader = null,
            [FromHeader(Name = "X-Guest-Token")] string? guestTokenHeader = null)
        {
            var (userId, guestId) = GetUserOrGuestId();

            if (!userId.HasValue && !guestId.HasValue)
            {
                return BadRequest(new { message = "Yêu cầu phải có xác thực người dùng hoặc GuestID trong header (X-Guest-ID)." });
            }

            try
            {
                var result = await _wishlistService.RemoveFromWishlistAsync(wishlistId, userId, guestId);
                if (!result) return NotFound(new { message = "Không tìm thấy mục trong danh sách yêu thích." });
                return Ok(new { message = "Đã xóa khỏi danh sách yêu thích." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi hệ thống.", details = ex.Message });
            }
        }
    }
}
