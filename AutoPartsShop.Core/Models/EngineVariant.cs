using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPartsShop.Core.Models
{
    public class EngineVariant
    {
        public int Id { get; set; }

        [Required]
        public int CarModelId { get; set; }

        [ForeignKey("CarModelId")]
        public CarModel? CarModel { get; set; }

        public string FuelType { get; set; } = string.Empty;
        public int EngineSize { get; set; }

        public int YearFrom { get; set; }
        public int YearTo { get; set; }

        public List<PartEngineVariant> PartEngineVariants { get; set; } = new();
    }
}
