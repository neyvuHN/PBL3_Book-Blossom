using System;

namespace BookBlossom.Core.Entities
{
    public class GuestDetail
    {
        // uniqueidentifier trong SQL tương ứng với Guid trong C#
        public Guid GuestID { get; set; } 

        public string SessionToken { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastActiveAt { get; set; }
        public string? IPAddress { get; set; }
        public string? DeviceInfo { get; set; }

        // Khóa ngoại (Cho phép NULL, lưu ID của User sau khi Guest này đăng ký tài khoản)
        public long? ConvertedUserID { get; set; } 

        // Navigation property trỏ tới User
        public virtual User? ConvertedUser { get; set; }
    }
}
