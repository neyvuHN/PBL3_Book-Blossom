using System.Threading.Tasks;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface ISMSService
    {
        Task SendSmsAsync(string phoneNumber, string message);
    }
}
