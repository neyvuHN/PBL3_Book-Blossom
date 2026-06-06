using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using BookBlossom.Core.Interfaces;
using BookBlossom.Core.DTOs;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace BookBlossom.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReturnController : ControllerBase
    {
        private readonly IReturnService _service;

        public ReturnController(IReturnService service)
        {
            _service = service;
        }

        // API 1: Khách hàng tạo yêu cầu khiếu nại & trả hàng
        [HttpPost("order/{orderId}")]
        [Authorize(Policy = "CustomerOnly")]
        [RequestSizeLimit(524288000)] // 500 MB
        [RequestFormLimits(MultipartBodyLengthLimit = 524288000)] // 500 MB
        public async Task<IActionResult> CreateReturnRequest(
            [FromRoute] long orderId, 
            [FromForm] CreateReturnRequestDTO dto)
        {
            if (dto == null) return BadRequest("Dữ liệu khiếu nại trống.");

            if (dto.VideoFile == null || dto.VideoFile.Length == 0)
            {
                ModelState.AddModelError("VideoFile", "Vui lòng tải lên video mở hộp (unbox) để đối chiếu.");
                return BadRequest(ModelState);
            }

            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !long.TryParse(customerIdStr, out long customerId))
            {
                return Unauthorized(new { message = "Hết phiên đăng nhập hoặc Token không hợp lệ." });
            }

            try
            {
                var result = await _service.CreateReturnRequestAsync(customerId, orderId, dto, dto.VideoFile);
                return StatusCode(201, result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi tạo yêu cầu khiếu nại.", detail = ex.Message });
            }
        }

        // API 2: Lấy danh sách khiếu nại (Staff Only - Moderator + StoreManager)
        [HttpGet("staff")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetReturnRequests([FromQuery] byte? status)
        {
            try
            {
                var result = await _service.GetReturnRequestsAsync(status);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy danh sách khiếu nại.", detail = ex.Message });
            }
        }

        // API 3: Xem chi tiết một khiếu nại (Staff Only)
        [HttpGet("staff/{requestId}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetReturnRequestDetail(long requestId)
        {
            try
            {
                var result = await _service.GetReturnRequestByIdAsync(requestId);
                if (result == null) return NotFound(new { message = "Không tìm thấy yêu cầu khiếu nại." });
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy chi tiết khiếu nại.", detail = ex.Message });
            }
        }

        // API 4: Moderator / StoreManager duyệt khiếu nại
        [HttpPost("staff/{requestId}/review")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ReviewReturnRequest(long requestId, [FromBody] ReviewReturnRequestDTO dto)
        {
            if (dto == null) return BadRequest("Dữ liệu duyệt trống.");

            var staffIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(staffIdStr) || !long.TryParse(staffIdStr, out long staffId))
            {
                return Unauthorized(new { message = "Hết phiên đăng nhập hoặc Token không hợp lệ." });
            }

            try
            {
                var success = await _service.ReviewReturnRequestAsync(staffId, requestId, dto);
                if (!success) return BadRequest(new { message = "Duyệt yêu cầu thất bại." });
                return Ok(new { message = dto.IsApproved ? "Chấp nhận khiếu nại thành công, hàng đã được hoàn và nhập kho." : "Đã từ chối khiếu nại trả hàng." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Đã xảy ra lỗi khi duyệt khiếu nại.", detail = ex.Message });
            }
        }
    }
}
