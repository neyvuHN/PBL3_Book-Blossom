using BookBlossom.Core.Enums;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace BookBlossom.Core.DTOs.Book
{
    public class CreateRealBookDTO
    {
        [Required(ErrorMessage = "CategoryID là bắt buộc")]
        public long CategoryID { get; set; }

        [Required(ErrorMessage = "Title (Tiêu đề sách) không được để trống")]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        public string? Publisher { get; set; }

        [Required(ErrorMessage = "Mã ISBN không được để trống")]
        [StringLength(13, MinimumLength = 10, ErrorMessage = "ISBN phải từ 10 đến 13 ký tự")]
        public string ISBN { get; set; } = string.Empty;

        [Range(1000, 2026, ErrorMessage = "Năm xuất bản không hợp lệ")]
        public int PublishYear { get; set; }

        public string? Description { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá tiền phải lớn hơn hoặc bằng 0")]
        public decimal Price { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Trọng lượng phải lớn hơn hoặc bằng 0")]
        public decimal Weight { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho ban đầu không được âm")]
        public int UnitsInStock { get; set; }

        public IFormFile? SampleFile { get; set; }
    }
}