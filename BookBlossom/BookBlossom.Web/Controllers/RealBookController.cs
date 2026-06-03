using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // 🚨 BẮT BUỘC: Thêm thư viện này để dùng Phân quyền
using BookBlossom.Core.Interfaces;
using BookBlossom.Core.DTOs.Book;
using BookBlossom.Core.Enums;
using BookBlossom.Core.Interfaces.Services;
using System.Security.Claims;
using System.Text.Json;

namespace BookBlossom.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RealBookController : ControllerBase
    {
        private readonly IRealBookService _realBookService;
        private readonly IAuditService _auditService;

        public RealBookController(IRealBookService realBookService, IAuditService auditService)
        {
            _realBookService = realBookService;
            _auditService = auditService;
        }

        // ==========================================
        // 🔒 CÁC API THAY ĐỔI DỮ LIỆU - CHỈ DÀNH CHO STAFF
        // ==========================================

        // 1. POST: api/realbook (Thêm mới sách + Upload file PDF)
        [HttpPost]
        [Authorize(Policy = "AdminOnly")] // 🚨 ĐÃ ĐÁP ỨNG: Chỉ StoreManager và Admin mới được vào
        public async Task<IActionResult> Create([FromForm] CreateRealBookDTO request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var result = await _realBookService.CreateRealBookAsync(request);
                if (result)
                {
                    var adminIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (long.TryParse(adminIdStr, out long adminId))
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                        await _auditService.LogActionAsync(
                            adminId,
                            null,
                            ActionType.ADD_BOOK,
                            "RealBooks",
                            null,
                            JsonSerializer.Serialize(new { Title = request.Title, ISBN = request.ISBN, Price = request.Price, Stock = request.UnitsInStock }),
                            ipAddress
                        );
                    }
                    return StatusCode(201, new { message = "Thêm mới sách thật thành công!" });
                }
                
                return BadRequest(new { message = "Không thể thêm sách. Vui lòng kiểm tra lại." });
            }
            catch (Exception ex)
            {
                // 🚨 CẢI TIẾN: Nếu lỗi do DB, bóc tách InnerException để Swagger hiển thị nguyên nhân gốc rễ (Trùng ISBN, v.v...)
                var detailedError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return BadRequest(new { message = detailedError });
            }
        }

        // 2. PUT: api/realbook/{id} (Cập nhật thông tin sách)
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")] // 🚨 ĐÃ ĐÁP ỨNG: Chặn đứng Buyer/Khách vãng lai sửa dữ liệu
        public async Task<IActionResult> Update(long id, [FromForm] UpdateRealBookDTO request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var result = await _realBookService.UpdateRealBookAsync(id, request);
                if (result) return Ok(new { message = "Cập nhật thông tin sách thành công!" });
                
                return BadRequest(new { message = "Cập nhật thất bại." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 3. DELETE: api/realbook/{id} (Xóa mềm - Ngừng kinh doanh sách)
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")] // 🚨 ĐÃ ĐÁP ỨNG: Bảo vệ kho hàng nghiêm ngặt
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _realBookService.DeleteRealBookAsync(id);
            if (result) return Ok(new { message = "Đã chuyển trạng thái ngừng kinh doanh thành công!" });
            
            return NotFound(new { message = $"Không tìm thấy cuốn sách có ID = {id} để xóa." });
        }


        // ==========================================
        // 🔓 CÁC API XEM THÔNG TIN - CHO PHÉP TRUY CẬP PUBLIC
        // ==========================================

        // 4. GET: api/realbook (Lấy danh sách sách công khai)
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string searchTerm = "", 
            [FromQuery] string category = "", 
            [FromQuery] SortOrder sortOrder = SortOrder.Ascending,
            [FromQuery] bool includeDiscontinued = false)
        {
            bool finalIncludeDiscontinued = includeDiscontinued;
            if (includeDiscontinued)
            {
                // Chỉ cho phép admin xem các sách đã ngừng kinh doanh
                var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                var statusClaim = User.FindFirst("AccountStatus")?.Value;
                
                bool isAdmin = roleClaim == "Admin" || roleClaim == ((int)UserRole.Admin).ToString();
                bool isActive = statusClaim == "Active" || statusClaim == "1" || statusClaim == ((int)AccountStatus.Active).ToString();
                
                if (!isAdmin || !isActive)
                {
                    finalIncludeDiscontinued = false;
                }
            }

            var books = await _realBookService.GetAllRealBooksAsync(searchTerm, category, sortOrder, finalIncludeDiscontinued);
            return Ok(books);
        }

        // 5. GET: api/realbook/{id} (Lấy chi tiết một cuốn sách)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var book = await _realBookService.GetRealBookByIdAsync(id);
            if (book == null) 
                return NotFound(new { message = $"Không tìm thấy sách hoặc sách đã ngừng kinh doanh." });
                
            return Ok(book);
        }

        // 6. GET: api/realbook/category/{categoryId} (Gợi ý sách liên quan)
        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult> GetByCategoryId(long categoryId, [FromQuery] SortOrder sortOrder = SortOrder.Ascending)
        {
            var books = await _realBookService.GetRealBooksByCategoryIdAsync(categoryId, sortOrder);
            return Ok(books);
        }
    }
}