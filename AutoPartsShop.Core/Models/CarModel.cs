using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AutoPartsShop.Core.Models
{
    public class CarModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int Year { get; set; }

        public int CarBrandId { get; set; }

        public CarBrand? CarBrand { get; set; } // Ezt `nullable`-re tettük (?)

        public List<Part> Parts { get; set; } = new List<Part>(); // Kapcsolat az alkatrészekkel
    
        public string FuelType { get; set; } = string.Empty; // Üzemanyag típusa (pl. benzin, dízel, elektromos)

        public int EngineSize { get; set; } // Motor mérete (pl. 1600cc)
    }
}
