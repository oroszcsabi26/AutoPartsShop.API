using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPartsShop.Core.DTOs
{
    public class AdminUserDetailsDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int OrderCount { get; set; }
        public string PhoneNumber { get; set; } = "";
        public string Address { get; set; } = "";
        public string ShippingAddress { get; set; } = "";
        public List<AdminUserOrderDto>? Orders { get; set; } 
    }
}
