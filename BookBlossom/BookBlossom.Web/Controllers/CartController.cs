using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BookBlossom.Core.DTOs.Cart;
using BookBlossom.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookBlossom.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
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
                // Try to get the validated GuestID from HttpContext.Items (populated by GuestSessionMiddleware)
                if (HttpContext.Items.TryGetValue("GuestID", out var guestIdObj) && guestIdObj is Guid parsedGuestId)
                {
                    guestId = parsedGuestId;
                }
            }

            return (userId, guestId);
        }

        [HttpGet]
        public async Task<IActionResult> GetCartItems(
            [FromHeader(Name = "X-Guest-Id")] string? guestIdHeader = null,
            [FromHeader(Name = "X-Guest-Token")] string? guestTokenHeader = null)
        {
            var (userId, guestId) = GetUserOrGuestId();

            if (!userId.HasValue && !guestId.HasValue)
            {
                return BadRequest(new { message = "Yêu cầu phải có xác thực người dùng hoặc GuestID trong header (X-Guest-ID)." });
            }

            var items = await _cartService.GetCartItemsAsync(userId, guestId);
            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(
            [FromBody] AddCartRequestDTO request,
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
                var cartItem = await _cartService.AddToCartAsync(userId, guestId, request);
                return Ok(cartItem);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                var details = ex.InnerException != null ? $"{ex.Message} Inner: {ex.InnerException.Message}" : ex.Message;
                return StatusCode(500, new { message = "Đã xảy ra lỗi hệ thống.", details = details });
            }
        }

        [HttpPut("{cartId}/quantity")]
        public async Task<IActionResult> UpdateCartQuantity(
            long cartId, 
            [FromBody] UpdateCartRequestDTO request,
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
                var cartItem = await _cartService.UpdateCartItemQuantityAsync(cartId, userId, guestId, request.Quantity);
                return Ok(cartItem);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
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

        [HttpDelete("{cartId}")]
        public async Task<IActionResult> RemoveFromCart(
            long cartId,
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
                var result = await _cartService.RemoveFromCartAsync(cartId, userId, guestId);
                if (!result) return NotFound(new { message = "Không tìm thấy mục trong giỏ hàng." });
                return Ok(new { message = "Đã xóa khỏi giỏ hàng." });
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
