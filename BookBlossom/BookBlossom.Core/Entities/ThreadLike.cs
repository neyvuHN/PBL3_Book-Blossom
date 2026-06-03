using System;

namespace BookBlossom.Core.Entities
{
    public class ThreadLike
    {
        public long PostID { get; set; }
        public long CustomerID { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ThreadPost Post { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
