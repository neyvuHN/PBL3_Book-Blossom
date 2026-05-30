using System;

namespace BookBlossom.Core.Entities
{
    public class GuestPreference
    {
        public Guid GuestID { get; set; }
        public long CategoryID { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual GuestDetail? GuestDetail { get; set; }
        public virtual Category? Category { get; set; }
    }
}
