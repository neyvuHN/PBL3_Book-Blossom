namespace BookBlossom.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class RealBook
{
   [Key]
   [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
   public long BookID {get; set;}
   public long CategoryID {get; set; }
   public string Title {get; set; }
   public string Publisher {get; set; }
   public string ISBN {get; set; }
   public int PublishYear {get; set;}
   public string? Description {get; set; }
   public decimal Price {get; set; }
   public string? SampleFilePath {get; set; }
   public decimal Weight {get; set; }
   public int UnitsInStock {get; set; }
   public int ReservedQuantity {get; set; } 
   public bool IsContinued {get; set; }

    [ForeignKey("CategoryID")]
    public virtual Category Category { get; set; } 
    public virtual BlindBook? BlindBook {get; set; }
    public virtual ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
    public virtual ICollection<BookImage> BookImages { get; set; } = new List<BookImage>();
}