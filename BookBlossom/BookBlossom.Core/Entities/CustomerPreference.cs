namespace BookBlossom.Core.Entities;

public class CustomerPreference
{
    public long CustomerID { get; set; } 
    public long CategoryID { get; set; } 
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual CustomerDetail CustomerDetail {get; set; }
    public virtual Category Category {get; set; }
}