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
        private readonly AppDbContext m_context;
        private readonly AzureBlobStorageService m_blobStorageService;

        public EquipmentController(AppDbContext context, AzureBlobStorageService blobStorageService)
        {
            m_context = context;
            m_blobStorageService = blobStorageService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<EquipmentDisplay>>> GetEquipments()
        {
            var equipments = await m_context.Equipments
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

        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<EquipmentDisplay>>> GetEquipmentsByCategory(int categoryId)
        {
            var equipments = await m_context.Equipments
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
        public async Task<ActionResult<Equipment>> AddEquipment([FromForm] Equipment p_newEquipment, IFormFile? p_imageFile)
        {
            if (string.IsNullOrWhiteSpace(p_newEquipment.Name) || string.IsNullOrWhiteSpace(p_newEquipment.Manufacturer))
                return BadRequest("A név és a gyártó megadása kötelező!");

            if (p_newEquipment.Price <= 0)
                return BadRequest("Az ár nem lehet nulla vagy negatív!");

            if (!await m_context.EquipmentCategories.AnyAsync(ec => ec.Id == p_newEquipment.EquipmentCategoryId))
                return BadRequest($"Nincs ilyen kategória ID: {p_newEquipment.EquipmentCategoryId}");

            if (p_imageFile != null && p_imageFile.Length > 0)
            {
                using var stream = p_imageFile.OpenReadStream();
                p_newEquipment.ImageUrl = await m_blobStorageService.UploadFileAsync(stream, p_imageFile.FileName);
            }

            m_context.Equipments.Add(p_newEquipment);
            await m_context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEquipments), new { id = p_newEquipment.Id }, p_newEquipment);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEquipment(int p_id, [FromBody] Equipment p_updatedEquipment, IFormFile? p_imageFile)
        {
            var existingEquipment = await m_context.Equipments.FindAsync(p_id);
            if (existingEquipment == null)
                return NotFound($"Nem található felszerelési cikk ezzel az ID-vel: {p_id}");

            existingEquipment.Name = p_updatedEquipment.Name;
            existingEquipment.Manufacturer = p_updatedEquipment.Manufacturer;
            existingEquipment.Size = p_updatedEquipment.Size;
            existingEquipment.Price = p_updatedEquipment.Price;
            existingEquipment.Description = p_updatedEquipment.Description;
            existingEquipment.Quantity = p_updatedEquipment.Quantity;
            existingEquipment.ImageUrl = p_updatedEquipment.ImageUrl;
            existingEquipment.Material = p_updatedEquipment.Material;
            existingEquipment.Side = p_updatedEquipment.Side;
            existingEquipment.EquipmentCategoryId = p_updatedEquipment.EquipmentCategoryId;

            if (p_imageFile != null && p_imageFile.Length > 0)
            {
                using var stream = p_imageFile.OpenReadStream();
                existingEquipment.ImageUrl = await m_blobStorageService.UploadFileAsync(stream, p_imageFile.FileName);
            }

            await m_context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEquipment(int p_id)
        {
            var equipment = await m_context.Equipments.FindAsync(p_id);
            if (equipment == null)
                return NotFound($"Nem található felszerelési cikk ezzel az ID-vel: {p_id}");

            m_context.Equipments.Remove(equipment);
            await m_context.SaveChangesAsync();
            return NoContent();
        }
    }
}
