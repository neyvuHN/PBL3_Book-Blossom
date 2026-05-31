namespace BookBlossom.Core.Enums
{
    public enum NotificationType : byte
    {
        OrderStatus = 0,
        ReturnUpdate = 1,
        ReEngagement = 2,
        NewThread = 3,
        PointChange = 4,
        BadgeEarned = 5,
        RankUp = 6,
        NewInteraction = 7,
        ModWarning = 8,
        NewBookArrival = 9,
        ReportAlert = 10,
        NewReturnRequest = 11,
        KpiWarning = 12,
        None = 13
    }
}
