using System;
using System.Collections.Generic;

namespace BookBlossom.Core.Entities
{
    public class Importing
    {
        public long ImportingID { get; set; }
        public long StaffID { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public DateTime ImportDate { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime? RequiredDate { get; set; }
        public DateTime? ShipDate { get; set; }
        public string? ShipAddress { get; set; }

        public virtual StaffDetail Staff { get; set; } = null!;
        public virtual ICollection<ImportingDetail> ImportingDetails { get; set; } = new List<ImportingDetail>();
    }
}
