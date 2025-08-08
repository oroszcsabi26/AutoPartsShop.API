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
        public int Id { get; set; } // Egyedi azonosító

        [Required]
        public string Name { get; set; } = string.Empty; // Alkatrész neve

        [Required]
        public decimal Price { get; set; } // Alkatrész ára

        [Required]
        public int CarModelId { get; set; } // Kapcsolat az autómodellel

        [ForeignKey("CarModelId")]
        public CarModel? CarModel { get; set; } // Egy alkatrész egy autómodellhez tartozik

        [Required]
        public int PartsCategoryId { get; set; } // Kapcsolat az alkatrész kategóriával

        [ForeignKey("PartsCategoryId")]
        public PartsCategory? PartsCategory { get; set; } // Egy alkatrész egy kategóriához tartozik

        [Required]
        public string Manufacturer { get; set; } = "Ismeretlen"; // Gyártó neve (KÖTELEZŐ)

        public string? Side { get; set; } = string.Empty; // Alkatrész oldala

        public string? Shape { get; set; } // Alkatrész alakja
        public string? Size { get; set; } // Alkatrész mérete

        public string? Type { get; set; } // Alkatrész leírása

        public string? Material { get; set; } // Alkatrész anyaga
        public string? Description { get; set; } // Alkatrész leírása

        public int Quantity { get; set; } // Alkatrész mennyisége

        public string? ImageUrl { get; set; } = string.Empty; // Alkatrész kép URL-je

        [NotMapped]
        public int[] CompatibleManufacturingYears { get; set; } = Array.Empty<int>();

        public string CompatibleYearsRaw
        {
            get => string.Join(",", CompatibleManufacturingYears);
            set => CompatibleManufacturingYears = string.IsNullOrWhiteSpace(value)
                ? Array.Empty<int>()
                : value.Split(',').Select(int.Parse).ToArray();
        }
    }
}
