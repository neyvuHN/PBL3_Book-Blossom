using System;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.Entities
{
    public class StaffDetail
    {
        public int StaffID { get; set; } // Vừa là PK, vừa là FK

        public string? Address { get; set; }
        public bool IsOnboardingCompleted { get; set; }
        public Department Department { get; set; } 
        public StaffPosition Position { get; set; }   
        public DateTime HireDate { get; set; }
        public ContractType ContractType { get; set; }
        public decimal Salary { get; set; }
        public string? BankAccount { get; set; }
        public string? TaxCode { get; set; }
        public string? Experience { get; set; }
        public decimal KPIScore { get; set; }

        public virtual User User { get; set; } = null!;
    }
}
