using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookBlossom.Core.Entities
{
    [Table("Review", Schema = "Review")]
    public class Review
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ReviewID { get; set; }

        public long CustomerID { get; set; }
        public long? BookID { get; set; }
        public long? BlindBookID { get; set; }
        public int Rating { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? ImageVideoPath { get; set; }

        public int LikeCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsHidden { get; set; } = false;
        public bool IsReputationAwarded { get; set; } = false;

        // Navigation properties
        [ForeignKey("CustomerID")]
        public virtual User User { get; set; } = null!;

    }
}