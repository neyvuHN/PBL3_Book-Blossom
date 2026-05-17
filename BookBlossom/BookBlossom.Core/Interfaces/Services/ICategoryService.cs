using System.Collections.Generic;
using System.Threading.Tasks;
using BookBlossom.Core.DTOs.Category;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDTO>> GetAllCategoriesAsync(CategoryStatus? status = null);
        Task<CategoryDTO?> GetCategoryByIdAsync(long categoryId);
        Task<CategoryDTO> CreateCategoryAsync(CreateCategoryRequestDTO request);
        Task<CategoryDTO?> UpdateCategoryAsync(long categoryId, UpdateCategoryRequestDTO request);
        Task<bool> DeleteCategorySoftAsync(long categoryId);
        Task<bool> CategoryNameExistsAsync(string categoryName, long? excludeCategoryId = null);
        Task<bool> HasAssociatedBooksAsync(long categoryId);
    }
}
