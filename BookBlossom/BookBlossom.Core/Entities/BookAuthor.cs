using System.ComponentModel.DataAnnotations.Schema;

namespace BookBlossom.Core.Entities
{
    [Table("BookAuthor", Schema = "Book")]
    public class BookAuthor
    {
        public long BookID { get; set; }
        public long AuthorID { get; set; }

        // Navigation properties
        [ForeignKey("BookID")]
        public virtual RealBook Book { get; set; } = null!;

        [ForeignKey("AuthorID")]
        public virtual Author Author { get; set; } = null!;
    }
}
