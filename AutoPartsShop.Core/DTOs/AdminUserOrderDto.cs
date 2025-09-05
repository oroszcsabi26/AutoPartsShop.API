using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPartsShop.Core.DTOs
{
    public class AdminUserOrderDto
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = "";
        public int ItemCount { get; set; }
        public decimal Total { get; set; }
    }
}
