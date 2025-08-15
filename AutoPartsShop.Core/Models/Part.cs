using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPartsShop.Core.Models
{
    public class Part
    {
        [Key]
        public int Id { get; set; } 

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public decimal Price { get; set; } 

        [Required]
        public int CarModelId { get; set; }

        [ForeignKey("CarModelId")]
        public CarModel? CarModel { get; set; } 

        [Required]
        public int PartsCategoryId { get; set; } 

        [ForeignKey("PartsCategoryId")]
        public PartsCategory? PartsCategory { get; set; } 

        [Required]
        public string Manufacturer { get; set; } = "Ismeretlen"; 

        public string? Side { get; set; } = string.Empty; 

        public string? Shape { get; set; } 
        public string? Size { get; set; } 

        public string? Type { get; set; } 

        public string? Material { get; set; } 
        public string? Description { get; set; } 

        public int Quantity { get; set; } 

        public string? ImageUrl { get; set; } = string.Empty; 

        public List<PartEngineVariant> PartEngineVariants { get; set; } = new();
    }
}
