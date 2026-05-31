using System.Threading.Tasks;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface IAuditService
    {
        Task LogActionAsync(long adminId, long? targetUserId, ActionType actionType, string tableName, string? oldData, string? newData, string ipAddress);
    }
}
