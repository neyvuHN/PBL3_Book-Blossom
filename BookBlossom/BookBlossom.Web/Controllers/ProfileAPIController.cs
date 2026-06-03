using System.Threading.Tasks;
using BookBlossom.Core.DTOs;
using BookBlossom.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookBlossom.Web.Controllers
{
    [Route("api/profile")]
    [ApiController]
    [Authorize(Roles = "Customer,Admin")]
    public class ProfileAPIController : ControllerBase
    {
        private readonly IUserService _userService;

        public ProfileAPIController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileDTO dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
            {
                return Unauthorized(new { message = "Không tìm thấy người dùng." });
            }

            try
            {
                var result = await _userService.UpdateProfileAsync(userId, dto);
                if (!result)
                {
                    return BadRequest(new { message = "Cập nhật hồ sơ thất bại." });
                }
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            return Ok(new { message = "Cập nhật hồ sơ thành công." });
        }
    }
}
