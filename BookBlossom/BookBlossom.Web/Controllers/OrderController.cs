using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookBlossom.Core.Interfaces;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using BookBlossom.Core.DTOs.CheckoutAndCreateOrder;
using BookBlossom.Infrastructure.Data; // Thêm để nhận diện ApplicationDbContext
using Microsoft.EntityFrameworkCore;   // Thêm để dùng FirstOrDefaultAsync
using System.Security.Claims;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BookBlossom.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _service;
        private readonly ApplicationDbContext _context; // Tiêm DBContext vào để phục vụ API xóa địa chỉ

        public OrderController(IOrderService service, ApplicationDbContext context)
        {
            _service = service;
            _context = context;
        }

        // API 1: Khởi tạo tiến trình Đặt hàng (Checkout & Tạo đơn hàng)
        [HttpPost("checkout")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequestDTO dto)
        {
            if (dto == null) return BadRequest("Dữ liệu truyền lên trống.");

            // Tự động bóc tách CustomerID từ chuỗi mã hóa JWT Token
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !long.TryParse(customerIdStr, out long customerId))
            {
                return Unauthorized(new { message = "Hết phiên đăng nhập hoặc Token không hợp lệ. Vui lòng đăng nhập lại!" });
            }

            try
            {
                var result = await _service.CreateOrderAsync(customerId, dto);
                
                // Trả về mã 201 Created kèm dữ liệu hóa đơn chi tiết
                return StatusCode(201, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Bắt các lỗi ràng buộc nghiệp vụ (Điểm uy tín thấp < 60, Hết hàng kho kép)
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Bắt các lỗi hệ thống phát sinh bất ngờ khác
                return BadRequest(new { message = "Đã xảy ra lỗi hệ thống khi xử lý đơn hàng.", detail = ex.Message });
            }
        }

        // 🚀 API 2 BỔ SUNG: Xóa địa chỉ giao hàng không cần thiết khỏi sổ địa chỉ
        [HttpDelete("address/{addressId}")]
        [Authorize(Policy = "CustomerOnly")] // Đồng bộ Policy phân quyền giống API checkout
        public async Task<IActionResult> DeleteAddress(long addressId)
        {
            // Tự động bóc tách CustomerID từ chuỗi mã hóa JWT Token để đảm bảo bảo mật
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !long.TryParse(customerIdStr, out long customerId))
            {
                return Unauthorized(new { message = "Hết phiên đăng nhập hoặc Token không hợp lệ." });
            }

            try
            {
                // Tìm địa chỉ thuộc đúng ID và bắt buộc phải thuộc quyền sở hữu của chính User đang đăng nhập
                var address = await _context.Set<DeliveryAddress>()
                    .FirstOrDefaultAsync(da => da.AddressID == addressId && da.CustomerID == customerId);

                if (address == null)
                {
                    return NotFound(new { message = "Địa chỉ không tồn tại hoặc bạn không có quyền xóa địa chỉ này." });
                }

                // Thực hiện xóa và cập nhật xuống DB
                _context.Set<DeliveryAddress>().Remove(address);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Xóa địa chỉ nhận hàng thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi xóa địa chỉ.", detail = ex.Message });
            }
        }
    }
}