using System.Collections.Generic;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.Entities
{
    public class Role
    {
        public UserRole RoleID { get; set; }
        public string RoleName { get; set; } = string.Empty;

        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
