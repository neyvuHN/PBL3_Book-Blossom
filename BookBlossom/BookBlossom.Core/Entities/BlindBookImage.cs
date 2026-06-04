using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookBlossom.Core.Entities
{
    [Table("BlindBookImages", Schema = "Book")]
    public class BlindBookImage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ImageID { get; set; }

        [Required]
        public long BlindBookID { get; set; }

        [Required]
        [MaxLength(500)]
        public string ImagePath { get; set; } = string.Empty;

        [Required]
        public bool IsMain { get; set; } = false;

        [Required]
        public int SortOrder { get; set; } = 0;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Property
        [ForeignKey("BlindBookID")]
        public virtual BlindBook BlindBook { get; set; } = null!;
    }
}
