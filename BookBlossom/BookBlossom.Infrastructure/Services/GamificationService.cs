using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookBlossom.Core.DTOs;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookBlossom.Infrastructure.Services
{
    public class GamificationService : IGamificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GamificationService> _logger;
        private readonly INotificationService _notificationService;

        public GamificationService(ApplicationDbContext context, ILogger<GamificationService> logger, INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<IEnumerable<BadgeCustomerDTO>> GetCustomerBadgesAsync(long customerId)
        {
            // Auto update/refresh checks before returning customer badges
            try
            {
                await CheckAndGrantInteractionBadgesAsync(customerId);
                await CheckAndGrantShoppingBadgesAsync(customerId);
                await CheckAndGrantReputationBadgesAsync(customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tự động cập nhật kiểm tra Badge cho Customer ID: {customerId}");
            }

            return await _context.BadgeCustomers
                .Where(bc => bc.CustomerID == customerId)
                .Include(bc => bc.Badge)
                .Select(bc => new BadgeCustomerDTO
                {
                    BadgeID = bc.BadgeID,
                    BadgeName = bc.Badge.BadgeName,
                    IconPath = bc.Badge.IconPath,
                    ConditionDescription = bc.Badge.ConditionDescription,
                    EarnedDate = bc.EarnedDate
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<BadgeDTO>> GetAllBadgesAsync()
        {
            return await _context.Badges
                .Select(b => new BadgeDTO
                {
                    BadgeID = b.BadgeID,
                    BadgeName = b.BadgeName,
                    IconPath = b.IconPath,
                    ConditionDescription = b.ConditionDescription
                })
                .ToListAsync();
        }

        public async Task CheckAndGrantInteractionBadgesAsync(long customerId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.CustomerID == customerId && !r.IsHidden)
                .ToListAsync();

            // 1. Chiến thần Review Cấp 1: 5 reviews có ảnh
            var imageReviewsCount = reviews.Count(r => !string.IsNullOrEmpty(r.ImageVideoPath));
            if (imageReviewsCount >= 5)
            {
                await TryGrantBadgeAsync(customerId, 1);
            }

            // 2. Chiến thần Review Cấp 2: 20 quality reviews (likes >= 5)
            var qualityReviewsCount = reviews.Count(r => r.LikeCount >= 5);
            if (qualityReviewsCount >= 20)
            {
                await TryGrantBadgeAsync(customerId, 2);
            }

            // 3. Chiến thần Review Cấp 3: 50 quality reviews (likes >= 5) + Top Review của tháng
            if (qualityReviewsCount >= 50)
            {
                bool isTopReview = false;
                var qualityReviews = reviews.Where(r => r.LikeCount >= 5).ToList();
                foreach (var r in qualityReviews)
                {
                    var year = r.CreatedAt.Year;
                    var month = r.CreatedAt.Month;

                    var topMonthlyReviewIds = await _context.Reviews
                        .Where(x => x.CreatedAt.Year == year && x.CreatedAt.Month == month && !x.IsHidden)
                        .OrderByDescending(x => x.LikeCount)
                        .ThenByDescending(x => x.CreatedAt)
                        .Take(5)
                        .Select(x => x.ReviewID)
                        .ToListAsync();

                    if (topMonthlyReviewIds.Contains(r.ReviewID))
                    {
                        isTopReview = true;
                        break;
                    }
                }

                if (isTopReview)
                {
                    await TryGrantBadgeAsync(customerId, 3);
                }
            }

            // 4. Sứ giả Tri thức: chia sẻ từ 5 lần trở lên
            var customer = await _context.CustomerDetails.FindAsync(customerId);
            if (customer != null && customer.ShareCount >= 5)
            {
                await TryGrantBadgeAsync(customerId, 4);
            }
        }

        public async Task CheckAndGrantShoppingBadgesAsync(long customerId)
        {
            var completedOrders = await _context.Orders
                .Include(o => o.OrderDetails)
                .Where(o => o.CustomerID == customerId && o.OrderStatus == OrderStatus.Completed)
                .ToListAsync();

            var blindDateOrdersCount = completedOrders.Count(o => o.OrderDetails.Any(od => od.BlindBookID != null));

            // 1. Trùm Blind Date Cấp 1 (Tò mò): 3 đơn
            if (blindDateOrdersCount >= 3)
            {
                await TryGrantBadgeAsync(customerId, 5);
            }

            // 2. Trùm Blind Date Cấp 2 (Kẻ săn tin): 10 đơn
            if (blindDateOrdersCount >= 10)
            {
                await TryGrantBadgeAsync(customerId, 6);
            }

            // 3. Trùm Blind Date Cấp 3 (Định mệnh): 25 đơn
            if (blindDateOrdersCount >= 25)
            {
                await TryGrantBadgeAsync(customerId, 7);
            }

            // 4. Mọt sách chính hiệu: mua >= 5 thể loại khác nhau
            var bookIds = completedOrders
                .SelectMany(o => o.OrderDetails)
                .Select(od => od.BookID)
                .Distinct()
                .ToList();

            if (bookIds.Any())
            {
                var categoryCount = await _context.RealBooks
                    .Where(rb => bookIds.Contains(rb.BookID))
                    .Select(rb => rb.CategoryID)
                    .Distinct()
                    .CountAsync();

                if (categoryCount >= 5)
                {
                    await TryGrantBadgeAsync(customerId, 8);
                }
            }
        }

        public async Task CheckAndGrantReputationBadgesAsync(long customerId)
        {
            // 1. Người dùng gương mẫu: Max Reputation (150) liên tục 3 tháng
            var reputation = await _context.Set<CustomerReputation>()
                .FirstOrDefaultAsync(cr => cr.CustomerID == customerId);

            if (reputation != null)
            {
                if (reputation.ReputationPoint == 150)
                {
                    if (reputation.ReputationMaxStreakStartDate == null)
                    {
                        reputation.ReputationMaxStreakStartDate = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }

                    var duration = DateTime.UtcNow - reputation.ReputationMaxStreakStartDate.Value;
                    if (duration.TotalDays >= 90)
                    {
                        await TryGrantBadgeAsync(customerId, 9);
                    }
                }
                else
                {
                    if (reputation.ReputationMaxStreakStartDate != null)
                    {
                        reputation.ReputationMaxStreakStartDate = null;
                        await _context.SaveChangesAsync();
                    }
                }
            }

            // 2. Cánh tay đắc lực: > 10 reports vi phạm chính xác
            var accurateReportsCount = await _context.Reports
                .CountAsync(r => r.CustomerID == customerId && r.IsAccurate == true);

            if (accurateReportsCount > 10)
            {
                await TryGrantBadgeAsync(customerId, 10);
            }
        }

        public async Task RecordShareActionAsync(long customerId)
        {
            var customer = await _context.CustomerDetails.FindAsync(customerId);
            if (customer == null)
            {
                throw new KeyNotFoundException("Không tìm thấy thông tin chi tiết khách hàng.");
            }

            customer.ShareCount += 1;
            await _context.SaveChangesAsync();

            // Kích hoạt kiểm tra huy hiệu sau khi chia sẻ thành công
            await CheckAndGrantInteractionBadgesAsync(customerId);
        }

        private async Task TryGrantBadgeAsync(long customerId, long badgeId)
        {
            var exists = await _context.BadgeCustomers
                .AnyAsync(bc => bc.CustomerID == customerId && bc.BadgeID == badgeId);

            if (!exists)
            {
                var badge = await _context.Badges.FindAsync(badgeId);
                if (badge != null)
                {
                    var badgeCustomer = new BadgeCustomer
                    {
                        CustomerID = customerId,
                        BadgeID = badgeId,
                        EarnedDate = DateTime.UtcNow
                    };

                    _context.BadgeCustomers.Add(badgeCustomer);
                    await _context.SaveChangesAsync();

                    try
                    {
                        await _notificationService.CreateAndSendNotificationAsync(
                            customerId,
                            "Bạn đã nhận huy hiệu mới!",
                            $"Chúc mừng! Bạn đã đạt được huy hiệu: {badge.BadgeName}",
                            NotificationType.BadgeEarned,
                            (int)badgeId
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Lỗi khi gửi thông báo nhận huy hiệu cho Customer ID {customerId}");
                    }
                }
            }
        }
    }
}
