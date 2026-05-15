using System.Threading.Tasks;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface IOTPService
    {
        Task<string> GenerateOtpAsync(string phoneNumber, string ipAddress);
        Task<bool> VerifyOtpAsync(string phoneNumber, string otpCode);
    }
}
