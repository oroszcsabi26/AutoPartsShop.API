using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPartsShop.Core.DTOs
{
    public class PartDisplay
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Manufacturer { get; set; } = string.Empty;
        public string? Side { get; set; }
        public string? Shape { get; set; }
        public string? Size { get; set; }
        public string? Type { get; set; }
        public string? Material { get; set; }
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CarModelName { get; set; } = string.Empty;
        public string CarBrandName { get; set; } = string.Empty;

        public int CarModelId { get; set; }

        public int PartsCategoryId { get; set; }
        public string? ImageUrl { get; set; }
    }
}
