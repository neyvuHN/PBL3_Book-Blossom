using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using BookBlossom.Core.Interfaces;
using BookBlossom.Infrastructure.Data;
using BookBlossom.DTOs.BlindBook;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            return await _context.BlindBooks
                .Include(b => b.RealBook)
                .Include(b => b.Images)
                .ToListAsync();
        }

        public async Task<BlindBook?> GetByIdAsync(long blindBookId)
        {
            return await _context.BlindBooks
                .Include(b => b.RealBook)
                .Include(b => b.Images)
                .FirstOrDefaultAsync(b => b.BlindBookID == blindBookId);
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
            var book = await _context.BlindBooks.Include(b => b.RealBook).FirstOrDefaultAsync(b => b.BlindBookID == blindBookId);
            if (book == null) return false;

            if (quantity <= 0) throw new ArgumentException("Số lượng bổ sung phải lớn hơn 0.");

            if (book.RealBook == null) throw new Exception("Không tìm thấy thông tin sách thật liên quan.");

            int availableStock = book.RealBook.UnitsInStock - book.RealBook.ReservedQuantity;
            if (quantity > availableStock)
                throw new Exception($"Số lượng bổ sung ({quantity}) không được vượt quá số lượng tồn kho khả dụng (Tồn kho: {book.RealBook.UnitsInStock}, Giữ chỗ: {book.RealBook.ReservedQuantity}, Khả dụng: {availableStock}).");

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

            if (book.RealBook == null) throw new Exception("Không tìm thấy thông tin sách thật liên quan.");

            int availableStock = book.RealBook.UnitsInStock - book.RealBook.ReservedQuantity;
            if (book.RequestQuantity > availableStock)
                throw new Exception($"Số lượng duyệt ({book.RequestQuantity}) không được vượt quá số lượng tồn kho khả dụng (Tồn kho: {book.RealBook.UnitsInStock}, Giữ chỗ: {book.RealBook.ReservedQuantity}, Khả dụng: {availableStock}).");

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

            if (book.RealBook == null) throw new Exception("Không tìm thấy thông tin sách thật liên quan.");

            int availableStock = book.RealBook.UnitsInStock - book.RealBook.ReservedQuantity;
            if (approvedQuantity > availableStock)
                throw new Exception($"Số lượng duyệt bổ sung ({approvedQuantity}) không được vượt quá số lượng tồn kho khả dụng (Tồn kho: {book.RealBook.UnitsInStock}, Giữ chỗ: {book.RealBook.ReservedQuantity}, Khả dụng: {availableStock}).");

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

        public async Task<bool> HasOrdersAsync(long blindBookId)
        {
            return await _context.OrderDetails.AnyAsync(od => od.BlindBookID == blindBookId);
        }

        public async Task<bool> UpdateBlindBookAsync(long id, UpdateBlindBookDTO dto)
        {
            var book = await _context.BlindBooks.FindAsync(id);
            if (book == null) return false;

            var hasOrders = await HasOrdersAsync(id);
            if (hasOrders && book.Price != dto.Price)
            {
                throw new Exception("Không thể sửa giá bán của Sách Mù khi đã có đơn hàng phát sinh.");
            }

            book.Keywords = dto.Keywords;
            book.Quotes = dto.Quotes;
            book.Category = dto.Category;
            book.Hashtags = dto.Hashtags;
            book.Price = dto.Price;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<HashSet<long>> GetBlindBookIdsWithOrdersAsync()
        {
            var ids = await _context.OrderDetails
                .Where(od => od.BlindBookID.HasValue)
                .Select(od => od.BlindBookID!.Value)
                .Distinct()
                .ToListAsync();
            return new HashSet<long>(ids);
        }
    }
}