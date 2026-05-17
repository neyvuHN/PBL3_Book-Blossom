using BookBlossom.Core.DTOs.Onboarding;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Interfaces;
using BookBlossom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookBlossom.Infrastructure.Services
{
    public class OnboardingService : IOnboardingService
    {
        private readonly ApplicationDbContext _context;

        public OnboardingService(ApplicationDbContext context)
        {
            _context = context;
        }

        // API 1: Lấy danh sách thể loại sách từ DB
        public async Task<IEnumerable<CategoryResponseDTO>> GetOnboardingTagsAsync()
        {
            return await _context.Categories
                .Select(c => new CategoryResponseDTO
                {
                    CategoryID = c.CategoryID,
                    CategoryName = c.CategoryName,
                    Description = c.Description
                })
                .ToListAsync();
        }

        // API 2: Chỉ xử lý lưu mảng sở thích, tuyệt đối chưa bật cờ true
        public async Task<bool> SaveCustomerPreferencesAsync(long userId, SavePreferencesRequestDTO request)
        {
            var customer = await _context.CustomerDetails
                .FirstOrDefaultAsync(cd => cd.CustomerID == userId);

            if (customer == null)
                throw new KeyNotFoundException("Không tìm thấy thông tin khách hàng.");

            if (customer.IsOnboardingCompleted)
                throw new InvalidOperationException("Khách hàng đã hoàn thành toàn bộ Onboarding từ trước.");

            // Nếu người dùng không chọn nút "Skip" sở thích
            if (!request.IsSkipped && request.SelectedTagIds.Any())
            {
                // Làm sạch: Xóa các sở thích cũ nếu có lỡ tay nhấn lại trước đó
                var existingPrefs = _context.CustomerPreferences.Where(cp => cp.CustomerID == userId);
                _context.CustomerPreferences.RemoveRange(existingPrefs);

                // Thêm danh sách sở thích mới
                var newPreferences = request.SelectedTagIds.Select(tagId => new CustomerPreference
                {
                    CustomerID = userId,
                    CategoryID = tagId,
                    CreatedAt = DateTime.Now
                }).ToList();

                await _context.CustomerPreferences.AddRangeAsync(newPreferences);
            }

            // Lưu thay đổi sở thích (cột IsOnboardingCompleted vẫn giữ nguyên là false)
            await _context.SaveChangesAsync();
            return true;
        }

        // API 3: Điểm chốt chặn cuối cùng - Khi User xem xong hoặc bấm Skip phần Hướng dẫn
        public async Task<bool> CompleteOnboardingTourAsync(long userId)
        {
            var customer = await _context.CustomerDetails
                .FirstOrDefaultAsync(cd => cd.CustomerID == userId);

            if (customer == null)
                throw new KeyNotFoundException("Không tìm thấy thông tin khách hàng.");

            // Chính thức bật Switch kích hoạt trạng thái hoàn thành để mở khóa vào trang chính
            customer.IsOnboardingCompleted = true;

            var result = await _context.SaveChangesAsync();
            return result > 0;
        }
    }
}