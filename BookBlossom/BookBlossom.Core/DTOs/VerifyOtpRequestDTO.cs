namespace BookBlossom.Core.DTOs
{
    public class VerifyOtpRequestDTO
    {
        public string PhoneNumber { get; set; }
        public string OtpCode { get; set; }
    }
}
