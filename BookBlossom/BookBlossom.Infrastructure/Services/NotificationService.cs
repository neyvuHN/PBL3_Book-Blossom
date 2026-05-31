using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BookBlossom.Core.Entities;
using BookBlossom.Core.DTOs.Notification;
using BookBlossom.Core.Enums;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;

namespace BookBlossom.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationPublisher _publisher;

        public NotificationService(ApplicationDbContext context, INotificationPublisher publisher)
        {
            _context = context;
            _publisher = publisher;
        }

        public async Task<IEnumerable<NotificationDTO>> GetUserNotificationsAsync(long userId, bool? isRead)
        {
            var query = _context.UserNotifications
                .Where(n => n.UserID == userId)
                .AsQueryable();

            if (isRead.HasValue)
            {
                query = query.Where(n => n.IsRead == isRead.Value);
            }

            var notifications = await query
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();

            return notifications.Select(n => new NotificationDTO
            {
                NotificationID = n.NotificationID,
                UserID = n.UserID,
                Title = n.Title,
                Content = n.Content,
                NotificationType = n.NotificationType,
                ReferenceID = n.ReferenceID,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedDate
            });
        }

        public async Task<bool> MarkAsReadAsync(long userId, long notificationId)
        {
            var notification = await _context.UserNotifications
                .FirstOrDefaultAsync(n => n.NotificationID == notificationId && n.UserID == userId);

            if (notification == null) return false;

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                _context.UserNotifications.Update(notification);
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(long userId)
        {
            var unreadNotifications = await _context.UserNotifications
                .Where(n => n.UserID == userId && !n.IsRead)
                .ToListAsync();

            if (!unreadNotifications.Any()) return true;

            foreach (var n in unreadNotifications)
            {
                n.IsRead = true;
            }

            _context.UserNotifications.UpdateRange(unreadNotifications);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task CreateAndSendNotificationAsync(long userId, string title, string content, NotificationType type, int? referenceId)
        {
            // Verify user exists
            var userExists = await _context.Users.AnyAsync(u => u.UserID == userId);
            if (!userExists) return;

            var notification = new UserNotification
            {
                UserID = userId,
                Title = title,
                Content = content,
                NotificationType = type,
                ReferenceID = referenceId,
                IsRead = false,
                CreatedDate = DateTime.UtcNow
            };

            _context.UserNotifications.Add(notification);
            await _context.SaveChangesAsync();

            var dto = new NotificationDTO
            {
                NotificationID = notification.NotificationID,
                UserID = notification.UserID,
                Title = notification.Title,
                Content = notification.Content,
                NotificationType = notification.NotificationType,
                ReferenceID = notification.ReferenceID,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedDate
            };

            try
            {
                await _publisher.PublishNotificationAsync(userId, dto);
            }
            catch
            {
                // Suppress publishing errors
            }
        }

        public async Task<bool> SubscribeAsync(long customerId, long targetId, string targetType)
        {
            if (!Enum.TryParse<SubscriptionTargetType>(targetType, true, out var targetTypeEnum))
            {
                throw new ArgumentException("Loại đối tượng theo dõi không hợp lệ.");
            }

            // Validate target exists
            if (targetTypeEnum == SubscriptionTargetType.Thread)
            {
                var exists = await _context.Users.AnyAsync(u => u.UserID == targetId);
                if (!exists) throw new KeyNotFoundException("Không tìm thấy người dùng được theo dõi.");
            }
            else if (targetTypeEnum == SubscriptionTargetType.Book)
            {
                var exists = await _context.RealBooks.AnyAsync(b => b.BookID == targetId);
                if (!exists) throw new KeyNotFoundException("Không tìm thấy sách được theo dõi.");
            }
            else if (targetTypeEnum == SubscriptionTargetType.Order)
            {
                var exists = await _context.Orders.AnyAsync(o => o.OrderID == targetId);
                if (!exists) throw new KeyNotFoundException("Không tìm thấy đơn hàng được theo dõi.");
            }
            else if (targetTypeEnum == SubscriptionTargetType.Badge)
            {
                // Badge entity is not defined in the C# model. Bypass verification.
                await Task.CompletedTask;
            }
            else if (targetTypeEnum == SubscriptionTargetType.Report)
            {
                var exists = await _context.Reports.AnyAsync(r => r.ReportID == targetId);
                if (!exists) throw new KeyNotFoundException("Không tìm thấy báo cáo được theo dõi.");
            }
            else if (targetTypeEnum == SubscriptionTargetType.ReturnRequest)
            {
                var exists = await _context.ReturnRequests.AnyAsync(r => r.ReturnRequestID == targetId);
                if (!exists) throw new KeyNotFoundException("Không tìm thấy yêu cầu trả hàng được theo dõi.");
            }

            var isAlreadySubscribed = await _context.Subscriptions
                .AnyAsync(s => s.CustomerID == customerId && s.TargetID == targetId && s.TargetType == targetTypeEnum);

            if (isAlreadySubscribed) return true;

            var subscription = new Subscription
            {
                CustomerID = customerId,
                TargetID = targetId,
                TargetType = targetTypeEnum,
                CreatedDate = DateTime.UtcNow
            };

            _context.Subscriptions.Add(subscription);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UnsubscribeAsync(long customerId, long targetId, string targetType)
        {
            if (!Enum.TryParse<SubscriptionTargetType>(targetType, true, out var targetTypeEnum))
            {
                throw new ArgumentException("Loại đối tượng theo dõi không hợp lệ.");
            }

            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.CustomerID == customerId && s.TargetID == targetId && s.TargetType == targetTypeEnum);

            if (subscription == null) return false;

            _context.Subscriptions.Remove(subscription);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<SubscriptionDTO>> GetSubscriptionsAsync(long customerId)
        {
            var subscriptions = await _context.Subscriptions
                .Where(s => s.CustomerID == customerId)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();

            if (!subscriptions.Any()) return Enumerable.Empty<SubscriptionDTO>();

            // Collect target IDs by type to batch fetch names
            var userIds = subscriptions.Where(s => s.TargetType == SubscriptionTargetType.Thread).Select(s => s.TargetID).Distinct().ToList();
            var bookIds = subscriptions.Where(s => s.TargetType == SubscriptionTargetType.Book).Select(s => s.TargetID).Distinct().ToList();
 
            var userNames = await _context.Users
                .Where(u => userIds.Contains(u.UserID))
                .ToDictionaryAsync(u => u.UserID, u => $"{u.LastName} {u.FirstName}".Trim());
 
            var bookTitles = await _context.RealBooks
                .Where(b => bookIds.Contains(b.BookID))
                .ToDictionaryAsync(b => b.BookID, b => b.Title);
 
            var result = new List<SubscriptionDTO>();
 
            foreach (var s in subscriptions)
            {
                string targetName = string.Empty;
                if (s.TargetType == SubscriptionTargetType.Thread && userNames.TryGetValue(s.TargetID, out var uName))
                {
                    targetName = string.IsNullOrEmpty(uName) ? "Người dùng ẩn danh" : uName;
                }
                else if (s.TargetType == SubscriptionTargetType.Book && bookTitles.TryGetValue(s.TargetID, out var bTitle))
                {
                    targetName = bTitle;
                }
                else
                {
                    targetName = $"ID: {s.TargetID}";
                }

                result.Add(new SubscriptionDTO
                {
                    FollowID = s.FollowID,
                    CustomerID = s.CustomerID,
                    TargetID = s.TargetID,
                    TargetType = s.TargetType.ToString(),
                    TargetName = targetName,
                    CreatedAt = s.CreatedDate
                });
            }

            return result;
        }
    }
}
