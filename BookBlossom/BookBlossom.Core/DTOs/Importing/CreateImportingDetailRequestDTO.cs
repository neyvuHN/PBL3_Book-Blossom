using System.ComponentModel.DataAnnotations;

namespace BookBlossom.Core.DTOs.Importing
{
    public class CreateImportingDetailRequestDTO
    {
        [Required(ErrorMessage = "Mã sách là bắt buộc.")]
        public long BookID { get; set; }

        [Required(ErrorMessage = "Đơn giá nhập là bắt buộc.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Đơn giá nhập phải lớn hơn 0.")]
        public decimal UnitPrice { get; set; }

        [Required(ErrorMessage = "Số lượng nhập là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng nhập phải ít nhất là 1.")]
        public int Quantity { get; set; }
    }
}
