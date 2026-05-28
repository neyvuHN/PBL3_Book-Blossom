using BookBlossom.Core.Enums;

namespace BookBlossom.Core.DTOs.Tindbook
{
    public class SwipeActionDTO
    {
        public long? BookID { get; set; }
        public long? BlindBookID { get; set; }
        
        public SwipeIntent Intent { get; set; }
    }
}