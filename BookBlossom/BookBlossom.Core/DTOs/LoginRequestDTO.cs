using System.ComponentModel.DataAnnotations;

namespace BookBlossom.Core.DTOs
{
    public class LoginRequestDTO
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        public string Password { get; set; }
    }
}