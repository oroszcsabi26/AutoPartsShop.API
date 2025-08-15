using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoPartsShop.Core.Models
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; } 

        [Required]
        public int OrderId { get; set; } 
        [ForeignKey("OrderId")]
        public Order? Order { get; set; }

        [Required]
        public string ItemType { get; set; } = "Part"; 

        public int? PartId { get; set; }
        [ForeignKey("PartId")]
        public Part? Part { get; set; }

        public int? EquipmentId { get; set; }
        [ForeignKey("EquipmentId")]
        public Equipment? Equipment { get; set; }

        [Required]
        public int Quantity { get; set; } = 1; 

        [Required]
        public decimal Price { get; set; } 

        [Required]
        public string Name { get; set; } = string.Empty; 
    }
}
