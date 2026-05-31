using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookBlossom.Core.Entities
{
    [Table("Category", Schema = "Preference")]
    public class Category
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long CategoryID { get; set; }

        [Required]
        [MaxLength(255)]
        public string CategoryName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Active";

        // Navigation Property: One Category has many Books
        public virtual ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
