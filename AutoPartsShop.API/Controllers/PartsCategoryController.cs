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
        private readonly AppDbContext _context;

        public PartsCategoryController(AppDbContext context)
        {
            _context = context;
        }

        // 🔹 Összes alkatrész kategória lekérése
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PartsCategory>>> GetPartsCategories()
        {
            return await _context.PartsCategories.ToListAsync();
        }

        // 🔹 Új alkatrész kategória hozzáadása
        [HttpPost]
        public async Task<ActionResult<PartsCategory>> AddPartsCategory([FromBody] PartsCategory newCategory)
        {
            if (newCategory == null || string.IsNullOrWhiteSpace(newCategory.Name))
            {
                return BadRequest("Az alkatrész kategória neve nem lehet üres!");
            }

            // Ellenőrizzük, hogy létezik-e már ugyanilyen nevű kategória
            var exists = await _context.PartsCategories.AnyAsync(pc => pc.Name == newCategory.Name);
            if (exists)
            {
                return Conflict($"Már létezik ilyen nevű alkatrész kategória: {newCategory.Name}");
            }

            _context.PartsCategories.Add(newCategory);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPartsCategories), new { id = newCategory.Id }, newCategory);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePartsCategory(int id, [FromBody] PartsCategory updatedCategory)
        {
            var existingCategory = await _context.PartsCategories.FindAsync(id);
            if (existingCategory == null)
            {
                return NotFound($"Nem található alkatrész kategória ezzel az ID-val: {id}");
            }

            if (string.IsNullOrWhiteSpace(updatedCategory.Name))
            {
                return BadRequest("Az alkatrész kategória neve nem lehet üres.");
            }

            existingCategory.Name = updatedCategory.Name;
            await _context.SaveChangesAsync();

            return NoContent(); // 204 No Content
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePartsCategory(int id)
        {
            var category = await _context.PartsCategories.FindAsync(id);
            if (category == null)
            {
                return NotFound($"Nem található alkatrész kategória ezzel az ID-val: {id}");
            }

            _context.PartsCategories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent(); // 204 No Content
        }
    }
}
