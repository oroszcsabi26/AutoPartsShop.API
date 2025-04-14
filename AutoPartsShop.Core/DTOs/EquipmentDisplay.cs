using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPartsShop.Core.DTOs
{
    public class EquipmentDisplay
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Size { get; set; }
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }
        public string? Material { get; set; }
        public string? Side { get; set; }

        public int EquipmentCategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }
}
