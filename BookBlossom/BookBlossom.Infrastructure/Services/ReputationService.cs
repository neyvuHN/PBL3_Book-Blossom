using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;

namespace BookBlossom.Infrastructure.Services
{
    public class ReputationService : IReputationService
    {
        private readonly ApplicationDbContext _context;

        public ReputationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CustomerReputation> GetReputationWithRankAsync(long customerId)
        {
            var reputation = await _context.CustomerReputations
                .Include(r => r.MembershipRank)
                .FirstOrDefaultAsync(r => r.CustomerID == customerId);

            if (reputation == null)
            {
                // Khi tạo tài khoản mới: Mặc định 100 điểm và Rank thấp nhất (Đồng)
                var defaultRank = await _context.MembershipRanks.OrderBy(r => r.MinSpending).FirstOrDefaultAsync();
                
                reputation = new CustomerReputation
                {
                    CustomerID = customerId,
                    ReputationPoint = 100,
                    RankID = defaultRank?.RankID
                };
                _context.CustomerReputations.Add(reputation);
                await _context.SaveChangesAsync();
            }
            return reputation;
        }

        public async Task HandleReputationChangeAsync(long customerId, ReputationAction action)
        {
            var reputation = await GetReputationWithRankAsync(customerId);
            int currentPoint = reputation.ReputationPoint ?? 100;

            // 1. Phân tích điểm cộng/trừ theo bảng nghiệp vụ tài liệu cung cấp
            int pointChange = action switch
            {
                ReputationAction.AccountCreation => 100,
                ReputationAction.CodDeliverySuccess => 2,        // Nhận hàng thành công (COD)
                ReputationAction.OnlinePaymentSuccess => 5,      // Thanh toán trước (Online)
                ReputationAction.ReviewWithMedia => 2,           // Review có ảnh + video
                ReputationAction.QualityReviewBonus => 5,        // Review chất lượng (>= 5 like)
                ReputationAction.ReportedTrue => -10,            // Bị Report đúng
                ReputationAction.ShopPackedCancellation => -5,   // Hủy đơn sau khi Shop đã đóng gói
                ReputationAction.OrderBombed => -25,             // "Bom" hàng
                ReputationAction.ReviewThreadDeleted => -5,      // Bị xóa bài reviews/threads
                _ => 0
            };

            int newPoint = currentPoint + pointChange;

            // Trần điểm tối đa (Max Score) = 150
            if (newPoint > 150) newPoint = 150;
            if (newPoint < 0) newPoint = 0;

            reputation.ReputationPoint = newPoint;
            await _context.SaveChangesAsync();

            // 2. Kiểm tra Hệ quả từ các Ngưỡng điểm nghiêm ngặt
            await EnforceReputationThresholdsAsync(customerId, newPoint);
        }

        private async Task EnforceReputationThresholdsAsync(long customerId, int score)
        {
            // Ngưỡng C (< 30 Điểm): Khai trừ / Khóa tài khoản vĩnh viễn
            if (score < 30)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == customerId);
                if (user != null)
                {
                    user.IsActive = true; // Khóa tài khoản vĩnh viễn
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateCustomerRankAsync(long customerId)
        {
            var reputation = await GetReputationWithRankAsync(customerId);

            // Phân biệt nghiệp vụ: Điểm uy tín chỉ cộng khi Đơn hàng đạt trạng thái "Completed" 
            // (Tức là sau thời hạn 7 ngày tự động hoặc khách xác nhận và không khiếu nại)
            var totalSpent = await _context.Orders
                .Where(o => o.CustomerID == customerId && o.OrderStatus == OrderStatus.Completed)
                .SumAsync(o => o.TotalAmount);

            // Tìm cấu hình Rank động dựa trên tổng chi tiêu thực tế
            var suitableRank = await _context.MembershipRanks
                .Where(r => totalSpent >= r.MinSpending)
                .OrderByDescending(r => r.MinSpending)
                .FirstOrDefaultAsync();

            if (suitableRank != null && reputation.RankID != suitableRank.RankID)
            {
                reputation.RankID = suitableRank.RankID;
            }

            await _context.SaveChangesAsync();
        }
    }
}