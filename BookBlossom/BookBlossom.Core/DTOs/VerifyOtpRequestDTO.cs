using System.ComponentModel.DataAnnotations;

namespace BookBlossom.Core.DTOs
{
    public class VerifyOtpRequestDTO
    {
        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã OTP là bắt buộc.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP phải có 6 chữ số.")]
        public string OtpCode { get; set; } = string.Empty;
    }
}
