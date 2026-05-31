// BookBlossom.Core/Entities/SystemConfiguration.cs
using System;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.Entities
{
    public class SystemConfiguration
    {
        public long ConfigID { get; set; }
        public long SystemAdminID { get; set; }
        public string ConfigName { get; set; } = string.Empty;
        public string ConfigValue { get; set; } = string.Empty;
        public string? Description { get; set; }
        public GroupType GroupType { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}