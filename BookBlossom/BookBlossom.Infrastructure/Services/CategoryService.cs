using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BookBlossom.Core.DTOs.Category;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;

namespace BookBlossom.Infrastructure.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;

        public CategoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CategoryDTO>> GetAllCategoriesAsync(CategoryStatus? status = null)
        {
            IQueryable<Category> query = _context.Categories;

            if (status.HasValue)
            {
                query = query.Where(c => c.Status == status.Value);
            }

            return await query
                .Select(c => new CategoryDTO
                {
                    CategoryID = c.CategoryID,
                    CategoryName = c.CategoryName,
                    Description = c.Description,
                    Status = c.Status
                })
                .ToListAsync();
        }

        public async Task<CategoryDTO?> GetCategoryByIdAsync(long categoryId)
        {
            var c = await _context.Categories
                .FirstOrDefaultAsync(x => x.CategoryID == categoryId);

            if (c == null) return null;

            return new CategoryDTO
            {
                CategoryID = c.CategoryID,
                CategoryName = c.CategoryName,
                Description = c.Description,
                Status = c.Status
            };
        }

        public async Task<CategoryDTO> CreateCategoryAsync(CreateCategoryRequestDTO request)
        {
            // Normalize & check uniqueness
            var trimmedName = request.CategoryName.Trim();
            var exists = await CategoryNameExistsAsync(trimmedName);
            if (exists)
            {
                throw new InvalidOperationException($"Tên danh mục '{trimmedName}' đã tồn tại.");
            }

            var category = new Category
            {
                CategoryName = trimmedName,
                Description = request.Description?.Trim() ?? string.Empty,
                Status = request.Status
            };

            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();

            return new CategoryDTO
            {
                CategoryID = category.CategoryID,
                CategoryName = category.CategoryName,
                Description = category.Description,
                Status = category.Status
            };
        }

        public async Task<CategoryDTO?> UpdateCategoryAsync(long categoryId, UpdateCategoryRequestDTO request)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null) return null;

            var trimmedName = request.CategoryName.Trim();
            var exists = await CategoryNameExistsAsync(trimmedName, categoryId);
            if (exists)
            {
                throw new InvalidOperationException($"Tên danh mục '{trimmedName}' đã tồn tại.");
            }

            category.CategoryName = trimmedName;
            category.Description = request.Description?.Trim() ?? string.Empty;
            category.Status = request.Status;

            _context.Categories.Update(category);
            await _context.SaveChangesAsync();

            return new CategoryDTO
            {
                CategoryID = category.CategoryID,
                CategoryName = category.CategoryName,
                Description = category.Description,
                Status = category.Status
            };
        }

        public async Task<bool> DeleteCategorySoftAsync(long categoryId)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null) return false;

            // Check if there are associated books
            var hasBooks = await HasAssociatedBooksAsync(categoryId);
            if (hasBooks)
            {
                throw new InvalidOperationException("Không thể xóa danh mục đang có sách liên kết.");
            }

            category.Status = CategoryStatus.Inactive; // soft delete
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CategoryNameExistsAsync(string categoryName, long? excludeCategoryId = null)
        {
            var query = _context.Categories.AsQueryable();

            if (excludeCategoryId.HasValue)
            {
                query = query.Where(c => c.CategoryID != excludeCategoryId.Value);
            }

            return await query.AnyAsync(c => c.CategoryName.ToLower() == categoryName.Trim().ToLower());
        }

        public async Task<bool> HasAssociatedBooksAsync(long categoryId)
        {
            return await _context.RealBooks.AnyAsync(b => b.CategoryID == categoryId);
        }
    }
}
