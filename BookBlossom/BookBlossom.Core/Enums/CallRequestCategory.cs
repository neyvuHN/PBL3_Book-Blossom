using System;

namespace BookBlossom.Core.Enums
{
    [Flags]
    public enum CallRequestCategory : byte
    {
        ProductOrBook = 1 << 0,     // 1
        Order = 1 << 1,             // 2
        Payment = 1 << 2,           // 4
        Shipping = 1 << 3,          // 8
        ComplaintOrRefund = 1 << 4, // 16
        Other = 1 << 5              // 32
    }
}
