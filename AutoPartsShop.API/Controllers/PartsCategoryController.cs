using Microsoft.AspNetCore.Http;
using AutoPartsShop.Core.Models;
using AutoPartsShop.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoPartsShop.API.Controllers
{
    [Route("api/parts/categories")]
    [ApiController]
    public class PartsCategoryController : ControllerBase
    {
        private readonly AppDbContext m_context;

        public PartsCategoryController(AppDbContext context)
        {
            m_context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PartsCategory>>> GetPartsCategories()
        {
            return await m_context.PartsCategories.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<PartsCategory>> AddPartsCategory([FromBody] PartsCategory p_newCategory)
        {
            if (p_newCategory == null || string.IsNullOrWhiteSpace(p_newCategory.Name))
            {
                return BadRequest("Az alkatrész kategória neve nem lehet üres!");
            }

            var exists = await m_context.PartsCategories.AnyAsync(pc => pc.Name == p_newCategory.Name);
            if (exists)
            {
                return Conflict($"Már létezik ilyen nevű alkatrész kategória: {p_newCategory.Name}");
            }

            m_context.PartsCategories.Add(p_newCategory);
            await m_context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPartsCategories), new { id = p_newCategory.Id }, p_newCategory);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePartsCategory(int p_id, [FromBody] PartsCategory p_updatedCategory)
        {
            var existingCategory = await m_context.PartsCategories.FindAsync(p_id);
            if (existingCategory == null)
            {
                return NotFound($"Nem található alkatrész kategória ezzel az ID-val: {p_id}");
            }

            if (string.IsNullOrWhiteSpace(p_updatedCategory.Name))
            {
                return BadRequest("Az alkatrész kategória neve nem lehet üres.");
            }

            existingCategory.Name = p_updatedCategory.Name;
            await m_context.SaveChangesAsync();

            return NoContent(); 
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePartsCategory(int p_id)
        {
            var category = await m_context.PartsCategories.FindAsync(p_id);
            if (category == null)
            {
                return NotFound($"Nem található alkatrész kategória ezzel az ID-val: {p_id}");
            }

            m_context.PartsCategories.Remove(category);
            await m_context.SaveChangesAsync();

            return NoContent(); 
        }
    }
}
