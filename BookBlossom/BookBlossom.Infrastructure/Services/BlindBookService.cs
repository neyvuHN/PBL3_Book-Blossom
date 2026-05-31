using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using BookBlossom.Core.Interfaces;
using BookBlossom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookBlossom.Infrastructure.Services
{
    public class BlindBookService : IBlindBookService
    {
        private readonly ApplicationDbContext _context;

        public BlindBookService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BlindBook>> GetAllBlindBooksAsync()
        {
            return await _context.BlindBooks.Include(b => b.RealBook).ToListAsync();
        }

        public async Task<BlindBook?> GetByIdAsync(long blindBookId)
        {
            return await _context.BlindBooks.Include(b => b.RealBook).FirstOrDefaultAsync(b => b.BlindBookID == blindBookId);
        }

        // --- Marketing Manager ---
        public async Task<BlindBook> CreateRequestAsync(BlindBook blindBook, string marketingId)
        {
            // Nghiệp vụ 1: Kiểm tra RealBookID có tồn tại không
            var realBookExists = await _context.RealBooks.AnyAsync(r => r.BookID == blindBook.RealBookID);
            if (!realBookExists)
                throw new Exception("Mã sách thật (RealBookID) không tồn tại trong hệ thống kho.");

            // Nghiệp vụ 2: Kiểm tra ràng buộc quan hệ 1-1
            if (await _context.BlindBooks.AnyAsync(b => b.RealBookID == blindBook.RealBookID))
                throw new Exception("Sách này đã tồn tại trong danh mục BlindBook.");

            blindBook.BlindBookRequestStatus = BlindBookRequestStatus.Pending;
            
            _context.BlindBooks.Add(blindBook);
            await _context.SaveChangesAsync();
            return blindBook;
        }

        public async Task<IEnumerable<BlindBook>> GetMarketingRequestsAsync()
        {
            return await _context.BlindBooks.Include(b => b.RealBook).ToListAsync();
        }

        public async Task<bool> CreateRestockRequestAsync(long blindBookId, int quantity, string marketingId)
        {
            var book = await _context.BlindBooks.FindAsync(blindBookId);
            if (book == null) return false;

            if (quantity <= 0) throw new ArgumentException("Số lượng bổ sung phải lớn hơn 0.");

            book.RequestQuantity = quantity;
            book.BlindBookRequestStatus = BlindBookRequestStatus.Pending; 
            book.RejectReason = null; 

            return await _context.SaveChangesAsync() > 0;
        }

        // --- Store Manager ---
        public async Task<IEnumerable<BlindBook>> GetPendingRequestsForStoreAsync()
        {
            return await _context.BlindBooks
                .Include(b => b.RealBook)
                .Where(b => b.BlindBookRequestStatus == BlindBookRequestStatus.Pending)
                .ToListAsync();
        }

        public async Task<bool> ConfirmBlindBookRequestAsync(long blindBookId, string storeManagerId)
        {
            var book = await _context.BlindBooks.Include(b => b.RealBook).FirstOrDefaultAsync(b => b.BlindBookID == blindBookId);
            if (book == null || book.BlindBookRequestStatus != BlindBookRequestStatus.Pending) return false;

            // Nghiệp vụ 4: Kiểm tra tồn kho thực tế của RealBook (Đã lược bỏ check OrderDetails chưa làm tới)
            if (book.RealBook == null || book.RequestQuantity > book.RealBook.UnitsInStock)
                throw new Exception($"Số lượng duyệt vượt quá lượng sách thật hiện có trong kho ({book.RealBook?.UnitsInStock ?? 0}).");

            // Tự động sinh mã Barcode duy nhất khi duyệt
            book.Barcode = $"BLD{DateTime.UtcNow:yyyyMMddHHmmssfff}{book.BlindBookID}";
            book.StockQuantity = book.RequestQuantity; // Chuyển số lượng yêu cầu thành số lượng trong kho
            book.RequestQuantity = 0;
            book.BlindBookRequestStatus = BlindBookRequestStatus.Approved;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> RejectBlindBookRequestAsync(long blindBookId, string reason, string storeManagerId)
        {
            var book = await _context.BlindBooks.FindAsync(blindBookId);
            if (book == null) return false;

            book.BlindBookRequestStatus = BlindBookRequestStatus.Rejected;
            book.RejectReason = reason;
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> ConfirmRestockRequestAsync(long blindBookId, int approvedQuantity, string storeManagerId)
        {
            var book = await _context.BlindBooks.Include(b => b.RealBook).FirstOrDefaultAsync(b => b.BlindBookID == blindBookId);
            if (book == null || book.BlindBookRequestStatus != BlindBookRequestStatus.Pending) return false;

            // Kiểm tra tồn kho thực tế của RealBook khi Restock
            if (book.RealBook == null || approvedQuantity > book.RealBook.UnitsInStock)
                throw new Exception($"Số lượng duyệt bổ sung vượt quá lượng sách thật hiện có trong kho ({book.RealBook?.UnitsInStock ?? 0}).");

            book.StockQuantity += approvedQuantity; 
            book.RequestQuantity = 0; 
            book.BlindBookRequestStatus = BlindBookRequestStatus.Approved;

            return await _context.SaveChangesAsync() > 0;
        }

        // --- Admin ---
        public async Task<bool> ToggleLockStatusAsync(long blindBookId)
        {
            var book = await _context.BlindBooks.FindAsync(blindBookId);
            if (book == null) return false;

            book.IsLocked = !book.IsLocked;
            return await _context.SaveChangesAsync() > 0;
        }
    }
}