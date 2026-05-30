namespace BookBlossom.Core.Enums
{
    public enum SwipeIntent : byte
    {
        Wishlist = 1,      // Quẹt Phải
        Hide = 2,          // Quẹt Trái
        AddToCart = 3,     // Quẹt Lên
        Skip = 4,          // Quẹt Xuống
        Undo = 5
    }
}