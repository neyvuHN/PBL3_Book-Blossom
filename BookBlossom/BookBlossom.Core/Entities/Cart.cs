using System;

namespace BookBlossom.Core.Entities
{
    public class Cart
    {
        public long CartID { get; set; }
        public long? UserID { get; set; }
        public Guid? GuestID { get; set; }
        public long? BookID { get; set; }
        public long? BlindBookID { get; set; }
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User? User { get; set; }
        public virtual RealBook? Book { get; set; }
        public virtual BlindBook? BlindBook { get; set; }
    }
}
