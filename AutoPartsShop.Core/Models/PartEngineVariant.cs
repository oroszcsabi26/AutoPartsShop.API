using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPartsShop.Core.Models
{
    public class PartEngineVariant
    {
        public int PartId { get; set; }
        public Part Part { get; set; }

        public int EngineVariantId { get; set; }
        public EngineVariant EngineVariant { get; set; }
    }
}
