using System;

namespace BookBlossom.Core.Entities
{
    public class AuditLog
    {
        public long LogID { get; set; }
        public long SystemAdminID { get; set; }
        public long? UserID { get; set; }
        public byte ActionType { get; set; }
        public string? TableName { get; set; }
        public string? OldData { get; set; }
        public string? NewData { get; set; }
        public string? IPAddress { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public enum AuditActionType : byte
    {
        USER_LOGIN = 1,
        LOGIN_FAILED = 2,
        USER_LOGOUT = 3,
        CREATE_STAFF_ACCOUNT = 4,
        UPDATE_STAFF_ACCOUNT = 5,
        DELETE_STAFF_ACCOUNT = 6,
        LOCK_ACCOUNT = 7,
        UNLOCK_ACCOUNT = 8,
        EXPORT = 9,
        CREATE = 10,
        UPDATE = 11,
        DELETE = 12
    }
}
