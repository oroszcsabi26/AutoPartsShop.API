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
    }
}
