using System;
using System.Collections.Generic;

namespace BookBlossom.Web.ViewModels.Profile
{
    public class ProfileViewModel
    {
        public string Avatar { get; set; } = "/images/Avatar/avatar1.jpg";
        public string FullName { get; set; } = "Jane Doe";
        public string Username { get; set; } = "janedoe_bookworm";
        public string PhoneNumber { get; set; } = "0987654321";
        public string Email { get; set; } = "janedoe@example.com";
        public string Gender { get; set; } = "Female";
        public DateTime Birthdate { get; set; } = new DateTime(2000, 5, 15);
        public string Bio { get; set; } = "Reading is a discount ticket to everywhere! Passionate about psychological thrillers, magic realism, and mystery blind dates.";
        public DateTime MemberSince { get; set; } = new DateTime(2025, 1, 10);
        
        // Membership details
        public string MembershipTier { get; set; } = "Gold"; // Copper, Silver, Gold, Diamond
        public decimal TotalSpending { get; set; } = 4250000; // in VND
        public decimal NextTierThreshold { get; set; } = 5000000; // threshold for next tier in VND
        public string NextTierName { get; set; } = "Silver"; // Name of the next membership tier
        
        // Reputation score and privileges
        public int ReputationScore { get; set; } = 110; // Out of 150
        public int MaxReputationScore { get; set; } = 150;
        
        // Order Streak
        public int CurrentOrderStreak { get; set; } = 2; // progress out of 3 successful orders
        
        // Badges Collection
        public List<string> Badges { get; set; } = new List<string>();
        public Dictionary<string, string> BadgeEarnedDates { get; set; } = new Dictionary<string, string>();
        public List<string> AllBadgeNames { get; set; } = new List<string>(); // All badges from DB
        
        // Subscription limits
        public string SubscriptionPackage { get; set; } = "Basic"; // Free, Basic (50k/month), Pro (200k/month)
        public int CurrentMonthThreadCount { get; set; } = 8;
        public int MaxMonthlyThreadLimit { get; set; } = 20; // 3 for Free, 20 for Basic, 100 for Pro
        public DateTime LastThreadResetDate { get; set; } = DateTime.Now.AddDays(-12);
        
        public int DailyUndoCount { get; set; } = 3;
        public int MaxDailyUndoLimit { get; set; } = 5; // 2 for Free, 5 for Basic, 15 for Pro
        public DateTime LastUndoDate { get; set; } = DateTime.Now;

        // Delivery Addresses
        public List<DeliveryAddressItem> DeliveryAddresses { get; set; } = new List<DeliveryAddressItem>();
    }

    public class DeliveryAddressItem
    {
        public long AddressID { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string DetailAddress { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }
}
