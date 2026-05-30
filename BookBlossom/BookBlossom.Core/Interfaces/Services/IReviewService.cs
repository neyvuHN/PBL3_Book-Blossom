using BookBlossom.Core.DTOs.Review;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface IReviewService
    {
        Task<ReviewDTO> CreateReviewAsync(long customerId, CreateReviewDTO dto, List<IFormFile>? mediaFiles);
        Task<ReviewDTO?> GetReviewByIdAsync(long reviewId);
        Task<IEnumerable<ReviewDTO>> GetReviewsForBookAsync(long? bookId, long? blindBookId);
        Task<ReviewDTO> UpdateReviewAsync(long customerId, long reviewId, UpdateReviewDTO dto);
        Task<bool> DeleteReviewAsync(long userId, UserRole role, long reviewId);
        Task<bool> HideReviewAsync(long reviewId, bool isHidden);
        Task<ReviewDTO> LikeReviewAsync(long reviewId);

        // Gamification sync & fetch helpers
        Task SyncCustomerBadgesAsync(long customerId);
        Task<int> GetCustomerReputationPointsAsync(long customerId);
        Task<IEnumerable<Badge>> GetCustomerBadgesAsync(long customerId);
    }
}
