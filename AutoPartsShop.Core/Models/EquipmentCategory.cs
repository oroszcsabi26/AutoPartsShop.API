using System.ComponentModel.DataAnnotations;

namespace AutoPartsShop.Core.Models
{
    public class EquipmentCategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty; 

        public List<Equipment> Equipments { get; set; } = new List<Equipment>();
    }
}

