using System;

namespace BookBlossom.Core.DTOs
{
    public class BadgeDTO
    {
        public long BadgeID { get; set; }
        public string BadgeName { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;
        public string ConditionDescription { get; set; } = string.Empty;
    }

    public class BadgeCustomerDTO
    {
        public long BadgeID { get; set; }
        public string BadgeName { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;
        public string ConditionDescription { get; set; } = string.Empty;
        public DateTime EarnedDate { get; set; }
    }

    public class ShareRequestDTO
    {
        public string ShareUrl { get; set; } = string.Empty;
    }
}
