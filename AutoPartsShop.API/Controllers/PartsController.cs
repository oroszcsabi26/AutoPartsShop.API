using AutoPartsShop.Core.Models;
using AutoPartsShop.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsShop.API.Controllers
{
    [Route("api/parts")] // 🔹 Az API végpont URL-je /api/parts lesz.
    [ApiController]
    public class PartsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PartsController(AppDbContext context)
        {
            _context = context;
        }

        // 🔹 Összes alkatrész lekérése
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Part>>> GetParts()
        {
            return await _context.Parts
                .Include(p => p.PartsCategory)
                .Include(p => p.CarModel)
                .ToListAsync();
        }

        // 🔹 Egy adott autómodell alkatrészeinek lekérése
        [HttpGet("carModel/{carModelId}")]
        public async Task<ActionResult<IEnumerable<Part>>> GetPartsByCarModel(int carModelId)
        {
            var parts = await _context.Parts
                .Where(p => p.CarModelId == carModelId)
                .Include(p => p.PartsCategory)
                .Include(p => p.CarModel)
                .Include(p => p.CarModel.CarBrand)
                .ToListAsync();

            if (!parts.Any())
            {
                return NotFound($"Nincs alkatrész ezzel az autómodell ID-vel: {carModelId}");
            }

            return parts;
        }

        // 🔹 Új alkatrész rögzítése
        [HttpPost]
        public async Task<ActionResult<Part>> AddPart([FromBody] Part newPart)
        {
            // Ellenőrzés: Létezik-e a megadott autómodell és kategória
            var carModelExists = await _context.CarModels.AnyAsync(cm => cm.Id == newPart.CarModelId);
            var categoryExists = await _context.PartsCategories.AnyAsync(pc => pc.Id == newPart.PartsCategoryId);

            if (!carModelExists)
            {
                return BadRequest($"Nincs ilyen autómodell ID: {newPart.CarModelId}");
            }

            if (!categoryExists)
            {
                return BadRequest($"Nincs ilyen alkatrészkategória ID: {newPart.PartsCategoryId}");
            }

            _context.Parts.Add(newPart);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetParts), new { id = newPart.Id }, newPart);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePart(int id, [FromBody] Part updatedPart)
        {
            var part = await _context.Parts.FindAsync(id);
            if (part == null)
            {
                return NotFound($"Nincs alkatrész ezzel az ID-vel: {id}");
            }

            // Frissítés
            part.Name = updatedPart.Name;
            part.Price = updatedPart.Price;
            part.PartsCategoryId = updatedPart.PartsCategoryId;
            part.CarModelId = updatedPart.CarModelId;

            await _context.SaveChangesAsync();
            return NoContent(); // 204 No Content, mert nincs visszaküldendő adat
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePart(int id)
        {
            var part = await _context.Parts.FindAsync(id);
            if (part == null)
            {
                return NotFound($"Nincs ilyen alkatrész az adatbázisban: {id}");
            }

            _context.Parts.Remove(part);
            await _context.SaveChangesAsync();
            return NoContent(); // 204 No Content, mert nincs visszaküldendő adat
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Part>>> SearchParts(
            [FromQuery] string? name,
            [FromQuery] int? carModelId,
            [FromQuery] int? partsCategoryId)
        {
            IQueryable<Part> query = _context.Parts
                .Include(p => p.CarModel)
                .Include(p => p.PartsCategory);

            // Ha sem autómodell, sem kategória nincs megadva, ne adjunk vissza találatot!
            if (!carModelId.HasValue && !partsCategoryId.HasValue)
            {
                return BadRequest("Legalább egy szűrési feltétel (autómodell vagy alkatrész kategória) szükséges!");
            }

            // Név szerinti szűrés
            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(p => p.Name.Contains(name));
            }

            // Autómodell szerinti szűrés
            if (carModelId.HasValue)
            {
                query = query.Where(p => p.CarModelId == carModelId.Value);
            }

            // Alkatrész kategória szerinti szűrés
            if (partsCategoryId.HasValue)
            {
                query = query.Where(p => p.PartsCategoryId == partsCategoryId.Value);
            }

            var result = await query.ToListAsync();

            // Ha nincs találat, üres listát adunk vissza 200-as kóddal
            return Ok(result);
        }
    }
}
