using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AutoPartsShop.Core.Models
{
    public class Cart
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; } // Kosárhoz kapcsolódó felhasználó

        public List<CartItem> Items { get; set; } = new List<CartItem>();
    }
}