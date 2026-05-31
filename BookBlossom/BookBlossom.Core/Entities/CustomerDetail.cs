using System;

namespace BookBlossom.Core.Entities
{
    public class CustomerDetail
    {
        public long CustomerID { get; set; } // Vừa là PK, vừa là FK

        public bool IsOnboardingCompleted { get; set; }
        public decimal TotalSpending { get; set; }
        public int DailyUndoCount { get; set; }
        public DateTime? LastUndoDate { get; set; }
        public int CurrentMonthThreadCount { get; set; }
        public DateTime? LastThreadResetDate { get; set; }
        public int CurrentOrderStreak { get; set; }
        public int ShareCount { get; set; } = 0;
        public virtual User User { get; set; }

        public virtual MembershipRank? MembershipRank { get; set; }
        public virtual ServicePackage? ServicePackage { get; set; }
        public virtual CustomerService? CustomerService { get; set; }
        public ICollection<CustomerReputation> CustomerReputations {get; set; } = new List<CustomerReputation>();
        public ICollection<CustomerPreference> CustomerPreferences {get; set; } = new List<CustomerPreference>();
        public virtual ICollection<BadgeCustomer> BadgeCustomers { get; set; } = new List<BadgeCustomer>();
    }
}