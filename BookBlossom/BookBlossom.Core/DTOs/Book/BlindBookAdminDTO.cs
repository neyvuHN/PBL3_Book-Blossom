// Dùng cho Staff duyệt BlindBook
using BookBlossom.Core.Enums;

namespace BookBlossom.DTOs.BlindBook
{
    public class BlindBookAdminDTO : CreateBlindBookDTO
    {
        public long BlindBookID { get; set; } 
        public string? RealBookTitle { get; set; } 
        public string? Barcode { get; set; }
        public int StockQuantity { get; set; } 
        public string? RejectReason { get; set; }

        public BlindBookRequestStatus Status { get; set; } 
    }
}