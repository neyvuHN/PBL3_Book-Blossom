using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookBlossom.Core.Entities
{
    [Table("RealBook", Schema = "Book")]
    public class Book
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long BookID { get; set; }

        [Required]
        public long CategoryID { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Publisher { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string ISBN { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }

        public string? SampleFilePath { get; set; }

        [Required]
        public double Weight { get; set; }

        [Required]
        public int UnitsInStock { get; set; }

        [Required]
        public bool IsContinued { get; set; }

        [Required]
        public int ReservedQuantity { get; set; } = 0;

        [Required]
        public int PublishYear { get; set; }

        // Navigation Properties
        [ForeignKey("CategoryID")]
        public virtual Category Category { get; set; } = null!;

        public virtual ICollection<BookImage> BookImages { get; set; } = new List<BookImage>();
        public virtual ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
    }
}

