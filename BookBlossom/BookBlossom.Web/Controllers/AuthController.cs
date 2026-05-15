using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using BookBlossom.Core.DTOs;
using BookBlossom.Core.DTOs.Auth;

namespace BookBlossom.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IOTPService _otpService;
        private readonly ISMSService _smsService;
        private readonly IMemoryCache _cache;
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;
        private readonly IGuestService _guestService;

        // Khai báo và tiêm các dependency cần thiết
        public AuthController(IOTPService otpService, ISMSService smsService, IMemoryCache cache, ApplicationDbContext context, IAuthService authService, IGuestService guestService)
        {
            _otpService = otpService;
            _smsService = smsService;
            _cache = cache;
            _context = context;
            _authService = authService;
            _guestService = guestService;
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
            var userExists = await _context.Users.AnyAsync(u => u.PhoneNumber == request.PhoneNumber);
            if (userExists)
            {
                return BadRequest(new { Message = "Số điện thoại này đã được đăng ký." });
            }

            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var otpCode = await _otpService.GenerateOtpAsync(request.PhoneNumber, ipAddress);

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                _cache.Set(request.PhoneNumber, request, cacheOptions);

                // Gửi SMS mô phỏng
                await _smsService.SendSmsAsync(request.PhoneNumber, $"Mã xác thực BookBlossom của bạn là: {otpCode}");

                return Ok(new 
                { 
                    Message = "Mã OTP đã được gửi" 
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(
            [FromBody] VerifyOtpRequestDTO request,
            [FromHeader(Name = "X-Guest-Id")] string? guestIdHeader,
            [FromHeader(Name = "X-Guest-Token")] string? guestTokenHeader)
        {
            try
            {
                var isOtpValid = await _otpService.VerifyOtpAsync(request.PhoneNumber, request.OtpCode);
                if (!isOtpValid)
                {
                    return BadRequest(new { Message = "Mã OTP không chính xác." });
                }

                if (!_cache.TryGetValue(request.PhoneNumber, out RegisterRequestDTO? cachedRequest) || cachedRequest == null)
                {
                    return BadRequest(new { Message = "Phiên đăng ký đã hết hạn hoặc không hợp lệ. Vui lòng đăng ký lại." });
                }
                
                await _authService.CompleteRegistrationAsync(cachedRequest);
                _cache.Remove(request.PhoneNumber);

                if (HttpContext.Items.TryGetValue("GuestID", out var guestIdObj) && guestIdObj is Guid guestId)
                {
                    var newUser = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == cachedRequest.PhoneNumber);
                    if (newUser != null)
                    {
                        await _guestService.MigrateGuestDataToUserAsync(guestId, newUser.UserID);
                    }
                }

                return Ok(new { Message = "Đăng ký tài khoản thành công!" });
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
