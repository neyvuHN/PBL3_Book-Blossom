using BookBlossom.Core.Enums;

namespace BookBlossom.Core.DTOs.Category
{
    public class CategoryDTO
    {
        public long CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public CategoryStatus Status { get; set; }
    }
}
