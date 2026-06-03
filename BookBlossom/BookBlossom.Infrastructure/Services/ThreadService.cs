using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using BookBlossom.Core.DTOs.Thread;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;

namespace BookBlossom.Infrastructure.Services
{
    public class ThreadService : IThreadService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ThreadService> _logger;
        private readonly INotificationService _notificationService;
        private readonly IReputationService _reputationService;
        private readonly IGamificationService? _gamificationService;
        private readonly string _uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "threads");

        public ThreadService(
            ApplicationDbContext context,
            ILogger<ThreadService> logger,
            INotificationService notificationService,
            IReputationService reputationService,
            IGamificationService? gamificationService = null)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
            _reputationService = reputationService;
            _gamificationService = gamificationService;
            if (!Directory.Exists(_uploadFolder))
            {
                Directory.CreateDirectory(_uploadFolder);
            }
        }

        public async Task<ThreadPostDTO> CreatePostAsync(long customerId, CreateThreadPostDTO dto, List<IFormFile>? images)
        {
            // 1. Validate Phone Numbers
            ValidateNoPhoneNumbers(dto.Title, "tiêu đề");
            ValidateNoPhoneNumbers(dto.Content, "nội dung");

            // 2. Check Customer details
            var customerDetail = await _context.CustomerDetails
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CustomerID == customerId);
            if (customerDetail == null)
            {
                throw new KeyNotFoundException("Không tìm thấy thông tin chi tiết khách hàng.");
            }

            // 3. Reset monthly thread count if new month
            var now = DateTime.UtcNow;
            if (!customerDetail.LastThreadResetDate.HasValue || 
                customerDetail.LastThreadResetDate.Value.Month != now.Month || 
                customerDetail.LastThreadResetDate.Value.Year != now.Year)
            {
                customerDetail.CurrentMonthThreadCount = 0;
                customerDetail.LastThreadResetDate = now;
            }

            // 4. Check Thread Limit based on Subscription
            var customerService = await _context.CustomerServices
                .Include(cs => cs.ServicePackage)
                .FirstOrDefaultAsync(cs => cs.CustomerID == customerId);
            int threadLimit = customerService?.ServicePackage?.ThreadLimit ?? 3; // Fallback to Free (3)

            if (customerDetail.CurrentMonthThreadCount >= threadLimit)
            {
                throw new InvalidOperationException($"Bạn đã vượt quá giới hạn đăng bài ({threadLimit} bài/tháng) của gói dịch vụ hiện tại.");
            }

            // 5. Create ThreadPost
            var post = new ThreadPost
            {
                CustomerID = customerId,
                Title = dto.Title,
                Content = dto.Content,
                Hashtags = dto.Hashtags,
                CreatedAt = now,
                IsHidden = false,
                ReportCount = 0
            };

            _context.ThreadPosts.Add(post);
            await _context.SaveChangesAsync(); // Save to generate PostID

            // 6. Handle Image Uploads
            if (images != null && images.Count > 0)
            {
                foreach (var file in images)
                {
                    if (file.Length == 0) continue;

                    var extension = Path.GetExtension(file.FileName).ToLower();
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    if (!allowedExtensions.Contains(extension))
                    {
                        throw new ArgumentException("Chỉ chấp nhận các tệp hình ảnh (.jpg, .jpeg, .png, .gif).");
                    }

                    var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                    var absolutePath = Path.Combine(_uploadFolder, uniqueFileName);

                    using (var stream = new FileStream(absolutePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var threadImage = new ThreadImage
                    {
                        PostID = post.PostID,
                        ImagePath = $"/uploads/threads/{uniqueFileName}"
                    };
                    _context.ThreadImages.Add(threadImage);
                }
            }

            // 7. Increment Monthly Post Count
            customerDetail.CurrentMonthThreadCount += 1;
            await _context.SaveChangesAsync();

            // Load complete entity to return DTO
            var createdPost = await _context.ThreadPosts
                .Include(p => p.User)
                .Include(p => p.Images)
                .Include(p => p.Comments)
                .FirstAsync(p => p.PostID == post.PostID);

            // 8. Send Notifications to Followers
            try
            {
                var followers = await _context.Subscriptions
                    .Where(s => s.TargetType == SubscriptionTargetType.Thread && s.TargetID == customerId)
                    .Select(s => s.CustomerID)
                    .ToListAsync();

                var creatorName = $"{customerDetail.User.LastName} {customerDetail.User.FirstName}".Trim();
                if (string.IsNullOrEmpty(creatorName)) creatorName = "Người dùng";

                foreach (var followerId in followers)
                {
                    await _notificationService.CreateAndSendNotificationAsync(
                        followerId,
                        "Bài viết mới từ người theo dõi",
                        $"{creatorName} đã đăng một bài viết mới: \"{post.Title}\"",
                        NotificationType.NewThread,
                        (int)post.PostID
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi thông báo bài viết mới đến những người theo dõi.");
            }

            return MapToPostDTO(createdPost);
        }

        public async Task<ThreadPostDTO> UpdatePostAsync(long customerId, long postId, UpdateThreadPostDTO dto)
        {
            ValidateNoPhoneNumbers(dto.Title, "tiêu đề");
            ValidateNoPhoneNumbers(dto.Content, "nội dung");

            var post = await _context.ThreadPosts
                .Include(p => p.User)
                .Include(p => p.Images)
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.PostID == postId);

            if (post == null)
            {
                throw new KeyNotFoundException("Không tìm thấy bài viết.");
            }

            if (post.CustomerID != customerId)
            {
                throw new UnauthorizedAccessException("Bạn không phải là chủ sở hữu của bài viết này.");
            }

            post.Title = dto.Title;
            post.Content = dto.Content;
            post.Hashtags = dto.Hashtags;

            await _context.SaveChangesAsync();
            var isLiked = await _context.ThreadLikes.AnyAsync(l => l.PostID == postId && l.CustomerID == customerId);
            return MapToPostDTO(post, isLiked);
        }

        public async Task<bool> DeletePostAsync(long userId, UserRole role, long postId)
        {
            var post = await _context.ThreadPosts.FindAsync(postId);
            if (post == null) return false;

            // Only post owner or Admin can delete
            if (post.CustomerID != userId && role != UserRole.Admin)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xóa bài viết này.");
            }

            // Also delete associated images from disk
            var images = await _context.ThreadImages.Where(ti => ti.PostID == postId).ToListAsync();
            foreach (var img in images)
            {
                var relativePath = img.ImagePath.TrimStart('/');
                var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);
                if (File.Exists(absolutePath))
                {
                    try
                    {
                        File.Delete(absolutePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Không thể xóa tệp ảnh: {absolutePath}");
                    }
                }
            }

            var postAuthorId = post.CustomerID;
            _context.ThreadPosts.Remove(post);
            
            var success = await _context.SaveChangesAsync() > 0;
            if (success)
            {
                // Nếu bị Admin/Moderator xóa (không phải tác giả tự xóa)
                if (role == UserRole.Admin && postAuthorId != userId)
                {
                    try
                    {
                        await _reputationService.HandleReputationChangeAsync(
                            postAuthorId, 
                            ReputationAction.ReviewThreadDeleted, 
                            "Bài viết bị ban quản trị xóa do vi phạm tiêu chuẩn cộng đồng.");
                        await _reputationService.UpdateCustomerRankAsync(postAuthorId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Lỗi khi trừ điểm uy tín của tác giả {postAuthorId} sau khi bài viết bị Admin xóa.");
                    }
                }
            }

            return success;
        }

        public async Task<bool> HidePostAsync(long postId, bool isHidden)
        {
            var post = await _context.ThreadPosts.FindAsync(postId);
            if (post == null) return false;

            post.IsHidden = isHidden;
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<ThreadPostDTO>> GetFeedAsync(int page, int pageSize, long? currentCustomerId = null)
        {
            var query = _context.ThreadPosts
                .Include(p => p.User)
                .Include(p => p.Images)
                .Include(p => p.Comments)
                .Where(p => !p.IsHidden)
                .OrderByDescending(p => p.CreatedAt)
                .AsQueryable();

            var pagedQuery = query.Skip((page - 1) * pageSize).Take(pageSize);
            var posts = await pagedQuery.ToListAsync();

            var likedPostIds = new HashSet<long>();
            if (currentCustomerId.HasValue && posts.Count > 0)
            {
                var postIds = posts.Select(p => p.PostID).ToList();
                likedPostIds = (await _context.ThreadLikes
                    .Where(l => l.CustomerID == currentCustomerId.Value && postIds.Contains(l.PostID))
                    .Select(l => l.PostID)
                    .ToListAsync())
                    .ToHashSet();
            }

            return posts.Select(p => MapToPostDTO(p, likedPostIds.Contains(p.PostID)));
        }

        public async Task<ThreadPostDTO?> GetPostByIdAsync(long postId, long? currentCustomerId = null)
        {
            var post = await _context.ThreadPosts
                .Include(p => p.User)
                .Include(p => p.Images)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(p => p.PostID == postId);

            if (post == null) return null;

            // Load comments sorted by CreatedAt asc
            var isLiked = currentCustomerId.HasValue &&
                await _context.ThreadLikes.AnyAsync(l => l.PostID == postId && l.CustomerID == currentCustomerId.Value);
            var dto = MapToPostDTO(post, isLiked);
            dto.Comments = post.Comments
                .OrderBy(c => c.CreatedAt)
                .Select(c => new ThreadCommentDTO
                {
                    CommentID = c.CommentID,
                    PostID = c.PostID,
                    CustomerID = c.CustomerID,
                    CustomerName = $"{c.User.LastName} {c.User.FirstName}".Trim(),
                    CustomerAvatar = c.User.Avatar,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt
                }).ToList();

            return dto;
        }

        public async Task<(bool IsLiked, int LikeCount)> ToggleLikeAsync(long customerId, long postId)
        {
            var post = await _context.ThreadPosts.FirstOrDefaultAsync(p => p.PostID == postId && !p.IsHidden);
            if (post == null)
            {
                throw new KeyNotFoundException("Bài viết không tồn tại hoặc đã bị ẩn.");
            }

            var existingLike = await _context.ThreadLikes.FindAsync(postId, customerId);
            var isLiked = existingLike == null;

            if (existingLike != null)
            {
                _context.ThreadLikes.Remove(existingLike);
                post.LikeCount = Math.Max(0, post.LikeCount - 1);
            }
            else
            {
                _context.ThreadLikes.Add(new ThreadLike
                {
                    PostID = postId,
                    CustomerID = customerId,
                    CreatedAt = DateTime.UtcNow
                });
                post.LikeCount += 1;
            }

            await _context.SaveChangesAsync();
            return (isLiked, post.LikeCount);
        }

        public async Task<int> SharePostAsync(long customerId, long postId, ShareThreadPostDTO dto)
        {
            var post = await _context.ThreadPosts.FirstOrDefaultAsync(p => p.PostID == postId && !p.IsHidden);
            if (post == null)
            {
                throw new KeyNotFoundException("Bài viết không tồn tại hoặc đã bị ẩn.");
            }

            var share = new ThreadShare
            {
                PostID = postId,
                CustomerID = customerId,
                ShareUrl = dto?.ShareUrl,
                CreatedAt = DateTime.UtcNow
            };

            _context.ThreadShares.Add(share);
            post.ShareCount += 1;
            await _context.SaveChangesAsync();

            try
            {
                if (_gamificationService != null)
                {
                    await _gamificationService.RecordShareActionAsync(customerId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật gamification sau khi chia sẻ bài viết cộng đồng.");
            }

            return post.ShareCount;
        }

        public async Task<ThreadCommentDTO> AddCommentAsync(long customerId, long postId, CreateThreadCommentDTO dto)
        {
            ValidateNoPhoneNumbers(dto.Content, "nội dung bình luận");

            var postExists = await _context.ThreadPosts.AnyAsync(p => p.PostID == postId && !p.IsHidden);
            if (!postExists)
            {
                throw new KeyNotFoundException("Bài viết không tồn tại hoặc đã bị ẩn.");
            }

            var user = await _context.Users.FindAsync(customerId);
            if (user == null)
            {
                throw new KeyNotFoundException("Không tìm thấy thông tin tài khoản người dùng.");
            }

            var comment = new ThreadComment
            {
                PostID = postId,
                CustomerID = customerId,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow
            };

            _context.ThreadComments.Add(comment);
            await _context.SaveChangesAsync();

            try
            {
                var post = await _context.ThreadPosts.FindAsync(postId);
                if (post != null && post.CustomerID != customerId)
                {
                    var commenterName = $"{user.LastName} {user.FirstName}".Trim();
                    if (string.IsNullOrEmpty(commenterName)) commenterName = "Một người dùng";

                    await _notificationService.CreateAndSendNotificationAsync(
                        post.CustomerID,
                        "Bình luận mới",
                        $"{commenterName} đã bình luận về bài viết của bạn: \"{comment.Content}\"",
                        NotificationType.NewInteraction,
                        (int)postId
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi thông báo bình luận mới.");
            }

            return new ThreadCommentDTO
            {
                CommentID = comment.CommentID,
                PostID = comment.PostID,
                CustomerID = comment.CustomerID,
                CustomerName = $"{user.LastName} {user.FirstName}".Trim(),
                CustomerAvatar = user.Avatar,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt
            };
        }

        public async Task<bool> DeleteCommentAsync(long userId, UserRole role, long commentId)
        {
            var comment = await _context.ThreadComments.FindAsync(commentId);
            if (comment == null) return false;

            // Only comment owner or Admin can delete
            if (comment.CustomerID != userId && role != UserRole.Admin)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xóa bình luận này.");
            }

            _context.ThreadComments.Remove(comment);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<int> ReportPostAsync(long customerId, long postId, CreateReportDTO dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var post = await _context.ThreadPosts.FindAsync(postId);
            if (post == null)
            {
                throw new KeyNotFoundException("Không tìm thấy bài viết để báo cáo.");
            }

            // Kiểm tra mô tả tối đa 500 từ
            if (!string.IsNullOrWhiteSpace(dto.Description))
            {
                var wordCount = dto.Description.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
                if (wordCount > 500)
                {
                    throw new ArgumentException("Nội dung mô tả báo cáo không được vượt quá 500 từ.");
                }
            }

            // Tạo bản ghi báo cáo vi phạm mới
            var report = new Report
            {
                PostID = postId,
                CustomerID = customerId,
                Reason = dto.Reason,
                Description = dto.Description ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reports.Add(report);
            post.ReportCount += 1;

            bool isNowHidden = false;
            if (post.ReportCount >= 5 && !post.IsHidden)
            {
                post.IsHidden = true;
                isNowHidden = true;
                _logger.LogWarning($"[Moderation Alert] Bài viết ID {post.PostID} của khách hàng ID {post.CustomerID} đã nhận đủ 5 báo cáo vi phạm. Hệ thống đã tự động ẩn bài viết này.");
            }

            await _context.SaveChangesAsync();

            try
            {
                await _notificationService.CreateAndSendNotificationAsync(
                    post.CustomerID,
                    "Bài viết bị báo cáo",
                    $"Bài viết \"{post.Title}\" của bạn đã bị báo cáo vi phạm.",
                    NotificationType.ModWarning,
                    (int)post.PostID
                );

                if (isNowHidden)
                {
                    await _notificationService.CreateAndSendNotificationAsync(
                        post.CustomerID,
                        "Bài viết đã bị ẩn",
                        $"Bài viết \"{post.Title}\" của bạn đã bị hệ thống tự động ẩn do nhận đủ 5 báo cáo vi phạm.",
                        NotificationType.ModWarning,
                        (int)post.PostID
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi thông báo báo cáo vi phạm.");
            }

            return post.ReportCount;
        }

        // --- HELPER METHODS ---

        private void ValidateNoPhoneNumbers(string text, string fieldName)
        {
            if (string.IsNullOrEmpty(text)) return;
            // Vietnamese phone numbers regex pattern
            var pattern = @"(?:\+84|84|0)[35789](?:[\s.-]*\d){8}";
            if (Regex.IsMatch(text, pattern))
            {
                throw new ArgumentException($"Nội dung {fieldName} chứa số điện thoại. Để đảm bảo an toàn bảo mật, hệ thống không cho phép đăng tải số điện thoại lên cộng đồng.");
            }
        }

        private static ThreadPostDTO MapToPostDTO(ThreadPost p, bool isLikedByCurrentUser = false)
        {
            return new ThreadPostDTO
            {
                PostID = p.PostID,
                CustomerID = p.CustomerID,
                CustomerName = p.User != null ? $"{p.User.LastName} {p.User.FirstName}".Trim() : string.Empty,
                CustomerAvatar = p.User?.Avatar,
                Title = p.Title,
                Content = p.Content,
                Hashtags = p.Hashtags,
                CreatedAt = p.CreatedAt,
                IsHidden = p.IsHidden,
                ReportCount = p.ReportCount,
                LikeCount = p.LikeCount,
                ShareCount = p.ShareCount,
                CommentsCount = p.Comments?.Count ?? 0,
                IsLikedByCurrentUser = isLikedByCurrentUser,
                Images = p.Images?.Select(img => new ThreadImageDTO
                {
                    ImageID = img.ImageID,
                    ImagePath = img.ImagePath
                }).ToList() ?? new List<ThreadImageDTO>()
            };
        }
    }
}
