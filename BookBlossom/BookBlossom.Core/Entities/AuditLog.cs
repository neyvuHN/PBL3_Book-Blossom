using System;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.Entities
{
    public class AuditLog
    {
        public long LogID { get; set; }
        public long SystemAdminID { get; set; } // ID của Staff thực hiện thay đổi
        public long? UserID { get; set; }        // Đối tượng User bị tác động (nếu có)
        public ActionType ActionType { get; set; }
        public string? TableName { get; set; }
        public string? OldData { get; set; }
        public string? NewData { get; set; }
        public string? IPAddress { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}