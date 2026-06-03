using System.Collections.Generic;

// Dành cho khách hàng mua - không thấy được RealBookID
namespace BookBlossom.DTOs.BlindBook
{
    public class BlindBookClientDTO
    {
        public long BlindBookID { get; set; } 
        public string Keywords { get; set; } = string.Empty; 
        public string Quotes { get; set; } = string.Empty; 
        public string Category { get; set; } = string.Empty; 
        public string Hashtags { get; set; } = string.Empty;
        public decimal Price { get; set; } 
        public int StockQuantity { get; set; }
        // Danh sách đường dẫn ảnh thật từ bảng BlindBookImages, sắp xếp theo SortOrder
        public List<string> ImagePaths { get; set; } = new List<string>();
    }
}