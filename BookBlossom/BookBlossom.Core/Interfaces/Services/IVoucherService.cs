using System.Collections.Generic;
using System.Threading.Tasks;
using BookBlossom.Core.DTOs;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface IVoucherService
    {
        // === CRUD (Marketing / Admin) ===
        Task<IEnumerable<VoucherDTO>> GetAllVouchersAsync();
        Task<VoucherDTO?> GetVoucherByIdAsync(long voucherId);
        Task<VoucherDTO> CreateVoucherAsync(CreateVoucherDTO dto);
        Task<VoucherDTO?> UpdateVoucherAsync(long voucherId, UpdateVoucherDTO dto);
        Task<bool> DeleteVoucherAsync(long voucherId);
        Task<VoucherUsageStatsDTO?> GetVoucherStatsAsync(long voucherId);

        // === Ví Voucher (Customer) ===
        // Danh sách voucher trong ví của khách hàng
        Task<IEnumerable<CustomerVoucherDTO>> GetMyVouchersAsync(long customerId);

        // Claim voucher vào ví (Customer tự lấy mã)
        Task<bool> ClaimVoucherAsync(long customerId, string voucherCode);

        // === Checkout Integration ===
        // Validate và tính giá trị giảm giá (gọi từ OrderService trước khi tạo đơn)
        Task<VoucherValidationResultDTO> ValidateAndApplyVoucherAsync(
            long customerId,
            string voucherCode,
            decimal orderSubTotal,
            List<long> bookCategoryIds,
            List<long> bookIds);

        // Đánh dấu đã dùng sau khi tạo đơn thành công
        Task MarkVoucherAsUsedAsync(long customerId, long voucherId, long orderId);

        // Hoàn trả voucher khi đơn bị hủy (nếu IsAutoRefundable = true)
        Task RefundVoucherIfApplicableAsync(long orderId);
    }
}
