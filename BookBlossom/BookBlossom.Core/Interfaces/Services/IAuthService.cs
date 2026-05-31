using BookBlossom.Core.DTOs.Auth;
using BookBlossom.Core.DTOs;
using System.Threading.Tasks;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface IAuthService
    {
        // Hàm đăng nhập trả về thông tin Token
        Task<AuthResponseDTO> LoginAsync(LoginRequestDTO request, string ipAddress);
        Task<AuthResponseDTO> RefreshTokenAsync(TokenRequestDTO request);
        
        // Hàm hoàn tất đăng ký sau khi xác thực OTP thành công
        Task<bool> CompleteRegistrationAsync(RegisterRequestDTO request);

        // Hàm đặt lại mật khẩu sau khi xác thực thành công
        Task<bool> ResetPasswordAsync(string phoneNumber, string newPassword);
    }
}