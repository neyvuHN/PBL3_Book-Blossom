namespace BookBlossom.Core.Enums
{
    public enum ReputationAction : byte
    {
        AccountCreation = 1,       // Khởi tạo: +100
        CodDeliverySuccess = 2,    // Nhận hàng COD thành công: +2
        OnlinePaymentSuccess = 3,  // Thanh toán trước: +5
        ReviewWithMedia = 4,       // Review có ảnh + video: +2
        QualityReviewBonus = 5,    // Review chất lượng (>= 5 like): +5
        ReportedTrue = 6,          // Bị report đúng: -10
        ShopPackedCancellation = 7,// Hủy đơn sau khi shop đóng gói: -5
        OrderBombed = 8,           // Bom hàng (Shipper giao không được): -25
        ReviewThreadDeleted = 9    // Bị xóa bài review/thread: -5
    }
}
