using System;

namespace BookBlossom.Core.DTOs
{
    public class RegisterRequestDTO
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Avatar { get; set; }
        public string Gender { get; set; }
        public DateTime Birthday { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}
