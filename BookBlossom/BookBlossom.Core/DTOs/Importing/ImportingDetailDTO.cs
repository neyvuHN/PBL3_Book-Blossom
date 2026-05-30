namespace BookBlossom.Core.DTOs.Importing
{
    public class ImportingDetailDTO
    {
        public long ImportingID { get; set; }
        public long BookID { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal { get; set; }
    }
}
