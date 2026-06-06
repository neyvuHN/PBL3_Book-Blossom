using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using BookBlossom.Core.DTOs.Review;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface IReviewService
    {
        Task<ReviewDTO> CreateReviewAsync(long customerId, CreateReviewDTO dto, List<IFormFile>? mediaFiles);
        Task<int> LikeReviewAsync(long customerId, long reviewId);
        Task<bool> HideReviewAsync(long reviewId, bool isHidden);
        Task<bool> ReplyReviewAsync(long reviewId, string replyContent);
        Task<IEnumerable<ReviewDTO>> GetReviewsByBookAsync(long? bookId, long? blindBookId);
    }
}