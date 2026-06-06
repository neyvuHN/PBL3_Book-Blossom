using System;
using System.Collections.Generic;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.DTOs
{
    public class OrderListItemDTO
    {
        public long OrderID { get; set; }
        public long CustomerID { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public byte PaymentStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public string? ShipReceiverName { get; set; } = string.Empty;
        public string? ShipPhoneNumber { get; set; } = string.Empty;
        public string? ShipDetailAddress { get; set; } = string.Empty;
        public string? Note { get; set; }
        public string? CancelReason { get; set; }
        public string? ReturnReason { get; set; }
        public ResolutionType? ResolutionType { get; set; }
        public ReturnStatus? ReturnStatus { get; set; }
        public List<OrderItemDTO> OrderItems { get; set; } = new List<OrderItemDTO>();
        public bool IsRated { get; set; }
    }

    public class OrderDetailDTO
    {
        public long OrderID { get; set; }
        public long CustomerID { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhoneNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public byte PaymentStatus { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? ShipReceiverName { get; set; } = string.Empty;
        public string? ShipPhoneNumber { get; set; } = string.Empty;
        public string? ShipDetailAddress { get; set; } = string.Empty;
        public string? Note { get; set; }
        public string? CancelReason { get; set; }
        public string? ReturnReason { get; set; }
        public ResolutionType? ResolutionType { get; set; }
        public ReturnStatus? ReturnStatus { get; set; }
        public List<OrderItemDTO> OrderItems { get; set; } = new List<OrderItemDTO>();
        public bool IsRated { get; set; }
    }

    public class OrderItemDTO
    {
        public long BookID { get; set; }
        public long? BlindBookID { get; set; }
        public bool IsRated { get; set; }
        public string Title { get; set; } = string.Empty;
        public string RealBookTitle { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Discount { get; set; }
        public decimal TotalItemAmount { get; set; }
        public string? SampleFilePath { get; set; }
        public string ISBN { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public string? VoucherBreakdown { get; set; } // [NEW] Dành cho hiển thị trên UI
    }

    public class BulkConfirmRequestDTO
    {
        public List<long> OrderIds { get; set; } = new List<long>();
    }

    public class CancelOrderRequestDTO
    {
        public string? Reason { get; set; }
    }

    public class BulkStatusUpdateRequestDTO
    {
        public List<long> OrderIds { get; set; } = new List<long>();
        public OrderStatus Status { get; set; }
    }

    public class CreateAddressRequestDTO
    {
        public string ReceiverName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string DetailAddress { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }

    public class AddressResponseDTO
    {
        public long AddressID { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string DetailAddress { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }
}
