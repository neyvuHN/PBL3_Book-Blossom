namespace BookBlossom.Core.Entities;

public class RealBook
{
   public long BookID {get; set;}
   public long CategoryID {get; set; }
   public string Title {get; set; }
   public string Publisher {get; set; }
   public string ISBN {get; set; }
   public DateTime PublishYear {get; set;}
   public string Description {get; set; }
   public decimal Price {get; set; }
   public string SampleFilePath {get; set; }
   public decimal Weight {get; set; }
   public int UnitsInStock {get; set; }
   public int ReservedQuantity {get; set; } 
   public bool IsContinued {get; set; }
    public virtual Category Category { get; set; } 
}