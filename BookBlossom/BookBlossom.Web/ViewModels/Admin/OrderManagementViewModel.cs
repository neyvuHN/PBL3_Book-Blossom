using System.Collections.Generic;

namespace BookBlossom.Web.ViewModels.Admin
{
    public class OrderManagementViewModel
    {
        public List<AdminOrderItemViewModel> Orders { get; set; } = new();
        public List<AdminReturnedItemViewModel> ReturnedItems { get; set; } = new();
        public List<AdminEscalatedComplaintViewModel> Complaints { get; set; } = new();
    }

    public class AdminOrderItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public string Isbn { get; set; } = string.Empty;
        public string ImagePreviewUrl { get; set; } = string.Empty;
        public string BuyerName { get; set; } = string.Empty;
        public string BuyerAvatarUrl { get; set; } = string.Empty;
        public string CustomerNote { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string FundsStatus { get; set; } = "Held in Escrow";
        
        /// <summary>
        /// Status: "Pending Confirmation", "To Ship", "In Transit", "Completed", "Refund/Dispute"
        /// </summary>
        public string Status { get; set; } = "Pending Confirmation";
        
        /// <summary>
        /// Mock Sub-status for simulator: "Packing", "Handed to carrier", "In Transit", "Delivered"
        /// </summary>
        public string SubStatus { get; set; } = string.Empty;
        
        public string CreatedAt { get; set; } = string.Empty;
        
        /// <summary>
        /// Remaining hours for 48h confirmation limit (avoid KPI penalty)
        /// e.g. "44h 12m"
        /// </summary>
        public string RemainingTimeText { get; set; } = string.Empty;
        public double RemainingHours { get; set; } = 48.0;
        
        // Blind date features
        public bool IsBlindDate { get; set; }
        public string BlindDateHiddenTitle { get; set; } = string.Empty;
        public string BlindDateNote { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
    }

    public class AdminReturnedItemViewModel
    {
        public string Id { get; set; } = string.Empty; // Return Ticket Id
        public string OrderId { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal RefundAmount { get; set; }
        public string ModeratorDecision { get; set; } = "Approve Return & Refund";
        public string ReturnReason { get; set; } = string.Empty;
        
        /// <summary>
        /// "Pending Restock", "Restocked"
        /// </summary>
        public string RestockStatus { get; set; } = "Pending Restock";
        public string TransferredDate { get; set; } = string.Empty;
    }

    public class AdminEscalatedComplaintViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string BuyerName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        
        /// <summary>
        /// "Review" or "Complaint"
        /// </summary>
        public string Type { get; set; } = "Complaint";
        public int Rating { get; set; } = 5; // relevant for reviews
        public string Content { get; set; } = string.Empty;
        public string ModeratorNote { get; set; } = string.Empty;
        
        /// <summary>
        /// "Pending Support", "Resolved"
        /// </summary>
        public string Status { get; set; } = "Pending Support";
        public string TransferredDate { get; set; } = string.Empty;
    }
}
