namespace BookBlossom.Core.DTOs.Tindbook
{
    public class BookResponseDTO
    {
        public long? BookID { get; set; }
        public long? BlindBookID { get; set; }
        public long CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty; // Lấy từ bảng liên kết Category
        public string Title { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Description { get; set; }
    }
}