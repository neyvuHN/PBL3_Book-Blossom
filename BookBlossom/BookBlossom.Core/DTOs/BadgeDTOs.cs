using System;

namespace BookBlossom.Core.DTOs
{
    public class BadgeDTO
    {
        public long BadgeID { get; set; }
        public string BadgeName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class BadgeCustomerDTO
    {
        public long BadgeID { get; set; }
        public string BadgeName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EarnedAt { get; set; }
    }

    public class ShareRequestDTO
    {
        public string ShareUrl { get; set; } = string.Empty;
    }
}
