using System.ComponentModel.DataAnnotations;

namespace BookBlossom.Core.DTOs.Auth
{
    public class LoginRequestDTO
    {
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        public string Password { get; set; }
    }
}