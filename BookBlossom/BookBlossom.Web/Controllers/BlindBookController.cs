using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookBlossom.Core.Interfaces;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using BookBlossom.Core.DTOs.Book;
using BookBlossom.DTOs.BlindBook;
using System.Security.Claims;

namespace BookBlossom.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlindBookController : ControllerBase
    {
        private readonly IBlindBookService _service;

        public BlindBookController(IBlindBookService service)
        {
            _service = service;
        }

        // API 1: Lấy danh sách (Phân trang + Tìm kiếm công khai / Nội bộ)
        // API 1: Lấy danh sách (Chỉ Tìm kiếm + Lọc theo Quyền hạn, KHÔNG PHÂN TRANG)
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? searchTerm = null, 
            [FromQuery] string? category = null,
            [FromQuery] bool isAwaitingApproval = false)
        {
            var rawBooks = await _service.GetAllBlindBooksAsync();
            var query = rawBooks.AsQueryable();

            // 1. Bộ lọc tìm kiếm theo từ khóa/trích dẫn
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(b => b.Keywords.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || 
                                        b.Quotes.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            // 2. Bộ lọc tìm kiếm theo thể loại
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(b => b.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            // TẦNG LỌC PHÂN QUYỀN VÀ TRẢ VỀ DATA
            
            // Trả về dữ liệu Quản trị cho Nhân viên (Staff công việc nội bộ)
            if (User.Identity?.IsAuthenticated == true && 
            (User.IsInRole("MarketingManager") || User.IsInRole("StoreManager") || User.IsInRole("Admin")))
            {
                if (isAwaitingApproval)
                {
                    query = query.Where(b => b.BlindBookRequestStatus == BlindBookRequestStatus.Pending);
                }

                // Không dùng Skip/Take nữa, lấy tuốt tuột danh sách đã lọc chuyển thành List
                var staffData = query.ToList(); 
                var staffResult = staffData.Select(b => new BlindBookAdminDTO
                {
                    BlindBookID = b.BlindBookID,
                    RealBookID = b.RealBookID,
                    RealBookTitle = b.RealBook?.Title, 
                    Keywords = b.Keywords,
                    Quotes = b.Quotes,
                    Category = b.Category,
                    Hashtags = b.Hashtags,
                    Price = b.Price,
                    RequestQuantity = b.RequestQuantity,
                    StockQuantity = b.StockQuantity,
                    Barcode = b.Barcode,
                    RejectReason = b.RejectReason,
                    Status = b.BlindBookRequestStatus 
                });

                return Ok(staffResult);
            }

            // Trả về dữ liệu bảo mật công khai cho Khách hàng (Buyer công khai)
            query = query.Where(b => b.BlindBookRequestStatus == BlindBookRequestStatus.Approved && !b.IsLocked);
            
            // Không dùng Skip/Take nữa, lấy toàn bộ danh sách thỏa mãn
            var clientData = query.ToList(); 
            var clientResult = clientData.Select(b => new BlindBookClientDTO
            {
                BlindBookID = b.BlindBookID,
                Keywords = b.Keywords,
                Quotes = b.Quotes,
                Category = b.Category,
                Hashtags = b.Hashtags,
                Price = b.Price,
                StockQuantity = b.StockQuantity
            });

            return Ok(clientResult);
        }

        // API 2: Chi tiết sách (Ẩn thông tin nhạy cảm)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var book = await _service.GetByIdAsync(id);
            if (book == null) return NotFound(new { message = "Không tìm thấy gói Sách Mù." });

            var clientDto = new BlindBookClientDTO
            {
                BlindBookID = book.BlindBookID,
                Keywords = book.Keywords,
                Quotes = book.Quotes,
                Category = book.Category,
                Hashtags = book.Hashtags,
                Price = book.Price,
                StockQuantity = book.StockQuantity
            };

            return Ok(clientDto);
        }

        // API 3: Marketing tạo yêu cầu
        [HttpPost]
        [Authorize(Roles = "MarketingManager")]
        public async Task<IActionResult> Create([FromBody] CreateBlindBookDTO dto)
        {
            if (dto == null) return BadRequest("Dữ liệu truyền lên trống.");

            var marketingId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var newBlindBook = new BlindBook
            {
                RealBookID = dto.RealBookID,
                Keywords = dto.Keywords,
                Quotes = dto.Quotes,
                Category = dto.Category,
                Hashtags = dto.Hashtags,
                Price = dto.Price,
                RequestQuantity = dto.RequestQuantity,
                StockQuantity = 0, 
                Barcode = null,     
                IsLocked = false
            };

            try
            {
                var result = await _service.CreateRequestAsync(newBlindBook, marketingId);
                return StatusCode(201, new { message = "Gửi yêu cầu đóng gói thành công!", id = result.BlindBookID });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // API 4: Store Manager duyệt yêu cầu ban đầu
        [HttpPut("approve/{id}")]
        [Authorize(Roles = "StoreManager")]
        public async Task<IActionResult> Approve(long id)
        {
            try
            {
                string storeManagerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                bool isSuccess = await _service.ConfirmBlindBookRequestAsync(id, storeManagerId);
                
                if (!isSuccess) return BadRequest(new { message = "Duyệt yêu cầu thất bại hoặc gói không ở trạng thái chờ." });
                return Ok(new { message = "Duyệt và mở bán gói Sách Mù thành công!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // API 5: Marketing gửi yêu cầu bổ sung hàng (Restock)
        [HttpPost("restock")]
        [Authorize(Roles = "MarketingManager")]
        public async Task<IActionResult> RestockRequest([FromBody] RestockBlindBookDTO dto)
        {
            if (dto == null) return BadRequest("Dữ liệu không hợp lệ.");

            try
            {
                string marketingId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                bool isSuccess = await _service.CreateRestockRequestAsync(dto.BlindBookID, dto.RequestQuantity, marketingId);
                
                if (!isSuccess) return BadRequest(new { message = "Không thể gửi yêu cầu bổ sung hàng." });
                return Ok(new { message = "Gửi yêu cầu bổ sung hàng thành công, đang chờ Kho duyệt!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // API 6: Store Manager duyệt yêu cầu Restock
        [HttpPut("restock/approve/{id}")]
        [Authorize(Roles = "StoreManager")]
        public async Task<IActionResult> ApproveRestock(long id, [FromQuery] int approvedQuantity)
        {
            try
            {
                string storeManagerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                bool isSuccess = await _service.ConfirmRestockRequestAsync(id, approvedQuantity, storeManagerId);
                
                if (!isSuccess) return BadRequest(new { message = "Duyệt bổ sung hàng thất bại." });
                return Ok(new { message = "Đã duyệt và cập nhật tăng số lượng tồn kho Sách Mù thành công!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}