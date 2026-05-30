namespace BookBlossom.Core.Entities
{
    public class ThreadImage
    {
        public long ImageID { get; set; }
        public long PostID { get; set; }
        public string ImagePath { get; set; } = string.Empty;

        // Navigation properties
        public virtual ThreadPost Post { get; set; } = null!;
    }
}
