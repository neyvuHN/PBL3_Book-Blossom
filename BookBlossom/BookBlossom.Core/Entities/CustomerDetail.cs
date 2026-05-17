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

        public virtual User User { get; set; }
    }
}