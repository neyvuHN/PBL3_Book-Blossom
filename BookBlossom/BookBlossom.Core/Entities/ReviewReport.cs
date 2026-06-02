using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.Entities
{
    [Table("ReviewReport", Schema = "Review")]
    public class ReviewReport
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ReportID { get; set; }

        public long ReviewID { get; set; }

        public long ReporterID { get; set; }

        [Required]
        [StringLength(255)]
        public string Reason { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public ReportStatus Status { get; set; } = ReportStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ReviewID")]
        public virtual Review Review { get; set; } = null!;

        [ForeignKey("ReporterID")]
        public virtual CustomerDetail Reporter { get; set; } = null!;
    }
}
