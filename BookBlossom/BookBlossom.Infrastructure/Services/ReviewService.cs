using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using BookBlossom.Core.DTOs.Review;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;

namespace BookBlossom.Infrastructure.Services
{
    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReviewService> _logger;
        private readonly string _uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "reviews");

        public ReviewService(ApplicationDbContext context, ILogger<ReviewService> logger)
        {
            _context = context;
            _logger = logger;
            if (!Directory.Exists(_uploadFolder))
            {
                Directory.CreateDirectory(_uploadFolder);
            }
        }

        public async Task<ReviewDTO> CreateReviewAsync(long customerId, CreateReviewDTO dto, List<IFormFile>? mediaFiles)
        {
            // 1. Kiểm tra đầu vào hợp lệ (hoặc BookID hoặc BlindBookID phải có)
            if (!dto.BookID.HasValue && !dto.BlindBookID.HasValue)
            {
                throw new ArgumentException("Đánh giá phải tương ứng với Sách thật hoặc Sách mù.");
            }

            // 2. Kiểm tra xem khách hàng đã mua và nhận hàng thành công trong vòng 30 ngày chưa
            var completedOrders = await _context.Orders
                .Include(o => o.OrderDetails)
                .Where(o => o.CustomerID == customerId 
                    && o.OrderStatus == OrderStatus.Completed
                    && o.CompletedDate.HasValue)
                .ToListAsync();

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var isEligible = completedOrders.Any(o => 
                o.CompletedDate.Value >= thirtyDaysAgo &&
                o.OrderDetails.Any(od => 
                    (dto.BookID.HasValue && od.BookID == dto.BookID.Value && !od.BlindBookID.HasValue) ||
                    (dto.BlindBookID.HasValue && od.BlindBookID == dto.BlindBookID.Value)
                ));

            if (!isEligible)
            {
                throw new InvalidOperationException("Bạn chỉ có thể đánh giá sách đã mua và nhận hàng thành công trong vòng 30 ngày.");
            }

            // 3. Xử lý tải ảnh/video
            var savedPaths = new List<string>();
            if (mediaFiles != null && mediaFiles.Count > 0)
            {
                foreach (var file in mediaFiles)
                {
                    if (file.Length == 0) continue;

                    var extension = Path.GetExtension(file.FileName).ToLower();
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".mov", ".avi", ".mkv" };
                    if (!allowedExtensions.Contains(extension))
                    {
                        throw new ArgumentException("Chỉ chấp nhận các tệp hình ảnh hoặc video (.jpg, .jpeg, .png, .gif, .mp4, .mov, .avi, .mkv).");
                    }

                    var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                    var absolutePath = Path.Combine(_uploadFolder, uniqueFileName);

                    using (var stream = new FileStream(absolutePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    savedPaths.Add($"/uploads/reviews/{uniqueFileName}");
                }
            }

            // 4. Tạo thực thể Review
            string? mediaPath = savedPaths.Any() ? string.Join(",", savedPaths) : null;
            var review = new Review
            {
                CustomerID = customerId,
                BookID = dto.BookID,
                BlindBookID = dto.BlindBookID,
                Rating = dto.Rating,
                Content = dto.Content,
                ImageVideoPath = mediaPath,
                LikeCount = 0,
                CreatedAt = DateTime.UtcNow,
                IsHidden = false,
                IsReputationAwarded = false
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // 5. Cộng điểm uy tín (+2 điểm nếu có ảnh/video)
            if (!string.IsNullOrEmpty(mediaPath))
            {
                await AddReputationPointsAsync(customerId, 2);
            }

            // 6. Đồng bộ hóa huy hiệu (Gamification)
            await SyncCustomerBadgesAsync(customerId);

            // Tải thông tin đầy đủ để trả về DTO
            var createdReview = await _context.Reviews
                .Include(r => r.Customer)
                .FirstAsync(r => r.ReviewID == review.ReviewID);

            return MapToReviewDTO(createdReview);
        }

        public async Task<ReviewDTO?> GetReviewByIdAsync(long reviewId)
        {
            var review = await _context.Reviews
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.ReviewID == reviewId);

            if (review == null) return null;
            return MapToReviewDTO(review);
        }

        public async Task<IEnumerable<ReviewDTO>> GetReviewsForBookAsync(long? bookId, long? blindBookId)
        {
            var query = _context.Reviews
                .Include(r => r.Customer)
                .Where(r => !r.IsHidden)
                .AsQueryable();

            if (bookId.HasValue)
            {
                query = query.Where(r => r.BookID == bookId.Value && !r.BlindBookID.HasValue);
            }
            else if (blindBookId.HasValue)
            {
                query = query.Where(r => r.BlindBookID == blindBookId.Value);
            }
            else
            {
                return new List<ReviewDTO>();
            }

            var list = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            return list.Select(MapToReviewDTO);
        }

        public async Task<ReviewDTO> UpdateReviewAsync(long customerId, long reviewId, UpdateReviewDTO dto)
        {
            var review = await _context.Reviews
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.ReviewID == reviewId);

            if (review == null)
            {
                throw new KeyNotFoundException("Không tìm thấy đánh giá.");
            }

            if (review.CustomerID != customerId)
            {
                throw new UnauthorizedAccessException("Bạn không phải là chủ sở hữu của đánh giá này.");
            }

            review.Rating = dto.Rating;
            review.Content = dto.Content;

            await _context.SaveChangesAsync();
            return MapToReviewDTO(review);
        }

        public async Task<bool> DeleteReviewAsync(long userId, UserRole role, long reviewId)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null) return false;

            // Chỉ chủ sở hữu hoặc Moderator/Admin mới được xóa
            if (review.CustomerID != userId && role != UserRole.Moderator && role != UserRole.SystemAdmin)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xóa đánh giá này.");
            }

            // Xóa các tệp đính kèm vật lý trên đĩa
            if (!string.IsNullOrEmpty(review.ImageVideoPath))
            {
                var paths = review.ImageVideoPath.Split(',');
                foreach (var path in paths)
                {
                    var relativePath = path.TrimStart('/');
                    var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);
                    if (File.Exists(absolutePath))
                    {
                        try
                        {
                            File.Delete(absolutePath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Không thể xóa tệp review đính kèm: {absolutePath}");
                        }
                    }
                }
            }

            _context.Reviews.Remove(review);
            var success = await _context.SaveChangesAsync() > 0;
            if (success)
            {
                await SyncCustomerBadgesAsync(review.CustomerID);
            }
            return success;
        }

        public async Task<bool> HideReviewAsync(long reviewId, bool isHidden)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null) return false;

            review.IsHidden = isHidden;
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<ReviewDTO> LikeReviewAsync(long reviewId)
        {
            var review = await _context.Reviews
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.ReviewID == reviewId);

            if (review == null)
            {
                throw new KeyNotFoundException("Không tìm thấy đánh giá.");
            }

            review.LikeCount += 1;
            await _context.SaveChangesAsync();

            // Trao điểm khi đạt 5 like lần đầu tiên
            if (review.LikeCount >= 5 && !review.IsReputationAwarded)
            {
                review.IsReputationAwarded = true;
                await _context.SaveChangesAsync();

                // +5 điểm uy tín cho tác giả review
                await AddReputationPointsAsync(review.CustomerID, 5);
            }

            // Đồng bộ lại huy hiệu cho tác giả review
            await SyncCustomerBadgesAsync(review.CustomerID);

            return MapToReviewDTO(review);
        }

        public async Task<int> GetCustomerReputationPointsAsync(long customerId)
        {
            var rep = await _context.CustomerReputations.FindAsync(customerId);
            return rep?.ReputationPoint ?? 100;
        }

        public async Task<IEnumerable<Badge>> GetCustomerBadgesAsync(long customerId)
        {
            await SyncCustomerBadgesAsync(customerId);

            var list = await _context.CustomerBadges
                .Include(cb => cb.Badge)
                .Where(cb => cb.CustomerID == customerId)
                .Select(cb => cb.Badge!)
                .ToListAsync();

            return list;
        }

        public async Task SyncCustomerBadgesAsync(long customerId)
        {
            // Lấy danh sách ID huy hiệu hiện có của user
            var currentBadgeIds = await _context.CustomerBadges
                .Where(cb => cb.CustomerID == customerId)
                .Select(cb => cb.BadgeID)
                .ToListAsync();

            // 1. Review Champion (lvl1: Explorer >= 1; lvl2: Critic >= 5; lvl3: Sage >= 15 reviews)
            var reviewCount = await _context.Reviews.CountAsync(r => r.CustomerID == customerId);
            if (reviewCount >= 1 && !currentBadgeIds.Contains(1)) await AwardBadgeAsync(customerId, 1);
            if (reviewCount >= 5 && !currentBadgeIds.Contains(2)) await AwardBadgeAsync(customerId, 2);
            if (reviewCount >= 15 && !currentBadgeIds.Contains(3)) await AwardBadgeAsync(customerId, 3);

            // 2. Knowledge Ambassador (đạt 5 like ở 1 review bất kỳ)
            var reviews = await _context.Reviews.Where(r => r.CustomerID == customerId).Select(r => r.LikeCount).ToListAsync();
            var maxLikes = reviews.Any() ? reviews.Max() : 0;
            if (maxLikes >= 5 && !currentBadgeIds.Contains(4)) await AwardBadgeAsync(customerId, 4);

            // 3. Blind Date Adventurer (lvl1: Curious >= 1, lvl2: Seeker >= 5, lvl3: Destiny >= 10 sách mù đã hoàn tất)
            var blindBookCount = await _context.OrderDetails
                .Include(od => od.Order)
                .Where(od => od.Order.CustomerID == customerId 
                    && od.Order.OrderStatus == OrderStatus.Completed 
                    && od.BlindBookID.HasValue)
                .SumAsync(od => od.Quantity);

            if (blindBookCount >= 1 && !currentBadgeIds.Contains(5)) await AwardBadgeAsync(customerId, 5);
            if (blindBookCount >= 5 && !currentBadgeIds.Contains(6)) await AwardBadgeAsync(customerId, 6);
            if (blindBookCount >= 10 && !currentBadgeIds.Contains(7)) await AwardBadgeAsync(customerId, 7);

            // 4. True Bookworm (mua ít nhất 10 cuốn sách)
            var totalBooksCount = await _context.OrderDetails
                .Include(od => od.Order)
                .Where(od => od.Order.CustomerID == customerId 
                    && od.Order.OrderStatus == OrderStatus.Completed)
                .SumAsync(od => od.Quantity);
            if (totalBooksCount >= 10 && !currentBadgeIds.Contains(8)) await AwardBadgeAsync(customerId, 8);

            // 5. Exemplary User (ReputationPoint >= 100)
            var reputation = await _context.CustomerReputations.FindAsync(customerId);
            var points = reputation?.ReputationPoint ?? 100;
            if (points >= 100 && !currentBadgeIds.Contains(9)) await AwardBadgeAsync(customerId, 9);

            // 6. Moderator Assistant (ReputationPoint >= 120)
            if (points >= 120 && !currentBadgeIds.Contains(10)) await AwardBadgeAsync(customerId, 10);
        }

        // --- HELPER METHODS ---

        private async Task AddReputationPointsAsync(long customerId, int points)
        {
            var rep = await _context.CustomerReputations.FindAsync(customerId);
            if (rep == null)
            {
                rep = new CustomerReputation
                {
                    CustomerID = customerId,
                    ReputationPoint = 100
                };
                _context.CustomerReputations.Add(rep);
            }
            rep.ReputationPoint = (rep.ReputationPoint ?? 100) + points;
            await _context.SaveChangesAsync();
        }

        private async Task AwardBadgeAsync(long customerId, long badgeId)
        {
            var badgeExists = await _context.Badges.AnyAsync(b => b.BadgeID == badgeId);
            if (!badgeExists) return;

            var cb = new CustomerBadge
            {
                CustomerID = customerId,
                BadgeID = badgeId,
                EarnedAt = DateTime.UtcNow
            };
            _context.CustomerBadges.Add(cb);
            await _context.SaveChangesAsync();
        }

        private static ReviewDTO MapToReviewDTO(Review r)
        {
            return new ReviewDTO
            {
                ReviewID = r.ReviewID,
                CustomerID = r.CustomerID,
                CustomerName = r.Customer != null ? $"{r.Customer.LastName} {r.Customer.FirstName}".Trim() : string.Empty,
                CustomerAvatar = r.Customer?.Avatar,
                BookID = r.BookID,
                BlindBookID = r.BlindBookID,
                Rating = r.Rating,
                Content = r.Content,
                ImageVideoPath = r.ImageVideoPath,
                LikeCount = r.LikeCount,
                CreatedAt = r.CreatedAt,
                IsHidden = r.IsHidden
            };
        }
    }
}
