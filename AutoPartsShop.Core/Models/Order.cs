using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AutoPartsShop.Core.Enums;

namespace AutoPartsShop.Core.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; } 

        [Required]
        public int UserId { get; set; } 

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow; 

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Feldolgozás; 

        [Required]
        public string ShippingAddress { get; set; } = string.Empty; 

        [Required]
        public string BillingAddress { get; set; } = string.Empty; 

        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>(); 

        [MaxLength(200)]
        public string? Comment { get; set; } 

        [Required]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Készpénz;

        public int ExtraFee { get; set; } = 0;

        [Required]
        public ShippingMethod ShippingMethod { get; set; } 
    }
}

