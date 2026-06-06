using System.Collections.Generic;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.DTOs.CheckoutAndCreateOrder
{
    public class CheckoutRequestDTO
    {
        public long AddressID { get; set; } // Chỉ cần ID này (Hệ thống sẽ tự LOOKUP ra Tên, SĐT, Địa chỉ chi tiết từ DB)
        public PaymentMethod PaymentMethod { get; set; }

        // Danh sách mã voucher muốn áp dụng (có thể trống)
        public List<string> VoucherCodes { get; set; } = new List<string>();

        public string? Note { get; set; }

        // Danh sách các sản phẩm + số lượng được chọn từ giỏ hàng Luminae
        public List<CartItemCheckoutDTO> CartItems { get; set; } = new List<CartItemCheckoutDTO>();
    }

    public class CartItemCheckoutDTO
    {
        public long? BookID { get; set; } // Mã sách thật (luôn luôn có để quản lý kho vật lý)
        public long? BlindBookID { get; set; } // Bắt buộc để Nullable. Nếu là sách mù thì truyền ID vào, sách thường thì để null
        public int Quantity { get; set; }
    }
}