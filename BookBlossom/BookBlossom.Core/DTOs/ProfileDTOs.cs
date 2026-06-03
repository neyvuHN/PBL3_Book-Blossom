using System;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BookBlossom.Core.DTOs
{
    public class UpdateProfileDTO
    {
        [StringLength(50)]
        public string? FirstName { get; set; }
        
        [StringLength(50)]
        public string? LastName { get; set; }
        
        [StringLength(50)]
        public string? UserName { get; set; }
        
        public string? Gender { get; set; }
        public DateTime? Birthdate { get; set; }
        public string? Bio { get; set; }
        public IFormFile? AvatarImage { get; set; }
    }

    public class AddressDTO
    {
        public long AddressID { get; set; }
        
        [Required]
        [StringLength(100)]
        public string ReceiverName { get; set; } = string.Empty;
        
        [Required]
        [Phone]
        [StringLength(15)]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [Required]
        [StringLength(255)]
        public string DetailAddress { get; set; } = string.Empty;
        
        public bool IsDefault { get; set; }
    }
}
