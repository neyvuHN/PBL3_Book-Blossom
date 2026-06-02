using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.Entities
{
    [Table("ReviewMedia", Schema = "Review")]
    public class ReviewMedia
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long MediaID { get; set; }

        public long ReviewID { get; set; }

        public MediaType MediaType { get; set; }

        [Required]
        [StringLength(2000)]
        public string MediaURL { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ReviewID")]
        public virtual Review Review { get; set; } = null!;
    }
}
