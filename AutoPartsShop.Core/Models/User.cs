using System.ComponentModel.DataAnnotations;

namespace AutoPartsShop.Core.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; } 

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
        [MinLength(8)]
        [MaxLength(1000)]
        public string PasswordHash { get; set; } = string.Empty; 

        [MaxLength(255)]
        public string Address { get; set; } = string.Empty; 

        [MaxLength(255)]
        public string ShippingAddress { get; set; } = string.Empty; 

        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty; 

        public Cart? Cart { get; set; }

        public List<Order> Orders { get; set; } = new List<Order>();

        public bool IsAdmin { get; set; } = false; 
    }
}
