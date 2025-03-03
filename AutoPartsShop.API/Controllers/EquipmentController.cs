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

        public EquipmentController(AppDbContext context)
        {
            _context = context;
        }

        // Összes felszerelési cikk lekérése
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Equipment>>> GetEquipments()
        {
            return await _context.Equipments
                .Include(e => e.EquipmentCategory) // Betöltjük a kategória adatait is
                .ToListAsync();
        }

        // Egy adott kategória szerinti felszerelési cikkek lekérése
        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<Equipment>>> GetEquipmentsByCategory(int categoryId)
        {
            var equipments = await _context.Equipments
                .Where(e => e.EquipmentCategoryId == categoryId)
                .Include(e => e.EquipmentCategory)
                .ToListAsync();

            if (!equipments.Any())
            {
                return NotFound($"Nem található felszerelési cikk ezzel a kategória ID-vel: {categoryId}");
            }

            return equipments;
        }

        // Új felszerelési cikk rögzítése
        [HttpPost]
        public async Task<ActionResult<Equipment>> AddEquipment([FromBody] Equipment newEquipment)
        {
            if (string.IsNullOrWhiteSpace(newEquipment.Name) || string.IsNullOrWhiteSpace(newEquipment.Manufacturer))
            {
                return BadRequest("A név és a gyártó megadása kötelező!");
            }

            if (newEquipment.Price <= 0)
            {
                return BadRequest("Az ár nem lehet nulla vagy negatív!");
            }

            var categoryExists = await _context.EquipmentCategories.AnyAsync(ec => ec.Id == newEquipment.EquipmentCategoryId);
            if (!categoryExists)
            {
                return BadRequest($"Nincs ilyen kategória ID: {newEquipment.EquipmentCategoryId}");
            }

            _context.Equipments.Add(newEquipment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEquipments), new { id = newEquipment.Id }, newEquipment);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEquipment(int id, [FromBody] Equipment updatedEquipment)
        {
            var existingEquipment = await _context.Equipments.FindAsync(id);
            if (existingEquipment == null)
            {
                return NotFound($"Nem található felszerelési cikk ezzel az ID-vel: {id}");
            }

            existingEquipment.Name = updatedEquipment.Name;
            existingEquipment.Manufacturer = updatedEquipment.Manufacturer;
            existingEquipment.Size = updatedEquipment.Size;
            existingEquipment.Price = updatedEquipment.Price;
            existingEquipment.EquipmentCategoryId = updatedEquipment.EquipmentCategoryId;

            await _context.SaveChangesAsync();
            return NoContent(); // 204 No Content
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEquipment(int id)
        {
            var equipment = await _context.Equipments.FindAsync(id);
            if (equipment == null)
            {
                return NotFound($"Nem található felszerelési cikk ezzel az ID-vel: {id}");
            }

            _context.Equipments.Remove(equipment);
            await _context.SaveChangesAsync();
            return NoContent(); // 204 No Content
        }
    }
}
