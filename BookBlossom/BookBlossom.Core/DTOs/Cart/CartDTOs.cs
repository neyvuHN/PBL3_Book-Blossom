using System;

namespace BookBlossom.Core.DTOs.Cart
{
    public class CartItemDTO
    {
        public long CartID { get; set; }
        public long? BookID { get; set; }
        public long? BlindBookID { get; set; }
        public long? CategoryID { get; set; }
        public string? Title { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public DateTime AddedAt { get; set; }
        public decimal LineTotal => Price * Quantity;
    }

    public class AddCartRequestDTO
    {
        public long? BookID { get; set; }
        public long? BlindBookID { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartRequestDTO
    {
        public int Quantity { get; set; }
    }
}
