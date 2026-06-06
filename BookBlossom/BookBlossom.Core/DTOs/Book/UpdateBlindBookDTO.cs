using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace BookBlossom.DTOs.BlindBook
{
    public class UpdateBlindBookDTO
    {
        public string Keywords { get; set; } = string.Empty;
        public string Quotes { get; set; } = string.Empty;

        public string Hashtags { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public List<IFormFile>? Images { get; set; }
    }
}
