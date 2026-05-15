using System;

namespace BookBlossom.Core.Entities
{
    public class CustomerService
    {
        public long CustomerID { get; set; }
        public long CurrentPackageID { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public virtual User User { get; set; } = null!;
        public virtual ServicePackage ServicePackage { get; set; } = null!;
    }
}
