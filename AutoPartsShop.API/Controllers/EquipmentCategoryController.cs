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
        private readonly AppDbContext m_context;

        public EquipmentCategoryController(AppDbContext context)
        {
            m_context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<EquipmentCategory>>> GetEquipmentCategories()
        {
            return await m_context.EquipmentCategories
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EquipmentCategory>> GetEquipmentCategory(int p_id)
        {
            var category = await m_context.EquipmentCategories.FindAsync(p_id);

            if (category == null)
            {
                return NotFound($"Nem található kategória ezzel az ID-vel: {p_id}");
            }

            return category;
        }

        [HttpPost]
        public async Task<ActionResult<EquipmentCategory>> AddEquipmentCategory([FromBody] EquipmentCategory p_newCategory)
        {
            if (string.IsNullOrWhiteSpace(p_newCategory.Name))
            {
                return BadRequest("A kategória neve nem lehet üres!");
            }

            var exists = await m_context.EquipmentCategories.AnyAsync(ec => ec.Name == p_newCategory.Name);
            if (exists)
            {
                return Conflict($"Már létezik ilyen nevű kategória: {p_newCategory.Name}");
            }

            m_context.EquipmentCategories.Add(p_newCategory);
            await m_context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEquipmentCategory), new { id = p_newCategory.Id }, p_newCategory);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEquipmentCategory(int p_id, [FromBody] EquipmentCategory p_updatedCategory)
        {
            var existingCategory = await m_context.EquipmentCategories.FindAsync(p_id);
            if (existingCategory == null)
            {
                return NotFound($"Nem található kategória ezzel az ID-vel: {p_id}");
            }

            if (string.IsNullOrWhiteSpace(p_updatedCategory.Name))
            {
                return BadRequest("A kategória neve nem lehet üres.");
            }

            existingCategory.Name = p_updatedCategory.Name;
            await m_context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEquipmentCategory(int p_id)
        {
            var category = await m_context.EquipmentCategories.FindAsync(p_id);
            if (category == null)
            {
                return NotFound($"Nem található kategória ezzel az ID-vel: {p_id}");
            }

            var hasEquipment = await m_context.Equipments.AnyAsync(e => e.EquipmentCategoryId == p_id);
            if (hasEquipment)
            {
                return BadRequest("Nem törölhető, mert vannak hozzá tartozó felszerelési cikkek!");
            }

            m_context.EquipmentCategories.Remove(category);
            await m_context.SaveChangesAsync();

            return NoContent();
        }
    }
}
