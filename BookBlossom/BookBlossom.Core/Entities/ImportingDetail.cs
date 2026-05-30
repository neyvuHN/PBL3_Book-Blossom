namespace BookBlossom.Core.Entities
{
    public class ImportingDetail
    {
        public long ImportingID { get; set; }
        public long BookID { get; set; }
        public decimal? UnitPrice { get; set; }
        public int? Quantity { get; set; }
        public decimal? LineTotal { get; set; } // computed column

        public virtual Importing Importing { get; set; } = null!;
        public virtual RealBook Book { get; set; } = null!;
    }
}
