using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPartsShop.Core.Models
{
    public class PartsCategory
    {
        [Key]
        public int Id { get; set; } // Egyedi azonosító

        [Required]
        public string Name { get; set; } = string.Empty; // Kategória neve

        // 🔹 Kapcsolat az alkatrészekkel: Egy kategóriához több alkatrész tartozhat
        public List<Part> Parts { get; set; } = new List<Part>();
    }
}
