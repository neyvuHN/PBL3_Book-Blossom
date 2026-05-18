using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BookBlossom.Core.DTOs.Importing;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;

namespace BookBlossom.Infrastructure.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly ApplicationDbContext _context;

        public InventoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ImportingDTO> CreateImportingAsync(long staffId, CreateImportingRequestDTO request)
        {
            // 1. Verify staff detail exists
            var staff = await _context.StaffDetails
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StaffID == staffId);
            if (staff == null)
            {
                throw new InvalidOperationException($"Nhân viên với ID {staffId} không tồn tại trong hệ thống.");
            }

            // 2. Create the importing header
            var importing = new Importing
            {
                StaffID = staffId,
                SupplierName = request.SupplierName.Trim(),
                ImportDate = DateTime.Now,
                RequiredDate = request.RequiredDate,
                ShipDate = request.ShipDate,
                ShipAddress = request.ShipAddress?.Trim(),
                TotalCost = 0 // Will compute below
            };

            await _context.Importings.AddAsync(importing);
            // Save once to generate the ImportingID
            await _context.SaveChangesAsync();

            decimal totalCost = 0;

            // 3. Process details
            foreach (var detailDto in request.Details)
            {
                // Verify book exists
                var book = await _context.RealBooks.FindAsync(detailDto.BookID);
                if (book == null)
                {
                    throw new InvalidOperationException($"Sách với ID {detailDto.BookID} không tồn tại trong hệ thống. Không cho phép nhập sách không tồn tại.");
                }

                var lineTotal = detailDto.UnitPrice * detailDto.Quantity;
                totalCost += lineTotal;

                var detail = new ImportingDetail
                {
                    ImportingID = importing.ImportingID,
                    BookID = detailDto.BookID,
                    UnitPrice = detailDto.UnitPrice,
                    Quantity = detailDto.Quantity,
                    LineTotal = lineTotal
                };

                await _context.ImportingDetails.AddAsync(detail);

                // Update units in stock
                book.UnitsInStock += detailDto.Quantity;
                _context.RealBooks.Update(book);
            }

            importing.TotalCost = totalCost;
            _context.Importings.Update(importing);
            await _context.SaveChangesAsync();

            // Return mapped DTO
            return await MapToDTOAsync(importing.ImportingID);
        }

        public async Task<ImportingDTO?> GetImportingByIdAsync(long id)
        {
            var exists = await _context.Importings.AnyAsync(i => i.ImportingID == id);
            if (!exists) return null;

            return await MapToDTOAsync(id);
        }

        public async Task<IEnumerable<ImportingDTO>> GetAllImportingsAsync()
        {
            var importingIds = await _context.Importings
                .OrderByDescending(i => i.ImportDate)
                .Select(i => i.ImportingID)
                .ToListAsync();

            var result = new List<ImportingDTO>();
            foreach (var id in importingIds)
            {
                result.Add(await MapToDTOAsync(id));
            }
            return result;
        }

        public async Task<ImportingDTO> UpdateImportingAsync(long id, UpdateImportingRequestDTO request)
        {
            var importing = await _context.Importings.FindAsync(id);
            if (importing == null)
            {
                throw new KeyNotFoundException($"Phiếu nhập kho với ID {id} không tồn tại.");
            }

            importing.SupplierName = request.SupplierName.Trim();
            importing.RequiredDate = request.RequiredDate;
            importing.ShipDate = request.ShipDate;
            importing.ShipAddress = request.ShipAddress?.Trim();

            _context.Importings.Update(importing);
            await _context.SaveChangesAsync();

            return await MapToDTOAsync(id);
        }

        public async Task<bool> DeleteImportingAsync(long id)
        {
            var importing = await _context.Importings
                .Include(i => i.ImportingDetails)
                .FirstOrDefaultAsync(i => i.ImportingID == id);

            if (importing == null) return false;

            // Revert units in stock for all books in this import
            foreach (var detail in importing.ImportingDetails)
            {
                var book = await _context.RealBooks.FindAsync(detail.BookID);
                if (book != null)
                {
                    book.UnitsInStock -= detail.Quantity;
                    if (book.UnitsInStock < 0) book.UnitsInStock = 0; // Prevent negative stock
                    _context.RealBooks.Update(book);
                }
            }

            _context.Importings.Remove(importing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ImportingDetailDTO> AddImportingDetailAsync(long importingId, CreateImportingDetailRequestDTO request)
        {
            var importing = await _context.Importings.FindAsync(importingId);
            if (importing == null)
            {
                throw new KeyNotFoundException($"Phiếu nhập kho với ID {importingId} không tồn tại.");
            }

            var book = await _context.RealBooks.FindAsync(request.BookID);
            if (book == null)
            {
                throw new InvalidOperationException($"Sách với ID {request.BookID} không tồn tại trong hệ thống. Không cho phép nhập sách không tồn tại.");
            }

            var detail = await _context.ImportingDetails
                .FirstOrDefaultAsync(d => d.ImportingID == importingId && d.BookID == request.BookID);

            if (detail != null)
            {
                // Book already in this import, let's increment the quantity and update unit price
                detail.Quantity += request.Quantity;
                detail.UnitPrice = request.UnitPrice;
                detail.LineTotal = detail.UnitPrice * detail.Quantity;
                _context.ImportingDetails.Update(detail);
            }
            else
            {
                // New detail item
                detail = new ImportingDetail
                {
                    ImportingID = importingId,
                    BookID = request.BookID,
                    UnitPrice = request.UnitPrice,
                    Quantity = request.Quantity,
                    LineTotal = request.UnitPrice * request.Quantity
                };
                await _context.ImportingDetails.AddAsync(detail);
            }

            // Adjust book stock
            book.UnitsInStock += request.Quantity;
            _context.RealBooks.Update(book);

            await _context.SaveChangesAsync();

            // Recalculate TotalCost for the header
            await RecalculateTotalCostAsync(importingId);

            return new ImportingDetailDTO
            {
                ImportingID = detail.ImportingID,
                BookID = detail.BookID,
                BookTitle = book.Title,
                UnitPrice = detail.UnitPrice,
                Quantity = detail.Quantity,
                LineTotal = detail.UnitPrice * detail.Quantity
            };
        }

        public async Task<ImportingDetailDTO> UpdateImportingDetailAsync(long importingId, long bookId, UpdateImportingDetailRequestDTO request)
        {
            var detail = await _context.ImportingDetails
                .FirstOrDefaultAsync(d => d.ImportingID == importingId && d.BookID == bookId);

            if (detail == null)
            {
                throw new KeyNotFoundException($"Chi tiết phiếu nhập với ID phiếu {importingId} và ID sách {bookId} không tồn tại.");
            }

            var book = await _context.RealBooks.FindAsync(bookId);
            if (book == null)
            {
                throw new InvalidOperationException($"Sách với ID {bookId} không tồn tại.");
            }

            // Adjust book stock based on quantity difference
            var qtyDiff = request.Quantity - detail.Quantity;
            book.UnitsInStock += qtyDiff;
            if (book.UnitsInStock < 0) book.UnitsInStock = 0; // Prevent negative stock
            _context.RealBooks.Update(book);

            // Update details
            detail.UnitPrice = request.UnitPrice;
            detail.Quantity = request.Quantity;
            detail.LineTotal = request.UnitPrice * request.Quantity;

            _context.ImportingDetails.Update(detail);
            await _context.SaveChangesAsync();

            // Recalculate header total cost
            await RecalculateTotalCostAsync(importingId);

            return new ImportingDetailDTO
            {
                ImportingID = detail.ImportingID,
                BookID = detail.BookID,
                BookTitle = book.Title,
                UnitPrice = detail.UnitPrice,
                Quantity = detail.Quantity,
                LineTotal = detail.LineTotal
            };
        }

        public async Task<bool> DeleteImportingDetailAsync(long importingId, long bookId)
        {
            var detail = await _context.ImportingDetails
                .FirstOrDefaultAsync(d => d.ImportingID == importingId && d.BookID == bookId);

            if (detail == null) return false;

            var book = await _context.RealBooks.FindAsync(bookId);
            if (book != null)
            {
                // Revert book stock
                book.UnitsInStock -= detail.Quantity;
                if (book.UnitsInStock < 0) book.UnitsInStock = 0; // Prevent negative stock
                _context.RealBooks.Update(book);
            }

            _context.ImportingDetails.Remove(detail);
            await _context.SaveChangesAsync();

            // Recalculate header total cost
            await RecalculateTotalCostAsync(importingId);

            return true;
        }

        #region Helper Methods

        private async Task RecalculateTotalCostAsync(long importingId)
        {
            var importing = await _context.Importings.FindAsync(importingId);
            if (importing != null)
            {
                var sum = await _context.ImportingDetails
                    .Where(d => d.ImportingID == importingId)
                    .SumAsync(d => d.UnitPrice * d.Quantity);

                importing.TotalCost = sum;
                _context.Importings.Update(importing);
                await _context.SaveChangesAsync();
            }
        }

        private async Task<ImportingDTO> MapToDTOAsync(long importingId)
        {
            var importing = await _context.Importings
                .Include(i => i.Staff)
                .ThenInclude(s => s.User)
                .Include(i => i.ImportingDetails)
                .ThenInclude(d => d.Book)
                .FirstOrDefaultAsync(i => i.ImportingID == importingId);

            if (importing == null)
            {
                throw new KeyNotFoundException($"Phiếu nhập kho với ID {importingId} không tồn tại.");
            }

            string staffName = "N/A";
            if (importing.Staff != null && importing.Staff.User != null)
            {
                var user = importing.Staff.User;
                staffName = string.IsNullOrEmpty(user.LastName) && string.IsNullOrEmpty(user.FirstName)
                    ? user.UserName
                    : $"{user.LastName} {user.FirstName}".Trim();
            }

            return new ImportingDTO
            {
                ImportingID = importing.ImportingID,
                StaffID = importing.StaffID,
                StaffName = staffName,
                SupplierName = importing.SupplierName,
                ImportDate = importing.ImportDate,
                TotalCost = importing.TotalCost,
                RequiredDate = importing.RequiredDate,
                ShipDate = importing.ShipDate,
                ShipAddress = importing.ShipAddress,
                Details = importing.ImportingDetails.Select(d => new ImportingDetailDTO
                {
                    ImportingID = d.ImportingID,
                    BookID = d.BookID,
                    BookTitle = d.Book?.Title ?? "Không rõ",
                    UnitPrice = d.UnitPrice,
                    Quantity = d.Quantity,
                    LineTotal = d.UnitPrice * d.Quantity
                }).ToList()
            };
        }

        #endregion
    }
}
