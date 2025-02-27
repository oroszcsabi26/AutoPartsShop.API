using System.ComponentModel.DataAnnotations;

namespace AutoPartsShop.Core.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; } // Egyedi azonosító

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty; // Keresztnév

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty; // Vezetéknév

        [Required]
        [MaxLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty; // E-mail cím (egyedi)

        [Required]
        [MinLength(8)]
        [MaxLength(64)]
        public string PasswordHash { get; set; } = string.Empty; // Jelszó (hash-elt)

        [MaxLength(255)]
        public string Address { get; set; } = string.Empty; // Cím

        [MaxLength(255)]
        public string ShippingAddress { get; set; } = string.Empty; // Szállítási cím

        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty; // Telefonszám

        // Kosár kapcsolata a felhasználóhoz
        public Cart? Cart { get; set; }
    }
}
