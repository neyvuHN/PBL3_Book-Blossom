namespace BookBlossom.Core.Enums
{
    public enum OrderStatus : byte
    {
        Pending = 0,
        AwaitingPickup = 1,
        Shipping = 2,
        Delivering = 3,
        Completed = 4,
        Cancelled = 5,
        Returning = 6
    }
}
