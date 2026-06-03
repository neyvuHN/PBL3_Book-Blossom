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
                .Include(c => c.Buyer)
                    .ThenInclude(b => b.User)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return requests.Select(c => new CallRequestDto
            {
                RequestID = c.CallRequestID,
                CustomerID = c.BuyerID,
                CustomerName = c.Buyer?.User?.UserName ?? "Unknown",
                Category = c.Category,
                PhoneNumber = c.PhoneNumber,
                Note = c.Note ?? string.Empty,
                Status = c.Status,
                CreatedAt = DateTime.SpecifyKind(c.CreatedAt, DateTimeKind.Local),
                ResolvedAt = c.ResolvedAt.HasValue ? DateTime.SpecifyKind(c.ResolvedAt.Value, DateTimeKind.Local) : null
            }).ToList();
        }

        public async Task<List<CallRequestDto>> GetCustomerCallRequestsAsync(long customerId)
        {
            var requests = await _context.CallRequests
                .Include(c => c.Buyer)
                    .ThenInclude(b => b.User)
                .Where(c => c.BuyerID == customerId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return requests.Select(c => new CallRequestDto
            {
                RequestID = c.CallRequestID,
                CustomerID = c.BuyerID,
                CustomerName = c.Buyer?.User?.UserName ?? "Unknown",
                Category = c.Category,
                PhoneNumber = c.PhoneNumber,
                Note = c.Note ?? string.Empty,
                Status = c.Status,
                CreatedAt = DateTime.SpecifyKind(c.CreatedAt, DateTimeKind.Local),
                ResolvedAt = c.ResolvedAt.HasValue ? DateTime.SpecifyKind(c.ResolvedAt.Value, DateTimeKind.Local) : null
            }).ToList();
        }

        public async Task<CallRequestDto> CreateCallRequestAsync(long customerId, CreateCallRequestDto dto)
        {
            var callRequest = new CallRequest
            {
                BuyerID = customerId,
                ConversationID = dto.ConversationID,
                Category = dto.Category,
                PhoneNumber = dto.PhoneNumber,
                Note = dto.Note,
                Status = CallRequestStatus.Pending,
                CreatedAt = DateTime.Now
            };

            _context.CallRequests.Add(callRequest);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(customerId);

            return new CallRequestDto
            {
                RequestID = callRequest.CallRequestID,
                CustomerID = callRequest.BuyerID,
                CustomerName = user?.UserName ?? "Unknown",
                Category = callRequest.Category,
                PhoneNumber = callRequest.PhoneNumber,
                Note = callRequest.Note ?? string.Empty,
                Status = callRequest.Status,
                CreatedAt = DateTime.SpecifyKind(callRequest.CreatedAt, DateTimeKind.Local)
            };
        }

        public async Task ResolveCallRequestAsync(long requestId)
        {
            var request = await _context.CallRequests.FindAsync(requestId);
            if (request == null) throw new Exception("Call Request not found.");

            request.Status = CallRequestStatus.Resolved;
            request.ResolvedAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task SetCallRequestInProgressAsync(long requestId)
        {
            var request = await _context.CallRequests.FindAsync(requestId);
            if (request == null) throw new Exception("Call Request not found.");

            if (request.Status == CallRequestStatus.Pending)
            {
                request.Status = CallRequestStatus.InProgress;
                await _context.SaveChangesAsync();
            }
        }
    }
}
