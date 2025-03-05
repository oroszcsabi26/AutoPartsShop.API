using System.ComponentModel.DataAnnotations;

namespace AutoPartsShop.Core.DTOs
{
    public class UserProfileUpdate
    {
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(255)]
        public string Address { get; set; } = string.Empty;

        [MaxLength(255)]
        public string ShippingAddress { get; set; } = string.Empty;
    }
}
