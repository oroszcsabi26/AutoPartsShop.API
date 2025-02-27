using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoPartsShop.Core.Models
{
    public class Equipment
    {
        [Key]
        public int Id { get; set; } // Egyedi azonosító

        [Required]
        public string Name { get; set; } = string.Empty; // A felszerelési cikk neve

        [Required]
        public string Manufacturer { get; set; } = string.Empty; // Gyártó neve (KÖTELEZŐ)

        public string? Size { get; set; } // Kiszerelési méret (NEM KÖTELEZŐ)

        [Required]
        public decimal Price { get; set; } // A felszerelési cikk ára

        [Required]
        public int EquipmentCategoryId { get; set; } // Kapcsolat a kategóriával (KÖTELEZŐ)

        [ForeignKey("EquipmentCategoryId")]
        public EquipmentCategory? EquipmentCategory { get; set; } // Egy felszerelési cikk egy kategóriába tartozik
    }
}
