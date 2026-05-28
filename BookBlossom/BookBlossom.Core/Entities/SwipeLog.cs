using System;

namespace BookBlossom.Core.Entities
{
    public class SwipeLog
    {
        public long SwipeLogID { get; set; }
        public long? CustomerID { get; set; }
        public Guid? GuestID { get; set; }
        public long? BookID { get; set; }
        public long? BlindBookID { get; set; }
        public string ActionType { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
