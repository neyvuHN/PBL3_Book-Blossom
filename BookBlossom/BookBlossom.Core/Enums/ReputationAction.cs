namespace BookBlossom.Core.Enums
{
    public enum ReputationAction : byte
    {
        AccountCreation,       // Khởi tạo: +100
        CodDeliverySuccess,   // Nhận hàng COD thành công: +2
        OnlinePaymentSuccess,  // Thanh toán trước: +5
        ReviewWithMedia,       // Review có ảnh + video: +2
        QualityReviewBonus,    // Review chất lượng (>= 5 like): +5
        ReportedTrue,          // Bị report đúng: -10
        ShopPackedCancellation,// Hủy đơn sau khi shop đóng gói: -5
        OrderBombed,           // Bom hàng (Shipper giao không được): -25
        ReviewThreadDeleted,   // Bị xóa bài review/thread: -5
        RankUpdate             // Thăng/giáng hạng thành viên
    }
}
