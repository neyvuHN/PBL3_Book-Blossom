using Microsoft.AspNetCore.Mvc;
using BookBlossom.Core.Interfaces;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BookBlossom.Core.Entities;

namespace BookBlossom.Web.Controllers
{
    [ApiController]
    public class PaymentController : Controller
    {
        private readonly IVnPayService _vnPayService;
        private readonly ApplicationDbContext _context;
        private readonly IOrderService _orderService;

        public PaymentController(IVnPayService vnPayService, ApplicationDbContext context, IOrderService orderService)
        {
            _vnPayService = vnPayService;
            _context = context;
            _orderService = orderService;
        }

        [HttpPost("/api/payment/vnpay/create")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> CreateVnPayUrl([FromBody] CreateVnPayRequest request)
        {
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !long.TryParse(customerIdStr, out long customerId))
            {
                return Unauthorized(new { success = false, message = "Hết phiên đăng nhập." });
            }

            var order = await _context.Set<Order>().FirstOrDefaultAsync(o => o.OrderID == request.OrderId);
            if (order == null)
            {
                return NotFound(new { success = false, message = "Đơn hàng không tồn tại." });
            }
            
            if (order.CustomerID != customerId)
            {
                return Forbid();
            }

            if (order.PaymentStatus == 1)
            {
                return BadRequest(new { success = false, message = "Đơn hàng đã được thanh toán." });
            }

            var paymentUrl = _vnPayService.CreatePaymentUrl(order.OrderID, order.TotalAmount, HttpContext);

            return Ok(new { success = true, paymentUrl = paymentUrl });
        }

        [HttpGet("/Payment/VnPayReturn")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> VnPayReturn()
        {
            var vnpayData = Request.Query;
            bool isValidSignature = _vnPayService.ValidateSignature(vnpayData);

            if (!isValidSignature)
            {
                return RedirectToAction("PaymentResult", "Order", new { orderId = 0, status = "failed", message = "Chữ ký không hợp lệ" });
            }

            string vnp_ResponseCode = vnpayData["vnp_ResponseCode"];
            string vnp_TransactionStatus = vnpayData["vnp_TransactionStatus"];
            string vnp_TxnRef = vnpayData["vnp_TxnRef"];

            var parts = vnp_TxnRef?.Split('_');
            if (parts == null || parts.Length < 2 || !long.TryParse(parts[1], out long orderId))
            {
                return RedirectToAction("PaymentResult", "Order", new { orderId = 0, status = "failed", message = "Mã giao dịch không hợp lệ" });
            }

            if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
            {
                await _orderService.ProcessPaymentSuccessAsync(orderId);
                return RedirectToAction("PaymentResult", "Order", new { orderId = orderId, status = "success" });
            }
            else
            {
                // Thất bại
                await _orderService.DeleteFailedOrderAsync(orderId);
                return RedirectToAction("PaymentResult", "Order", new { orderId = orderId, status = "failed", vnp_ResponseCode = vnp_ResponseCode });
            }
        }
    }

    public class CreateVnPayRequest
    {
        public long OrderId { get; set; }
    }
}
