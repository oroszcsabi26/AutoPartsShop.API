using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPartsShop.Core.Models
{
    public class CarBrand
    {
        public int Id { get; set; }  
        public string Name { get; set; } = string.Empty;  

        public List<CarModel> CarModels { get; set; } = new List<CarModel>();
    }
}
