using AutoPartsShop.Core.Models;
using AutoPartsShop.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsShop.API.Controllers
{
    [Route("api/equipmentcategories")]
    [ApiController]
    public class EquipmentCategoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EquipmentCategoryController(AppDbContext context)
        {
            _context = context;
        }

        // Összes kategória lekérése
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EquipmentCategory>>> GetEquipmentCategories()
        {
            return await _context.EquipmentCategories
                //.Include(ec => ec.Equipments)
                .ToListAsync();
        }

        // Egy kategória lekérése ID alapján
        [HttpGet("{id}")]
        public async Task<ActionResult<EquipmentCategory>> GetEquipmentCategory(int id)
        {
            var category = await _context.EquipmentCategories.FindAsync(id);

            if (category == null)
            {
                return NotFound($"Nem található kategória ezzel az ID-vel: {id}");
            }

            return category;
        }

        // Új kategória hozzáadása
        [HttpPost]
        public async Task<ActionResult<EquipmentCategory>> AddEquipmentCategory([FromBody] EquipmentCategory newCategory)
        {
            if (string.IsNullOrWhiteSpace(newCategory.Name))
            {
                return BadRequest("A kategória neve nem lehet üres!");
            }

            // Ellenőrizzük, hogy létezik-e már ilyen nevű kategória
            var exists = await _context.EquipmentCategories.AnyAsync(ec => ec.Name == newCategory.Name);
            if (exists)
            {
                return Conflict($"Már létezik ilyen nevű kategória: {newCategory.Name}");
            }

            _context.EquipmentCategories.Add(newCategory);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEquipmentCategory), new { id = newCategory.Id }, newCategory);
        }

        // Kategória módosítása
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEquipmentCategory(int id, [FromBody] EquipmentCategory updatedCategory)
        {
            var existingCategory = await _context.EquipmentCategories.FindAsync(id);
            if (existingCategory == null)
            {
                return NotFound($"Nem található kategória ezzel az ID-vel: {id}");
            }

            if (string.IsNullOrWhiteSpace(updatedCategory.Name))
            {
                return BadRequest("A kategória neve nem lehet üres.");
            }

            existingCategory.Name = updatedCategory.Name;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Kategória törlése
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEquipmentCategory(int id)
        {
            var category = await _context.EquipmentCategories.FindAsync(id);
            if (category == null)
            {
                return NotFound($"Nem található kategória ezzel az ID-vel: {id}");
            }

            // Ellenőrizzük, hogy van-e hozzá tartozó felszerelési cikk
            var hasEquipment = await _context.Equipments.AnyAsync(e => e.EquipmentCategoryId == id);
            if (hasEquipment)
            {
                return BadRequest("Nem törölhető, mert vannak hozzá tartozó felszerelési cikkek!");
            }

            _context.EquipmentCategories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
