namespace BookBlossom.Core.DTOs.Auth
{
    public class AuthResponseDTO
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int RoleID { get; set; }
    }
}