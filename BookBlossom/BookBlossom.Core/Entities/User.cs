using System.Collections.Generic;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.Entities
{
    public class User
    {
        public long UserID { get; set; }
        public UserRole RoleID { get; set; }
        
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public AccountStatus AccountStatus { get; set; }
        public string? LastName { get; set; }
        public string? FirstName { get; set; }
        public string? Avatar { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public DateTime? Birthday { get; set; }
        public bool? IsActive { get; set; }
        
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }

        public virtual StaffDetail StaffDetail { get; set; }
        public virtual CustomerDetail CustomerDetail { get; set; }
        public virtual ICollection<GuestDetail> GuestDetails { get; set; } = new List<GuestDetail>();
    }
}
