using System;

namespace BookBlossom.Core.DTOs.Wishlist
{
    public class WishlistItemDTO
    {
        public long WishlistID { get; set; }
        public long? BookID { get; set; }
        public long? BlindBookID { get; set; }
        public string? Title { get; set; }
        public string? Author { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime AddedAt { get; set; }
    }

    public class AddWishlistRequestDTO
    {
        public long? BookID { get; set; }
        public long? BlindBookID { get; set; }
    }
}
