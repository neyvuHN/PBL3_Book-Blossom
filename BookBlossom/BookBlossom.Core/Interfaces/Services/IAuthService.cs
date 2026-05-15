using BookBlossom.Core.DTOs.Auth;
using BookBlossom.Core.DTOs;
using System.Threading.Tasks;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface IAuthService
    {
        // Hàm đăng nhập trả về thông tin Token
        Task<AuthResponseDTO> LoginAsync(LoginRequestDTO request);
        Task<AuthResponseDTO> RefreshTokenAsync(TokenRequestDTO request);
        
        // Hàm hoàn tất đăng ký sau khi xác thực OTP thành công
        Task<bool> CompleteRegistrationAsync(RegisterRequestDTO request);
    }
}