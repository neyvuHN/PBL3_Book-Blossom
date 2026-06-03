using System;
using System.Threading.Tasks;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;

namespace BookBlossom.Infrastructure.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogActionAsync(long adminId, long? targetUserId, ActionType actionType, string tableName, string? oldData, string? newData, string ipAddress)
        {
            var log = new AuditLog
            {
                SystemAdminID = adminId,
                UserID = targetUserId,
                ActionType = actionType,
                TableName = tableName,
                OldData = oldData,
                NewData = newData,
                IPAddress = ipAddress,
                CreatedAt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"))
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
