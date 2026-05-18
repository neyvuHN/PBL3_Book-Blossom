using System;
using System.Collections.Generic;

namespace BookBlossom.Core.DTOs.Importing
{
    public class ImportingDTO
    {
        public long ImportingID { get; set; }
        public long StaffID { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public DateTime ImportDate { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime? RequiredDate { get; set; }
        public DateTime? ShipDate { get; set; }
        public string? ShipAddress { get; set; }

        public List<ImportingDetailDTO> Details { get; set; } = new List<ImportingDetailDTO>();
    }
}
