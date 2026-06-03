using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookBlossom.Core.Interfaces;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using BookBlossom.Core.DTOs;
using BookBlossom.Core.DTOs.CheckoutAndCreateOrder;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace BookBlossom.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : Controller
    {
        private readonly IOrderService _service;
        private readonly ApplicationDbContext _context;
        private readonly IReputationService _reputationService;

        public OrderController(IOrderService service, ApplicationDbContext context, IReputationService reputationService)
        {
            _service = service;
            _context = context;
            _reputationService = reputationService;
        }

        // =========================
        // MVC VIEW ACTIONS - FRONTEND
        // =========================

        [HttpGet("/Cart")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Cart()
        {
            return View();
        }

        [HttpGet("/Checkout")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Checkout()
        {
            return View();
        }

        [HttpGet("/Orders")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Orders()
        {
            return View();
        }

        [HttpGet("/Order/PaymentResult")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult PaymentResult(long orderId, string status, string message = "")
        {
            ViewBag.OrderId = orderId;
            ViewBag.Status = status;
            ViewBag.Message = message;
            return View();
        }

        // =========================
        // API ACTIONS - BACKEND
        // =========================

        // API 1: Khởi tạo tiến trình Đặt hàng (Checkout & Tạo đơn hàng)
        [HttpPost("checkout")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> CheckoutApi([FromBody] CheckoutRequestDTO dto)
        {
            if (dto == null) return BadRequest("Dữ liệu truyền lên trống.");

            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !long.TryParse(customerIdStr, out long customerId))
            {
                return Unauthorized(new { message = "Hết phiên đăng nhập hoặc Token không hợp lệ. Vui lòng đăng nhập lại!" });
            }

            try
            {
                var result = await _service.CreateOrderAsync(customerId, dto);
                return StatusCode(201, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi hệ thống khi xử lý đơn hàng.", detail = ex.Message });
            }
        }

        // API 1.1: Lấy lịch sử danh sách đơn hàng của chính Customer đăng nhập
        [HttpGet("customer/my-orders")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> GetMyOrders([FromQuery] OrderStatus? status)
        {
            // Trích xuất CustomerID từ Token bảo mật
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !long.TryParse(customerIdStr, out long customerId))
            {
                return Unauthorized(new { message = "Hết phiên đăng nhập hoặc Token không hợp lệ. Vui lòng đăng nhập lại!" });
            }

            try
            {
                var result = await _service.GetOrdersForCustomerAsync(customerId, status);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi hệ thống khi lấy danh sách lịch sử đơn hàng.", detail = ex.Message });
            }
        }

        // API 1.2: Lấy chi tiết đơn hàng của Customer
        [HttpGet("customer/my-orders/{orderId}")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> GetMyOrderDetail(long orderId)
        {
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !long.TryParse(customerIdStr, out long customerId))
            {
                return Unauthorized(new { message = "Hết phiên đăng nhập hoặc Token không hợp lệ. Vui lòng đăng nhập lại!" });
            }

            try
            {
                var result = await _service.GetOrderDetailForCustomerAsync(customerId, orderId);
                if (result == null) return NotFound(new { message = "Không tìm thấy đơn hàng hoặc bạn không có quyền truy cập." });
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi hệ thống khi lấy chi tiết đơn hàng.", detail = ex.Message });
            }
        }

        // API 1.3: Reveal Sách Thật (Sách Mù) trong đơn hàng đã hoàn thành
        [HttpGet("customer/my-orders/{orderId}/reveal-real-book/{blindBookId}")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> RevealRealBook(long orderId, long blindBookId)
        {
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !long.TryParse(customerIdStr, out long customerId))
            {
                return Unauthorized(new { message = "Hết phiên đăng nhập hoặc Token không hợp lệ. Vui lòng đăng nhập lại!" });
            }

            try
            {
                var result = await _service.RevealBlindBookAsync(customerId, orderId, blindBookId);
                if (result == null) return BadRequest(new { message = "Đơn hàng này chưa hoàn thành hoặc không có Sách Mù nào cần Reveal." });
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi hệ thống khi Reveal sách thật.", detail = ex.Message });
            }
        }

        // API 1.4: Hủy đơn hàng của Customer
        [HttpPut("customer/my-orders/{orderId}/cancel")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> CancelMyOrder(long orderId, [FromBody] CancelOrderRequestDTO dto)
        {
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !long.TryParse(customerIdStr, out long customerId))
            {
                return Unauthorized(new { message = "Hết phiên đăng nhập hoặc Token không hợp lệ. Vui lòng đăng nhập lại!" });
            }

            try
            {
                var result = await _service.CancelOrderCustomerAsync(customerId, orderId, dto?.Reason ?? "Không có lý do");
                if (!result) return BadRequest(new { message = "Không thể hủy đơn hàng này." });
                return Ok(new { message = "Hủy đơn hàng thành công." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi hủy đơn hàng.", detail = ex.Message });
            }
        }

        // API 1.4: Xác nhận đã nhận hàng
        [HttpPut("customer/my-orders/{orderId}/confirm-received")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> ConfirmMyOrderReceived(long orderId)
        {
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !long.TryParse(customerIdStr, out long customerId))
            {
                return Unauthorized(new { message = "Hết phiên đăng nhập hoặc Token không hợp lệ. Vui lòng đăng nhập lại!" });
            }

            try
            {
                var result = await _service.ConfirmOrderReceivedCustomerAsync(customerId, orderId);
                if (!result) return BadRequest(new { message = "Không thể xác nhận đơn hàng này." });
                return Ok(new { message = "Xác nhận đã nhận hàng thành công." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi xác nhận nhận hàng.", detail = ex.Message });
            }
        }

        // API 2: Xóa địa chỉ giao hàng không cần thiết khỏi sổ địa chỉ
        [HttpDelete("address/{addressId}")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> DeleteAddress(long addressId)
        {
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !long.TryParse(customerIdStr, out long customerId))
            {
                return Unauthorized(new { message = "Hết phiên đăng nhập hoặc Token không hợp lệ." });
            }

            try
            {
                var address = await _context.Set<DeliveryAddress>()
                    .FirstOrDefaultAsync(da => da.AddressID == addressId && da.CustomerID == customerId);

                if (address == null)
                {
                    return NotFound(new { message = "Địa chỉ không tồn tại hoặc bạn không có quyền xóa địa chỉ này." });
                }

                _context.Set<DeliveryAddress>().Remove(address);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Xóa địa chỉ nhận hàng thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi xóa địa chỉ.", detail = ex.Message });
            }
        }

        // API 2.1: Lấy danh sách địa chỉ giao hàng của User hiện tại (Customer)
        [HttpGet("address")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> GetAddresses()
        {
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !long.TryParse(customerIdStr, out long customerId))
            {
                return Unauthorized(new { message = "Hết phiên đăng nhập hoặc Token không hợp lệ." });
            }

            try
            {
                var addresses = await _context.Set<DeliveryAddress>()
                    .Where(da => da.CustomerID == customerId)
                    .Select(da => new AddressResponseDTO
                    {
                        AddressID = da.AddressID,
                        ReceiverName = da.ReceiverName,
                        PhoneNumber = da.PhoneNumber,
                        DetailAddress = da.DetailAddress,
                        IsDefault = da.IsDefault ?? false
                    })
                    .ToListAsync();

                return Ok(addresses);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi lấy danh sách địa chỉ.", detail = ex.Message });
            }
        }

        // API 2.2: Thêm mới địa chỉ giao hàng (Customer)
        [HttpPost("address")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> CreateAddress([FromBody] CreateAddressRequestDTO dto)
        {
            if (dto == null) return BadRequest("Dữ liệu trống.");

            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !long.TryParse(customerIdStr, out long customerId))
            {
                return Unauthorized(new { message = "Hết phiên đăng nhập hoặc Token không hợp lệ." });
            }

            try
            {
                if (dto.IsDefault)
                {
                    var existingDefaults = await _context.Set<DeliveryAddress>()
                        .Where(da => da.CustomerID == customerId && da.IsDefault == true)
                        .ToListAsync();

                    foreach (var addr in existingDefaults)
                    {
                        addr.IsDefault = false;
                    }
                }

                var newAddress = new DeliveryAddress
                {
                    CustomerID = customerId,
                    ReceiverName = dto.ReceiverName,
                    PhoneNumber = dto.PhoneNumber,
                    DetailAddress = dto.DetailAddress,
                    IsDefault = dto.IsDefault
                };

                await _context.Set<DeliveryAddress>().AddAsync(newAddress);
                await _context.SaveChangesAsync();

                return StatusCode(201, new AddressResponseDTO
                {
                    AddressID = newAddress.AddressID,
                    ReceiverName = newAddress.ReceiverName,
                    PhoneNumber = newAddress.PhoneNumber,
                    DetailAddress = newAddress.DetailAddress,
                    IsDefault = newAddress.IsDefault ?? false
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi thêm địa chỉ mới.", detail = ex.Message });
            }
        }

        // --- CÁC API DÀNH CHO STORE MANAGER (Module 3.3) ---

        // API 3: Lấy danh sách đơn hàng cho StoreManager (Kanban/List)
        [HttpGet("store")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetOrders([FromQuery] OrderStatus? status, [FromQuery] string? searchTerm)
        {
            try
            {
                var result = await _service.GetOrdersForStoreAsync(status, searchTerm);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy danh sách đơn hàng.", detail = ex.Message });
            }
        }

        // API 4: Lấy thông tin chi tiết của một đơn hàng
        [HttpGet("store/{orderId}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetOrderDetail(long orderId)
        {
            try
            {
                var result = await _service.GetOrderDetailForStoreAsync(orderId);
                if (result == null) return NotFound(new { message = "Không tìm thấy đơn hàng." });
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy chi tiết đơn hàng.", detail = ex.Message });
            }
        }

        // API 5: Xác nhận đơn hàng loạt (Store confirm)
        [HttpPost("store/confirm")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ConfirmOrders([FromBody] BulkConfirmRequestDTO request)
        {
            if (request == null || request.OrderIds == null || !request.OrderIds.Any())
            {
                return BadRequest("Danh sách ID đơn hàng trống.");
            }

            try
            {
                var success = await _service.ConfirmOrdersAsync(request.OrderIds);
                if (!success) return BadRequest(new { message = "Không có đơn hàng nào hợp lệ được xác nhận." });
                return Ok(new { message = "Xác nhận các đơn hàng thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi xác nhận đơn hàng.", detail = ex.Message });
            }
        }

        // API 6: Cập nhật trạng thái đơn hàng loạt
        [HttpPut("store/status")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdateOrdersStatus([FromBody] BulkStatusUpdateRequestDTO request)
        {
            if (request == null || request.OrderIds == null || !request.OrderIds.Any())
            {
                return BadRequest("Danh sách ID đơn hàng trống.");
            }

            try
            {
                var success = await _service.UpdateOrdersStatusAsync(request.OrderIds, request.Status);
                if (!success) return BadRequest(new { message = "Cập nhật trạng thái thất bại hoặc không có đơn hàng nào thay đổi." });
                return Ok(new { message = "Cập nhật trạng thái các đơn hàng thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi cập nhật trạng thái đơn hàng.", detail = ex.Message });
            }
        }

        // API 7: Xuất PDF hóa đơn cho từng đơn
        [HttpGet("store/{orderId}/invoice")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetInvoicePdf(long orderId)
        {
            try
            {
                var pdfBytes = await _service.GenerateInvoicePdfAsync(orderId);
                return File(pdfBytes, "application/pdf", $"Invoice_{orderId}.pdf");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi xuất hóa đơn PDF.", detail = ex.Message });
            }
        }

        // API 8: Xuất PDF hóa đơn gộp cho danh sách đơn hàng
        [HttpPost("store/invoices")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetInvoicesPdf([FromBody] List<long> orderIds)
        {
            if (orderIds == null || !orderIds.Any())
            {
                return BadRequest("Danh sách ID đơn hàng trống.");
            }

            try
            {
                var pdfBytes = await _service.GenerateInvoicesPdfAsync(orderIds);
                return File(pdfBytes, "application/pdf", "Invoices.pdf");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi xuất danh sách hóa đơn PDF.", detail = ex.Message });
            }
        }

        // API 9: Xác nhận thanh toán online thành công cho đơn hàng
        [HttpPost("{orderId}/payment-success")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> ProcessPaymentSuccess(long orderId)
        {
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !long.TryParse(customerIdStr, out long customerId))
            {
                return Unauthorized(new { message = "Hết phiên đăng nhập hoặc Token không hợp lệ." });
            }

            try
            {
                var order = await _context.Set<Order>().FirstOrDefaultAsync(o => o.OrderID == orderId);
                if (order == null)
                {
                    return NotFound(new { message = "Không tìm thấy đơn hàng." });
                }

                if (order.CustomerID != customerId)
                {
                    return Forbid();
                }

                var success = await _service.ProcessPaymentSuccessAsync(orderId);
                if (!success)
                {
                    return BadRequest(new { message = "Xác nhận thanh toán trực tuyến thất bại." });
                }

                return Ok(new { message = "Thanh toán thành công và đã cộng điểm uy tín." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi xử lý thanh toán thành công.", detail = ex.Message });
            }
        }
    }
}