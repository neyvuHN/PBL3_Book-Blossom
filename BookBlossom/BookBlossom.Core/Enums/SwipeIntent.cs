namespace BookBlossom.Core.Enums
{
    public enum SwipeIntent : byte
    {
        Wishlist = 0,      // Quẹt Phải
        Hide = 1,          // Quẹt Trái
        AddToCart = 2,     // Quẹt Lên
        Skip = 3,          // Quẹt Xuống
        Undo = 4
    }
}