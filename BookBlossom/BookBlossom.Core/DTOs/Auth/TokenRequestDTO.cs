using System.ComponentModel.DataAnnotations;

namespace BookBlossom.Core.DTOs.Auth
{
    public class TokenRequestDTO
    {
        [Required]
        public string AccessToken { get; set; }

        [Required]
        public string RefreshToken { get; set; }
    }
}