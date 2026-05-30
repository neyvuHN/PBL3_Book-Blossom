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
        Task<OrderDetailDTO?> GetOrderDetailForStoreAsync(long orderId);
        Task<bool> ConfirmOrdersAsync(List<long> orderIds);
        Task<bool> UpdateOrdersStatusAsync(List<long> orderIds, OrderStatus status);
        Task<byte[]> GenerateInvoicePdfAsync(long orderId);
        Task<byte[]> GenerateInvoicesPdfAsync(List<long> orderIds);
    }
}