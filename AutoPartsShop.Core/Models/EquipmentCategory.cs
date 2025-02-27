using System.ComponentModel.DataAnnotations;

namespace AutoPartsShop.Core.Models
{
    public class EquipmentCategory
    {
        [Key]
        public int Id { get; set; } // Egyedi azonosító

        [Required]
        public string Name { get; set; } = string.Empty; // Kategória neve

        // Kapcsolat a felszerelési cikkekkel (1:N kapcsolat)
        public List<Equipment> Equipments { get; set; } = new List<Equipment>();
    }
}

