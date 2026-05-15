using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Core.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;

namespace BookBlossom.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase 
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO request)
        {
            try
            {
                var result = await _authService.RegisterAsync(request);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
        
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequestDTO request)
        {
            if (request == null)
            {
                return BadRequest("Invalid client request");
            }

            try
            {
                var result = await _authService.RefreshTokenAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message }); 
            }
        }
    }
}