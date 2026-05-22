using System.Threading.Tasks;
using BookBlossom.Core.DTOs.CheckoutAndCreateOrder;

namespace BookBlossom.Core.Interfaces
{
    public interface IOrderService
    {
        // Xử lý tạo đơn hàng từ giỏ hàng, validate kho kép, check điểm uy tín và áp voucher
        Task<OrderResponseDTO> CreateOrderAsync(long customerId, CheckoutRequestDTO request);
        
        // Bạn có thể bổ sung thêm các hàm quản lý vòng đời đơn hàng ở đây sau này:
        // Task<bool> CancelOrderAsync(long orderId, long customerId);
    }
}