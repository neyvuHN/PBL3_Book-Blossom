using System.Threading.Tasks;
using BookBlossom.Core.DTOs.Notification;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface INotificationPublisher
    {
        Task PublishNotificationAsync(long userId, NotificationDTO notification);
    }
}
