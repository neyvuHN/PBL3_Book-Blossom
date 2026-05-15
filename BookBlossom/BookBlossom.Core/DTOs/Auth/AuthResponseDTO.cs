using BookBlossom.Core.Enums;

namespace BookBlossom.Core.DTOs.Auth
{
    public class AuthResponseDTO
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; }
        public UserRole RoleID { get; set; }
    }
}