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

        public async Task HandleReputationChangeAsync(long customerId, ReputationAction action, string reason)
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
                ReputationAction.StreakBonusLvl1 => 10,          // Đạt Streak 3 lần 1 trong tháng: +10
                ReputationAction.StreakBonusLvl2 => 5,           // Đạt Streak 3 lần 2 trong tháng: +5
                ReputationAction.StreakBonusLvl3 => 2,           // Đạt Streak 3 lần 3 trong tháng: +2
                _ => 0
            };

            int newPoint = Math.Clamp(currentPoint + pointChange, 0, 150);
            reputation.ReputationPoint = newPoint;

            // LƯU LỊCH SỬ
            var history = new ReputationHistory
            {
                CustomerID = customerId,
                ChangeAmount = pointChange,
                Reason = reason,
                ReferenceType = (byte)action,
                CreateAt = DateTime.UtcNow
            };
            _context.ReputationHistories.Add(history);

            if (newPoint < 30)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == customerId);
                if (user != null && user.IsActive == true) 
                {
                    user.IsActive = false;
                }
            }
    
            await _context.SaveChangesAsync();
            await EnforceReputationThresholdsAsync(customerId, newPoint);
        }

        public async Task UpdateCustomerRankAsync(long customerId)
        {
            // 1. Lấy thông tin uy tín hiện tại
            var reputation = await GetReputationWithRankAsync(customerId);

            // 2. Tính tổng chi tiêu từ các đơn hàng "Completed"
            var totalSpent = await _context.Orders
                .Where(o => o.CustomerID == customerId && o.OrderStatus == OrderStatus.Completed)
                .SumAsync(o => o.TotalAmount);

            // 3. Tìm Rank phù hợp nhất (dựa trên MinSpending)
            var suitableRank = await _context.MembershipRanks
                .Where(r => totalSpent >= (r.MinSpending ?? 0)) // Thêm xử lý null cho MinSpending
                .OrderByDescending(r => r.MinSpending)
                .FirstOrDefaultAsync();

            // 4. Kiểm tra và cập nhật nếu có thay đổi
            if (suitableRank != null && reputation.RankID != suitableRank.RankID)
            {
                reputation.RankID = suitableRank.RankID;
                
                // BỔ SUNG: Nên lưu lại lịch sử khi thăng/giáng hạng để người dùng theo dõi
                _context.ReputationHistories.Add(new ReputationHistory
                {
                    CustomerID = customerId,
                    ChangeAmount = 0, // Không cộng điểm, chỉ thay đổi rank
                    Reason = $"Đã thăng/giáng hạng thành viên lên {suitableRank.RankType}", // Giả sử RankType là tên hoặc mã hạng
                    ReferenceType = (byte)ReputationAction.RankUpdate, // Bạn cần thêm enum này vào
                    CreateAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }
        }

        private async Task EnforceReputationThresholdsAsync(long customerId, int score)
        {
            if (score < 30)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == customerId);
                if (user != null)
                {
                    user.IsActive = false; // ĐÃ SỬA: false để vô hiệu hóa tài khoản
                }
                await _context.SaveChangesAsync();
            }
        }

        // Các hàm kiểm tra để gọi ở Controller hoặc Service khác
        public async Task<bool> CanCommentAsync(long customerId) 
        {
            var rep = await GetReputationWithRankAsync(customerId);
            return rep.ReputationPoint >= 80; // Ngưỡng 80 để comment
        }

        public async Task<bool> CanUseCodAsync(long customerId) 
        {
            var rep = await GetReputationWithRankAsync(customerId);
            return rep.ReputationPoint >= 60; // Ngưỡng 60 để dùng COD
        }
    }
}