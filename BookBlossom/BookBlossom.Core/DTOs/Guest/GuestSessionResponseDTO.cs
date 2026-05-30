using System;

namespace BookBlossom.Core.DTOs.Guest
{
    public class GuestSessionResponseDTO
    {
        public Guid GuestID { get; set; }
        public string SessionToken { get; set; } = string.Empty;
        public DateTime ExpireAt { get; set; }
    }
}
