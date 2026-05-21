using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using BookBlossom.Core.Entities;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Core.DTOs.Auth;
using BookBlossom.Core.DTOs;
using BookBlossom.Core.Enums;
using BookBlossom.Core.Exceptions;
using BookBlossom.Infrastructure.Data;

namespace BookBlossom.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IGuestService _guestService;

        public AuthService(ApplicationDbContext context, IConfiguration configuration, IGuestService guestService)
        {
            _context = context;
            _configuration = configuration;
            _guestService = guestService;
        }

        public async Task<AuthResponseDTO> LoginAsync(LoginRequestDTO request)
        {
            var user = await _context.Users
                .Include(u => u.CustomerDetail)
                .FirstOrDefaultAsync(u => u.UserName == request.UserName || u.PhoneNumber == request.UserName);
            
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                throw new UnauthorizedActionException("Tên đăng nhập, số điện thoại hoặc mật khẩu không chính xác.");

            if (user.AccountStatus != AccountStatus.Active)
                throw new UnauthorizedActionException("Tài khoản của bạn đã bị khóa hoặc chưa được xác thực.");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.RoleID.ToString()),
                new Claim("AccountStatus", ((int)user.AccountStatus).ToString())
            };

            var token = CreateJwtToken(claims);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7);
            await _context.SaveChangesAsync();

            bool isOnboarding = true; 
            if (user.RoleID == UserRole.Customer)
            {
                if (user.CustomerDetail == null)
                {
                    var customerDetail = new CustomerDetail
                    {
                        CustomerID = user.UserID,
                        IsOnboardingCompleted = false,
                        TotalSpending = 0,
                        DailyUndoCount = 0,
                        CurrentMonthThreadCount = 0,
                        CurrentOrderStreak = 0
                    };
                    _context.CustomerDetails.Add(customerDetail);
                    await _context.SaveChangesAsync();
                    user.CustomerDetail = customerDetail;
                }
                isOnboarding = user.CustomerDetail.IsOnboardingCompleted;
            }

            return new AuthResponseDTO 
            { 
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = refreshToken,
                UserId = user.UserID,
                UserName = user.UserName, 
                RoleID = user.RoleID,
                IsOnboardingCompleted = isOnboarding
            };
        }

        public async Task<bool> CompleteRegistrationAsync(RegisterRequestDTO request)
        {
            if (await _context.Users.AnyAsync(u => u.UserName == request.UserName))
            {
                throw new Exception("Tên đăng nhập đã tồn tại trong hệ thống.");
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var newUser = new User
            {
                UserName = request.UserName,
                Password = hashedPassword,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                LastName = request.LastName,
                FirstName = request.FirstName,
                Avatar = request.Avatar,
                Gender = request.Gender,
                Birthday = request.Birthday,
                RoleID = UserRole.Customer,
                AccountStatus = AccountStatus.Active,
                IsActive = true
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            
            var customerDetail = new CustomerDetail
            {
                CustomerID = newUser.UserID,
                IsOnboardingCompleted = false,
                TotalSpending = 0,
                DailyUndoCount = 0,
                CurrentMonthThreadCount = 0,
                CurrentOrderStreak = 0
            };
            _context.CustomerDetails.Add(customerDetail);

            await _context.SaveChangesAsync();

            // Nếu có GuestID, thực hiện migrate dữ liệu sang User mới
            if (request.GuestID.HasValue)
            {
                await _guestService.MigrateGuestDataToUserAsync(request.GuestID.Value, newUser.UserID);
            }

            return true;
        }

        public async Task<AuthResponseDTO> RefreshTokenAsync(TokenRequestDTO request)
        {
            string accessToken = request.AccessToken;
            string refreshToken = request.RefreshToken;

            var principal = GetPrincipalFromExpiredToken(accessToken);
            if (principal == null)
            {
                throw new Exception("Access Token không hợp lệ.");
            }

            var userName = principal.Identity.Name; 
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);

            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
            {
                throw new Exception("Refresh Token không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại.");
            }

            var newAccessToken = CreateJwtToken(principal.Claims.ToList());
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            await _context.SaveChangesAsync();

            return new AuthResponseDTO
            {
                Token = new JwtSecurityTokenHandler().WriteToken(newAccessToken),
                RefreshToken = newRefreshToken,
                UserId = user.UserID,
                UserName = user.UserName,
                RoleID = user.RoleID
            };
        }

        private JwtSecurityToken CreateJwtToken(List<Claim> claims)
        {
            var keyStr = _configuration["Jwt:Key"] ?? "Nuocmatemroitrochoiketthuc_BookBlossom_Security_Key_2026";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            return new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(15), 
                signingCredentials: creds
            );
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Token không đúng định dạng an toàn.");
            }

            return principal;
        }

        public async Task<bool> ResetPasswordAsync(string phoneNumber, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
            if (user == null)
            {
                throw new Exception("Không tìm thấy người dùng với số điện thoại này.");
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}