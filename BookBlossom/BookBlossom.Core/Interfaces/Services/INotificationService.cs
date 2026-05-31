using System.Collections.Generic;
using System.Threading.Tasks;
using BookBlossom.Core.DTOs.Notification;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationDTO>> GetUserNotificationsAsync(long userId, bool? isRead);
        Task<bool> MarkAsReadAsync(long userId, long notificationId);
        Task<bool> MarkAllAsReadAsync(long userId);
        Task CreateAndSendNotificationAsync(long userId, string title, string content, NotificationType type, int? referenceId);
        Task<bool> SubscribeAsync(long customerId, long targetId, string targetType);
        Task<bool> UnsubscribeAsync(long customerId, long targetId, string targetType);
        Task<IEnumerable<SubscriptionDTO>> GetSubscriptionsAsync(long customerId);
    }
}
