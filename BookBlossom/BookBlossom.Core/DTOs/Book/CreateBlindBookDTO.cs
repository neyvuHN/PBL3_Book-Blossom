namespace BookBlossom.DTOs.BlindBook
{
    public class CreateBlindBookDTO
    {
        public long RealBookID { get; set; } 
        public string Keywords { get; set; } = string.Empty; 
        public string Quotes { get; set; } = string.Empty; 

        public string Hashtags { get; set; } = string.Empty; 
        public decimal Price { get; set; }
        public int RequestQuantity { get; set; } 
    }
}