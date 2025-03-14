﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoPartsShop.Core.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; } // Egyedi rendelés azonosító

        [Required]
        public int UserId { get; set; } // Felhasználó azonosítója
        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow; // Rendelés dátuma

        [Required]
        public string Status { get; set; } = "Feldolgozás alatt"; // Rendelés állapota

        [Required]
        public string ShippingAddress { get; set; } = string.Empty; // Szállítási cím

        [Required]
        public string BillingAddress { get; set; } = string.Empty; // Számlázási cím

        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>(); // Rendelés tételei

        [MaxLength(200)]
        public string? Comment { get; set; } // Opcionális megjegyzés mező (max 200 karakter)
    }
}

