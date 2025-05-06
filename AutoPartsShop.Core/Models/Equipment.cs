using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoPartsShop.Core.Models
{
    public class Equipment
    {
        [Key]
        public int Id { get; set; } 

        [Required]
        public string Name { get; set; } = string.Empty; 

        [Required]
        public string Manufacturer { get; set; } = string.Empty; 

        public string? Size { get; set; } 

        [Required]
        public decimal Price { get; set; } 

        [Required]
        public int EquipmentCategoryId { get; set; } 

        [ForeignKey("EquipmentCategoryId")]
        public EquipmentCategory? EquipmentCategory { get; set; } 

        public string? Description { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "A mennyiség legalább 1 legyen.")]
        public int Quantity { get; set; } = 1;

        public string? ImageUrl { get; set; }
        public string? Material { get; set; }
        public string? Side { get; set; }
    }
}
