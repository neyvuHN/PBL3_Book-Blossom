using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Core.Enums;
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
        private readonly IAuditService _auditService;

        // Khai báo và tiêm các dependency cần thiết
        public AuthController(IOTPService otpService, ISMSService smsService, IMemoryCache cache, ApplicationDbContext context, IAuthService authService, IGuestService guestService, IAuditService auditService)
        {
            _otpService = otpService;
            _smsService = smsService;
            _cache = cache;
            _context = context;
            _authService = authService;
            _guestService = guestService;
            _auditService = auditService;
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
        public IActionResult LogoutView()
        {
            return RedirectToAction("Login");
        }

        // ==================== API Endpoints ====================

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
        {
            try
            {
                var ipAddress = GetClientIpAddress();
                var result = await _authService.LoginAsync(request, ipAddress);

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

                return Ok(result);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // Lấy thông tin ID tài khoản từ Claims Token hiện tại
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var roleClaim = User.FindFirstValue(ClaimTypes.Role);
                var ipAddress = GetClientIpAddress();

                if (!string.IsNullOrEmpty(userIdClaim))
                {
                    long currentAdminId = long.Parse(userIdClaim);

                    // Kiểm tra xem User này có phải thuộc nhóm quản trị dựa trên claim Role không
                    // Note: Nếu trong Token bạn lưu Role dạng ID (Chuỗi số), hãy parse ra để so sánh với Enum.
                    if (Enum.TryParse(roleClaim, out UserRole roleEnum) && 
                        (roleEnum == UserRole.Admin))
                    {
                        // Tiến hành ghi nhận hành động Logout của Admin vào bảng AuditLog
                        await _auditService.LogActionAsync(
                            currentAdminId, 
                            null, 
                            ActionType.ADMIN_LOGOUT, 
                            "Users", 
                            null, 
                            "Quản trị viên đăng xuất khỏi hệ thống.", 
                            ipAddress
                        );
                    }
                }

                return Ok(new { Message = "Đăng xuất thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
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

                // Bỏ qua gửi eSMS thực tế, mã OTP mock sẽ được trả về trực tiếp trong response để test
                // await _smsService.SendSmsAsync(request.PhoneNumber, $"Ma OTP dang ky tai khoan tai Book Blossom la {otpCode}. Ma co hieu luc trong 5 phut.");

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
                var ipAddress = GetClientIpAddress();
                var authResponse = await _authService.LoginAsync(loginRequest, ipAddress);

                return Ok(authResponse);
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

                // Bỏ qua gửi eSMS thực tế, mã OTP mock sẽ được trả về trực tiếp trong response để test
                // await _smsService.SendSmsAsync(request.PhoneNumber, $"Ma OTP khoi phuc mat khau tai Book Blossom la {otpCode}. Ma co hieu luc trong 5 phut.");

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

        private string GetClientIpAddress()
        {
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            }
            if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1")
            {
                ipAddress = "127.0.0.1";
            }
            return ipAddress;
        }
    }
}
