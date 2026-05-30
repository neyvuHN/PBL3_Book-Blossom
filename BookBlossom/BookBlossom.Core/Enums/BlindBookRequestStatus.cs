// Các trạng thái của yêu cầu duyệt blindbook từ manager gửi về store manager
namespace BookBlossom.Core.Enums
{
    public enum BlindBookRequestStatus : byte
    {
        Pending, // Chờ xử lý
        Approved, // Đã duyệt
        Rejected // Đã từ chối
    }
}