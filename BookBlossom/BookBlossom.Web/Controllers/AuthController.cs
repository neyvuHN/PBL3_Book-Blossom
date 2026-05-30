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
    public class AuthController : Controller
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

        // ==================== MVC Razor Views ====================

        [HttpGet("/Auth/Login")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet("/Auth/Register")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Register()
        {
            return View();
        }

        [HttpGet("/Auth/InterestSelection")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult InterestSelection()
        {
            return View();
        }

        [HttpGet("/Auth/ForgotPassword")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpGet("/Auth/Logout")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt");
            return RedirectToAction("Login");
        }

        // ==================== API Endpoints ====================

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);

                // Migrate Guest Cart and Wishlist if GuestID is present
                if (Request.Headers.TryGetValue("X-Guest-ID", out var guestIdHeader) && Guid.TryParse(guestIdHeader.ToString(), out var guestId))
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);
                    if (user != null)
                    {
                        await _guestService.MigrateGuestDataToUserAsync(guestId, user.UserID);
                    }
                }
                else if (HttpContext.Items.TryGetValue("GuestID", out var guestIdObj) && guestIdObj is Guid guestIdFromItems)
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);
                    if (user != null)
                    {
                        await _guestService.MigrateGuestDataToUserAsync(guestIdFromItems, user.UserID);
                    }
                }

                // Set jwt cookie
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true, // In production, require HTTPS
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(7)
                };
                Response.Cookies.Append("jwt", result.Token, cookieOptions);

                // Determine redirect url based on role
                string redirectUrl = "/";
                if (result.RoleID == BookBlossom.Core.Enums.UserRole.SystemAdmin || 
                    result.RoleID == BookBlossom.Core.Enums.UserRole.Moderator || 
                    result.RoleID == BookBlossom.Core.Enums.UserRole.MarketingManager || 
                    result.RoleID == BookBlossom.Core.Enums.UserRole.StoreManager)
                {
                    redirectUrl = "/Admin/Dashboard";
                }

                return Ok(new { 
                    Token = result.Token, 
                    RoleID = result.RoleID,
                    RedirectUrl = redirectUrl,
                    Message = "Đăng nhập thành công" 
                });
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
                // Nếu FE không gửi GuestID trong body, thử lấy từ Middleware (Header X-Guest-Id)
                if (!request.GuestID.HasValue && HttpContext.Items.TryGetValue("GuestID", out var gid) && gid is Guid guestGuid)
                {
                    request.GuestID = guestGuid;
                }

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var otpCode = await _otpService.GenerateOtpAsync(request.PhoneNumber, ipAddress);

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                _cache.Set(request.PhoneNumber, request, cacheOptions);

                // Gửi SMS thông qua eSMS với mẫu đã đăng ký
                await _smsService.SendSmsAsync(request.PhoneNumber, $"Ma OTP dang ky tai khoan tai Book Blossom la {otpCode}. Ma co hieu luc trong 5 phut.");

                return Ok(new 
                { 
                    Message = "Mã OTP đã được gửi",
                    OtpCode = otpCode
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
                    var newUserObj = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == cachedRequest.PhoneNumber);
                    if (newUserObj != null)
                    {
                        await _guestService.MigrateGuestDataToUserAsync(guestId, newUserObj.UserID);
                    }
                }

                // Tự động đăng nhập sau khi đăng ký thành công
                var loginRequest = new LoginRequestDTO
                {
                    PhoneNumber = cachedRequest.PhoneNumber,
                    Password = cachedRequest.Password
                };
                var authResponse = await _authService.LoginAsync(loginRequest);

                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true, // In production, require HTTPS
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(7)
                };
                Response.Cookies.Append("jwt", authResponse.Token, cookieOptions);

                string redirectUrl = "/";
                if (authResponse.RoleID == BookBlossom.Core.Enums.UserRole.SystemAdmin || 
                    authResponse.RoleID == BookBlossom.Core.Enums.UserRole.Moderator || 
                    authResponse.RoleID == BookBlossom.Core.Enums.UserRole.MarketingManager || 
                    authResponse.RoleID == BookBlossom.Core.Enums.UserRole.StoreManager)
                {
                    redirectUrl = "/Admin/Dashboard";
                }

                return Ok(new { 
                    Token = authResponse.Token, 
                    RoleID = authResponse.RoleID,
                    RedirectUrl = redirectUrl,
                    Message = "Đăng ký và đăng nhập thành công" 
                });
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

        [HttpPost("forgot-password/send-otp")]
        public async Task<IActionResult> ForgotPasswordSendOtp([FromBody] ForgotPasswordRequestDTO request)
        {
            try
            {
                // Kiểm tra xem số điện thoại có tồn tại trong hệ thống hay chưa
                var userExists = await _context.Users.AnyAsync(u => u.PhoneNumber == request.PhoneNumber);
                if (!userExists)
                {
                    return BadRequest(new { Message = "Số điện thoại này chưa được đăng ký trong hệ thống." });
                }

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var otpCode = await _otpService.GenerateOtpAsync(request.PhoneNumber, ipAddress);

                // Gửi SMS chứa mã OTP với mẫu đã được đăng ký
                await _smsService.SendSmsAsync(request.PhoneNumber, $"Ma OTP khoi phuc mat khau tai Book Blossom la {otpCode}. Ma co hieu luc trong 5 phut.");

                return Ok(new 
                { 
                    Message = "Mã OTP khôi phục mật khẩu đã được gửi thành công.",
                    OtpCode = otpCode
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("forgot-password/verify-otp")]
        public async Task<IActionResult> ForgotPasswordVerifyOtp([FromBody] VerifyOtpRequestDTO request)
        {
            try
            {
                var isOtpValid = await _otpService.VerifyOtpAsync(request.PhoneNumber, request.OtpCode);
                if (!isOtpValid)
                {
                    return BadRequest(new { Message = "Mã OTP không chính xác." });
                }

                // Đánh dấu là số điện thoại này đã xác thực OTP khôi phục thành công trong 5 phút
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                _cache.Set($"ForgotPassword_Verified_{request.PhoneNumber}", true, cacheOptions);

                return Ok(new { Message = "Xác thực mã OTP thành công. Vui lòng đặt lại mật khẩu mới." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("forgot-password/reset")]
        public async Task<IActionResult> ForgotPasswordReset([FromBody] ResetPasswordRequestDTO request)
        {
            try
            {
                // Kiểm tra xem số điện thoại đã được xác thực OTP khôi phục mật khẩu trước đó chưa
                if (!_cache.TryGetValue($"ForgotPassword_Verified_{request.PhoneNumber}", out bool isVerified) || !isVerified)
                {
                    return BadRequest(new { Message = "Yêu cầu khôi phục mật khẩu không hợp lệ hoặc đã hết hạn. Vui lòng xác thực lại mã OTP." });
                }

                if (request.NewPassword != request.ConfirmPassword)
                {
                    return BadRequest(new { Message = "Mật khẩu xác nhận không khớp với mật khẩu mới." });
                }

                await _authService.ResetPasswordAsync(request.PhoneNumber, request.NewPassword);

                // Xóa cache xác thực để tránh dùng lại
                _cache.Remove($"ForgotPassword_Verified_{request.PhoneNumber}");

                return Ok(new { Message = "Đặt lại mật khẩu mới thành công!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
