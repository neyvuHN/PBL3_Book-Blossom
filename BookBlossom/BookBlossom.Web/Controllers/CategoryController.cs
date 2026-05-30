using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookBlossom.Core.DTOs.Category;
using BookBlossom.Core.Enums;
using BookBlossom.Core.Interfaces.Services;

namespace BookBlossom.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách danh mục (có thể lọc theo trạng thái)
        /// GET /api/category
        /// </summary>
        [HttpGet]
        [AllowAnonymous] // Anyone (all roles & guests) can view categories for operation or shopping
        public async Task<IActionResult> GetAll([FromQuery] CategoryStatus? status = null)
        {
            var categories = await _categoryService.GetAllCategoriesAsync(status);
            return Ok(categories);
        }

        /// <summary>
        /// Lấy chi tiết một danh mục theo ID
        /// GET /api/category/{id}
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous] // Anyone (all roles & guests) can view category details
        public async Task<IActionResult> GetById(long id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound(new { message = $"Không tìm thấy danh mục với ID = {id}." });
            }
            return Ok(category);
        }

        /// <summary>
        /// Tạo mới một danh mục sách
        /// POST /api/category
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "AdminOnly")] // Only System Admin can add new categories
        public async Task<IActionResult> Create([FromBody] CreateCategoryRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var createdCategory = await _categoryService.CreateCategoryAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = createdCategory.CategoryID }, createdCategory);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi tạo mới danh mục sách." });
            }
        }

        /// <summary>
        /// Cập nhật thông tin danh mục sách
        /// PUT /api/category/{id}
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")] // Only System Admin can update categories
        public async Task<IActionResult> Update(long id, [FromBody] UpdateCategoryRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var updatedCategory = await _categoryService.UpdateCategoryAsync(id, request);
                if (updatedCategory == null)
                {
                    return NotFound(new { message = $"Không tìm thấy danh mục với ID = {id} để cập nhật." });
                }
                return Ok(updatedCategory);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi cập nhật danh mục sách." });
            }
        }

        /// <summary>
        /// Soft delete danh mục sách bằng cách chuyển trạng thái sang Inactive
        /// DELETE /api/category/{id}
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")] // Only System Admin can delete categories
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var success = await _categoryService.DeleteCategorySoftAsync(id);
                if (!success)
                {
                    return NotFound(new { message = $"Không tìm thấy danh mục với ID = {id} để xóa." });
                }
                return Ok(new { message = "Xóa mềm danh mục sách thành công!" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi xóa danh mục sách." });
            }
        }
    }
}
