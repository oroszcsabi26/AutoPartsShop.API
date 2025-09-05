using System.ComponentModel.DataAnnotations;

namespace AutoPartsShop.Core.DTOs
{
    public class ResetPasswordRequestDto
    {
        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = string.Empty;
    }
}
