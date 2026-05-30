using BookBlossom.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace BookBlossom.Core.DTOs.Thread
{
    public class CreateReportDTO
    {
        [Required(ErrorMessage = "Lý do báo cáo không được để trống.")]
        public ReportType Reason { get; set; }

        [Required(ErrorMessage = "Nội dung mô tả báo cáo không được để trống.")]
        public string Description { get; set; } = string.Empty;
    }
}
