using System;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.Entities
{
    public class Report
    {
        public long ReportID { get; set; }
        public long PostID { get; set; }
        public long CustomerID { get; set; }
        public ReportType Reason { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool? IsAccurate { get; set; }

        // Navigation properties
        public virtual ThreadPost Post { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
