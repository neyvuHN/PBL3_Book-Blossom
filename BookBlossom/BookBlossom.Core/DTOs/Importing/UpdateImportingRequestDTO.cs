using System;
using System.ComponentModel.DataAnnotations;

namespace BookBlossom.Core.DTOs.Importing
{
    public class UpdateImportingRequestDTO
    {
        [Required(ErrorMessage = "Tên nhà cung cấp không được để trống.")]
        [StringLength(255, ErrorMessage = "Tên nhà cung cấp không được vượt quá 255 ký tự.")]
        public string SupplierName { get; set; } = string.Empty;

        public DateTime? RequiredDate { get; set; }
        public DateTime? ShipDate { get; set; }

        [StringLength(500, ErrorMessage = "Địa chỉ giao hàng không được vượt quá 500 ký tự.")]
        public string? ShipAddress { get; set; }
    }
}
