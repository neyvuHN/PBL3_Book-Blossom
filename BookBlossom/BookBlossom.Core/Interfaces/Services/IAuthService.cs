using BookBlossom.Core.DTOs.Auth;
using System.Threading.Tasks;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface IAuthService
    {
        // Hàm đăng nhập trả về thông tin Token
        Task<AuthResponseDTO> LoginAsync(LoginRequestDTO request);
        Task<AuthResponseDTO> RefreshTokenAsync(TokenRequestDTO request);
        
        // Hàm đăng ký (thêm vào để luồng Auth được hoàn chỉnh)
        Task<bool> RegisterAsync(RegisterRequestDTO request);
    }
}