using System.ComponentModel.DataAnnotations;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.DTOs.Category
{
    public class CreateCategoryRequestDTO
    {
        [Required(ErrorMessage = "Tên danh mục không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự.")]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
        public string Description { get; set; } = string.Empty;

        public CategoryStatus Status { get; set; } = CategoryStatus.Active;
    }
}
