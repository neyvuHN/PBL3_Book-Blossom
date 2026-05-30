using System;

namespace BookBlossom.Core.Entities
{
    public class Badge
    {
        public long BadgeID { get; set; }
        public string BadgeName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? IconPath { get; set; }
    }
}
