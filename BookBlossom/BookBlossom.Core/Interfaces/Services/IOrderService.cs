using System.Collections.Generic;
using System.Threading.Tasks;
using BookBlossom.Core.DTOs;
using BookBlossom.Core.DTOs.CheckoutAndCreateOrder;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.Interfaces
{
    public interface IOrderService
    {
        // Xử lý tạo đơn hàng từ giỏ hàng, validate kho kép, check điểm uy tín và áp voucher
        Task<OrderResponseDTO> CreateOrderAsync(long customerId, CheckoutRequestDTO request);
        
        // --- QUẢN LÝ ĐƠN HÀNG (STORE MANAGER) ---
        Task<IEnumerable<OrderListItemDTO>> GetOrdersForStoreAsync(OrderStatus? status, string? searchTerm);
        Task<IEnumerable<OrderListItemDTO>> GetOrdersForCustomerAsync(long customerId, OrderStatus? status);
        Task<OrderDetailDTO?> GetOrderDetailForCustomerAsync(long customerId, long orderId);
        Task<bool> CancelOrderCustomerAsync(long customerId, long orderId, string reason);
        Task<bool> ConfirmOrderReceivedCustomerAsync(long customerId, long orderId);
        
        Task<OrderDetailDTO?> GetOrderDetailForStoreAsync(long orderId);
        Task<bool> ConfirmOrdersAsync(List<long> orderIds);
        Task<bool> UpdateOrdersStatusAsync(List<long> orderIds, OrderStatus status);
        Task<bool> ProcessPaymentSuccessAsync(long orderId);
        Task<byte[]> GenerateInvoicePdfAsync(long orderId);
        Task<byte[]> GenerateInvoicesPdfAsync(List<long> orderIds);
        
        // --- TÍNH NĂNG ĐẶC BIỆT ---
        Task<BookBlossom.Core.DTOs.Book.RealBookDTO?> RevealBlindBookAsync(long customerId, long orderId, long blindBookId);
    }
}