using System;
using System.ComponentModel.DataAnnotations;

namespace BookBlossom.Core.DTOs
{
    public class RegisterRequestDTO
    {
        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3 đến 50 ký tự.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ là bắt buộc.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên là bắt buộc.")]
        public string FirstName { get; set; } = string.Empty;

        public string? Avatar { get; set; }
        public string? Gender { get; set; }
        public DateTime? Birthday { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string Email { get; set; } = string.Empty;

        public Guid? GuestID { get; set; }
    }
}
