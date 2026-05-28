using System;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.DTOs
{
    public class CreateReturnRequestDTO
    {
        public long BookID { get; set; }
        public int ReturnQuantity { get; set; }
        public string ReturnReason { get; set; } = string.Empty;
        public ResolutionType ResolutionType { get; set; }
    }

    public class ReturnRequestDetailDTO
    {
        public long ReturnRequestID { get; set; }
        public long OrderID { get; set; }
        public long BookID { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public long? StaffID { get; set; }
        public string? StaffName { get; set; }
        public int ReturnQuantity { get; set; }
        public DateTime RequestDate { get; set; }
        public string ReturnReason { get; set; } = string.Empty;
        public string UnboxVideoPath { get; set; } = string.Empty;
        public ResolutionType ResolutionType { get; set; }
        public ReturnStatus ReturnStatus { get; set; }
        public string? RejectReason { get; set; }
        public string? GatewayTransactionID { get; set; }
        public decimal? RefundAmount { get; set; }
        public DateTime? RefundCompletedDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhoneNumber { get; set; } = string.Empty;
    }

    public class ReviewReturnRequestDTO
    {
        public bool IsApproved { get; set; }
        public string? RejectReason { get; set; }
    }
}
