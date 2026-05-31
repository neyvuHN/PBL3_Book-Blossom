using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookBlossom.Core.DTOs.Importing;
using BookBlossom.Core.Interfaces.Services;

namespace BookBlossom.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")] // Restricted to Store Managers
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        #region Importing Header Endpoints

        /// <summary>
        /// Tạo mới phiếu nhập kho (kèm theo danh sách chi tiết các sách nếu có)
        /// POST /api/inventory/importings
        /// </summary>
        [HttpPost("importings")]
        public async Task<IActionResult> CreateImporting([FromBody] CreateImportingRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out long staffId))
            {
                return Unauthorized(new { message = "Không tìm thấy thông tin định danh nhân viên." });
            }

            try
            {
                var createdImport = await _inventoryService.CreateImportingAsync(staffId, request);
                return CreatedAtAction(nameof(GetImportingById), new { id = createdImport.ImportingID }, createdImport);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi tạo mới phiếu nhập kho.", details = ex.Message });
            }
        }

        /// <summary>
        /// Lấy chi tiết phiếu nhập kho theo ID
        /// GET /api/inventory/importings/{id}
        /// </summary>
        [HttpGet("importings/{id}")]
        public async Task<IActionResult> GetImportingById(long id)
        {
            try
            {
                var importing = await _inventoryService.GetImportingByIdAsync(id);
                if (importing == null)
                {
                    return NotFound(new { message = $"Không tìm thấy phiếu nhập kho với ID = {id}." });
                }
                return Ok(importing);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi truy vấn phiếu nhập kho.", details = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách toàn bộ các phiếu nhập kho
        /// GET /api/inventory/importings
        /// </summary>
        [HttpGet("importings")]
        public async Task<IActionResult> GetAllImportings()
        {
            try
            {
                var importings = await _inventoryService.GetAllImportingsAsync();
                return Ok(importings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi lấy danh sách phiếu nhập kho.", details = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông tin chung của phiếu nhập kho (Header)
        /// PUT /api/inventory/importings/{id}
        /// </summary>
        [HttpPut("importings/{id}")]
        public async Task<IActionResult> UpdateImporting(long id, [FromBody] UpdateImportingRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var updatedImport = await _inventoryService.UpdateImportingAsync(id, request);
                return Ok(updatedImport);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi cập nhật phiếu nhập kho.", details = ex.Message });
            }
        }

        /// <summary>
        /// Xóa phiếu nhập kho và hoàn tác số lượng sách trong kho
        /// DELETE /api/inventory/importings/{id}
        /// </summary>
        [HttpDelete("importings/{id}")]
        public async Task<IActionResult> DeleteImporting(long id)
        {
            try
            {
                var success = await _inventoryService.DeleteImportingAsync(id);
                if (!success)
                {
                    return NotFound(new { message = $"Không tìm thấy phiếu nhập kho với ID = {id} để xóa." });
                }
                return Ok(new { message = "Xóa phiếu nhập kho và hoàn tác số lượng sách trong kho thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi xóa phiếu nhập kho.", details = ex.Message });
            }
        }

        #endregion

        #region Importing Detail Endpoints

        /// <summary>
        /// Thêm chi tiết một sách nhập vào phiếu nhập kho
        /// POST /api/inventory/importings/{id}/details
        /// </summary>
        [HttpPost("importings/{id}/details")]
        public async Task<IActionResult> AddImportingDetail(long id, [FromBody] CreateImportingDetailRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var detail = await _inventoryService.AddImportingDetailAsync(id, request);
                return Ok(detail);
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
                return StatusCode(500, new { message = "Lỗi hệ thống khi thêm chi tiết sách nhập.", details = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật chi tiết một sách nhập trong phiếu nhập kho
        /// PUT /api/inventory/importings/{importingId}/details/{bookId}
        /// </summary>
        [HttpPut("importings/{importingId}/details/{bookId}")]
        public async Task<IActionResult> UpdateImportingDetail(long importingId, long bookId, [FromBody] UpdateImportingDetailRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var detail = await _inventoryService.UpdateImportingDetailAsync(importingId, bookId, request);
                return Ok(detail);
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
                return StatusCode(500, new { message = "Lỗi hệ thống khi cập nhật chi tiết sách nhập.", details = ex.Message });
            }
        }

        /// <summary>
        /// Xóa chi tiết sách nhập khỏi phiếu nhập kho và hoàn tác số lượng sách
        /// DELETE /api/inventory/importings/{importingId}/details/{bookId}
        /// </summary>
        [HttpDelete("importings/{importingId}/details/{bookId}")]
        public async Task<IActionResult> DeleteImportingDetail(long importingId, long bookId)
        {
            try
            {
                var success = await _inventoryService.DeleteImportingDetailAsync(importingId, bookId);
                if (!success)
                {
                    return NotFound(new { message = $"Không tìm thấy chi tiết sách nhập với ID phiếu = {importingId} và ID sách = {bookId} để xóa." });
                }
                return Ok(new { message = "Xóa chi tiết sách nhập và hoàn tác số lượng sách thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi xóa chi tiết sách nhập.", details = ex.Message });
            }
        }

        #endregion
    }
}
