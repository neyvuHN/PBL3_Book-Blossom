namespace BookBlossom.Core.DTOs.Book
{
    public class RestockBlindBookDTO
    {
        public long BlindBookID { get; set; } // Chỉ định chính xác sản phẩm cũ cần bơm hàng
        public int RequestQuantity { get; set; } // Số lượng muốn bổ sung thêm vào kho
    }
}