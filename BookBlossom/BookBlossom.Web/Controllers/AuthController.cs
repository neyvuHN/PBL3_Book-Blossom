using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using BookBlossom.Core.DTOs;

namespace BookBlossom.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IOTPService _otpService;
        private readonly IMemoryCache _cache;
        private readonly ApplicationDbContext _context;

        // Khai báo và tiêm các dependency cần thiết
        public AuthController(IOTPService otpService, IMemoryCache cache, ApplicationDbContext context)
        {
            _otpService = otpService;
            _cache = cache;
            _context = context;
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

        // Triển khai API đăng ký
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO request)
        {
            // Kiểm tra số điện thoại đã tồn tại trong DB chưa
            var userExists = await _context.Users.AnyAsync(u => u.PhoneNumber == request.PhoneNumber);
            if (userExists)
            {
                return BadRequest(new { Message = "Số điện thoại này đã được đăng ký." });
            }

            try
            {
                // Gọi OtpService tạo mã
                var otpCode = await _otpService.GenerateOtpAsync(request.PhoneNumber);

                // Lưu thông tin người dùng tạm vào IMemoryCache
                // Khóa là số điện thoại, thời gian sống bằng với thời gian OTP (5 phút)
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                _cache.Set(request.PhoneNumber, request, cacheOptions);

                // Ở thực tế, bạn sẽ gọi SMS Service ở đây để gửi otpCode tới người dùng.
                // Trong môi trường dev, chúng ta trả thẳng về response để dễ test.
                return Ok(new 
                { 
                    Message = "Mã OTP đã được gửi", 
                    Developer_OtpCode_For_Testing = otpCode 
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // Triển khai API xác thực OTP
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDTO request)
        {
            // Kiểm tra OTP
            var isOtpValid = await _otpService.VerifyOtpAsync(request.PhoneNumber, request.OtpCode);
            if (!isOtpValid)
            {
                return BadRequest(new { Message = "Mã OTP không chính xác hoặc đã hết hạn" });
            }

            // Lấy thông tin đăng ký từ Cache
            if (!_cache.TryGetValue(request.PhoneNumber, out RegisterRequestDTO cachedRequest))
            {
                return BadRequest(new { Message = "Phiên đăng ký đã hết hạn. Vui lòng đăng ký lại" });
            }

            // Hash password (Nên dùng BCrypt.Net hoặc thư viện hash tương tự)
            // Tạm thời ở đây gán thẳng hoặc bạn có thể tự thêm hàm Hash
            var hashedPassword = cachedRequest.Password; // TODO: Implement Hashing

            // Tạo User chính thức
            var newUser = new User
            {
                PhoneNumber = cachedRequest.PhoneNumber,
                UserName = cachedRequest.UserName,
                Email = cachedRequest.Email,
                Password = hashedPassword,
                AccountStatus = 1, // Kích hoạt
                IsActive = true
                // Khởi tạo các trường khác nếu cần
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Xóa thông tin tạm khỏi cache
            _cache.Remove(request.PhoneNumber);

            return Ok(new { Message = "Đăng ký tài khoản thành công!", UserId = newUser.UserID });
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
