namespace BookBlossom.Core.Enums
{
    public enum OrderStatus : byte
    {
        Pending = 1,
        AwaitingPickup = 2,
        Shipping = 3,
        Delivering = 4,
        Completed = 5,
        Cancelled = 6,
        Returning = 7
    }
}
