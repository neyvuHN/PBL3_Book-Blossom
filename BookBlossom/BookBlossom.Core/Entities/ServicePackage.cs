using System.Collections.Generic;

namespace BookBlossom.Core.Entities
{
    public class ServicePackage
    {
        public long PackageID { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DurationDay { get; set; }
        public int ThreadLimit { get; set; }
        public int UndoLimit { get; set; }
        public string? Description { get; set; }

        public virtual ICollection<CustomerService> CustomerServices { get; set; } = new List<CustomerService>();
        public virtual ICollection<CustomerDetail> CustomerDetails { get; set; } = new List<CustomerDetail>();
    }
}
