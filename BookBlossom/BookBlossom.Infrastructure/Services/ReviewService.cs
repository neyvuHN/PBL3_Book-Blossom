// File: Infrastructure/Services/ReviewService.cs
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums; // Đảm bảo import namespace chứa OrderStatus
using BookBlossom.Core.DTOs.Review;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;

namespace BookBlossom.Infrastructure.Services
{
    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReviewService> _logger;
        private readonly IGamificationService _gamificationService;
        private readonly string _uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "reviews");

        public ReviewService(ApplicationDbContext context, ILogger<ReviewService> logger, IGamificationService gamificationService)
        {
            _context = context;
            _logger = logger;
            _gamificationService = gamificationService;
            
            if (!Directory.Exists(_uploadFolder))
            {
                Directory.CreateDirectory(_uploadFolder);
            }
        }

        public async Task<ReviewDTO> CreateReviewAsync(long customerId, CreateReviewDTO dto, List<IFormFile>? mediaFiles)
        {
            // 1. KIỂM TRA ĐƠN HÀNG (Security & Authorization)
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderID == dto.OrderID && o.CustomerID == customerId);

            if (order == null)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền đánh giá đơn hàng này hoặc đơn hàng không tồn tại.");
            }

            // SỬA LỖI CS0019: So sánh chính xác thuộc tính Enum với OrderStatus.Completed
            if (order.OrderStatus != OrderStatus.Completed) 
            {
                throw new InvalidOperationException("Bạn chỉ có thể đánh giá sản phẩm sau khi đơn hàng đã hoàn thành và giao công.");
            }

            // 2. KIỂM TRA THỜI HẠN 30 NGÀY
            // Ép kiểu hoặc xử lý Nullable nếu trường DeliveredDate trong DB cho phép null
            if (order.DeliveredDate == null)
            {
                throw new InvalidOperationException("Không tìm thấy thông tin ngày nhận hàng của đơn hàng này.");
            }

            var daysSinceDelivery = (DateTime.UtcNow - order.DeliveredDate.Value).TotalDays;
            if (daysSinceDelivery > 30)
            {
                throw new InvalidOperationException("Đã quá thời hạn 30 ngày cho phép đánh giá sản phẩm.");
            }

            // 3. CHỐNG SPAM (Mỗi người dùng chỉ được review sách này 1 lần trong đơn hàng này)
            var isAlreadyReviewed = await _context.Reviews.AnyAsync(r => 
                r.CustomerID == customerId && 
                ((dto.BookID.HasValue && r.BookID == dto.BookID) || (dto.BlindBookID.HasValue && r.BlindBookID == dto.BlindBookID))
            );

            if (isAlreadyReviewed)
            {
                throw new InvalidOperationException("Bạn đã gửi đánh giá cho sản phẩm này trước đó rồi.");
            }

            // 4. XỬ LÝ UPLOAD FILE MEDIA (Đã giải quyết lỗi CS0103 bằng cách gọi hàm private phía dưới)
            List<string> savedPaths = await ProcessMediaUploadsAsync(mediaFiles);

            // 5. LƯU REVIEW VÀO DATABASE
            var review = new Review
            {
                CustomerID = customerId,
                BookID = dto.BookID,
                BlindBookID = dto.BlindBookID,
                Rating = dto.Rating,
                Content = dto.Content,
                ImageVideoPath = savedPaths.Any() ? string.Join(",", savedPaths) : null,
                LikeCount = 0,
                CreatedAt = DateTime.UtcNow,
                IsHidden = false,
                IsReputationAwarded = false 
            };

            _context.Reviews.Add(review);
            
            // TÍNH TOÁN LẠI ĐIỂM TRUNG BÌNH CỦA SÁCH (Nghiệp vụ mở rộng tương lai)

            await _context.SaveChangesAsync();

            try
            {
                await _gamificationService.CheckAndGrantInteractionBadgesAsync(customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi kiểm tra Badge sau khi tạo review cho Customer ID: {customerId}");
            }

            // Lấy lại đầy đủ thông tin kèm User để trả về Client hiển thị đầy đủ
            var createdReview = await _context.Reviews
                .Include(r => r.User)
                .FirstAsync(r => r.ReviewID == review.ReviewID);

            return MapToReviewDTO(createdReview);
        }

        public async Task<int> LikeReviewAsync(long customerId, long reviewId)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null || review.IsHidden)
            {
                throw new KeyNotFoundException("Không tìm thấy bài đánh giá hoặc bài đánh giá đã bị ẩn.");
            }

            review.LikeCount += 1;
            await _context.SaveChangesAsync();

            try
            {
                await _gamificationService.CheckAndGrantInteractionBadgesAsync(review.CustomerID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi kiểm tra Badge sau khi like review cho Customer ID: {review.CustomerID}");
            }

            return review.LikeCount;
        }

        public async Task<bool> HideReviewAsync(long reviewId, bool isHidden)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null) return false;

            review.IsHidden = isHidden;
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<ReviewDTO>> GetReviewsByBookAsync(long? bookId, long? blindBookId)
        {
            var query = _context.Reviews
                .Include(r => r.User)
                .Where(r => !r.IsHidden)
                .AsQueryable();

            if (bookId.HasValue)
            {
                query = query.Where(r => r.BookID == bookId.Value);
            }
            if (blindBookId.HasValue)
            {
                query = query.Where(r => r.BlindBookID == blindBookId.Value);
            }

            var results = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            return results.Select(MapToReviewDTO);
        }

        // ĐỊNH NGHĨA HÀM XỬ LÝ UPLOAD ĐỂ FIX LỖI CS0103
        private async Task<List<string>> ProcessMediaUploadsAsync(List<IFormFile>? mediaFiles)
        {
            List<string> savedPaths = new List<string>();
            if (mediaFiles != null && mediaFiles.Count > 0)
            {
                foreach (var file in mediaFiles)
                {
                    if (file.Length == 0) continue;

                    var extension = Path.GetExtension(file.FileName).ToLower();
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".mov" };
                    if (!allowedExtensions.Contains(extension))
                    {
                        throw new ArgumentException("Hệ thống chỉ chấp nhận định dạng hình ảnh (.jpg, .png...) hoặc video (.mp4, .mov).");
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
            return savedPaths;
        }

        private static ReviewDTO MapToReviewDTO(Review r)
        {
            return new ReviewDTO
            {
                ReviewID = r.ReviewID,
                CustomerID = r.CustomerID,
                CustomerName = r.User != null ? $"{r.User.LastName} {r.User.FirstName}".Trim() : "Người dùng BookBlossom",
                CustomerAvatar = r.User?.Avatar,
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