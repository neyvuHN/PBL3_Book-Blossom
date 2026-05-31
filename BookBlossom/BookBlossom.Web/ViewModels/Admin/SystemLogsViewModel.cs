using BookBlossom.Core.Entities;

namespace BookBlossom.Web.ViewModels.Admin
{
    public class SystemLogsViewModel
    {
        public List<AuditLogItemViewModel> Logs { get; set; } = new List<AuditLogItemViewModel>();
    }

    public class AuditLogItemViewModel
    {
        public long LogID { get; set; }
        public long SystemAdminID { get; set; }
        public string AdminUsername { get; set; } = string.Empty;
        public long? UserID { get; set; }
        public string? TargetUsername { get; set; }
        public byte ActionType { get; set; }
        public string ActionTypeName { get; set; } = string.Empty;
        public string? TableName { get; set; }
        public string? OldData { get; set; }
        public string? NewData { get; set; }
        public string? IPAddress { get; set; }
        public string? CreatedAt { get; set; }
    }
}
