using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BookBlossom.Core.DTOs;
using BookBlossom.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookBlossom.Web.Areas.Management.Controllers
{
    [Area("Management")]
    [Route("api/management/[controller]")]
    [ApiController]
    public class VoucherController : ControllerBase
    {
        private readonly IVoucherService _voucherService;

        public VoucherController(IVoucherService voucherService)
        {
            _voucherService = voucherService;
        }

        // ─── ADMIN / MARKETING: CRUD ─────────────────────────────────────────

        /// <summary>Lấy danh sách tất cả voucher (Admin/Marketing)</summary>
        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetAll()
        {
            var vouchers = await _voucherService.GetAllVouchersAsync();
            return Ok(vouchers);
        }

        /// <summary>Lấy chi tiết một voucher theo ID</summary>
        [HttpGet("{id:long}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetById(long id)
        {
            var voucher = await _voucherService.GetVoucherByIdAsync(id);
            if (voucher == null) return NotFound(new { message = $"Không tìm thấy voucher ID {id}." });
            return Ok(voucher);
        }

        /// <summary>Tạo voucher mới (Admin/Marketing)</summary>
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create([FromBody] CreateVoucherDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var created = await _voucherService.CreateVoucherAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.VoucherID }, created);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>Cập nhật voucher (Admin/Marketing)</summary>
        [HttpPut("{id:long}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateVoucherDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updated = await _voucherService.UpdateVoucherAsync(id, dto);
            if (updated == null) return NotFound(new { message = $"Không tìm thấy voucher ID {id}." });
            return Ok(updated);
        }

        /// <summary>Xóa voucher (Admin)</summary>
        [HttpDelete("{id:long}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _voucherService.DeleteVoucherAsync(id);
            if (!result) return NotFound(new { message = $"Không tìm thấy voucher ID {id}." });
            return Ok(new { message = $"Đã xóa voucher ID {id} thành công." });
        }

        /// <summary>Thống kê sử dụng voucher (Usage Rate & ROI)</summary>
        [HttpGet("{id:long}/stats")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetStats(long id)
        {
            var stats = await _voucherService.GetVoucherStatsAsync(id);
            if (stats == null) return NotFound(new { message = $"Không tìm thấy voucher ID {id}." });
            return Ok(stats);
        }

        // ─── CUSTOMER: VÍ VOUCHER ────────────────────────────────────────────

        /// <summary>Xem danh sách voucher trong ví (Customer)</summary>
        [HttpGet("my-vouchers")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> GetMyVouchers()
        {
            var customerIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(customerIdStr, out long customerId))
                return Unauthorized();

            var vouchers = await _voucherService.GetMyVouchersAsync(customerId);
            return Ok(vouchers);
        }

        /// <summary>Claim voucher vào ví bằng mã code (Customer)</summary>
        [HttpPost("claim")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> Claim([FromBody] ClaimVoucherRequestDTO request)
        {
            var customerIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(customerIdStr, out long customerId))
                return Unauthorized();

            try
            {
                await _voucherService.ClaimVoucherAsync(customerId, request.VoucherCode);
                return Ok(new { message = $"Đã thêm voucher '{request.VoucherCode}' vào ví thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    // DTO đơn giản cho API claim
    public class ClaimVoucherRequestDTO
    {
        public string VoucherCode { get; set; } = string.Empty;
    }
}
