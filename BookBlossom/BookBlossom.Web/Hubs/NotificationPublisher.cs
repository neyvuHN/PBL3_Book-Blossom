using BookBlossom.Core.DTOs.Notification;
using BookBlossom.Core.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace BookBlossom.Web.Hubs
{
    public class NotificationPublisher : INotificationPublisher
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationPublisher(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task PublishNotificationAsync(long userId, NotificationDTO notification)
        {
            // SignalR maps Context.UserIdentifier to ClaimTypes.NameIdentifier
            // We send the notification DTO to the client
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", notification);
        }
    }
}
