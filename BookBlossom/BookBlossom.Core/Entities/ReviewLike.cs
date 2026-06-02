using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookBlossom.Core.Entities
{
    [Table("ReviewLike", Schema = "Review")]
    public class ReviewLike
    {
        public long ReviewID { get; set; }

        public long CustomerID { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ReviewID")]
        public virtual Review Review { get; set; } = null!;

        [ForeignKey("CustomerID")]
        public virtual CustomerDetail CustomerDetail { get; set; } = null!;
    }
}
