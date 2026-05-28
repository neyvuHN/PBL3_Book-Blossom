using System;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.Entities
{
    public class ReturnRequest
    {
        public long ReturnRequestID { get; set; }
        public long OrderID { get; set; }
        public long BookID { get; set; }
        public long? StaffID { get; set; }
        public int ReturnQuantity { get; set; }
        public DateTime? RequestDate { get; set; } = DateTime.UtcNow;
        public string ReturnReason { get; set; } = string.Empty;
        public string UnboxVideoPath { get; set; } = string.Empty;
        public ResolutionType ResolutionType { get; set; }
        public ReturnStatus ReturnStatus { get; set; } = ReturnStatus.Pending;
        public string? RejectReason { get; set; }
        public string? GatewayTransactionID { get; set; }
        public decimal? RefundAmount { get; set; }
        public DateTime? RefundCompletedDate { get; set; }

        // Navigation Properties
        public virtual Order? Order { get; set; }
        public virtual RealBook? RealBook { get; set; }
        public virtual StaffDetail? StaffDetail { get; set; }
    }
}
