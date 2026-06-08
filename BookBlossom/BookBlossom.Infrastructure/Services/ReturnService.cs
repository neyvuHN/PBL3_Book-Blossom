using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using BookBlossom.Core.DTOs;
using BookBlossom.Core.Interfaces;
using BookBlossom.Infrastructure.Data;

namespace BookBlossom.Infrastructure.Services
{
    public class ReturnService : IReturnService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _videoUploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "return_videos");

        public ReturnService(ApplicationDbContext context)
        {
            _context = context;
            // Tự động tạo thư mục lưu file video nếu chưa có
            if (!Directory.Exists(_videoUploadFolder))
            {
                Directory.CreateDirectory(_videoUploadFolder);
            }
        }

        public async Task<ReturnRequestDetailDTO> CreateReturnRequestAsync(long customerId, long orderId, CreateReturnRequestDTO dto, IFormFile videoFile)
        {
            // 1. Kiểm tra đơn hàng tồn tại và thuộc Customer này
            var order = await _context.Set<Order>()
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderID == orderId && o.CustomerID == customerId);

            if (order == null)
            {
                throw new KeyNotFoundException("Không tìm thấy đơn hàng hoặc đơn hàng không thuộc quyền sở hữu của bạn.");
            }

            // 2. Kiểm tra trạng thái đơn hàng (Phải là Delivered mới được hoàn trả)
            if (order.OrderStatus != OrderStatus.Delivered)
            {
                throw new InvalidOperationException("Chỉ đơn hàng đã giao (chờ xác nhận) mới được khiếu nại trả hàng.");
            }

            // 3. Kiểm tra sản phẩm có thuộc đơn hàng hay không
            var detail = order.OrderDetails.FirstOrDefault(od => od.BookID == dto.BookID);
            if (detail == null)
            {
                throw new KeyNotFoundException("Sách này không thuộc đơn hàng của bạn.");
            }

            // 4. Kiểm tra số lượng trả hàng
            if (dto.ReturnQuantity <= 0)
            {
                throw new ArgumentException("Số lượng trả hàng phải lớn hơn 0.");
            }
            if (dto.ReturnQuantity > detail.Quantity)
            {
                throw new ArgumentException($"Số lượng trả hàng ({dto.ReturnQuantity}) vượt quá số lượng đã mua ({detail.Quantity}).");
            }

            // 5. Kiểm tra trùng yêu cầu (Không cho gửi trùng khi đang xử lý hoặc đã duyệt)
            var existingRequest = await _context.Set<ReturnRequest>()
                .AnyAsync(r => r.OrderID == orderId && r.BookID == dto.BookID && 
                               (r.ReturnStatus == ReturnStatus.Pending || r.ReturnStatus == ReturnStatus.Approved));
            if (existingRequest)
            {
                throw new InvalidOperationException("Yêu cầu khiếu nại cho sản phẩm này đã tồn tại và đang được xử lý hoặc đã được chấp thuận.");
            }

            // 6. Kiểm tra và tải lên Video Unbox (Bắt buộc)
            if (videoFile == null || videoFile.Length == 0)
            {
                throw new ArgumentException("Bắt buộc phải tải lên video unbox làm bằng chứng.");
            }

            var extension = Path.GetExtension(videoFile.FileName).ToLower();
            var allowedExtensions = new[] { ".mp4", ".mov", ".avi", ".mkv" };
            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException("Định dạng video không được hỗ trợ. Vui lòng tải lên file .mp4, .mov, .avi hoặc .mkv.");
            }

            if (videoFile.Length > 100 * 1024 * 1024) // Giới hạn dưới 100MB
            {
                throw new ArgumentException("Dung lượng video vượt quá giới hạn 100MB cho phép.");
            }

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var absolutePath = Path.Combine(_videoUploadFolder, uniqueFileName);

            using (var stream = new FileStream(absolutePath, FileMode.Create))
            {
                await videoFile.CopyToAsync(stream);
            }

            var videoRelativePath = $"/uploads/return_videos/{uniqueFileName}";

            // 7. Tạo thực thể ReturnRequest và lưu xuống DB
            var returnRequest = new ReturnRequest
            {
                OrderID = orderId,
                BookID = dto.BookID,
                ReturnQuantity = dto.ReturnQuantity,
                ReturnReason = dto.ReturnReason,
                UnboxVideoPath = videoRelativePath,
                ResolutionType = dto.ResolutionType,
                ReturnStatus = ReturnStatus.Pending,
                RequestDate = DateTime.UtcNow
            };

            await _context.Set<ReturnRequest>().AddAsync(returnRequest);
            
            // Update order status to Returning
            order.OrderStatus = OrderStatus.Returning;
            
            await _context.SaveChangesAsync();

            // Lấy thông tin phụ trợ cho DTO
            var customerUser = await _context.Users.FindAsync(customerId);
            var realBook = await _context.Set<RealBook>().FindAsync(dto.BookID);

            return new ReturnRequestDetailDTO
            {
                ReturnRequestID = returnRequest.ReturnRequestID,
                OrderID = returnRequest.OrderID,
                BookID = returnRequest.BookID,
                BookTitle = realBook?.Title ?? "Sách không xác định",
                ReturnQuantity = returnRequest.ReturnQuantity,
                RequestDate = returnRequest.RequestDate ?? DateTime.UtcNow,
                ReturnReason = returnRequest.ReturnReason,
                UnboxVideoPath = returnRequest.UnboxVideoPath,
                ResolutionType = returnRequest.ResolutionType,
                ReturnStatus = returnRequest.ReturnStatus,
                CustomerName = customerUser != null ? $"{customerUser.LastName} {customerUser.FirstName}".Trim() : "Ẩn danh",
                CustomerPhoneNumber = customerUser?.PhoneNumber ?? string.Empty
            };
        }

        public async Task<IEnumerable<ReturnRequestDetailDTO>> GetReturnRequestsAsync(byte? status)
        {
            var query = _context.Set<ReturnRequest>()
                .Include(r => r.Order)
                .Include(r => r.RealBook)
                .Include(r => r.StaffDetail)
                    .ThenInclude(s => s.User)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(r => (byte)r.ReturnStatus == status.Value);
            }

            var requests = await query
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            var customerIds = requests.Select(r => r.Order != null ? r.Order.CustomerID : 0).Distinct().ToList();
            var customers = await _context.Users
                .Where(u => customerIds.Contains(u.UserID))
                .ToDictionaryAsync(u => u.UserID, u => u);

            return requests.Select(r =>
            {
                User? cust = null;
                if (r.Order != null) customers.TryGetValue(r.Order.CustomerID, out cust);

                return new ReturnRequestDetailDTO
                {
                    ReturnRequestID = r.ReturnRequestID,
                    OrderID = r.OrderID,
                    BookID = r.BookID,
                    BookTitle = r.RealBook?.Title ?? "Sách không xác định",
                    StaffID = r.StaffID,
                    StaffName = r.StaffDetail != null && r.StaffDetail.User != null 
                        ? $"{r.StaffDetail.User.LastName} {r.StaffDetail.User.FirstName}".Trim()
                        : null,
                    ReturnQuantity = r.ReturnQuantity,
                    RequestDate = r.RequestDate ?? DateTime.UtcNow,
                    ReturnReason = r.ReturnReason,
                    UnboxVideoPath = r.UnboxVideoPath,
                    ResolutionType = r.ResolutionType,
                    ReturnStatus = r.ReturnStatus,
                    RejectReason = r.RejectReason,
                    GatewayTransactionID = r.GatewayTransactionID,
                    RefundAmount = r.RefundAmount,
                    RefundCompletedDate = r.RefundCompletedDate,
                    CustomerName = cust != null ? $"{cust.LastName} {cust.FirstName}".Trim() : "Ẩn danh",
                    CustomerPhoneNumber = cust?.PhoneNumber ?? string.Empty
                };
            });
        }

        public async Task<ReturnRequestDetailDTO?> GetReturnRequestByIdAsync(long requestId)
        {
            var r = await _context.Set<ReturnRequest>()
                .Include(r => r.Order)
                .Include(r => r.RealBook)
                .Include(r => r.StaffDetail)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(req => req.ReturnRequestID == requestId);

            if (r == null) return null;

            User? cust = null;
            if (r.Order != null)
            {
                cust = await _context.Users.FindAsync(r.Order.CustomerID);
            }

            return new ReturnRequestDetailDTO
            {
                ReturnRequestID = r.ReturnRequestID,
                OrderID = r.OrderID,
                BookID = r.BookID,
                BookTitle = r.RealBook?.Title ?? "Sách không xác định",
                StaffID = r.StaffID,
                StaffName = r.StaffDetail != null && r.StaffDetail.User != null 
                    ? $"{r.StaffDetail.User.LastName} {r.StaffDetail.User.FirstName}".Trim()
                    : null,
                ReturnQuantity = r.ReturnQuantity,
                RequestDate = r.RequestDate ?? DateTime.UtcNow,
                ReturnReason = r.ReturnReason,
                UnboxVideoPath = r.UnboxVideoPath,
                ResolutionType = r.ResolutionType,
                ReturnStatus = r.ReturnStatus,
                RejectReason = r.RejectReason,
                GatewayTransactionID = r.GatewayTransactionID,
                RefundAmount = r.RefundAmount,
                RefundCompletedDate = r.RefundCompletedDate,
                CustomerName = cust != null ? $"{cust.LastName} {cust.FirstName}".Trim() : "Ẩn danh",
                CustomerPhoneNumber = cust?.PhoneNumber ?? string.Empty
            };
        }

        public async Task<bool> ReviewReturnRequestAsync(long staffId, long requestId, ReviewReturnRequestDTO dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var request = await _context.Set<ReturnRequest>()
                    .Include(r => r.Order)
                        .ThenInclude(o => o.OrderDetails)
                    .Include(r => r.RealBook)
                    .FirstOrDefaultAsync(r => r.ReturnRequestID == requestId);

                if (request == null)
                {
                    throw new KeyNotFoundException("Không tìm thấy yêu cầu khiếu nại trả hàng.");
                }

                if (request.ReturnStatus != ReturnStatus.Pending)
                {
                    throw new InvalidOperationException("Yêu cầu khiếu nại này đã được xử lý trước đó.");
                }

                request.StaffID = staffId;

                if (!dto.IsApproved)
                {
                    request.ReturnStatus = ReturnStatus.Rejected;
                    request.RejectReason = dto.RejectReason ?? "Không có lý do từ chối cụ thể.";

                    if (request.Order != null)
                    {
                        request.Order.OrderStatus = OrderStatus.Completed;
                        request.Order.CompletedDate = DateTime.UtcNow;
                    }
                }
                else
                {
                    request.ReturnStatus = ReturnStatus.Approved;

                    var order = request.Order;
                    if (order == null) throw new Exception("Không có thông tin đơn hàng tương ứng.");

                    var detail = order.OrderDetails.FirstOrDefault(od => od.BookID == request.BookID);
                    if (detail == null) throw new Exception("Thông tin sản phẩm trong hóa đơn không hợp lệ.");

                    // 1. Tính tiền hoàn trả
                    decimal discountPerItem = (detail.Discount ?? 0) / detail.Quantity;
                    request.RefundAmount = (detail.UnitPrice - discountPerItem) * request.ReturnQuantity;
                    request.RefundCompletedDate = DateTime.UtcNow;

                    // Nếu thanh toán online, tạo mã giao dịch hoàn trả
                    if (order.PaymentMethod != PaymentMethod.COD)
                    {
                        request.GatewayTransactionID = "REFUND_" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 12).ToUpper();
                    }

                    // 4. Cập nhật trạng thái đơn hàng sang Returning
                    order.OrderStatus = OrderStatus.Returning;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> RestockReturnAsync(long staffId, long requestId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var request = await _context.Set<ReturnRequest>()
                    .Include(r => r.Order)
                        .ThenInclude(o => o.OrderDetails)
                    .Include(r => r.RealBook)
                    .FirstOrDefaultAsync(r => r.ReturnRequestID == requestId);

                if (request == null)
                {
                    throw new KeyNotFoundException("Không tìm thấy yêu cầu khiếu nại trả hàng.");
                }

                if (request.ReturnStatus != ReturnStatus.Approved)
                {
                    throw new InvalidOperationException("Chỉ có thể nhập kho cho yêu cầu đã được duyệt hoàn trả.");
                }

                var order = request.Order;
                if (order == null) throw new Exception("Không có thông tin đơn hàng tương ứng.");

                var detail = order.OrderDetails.FirstOrDefault(od => od.BookID == request.BookID);
                if (detail == null) throw new Exception("Thông tin sản phẩm trong hóa đơn không hợp lệ.");

                // Nhập lại kho vật lý của RealBook
                if (request.RealBook != null)
                {
                    request.RealBook.UnitsInStock += request.ReturnQuantity;
                }

                // Nếu là sách của BlindBook, nhập kho gói BlindBook tương ứng
                if (detail.BlindBookID.HasValue)
                {
                    var blindBook = await _context.Set<BlindBook>().FindAsync(detail.BlindBookID.Value);
                    if (blindBook != null)
                    {
                        blindBook.StockQuantity += request.ReturnQuantity;
                    }
                }

                // Cập nhật trạng thái ReturnRequest sang Restocked
                request.ReturnStatus = ReturnStatus.Restocked;
                request.StaffID = staffId; // Ghi nhận nhân viên Logistics đã xử lý

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
