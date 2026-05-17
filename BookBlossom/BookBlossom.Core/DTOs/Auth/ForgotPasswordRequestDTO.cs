using System.ComponentModel.DataAnnotations;

namespace BookBlossom.Core.DTOs.Auth
{
    public class ForgotPasswordRequestDTO
    {
        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
