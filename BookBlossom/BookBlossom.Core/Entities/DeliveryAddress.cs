using System;

namespace BookBlossom.Core.Entities
{
    public class DeliveryAddress
    {
        public long AddressID { get; set; }
        public long CustomerID { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string DetailAddress { get; set; } = string.Empty;
        public bool? IsDefault { get; set; } = false;
    }
}