using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Interfaces;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Core.DTOs.Book;
using BookBlossom.Core.Enums;
using BookBlossom.Infrastructure.Data;

namespace BookBlossom.Infrastructure.Services
{
    public class RealBookService : IRealBookService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        // Đường dẫn vật lý lưu file PDF đọc thử trên Server
        private readonly string _uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "samples");

        public RealBookService(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
            // Tự động tạo thư mục lưu file PDF nếu hệ thống chưa có folder này
            if (!Directory.Exists(_uploadFolder)) Directory.CreateDirectory(_uploadFolder);
        }

        // 1. HÀM THÊM MỚI SÁCH (CREATE)
        public async Task<bool> CreateRealBookAsync(CreateRealBookDTO request)
        {
            // Kiểm tra trùng mã ISBN dưới Database
            if (await _context.RealBooks.AnyAsync(b => b.ISBN == request.ISBN))
            {
                throw new Exception("Mã ISBN này đã tồn tại trên hệ thống. Không thể tạo trùng");
            }

            // Kiểm tra khóa ngoại CategoryID có tồn tại thực sự không
            var category = await _context.Categories.FindAsync(request.CategoryID);
            if (category == null) throw new Exception("Danh mục sách (Category) không tồn tại.");

            // Xử lý Validate File đọc thử (.pdf < 10MB)
            string? storedPath = null;
            if (request.SampleFile != null)
            {
                storedPath = await HandleUploadFileAsync(request.SampleFile);
            }

            // Ánh xạ chuẩn xác 100% các trường vào Entity lưu xuống SQL
            var realBook = new RealBook
            {
                CategoryID = request.CategoryID,
                Title = request.Title,
                Publisher = request.Publisher,
                ISBN = request.ISBN,
                PublishYear = request.PublishYear,
                Description = request.Description,
                Price = request.Price,
                SampleFilePath = storedPath,
                Weight = request.Weight,
                UnitsInStock = request.UnitsInStock,
                ReservedQuantity = 0, // Mặc định sách mới nhập chưa ai giữ chỗ
                IsContinued = true    // Mặc định cho phép hiển thị kinh doanh công khai
            };

            _context.RealBooks.Add(realBook);
            var success = await _context.SaveChangesAsync() > 0;

            if (success)
            {
                try
                {
                    // Fetch category name
                    var cat = await _context.Categories.FindAsync(request.CategoryID);
                    var categoryName = cat?.CategoryName ?? "Thể loại";

                    // Category subscription is no longer supported in the updated SubscriptionTargetType enum.
                    await Task.CompletedTask;
                }
                catch { /* Suppress notification errors */ }
            }

            return success;
        }

        // 2. HÀM LẤY DANH SÁCH + TÌM KIẾM + PHÂN LOẠI + SẮP XẾP (GET ALL)
        public async Task<List<RealBookDTO>> GetAllRealBooksAsync(string searchTerm = "", string category = "", SortOrder sortOrder = SortOrder.Ascending)
        {
            // Chỉ lấy những sách có trạng thái kinh doanh hợp lệ (IsContinued = true)
            var query = _context.RealBooks.Include(b => b.Category).Where(b => b.IsContinued).AsQueryable();

            // Tìm kiếm theo Tên sách hoặc mã ISBN
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string term = searchTerm.Trim().ToLower();
                query = query.Where(b => b.Title.ToLower().Contains(term) || b.ISBN.Contains(term));
            }

            // Tìm lọc theo tên chuỗi Category (nếu có truyền)
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(b => b.Category.CategoryName.ToLower() == category.Trim().ToLower());
            }

            // Xử lý sắp xếp theo giá tiền dựa trên Enum SortOrder của bạn
            if (sortOrder == SortOrder.Ascending) query = query.OrderBy(b => b.Price);
            else query = query.OrderByDescending(b => b.Price);

            return await query.Select(b => MapToDTO(b)).ToListAsync();
        }

        // 3. HÀM LẤY SÁCH THEO CATEGORY ID (GET BY CATEGORY)
        public async Task<List<RealBookDTO>> GetRealBooksByCategoryIdAsync(long categoryId, SortOrder sortOrder = SortOrder.Ascending)
        {
            var query = _context.RealBooks.Include(b => b.Category)
                .Where(b => b.CategoryID == categoryId && b.IsContinued).AsQueryable();

            if (sortOrder == SortOrder.Ascending) query = query.OrderBy(b => b.Price);
            else query = query.OrderByDescending(b => b.Price);

            return await query.Select(b => MapToDTO(b)).ToListAsync();
        }

        // 4. HÀM LẤY CHI TIẾT MỘT CUỐN SÁCH (GET BY ID)
        public async Task<RealBookDTO?> GetRealBookByIdAsync(long id)
        {
            var book = await _context.RealBooks.Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.BookID == id && b.IsContinued);

            if (book == null) return null;
            return MapToDTO(book);
        }

        // 5. HÀM CẬP NHẬT THÔNG TIN SÁCH (UPDATE)
        public async Task<bool> UpdateRealBookAsync(long id, UpdateRealBookDTO request)
        {
            var book = await _context.RealBooks.FindAsync(id);
            if (book == null || !book.IsContinued) throw new Exception("Không tìm thấy cuốn sách cần cập nhật.");

            // Kiểm tra chống trùng mã ISBN với các cuốn sách KHÁC cuốn đang sửa
            if (await _context.RealBooks.AnyAsync(b => b.ISBN == request.ISBN && b.BookID != id))
            {
                throw new Exception("Mã ISBN này đã bị trùng với một cuốn sách khác trên hệ thống.");
            }

            // Nếu Store Manager tải lên file đọc thử mới, thực hiện lưu đè/lưu mới
            if (request.SampleFile != null)
            {
                // Xóa file cũ trên ổ đĩa server trước nếu có để tiết kiệm dung lượng
                if (!string.IsNullOrEmpty(book.SampleFilePath))
                {
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", book.SampleFilePath.TrimStart('/'));
                    if (File.Exists(oldFilePath)) File.Delete(oldFilePath);
                }
                book.SampleFilePath = await HandleUploadFileAsync(request.SampleFile);
            }

            book.CategoryID = request.CategoryID;
            book.Title = request.Title;
            book.Publisher = request.Publisher;
            book.ISBN = request.ISBN;
            book.PublishYear = request.PublishYear;
            book.Description = request.Description;
            book.Price = request.Price;
            book.Weight = request.Weight;
            book.UnitsInStock = request.UnitsInStock;

            return await _context.SaveChangesAsync() > 0;
        }

        // 6. HÀM XÓA MỀM (DELETE)
        public async Task<bool> DeleteRealBookAsync(long id)
        {
            var book = await _context.RealBooks.FindAsync(id);
            if (book == null) return false;

            // Xóa mềm: Chuyển cờ IsContinued về false thay vì xóa hẳn bản ghi khỏi database
            book.IsContinued = false;
            return await _context.SaveChangesAsync() > 0;
        }

        // --- CÁC HÀM TRỢ GIÚP (HELPER FUNCTIONS) TRONG TẦNG SERVICE ---

        // Hàm xử lý kiểm tra và upload file PDF chuyên biệt
        private async Task<string> HandleUploadFileAsync(Microsoft.AspNetCore.Http.IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".pdf")
            {
                throw new Exception("Hệ thống chỉ chấp nhận file đọc thử định dạng PDF.");
            }

            if (file.Length > 10 * 1024 * 1024) // Giới hạn dưới 10MB
            {
                throw new Exception("Dung lượng file vượt quá giới hạn 10MB cho phép.");
            }

            // Sinh tên ngẫu nhiên dạng GUID để bảo vệ file không bị ghi đè trùng tên
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var absolutePath = Path.Combine(_uploadFolder, uniqueFileName);

            using (var stream = new FileStream(absolutePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/samples/{uniqueFileName}";
        }

        // Hàm Map nội bộ từ Entity sang DTO để tránh lặp code mapping nhiều nơi
        private static RealBookDTO MapToDTO(RealBook b)
        {
            return new RealBookDTO
            {
                BookID = b.BookID,
                CategoryID = b.CategoryID,
                CategoryName = b.Category != null ? b.Category.CategoryName : string.Empty,
                Title = b.Title,
                Publisher = b.Publisher,
                ISBN = b.ISBN,
                PublishYear = b.PublishYear,
                Description = b.Description,
                Price = b.Price,
                SampleFilePath = b.SampleFilePath,
                Weight = b.Weight,
                UnitsInStock = b.UnitsInStock,
                ReservedQuantity = b.ReservedQuantity,
                IsContinued = b.IsContinued
            };
        }

        public async Task<bool> ReserveStockAsync(long bookId, int quantity)
        {
            // 1. Tìm cuốn sách cần giữ kho dưới DB
            var book = await _context.RealBooks.FindAsync(bookId);
            if (book == null || !book.IsContinued)
            {
                throw new Exception("Không tìm thấy cuốn sách yêu cầu hoặc sản phẩm đã ngừng kinh doanh");
            }

            // 2. Kiểm tra điều kiện logic nghiêm ngặt của kho hàng
            // Tổng lượng giữ chỗ dự kiến = Lượng đã giữ cũ + Lượng muốn giữ thêm
            int expectedReserved = book.ReservedQuantity + quantity;

            if (expectedReserved > book.UnitsInStock)
            {
                // Nếu vượt quá, ném lỗi ra ngoài để Controller trả về 400 BadRequest
                throw new Exception("Số lượng sách khả dụng trong kho không đủ để thực hiện giữ chỗ đơn hàng");
            }

            // 3. Nếu hợp lệ, tiến hành cộng dồn vào biến ReservedQuantity
            book.ReservedQuantity = expectedReserved;

            // Lưu thay đổi xuống Database
            return await _context.SaveChangesAsync() > 0;
        }
    }
}