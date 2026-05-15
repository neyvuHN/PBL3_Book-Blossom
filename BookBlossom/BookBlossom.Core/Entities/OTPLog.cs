using System;
using System.ComponentModel.DataAnnotations;

namespace BookBlossom.Core.Entities
{
    public class OTPLog
    {
        [Key]
        public long LogID { get; set; }
        public long? UserID { get; set; }
        public Guid? GuestID { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string OTPCode { get; set; } = string.Empty;
        public DateTime ExpireAt { get; set; }
        public bool? IsUsed { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
