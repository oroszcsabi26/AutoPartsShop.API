using AutoPartShop.Core.Helpers;
using AutoPartsShop.Core.DTOs;
using AutoPartsShop.Core.Models;
using AutoPartsShop.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsShop.API.Controllers
{
    [Route("api/equipment")]
    [ApiController]
    public class EquipmentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AzureBlobStorageService _blobStorageService;

        public EquipmentController(AppDbContext context, AzureBlobStorageService blobStorageService)
        {
            _context = context;
            _blobStorageService = blobStorageService;
        }

        // Összes felszerelési cikk lekérése DTO-val
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EquipmentDisplay>>> GetEquipments()
        {
            var equipments = await _context.Equipments
                .Include(e => e.EquipmentCategory)
                .ToListAsync();

            return equipments.Select(e => new EquipmentDisplay
            {
                Id = e.Id,
                Name = e.Name,
                Manufacturer = e.Manufacturer,
                Price = e.Price,
                Size = e.Size,
                Description = e.Description,
                Quantity = e.Quantity,
                ImageUrl = e.ImageUrl,
                Material = e.Material,
                Side = e.Side,
                EquipmentCategoryId = e.EquipmentCategoryId,
                CategoryName = e.EquipmentCategory?.Name ?? ""
            }).ToList();
        }

        // Egy adott kategória szerinti felszerelések DTO-val
        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<EquipmentDisplay>>> GetEquipmentsByCategory(int categoryId)
        {
            var equipments = await _context.Equipments
                .Where(e => e.EquipmentCategoryId == categoryId)
                .Include(e => e.EquipmentCategory)
                .ToListAsync();

            if (!equipments.Any())
            {
                return NotFound($"Nem található felszerelési cikk ezzel a kategória ID-vel: {categoryId}");
            }

            return equipments.Select(e => new EquipmentDisplay
            {
                Id = e.Id,
                Name = e.Name,
                Manufacturer = e.Manufacturer,
                Price = e.Price,
                Size = e.Size,
                Description = e.Description,
                Quantity = e.Quantity,
                ImageUrl = e.ImageUrl,
                Material = e.Material,
                Side = e.Side,
                EquipmentCategoryId = e.EquipmentCategoryId,
                CategoryName = e.EquipmentCategory?.Name ?? ""
            }).ToList();
        }

        [HttpPost]
        public async Task<ActionResult<Equipment>> AddEquipment([FromForm] Equipment newEquipment, IFormFile? imageFile)
        {
            if (string.IsNullOrWhiteSpace(newEquipment.Name) || string.IsNullOrWhiteSpace(newEquipment.Manufacturer))
                return BadRequest("A név és a gyártó megadása kötelező!");

            if (newEquipment.Price <= 0)
                return BadRequest("Az ár nem lehet nulla vagy negatív!");

            if (!await _context.EquipmentCategories.AnyAsync(ec => ec.Id == newEquipment.EquipmentCategoryId))
                return BadRequest($"Nincs ilyen kategória ID: {newEquipment.EquipmentCategoryId}");

            // Kép mentése, ha van
            if (imageFile != null && imageFile.Length > 0)
            {
                using var stream = imageFile.OpenReadStream();
                newEquipment.ImageUrl = await _blobStorageService.UploadFileAsync(stream, imageFile.FileName);
            }

            _context.Equipments.Add(newEquipment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEquipments), new { id = newEquipment.Id }, newEquipment);
        }

        // Felszerelés módosítása (Angularban még úgy van beállítva hogy nem küldi a képet is, ezt módosítani kell!!)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEquipment(int id, [FromBody] Equipment updatedEquipment, IFormFile? imageFile)
        {
            var existingEquipment = await _context.Equipments.FindAsync(id);
            if (existingEquipment == null)
                return NotFound($"Nem található felszerelési cikk ezzel az ID-vel: {id}");

            existingEquipment.Name = updatedEquipment.Name;
            existingEquipment.Manufacturer = updatedEquipment.Manufacturer;
            existingEquipment.Size = updatedEquipment.Size;
            existingEquipment.Price = updatedEquipment.Price;
            existingEquipment.Description = updatedEquipment.Description;
            existingEquipment.Quantity = updatedEquipment.Quantity;
            existingEquipment.ImageUrl = updatedEquipment.ImageUrl;
            existingEquipment.Material = updatedEquipment.Material;
            existingEquipment.Side = updatedEquipment.Side;
            existingEquipment.EquipmentCategoryId = updatedEquipment.EquipmentCategoryId;

            // Új kép feltöltése, ha van
            if (imageFile != null && imageFile.Length > 0)
            {
                using var stream = imageFile.OpenReadStream();
                existingEquipment.ImageUrl = await _blobStorageService.UploadFileAsync(stream, imageFile.FileName);
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Felszerelés törlése
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEquipment(int id)
        {
            var equipment = await _context.Equipments.FindAsync(id);
            if (equipment == null)
                return NotFound($"Nem található felszerelési cikk ezzel az ID-vel: {id}");

            _context.Equipments.Remove(equipment);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
