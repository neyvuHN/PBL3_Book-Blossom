using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookBlossom.Core.DTOs;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookBlossom.Infrastructure.Services
{
    public class CallRequestService : ICallRequestService
    {
        private readonly ApplicationDbContext _context;

        public CallRequestService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CallRequestDto>> GetAllCallRequestsAsync()
        {
            var requests = await _context.CallRequests
                .Include(c => c.User)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return requests.Select(c => new CallRequestDto
            {
                RequestID = c.RequestID,
                CustomerID = c.CustomerID,
                CustomerName = c.User?.UserName ?? "Unknown",
                Category = c.Category,
                PhoneNumber = c.PhoneNumber,
                Note = c.Note,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                ResolvedAt = c.ResolvedAt
            }).ToList();
        }

        public async Task<List<CallRequestDto>> GetCustomerCallRequestsAsync(long customerId)
        {
            var requests = await _context.CallRequests
                .Include(c => c.User)
                .Where(c => c.CustomerID == customerId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return requests.Select(c => new CallRequestDto
            {
                RequestID = c.RequestID,
                CustomerID = c.CustomerID,
                CustomerName = c.User?.UserName ?? "Unknown",
                Category = c.Category,
                PhoneNumber = c.PhoneNumber,
                Note = c.Note,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                ResolvedAt = c.ResolvedAt
            }).ToList();
        }

        public async Task<CallRequestDto> CreateCallRequestAsync(long customerId, CreateCallRequestDto dto)
        {
            var callRequest = new CallRequest
            {
                CustomerID = customerId,
                Category = dto.Category,
                PhoneNumber = dto.PhoneNumber,
                Note = dto.Note,
                Status = CallRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.CallRequests.Add(callRequest);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(customerId);

            return new CallRequestDto
            {
                RequestID = callRequest.RequestID,
                CustomerID = callRequest.CustomerID,
                CustomerName = user?.UserName ?? "Unknown",
                Category = callRequest.Category,
                PhoneNumber = callRequest.PhoneNumber,
                Note = callRequest.Note,
                Status = callRequest.Status,
                CreatedAt = callRequest.CreatedAt
            };
        }

        public async Task ResolveCallRequestAsync(long requestId)
        {
            var request = await _context.CallRequests.FindAsync(requestId);
            if (request == null) throw new Exception("Call Request not found.");

            request.Status = CallRequestStatus.Resolved;
            request.ResolvedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }
}
