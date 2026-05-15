using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BookBlossom.Core.Interfaces.Services;

namespace BookBlossom.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GuestController : ControllerBase
    {
        private readonly IGuestService _guestService;

        public GuestController(IGuestService guestService)
        {
            _guestService = guestService;
        }

        [HttpPost("session")]
        public async Task<IActionResult> CreateSession()
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = Request.Headers["User-Agent"].ToString();

                var session = await _guestService.CreateGuestSessionAsync(ipAddress, userAgent);
                return Ok(session);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("current")]
        public IActionResult GetCurrentGuest(
            [FromHeader(Name = "X-Guest-Id")] string? guestIdHeader, 
            [FromHeader(Name = "X-Guest-Token")] string? guestTokenHeader)
        {
            if (HttpContext.Items.TryGetValue("GuestID", out var guestId))
            {
                return Ok(new { GuestID = guestId, Message = "Middleware đã nhận diện được Guest!" });
            }
            return NotFound(new { Message = "Không tìm thấy thông tin Guest trong Header. Hãy đảm bảo bạn đã nhập đúng 2 Header trên Swagger." });
        }

    }
}
