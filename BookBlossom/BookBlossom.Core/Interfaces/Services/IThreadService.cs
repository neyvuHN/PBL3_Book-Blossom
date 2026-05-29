using System.Collections.Generic;
using System.Threading.Tasks;
using BookBlossom.Core.DTOs.Thread;
using BookBlossom.Core.Enums;
using Microsoft.AspNetCore.Http;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface IThreadService
    {
        Task<ThreadPostDTO> CreatePostAsync(long customerId, CreateThreadPostDTO dto, List<IFormFile>? images);
        Task<ThreadPostDTO> UpdatePostAsync(long customerId, long postId, UpdateThreadPostDTO dto);
        Task<bool> DeletePostAsync(long userId, UserRole role, long postId);
        Task<bool> HidePostAsync(long postId, bool isHidden);
        Task<IEnumerable<ThreadPostDTO>> GetFeedAsync(int page, int pageSize);
        Task<ThreadPostDTO?> GetPostByIdAsync(long postId);
        Task<ThreadCommentDTO> AddCommentAsync(long customerId, long postId, CreateThreadCommentDTO dto);
        Task<bool> DeleteCommentAsync(long userId, UserRole role, long commentId);
        Task<int> ReportPostAsync(long postId);
    }
}
