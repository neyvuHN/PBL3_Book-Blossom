using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookBlossom.Core.Enums; 

namespace BookBlossom.Core.Entities
{
    [Table("BlindBook", Schema = "Book")]
    public class BlindBook
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long BlindBookID { get; set; }

        [Required]
        public long RealBookID { get; set; }

        [Required]
        // Mình tăng lên 500 theo file config để Marketing gõ từ khóa thoải mái nhé
        [StringLength(500)] 
        public string Keywords { get; set; } = string.Empty;

        [Required]
        // Câu Quotes trích dẫn thường rất dài, tăng lên 1000 ký tự để không bị cắt cụt chữ
        [StringLength(1000)] 
        public string Quotes { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Category { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Hashtags { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        public int StockQuantity { get; set; }

        [StringLength(100)]
        public string? Barcode { get; set; }

        public bool IsLocked { get; set; } = false;

        [Required]
        public int RequestQuantity { get; set; } // Số lượng đề xuất bơm thêm

        [StringLength(500)]
        public string? RejectReason { get; set; } 
        [Required]
        [Column("Status")]
        public BlindBookRequestStatus BlindBookRequestStatus { get; set; } 

        [ForeignKey("RealBookID")]
        public virtual RealBook? RealBook { get; set; }

        public virtual ICollection<BlindBookImage> Images { get; set; } = new List<BlindBookImage>();
    }
}