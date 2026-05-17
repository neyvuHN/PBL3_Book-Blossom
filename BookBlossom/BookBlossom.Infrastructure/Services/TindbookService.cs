using BookBlossom.Core.DTOs.Tindbook;
using BookBlossom.Core.Interfaces;
using BookBlossom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookBlossom.Infrastructure.Services
{
    public class TindbookService : ITindbookService
    {
        private readonly ApplicationDbContext _context;

        public TindbookService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BookResponseDTO>> GetRecommendedBooksForTindbookAsync(long userId, int limit = 20)
        {
            // 1. Lấy ra danh sách các CategoryID mà User này đã chọn lúc Onboarding
            var userPreferenceCategoryIds = await _context.CustomerPreferences
                .Where(cp => cp.CustomerID == userId)
                .Select(cp => cp.CategoryID)
                .ToListAsync();

            // 2. Query danh sách sách và dùng cấu trúc sắp xếp ưu tiên dựa trên mảng sở thích
            var recommendedBooks = await _context.RealBooks
                .Include(b => b.Category) // Đảm bảo lấy được thông tin thể loại để đối chiếu
                .Select(b => new 
                {
                    Book = b,
                    // Nếu sách thuộc thể loại yêu thích thì gán trọng số ưu tiên = 1, ngược lại = 2
                    Priority = userPreferenceCategoryIds.Contains(b.CategoryID) ? 1 : 2
                })
                // Ưu tiên 1 lên trước, ưu tiên 2 ra sau. Trong cùng mức ưu tiên thì lấy ngẫu nhiên hoặc theo ID
                .OrderBy(x => x.Priority)
                .ThenBy(x => Guid.NewGuid()) // Sắp xếp ngẫu nhiên (Randomize) để tăng tính trải nghiệm Tindbook quẹt sách
                .Select(x => new BookResponseDTO
                {
                    BookID = x.Book.BookID,
                    CategoryID = x.Book.CategoryID,
                    CategoryName = x.Book.Category.CategoryName,
                    Title = x.Book.Title,
                    Publisher = x.Book.Publisher,
                    Price = x.Book.Price
                })
                .Take(limit) // Giới hạn số lượng bản ghi trả về cho mỗi lượt load dữ liệu
                .ToListAsync();

            return recommendedBooks;
        }
    }
}