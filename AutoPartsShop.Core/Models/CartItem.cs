using AutoPartsShop.Core.Models;
using System.ComponentModel.DataAnnotations;

public class CartItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CartId { get; set; } 
    public Cart? Cart { get; set; }

    public int? PartId { get; set; } 
    public Part? Part { get; set; }

    public int? EquipmentId { get; set; } 
    public Equipment? Equipment { get; set; }

    [Required]
    public string ItemType { get; set; } = "Part";

    [Required]
    public int Quantity { get; set; } = 1;

    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

