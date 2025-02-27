using AutoPartsShop.Core.Models;
using AutoPartsShop.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsShop.API.Controllers
{
    [Route("api/cars")]     //Az API végpont URL-je /api/carbrand lesz.
    [ApiController]
    public class CarBrandController : ControllerBase
    {
        private readonly AppDbContext _context;     //Az AppDbContext segítségével érjük el az adatbázist.

        public CarBrandController(AppDbContext context)    // Dependency Injection segítségével kapjuk meg az adatbázis kapcsolatot.
        {
            _context = context;
        }

        // 🔹 Összes autómárka lekérése
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CarBrand>>> GetCarBrands()
        {
            return await _context.CarBrands
                .Include(cb => cb.CarModels)
                .ToListAsync();
        }

        // 🔹 Új autómárka rögzítése
        [HttpPost]
        public async Task<ActionResult<CarBrand>> AddCarBrand(CarBrand newBrand)
        {
            if (newBrand == null || string.IsNullOrWhiteSpace(newBrand.Name))
            {
                return BadRequest("Az autómárka neve nem lehet üres!");
            }

            _context.CarBrands.Add(newBrand);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCarBrands), new { id = newBrand.Id }, newBrand);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCarBrand(int id, [FromBody] CarBrand updatedBrand)
        {
            if (updatedBrand == null || string.IsNullOrWhiteSpace(updatedBrand.Name))
            {
                return BadRequest("Az autómárka neve nem lehet üres!");
            }

            var existingBrand = await _context.CarBrands.FindAsync(id);
            if (existingBrand == null)
            {
                return NotFound($"Nincs autómárka ezzel az ID-vel: {id}");
            }

            existingBrand.Name = updatedBrand.Name;
            await _context.SaveChangesAsync();

            return Ok(existingBrand);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCarBrand(int id)
        {
            var brand = await _context.CarBrands
                .Include(cb => cb.CarModels) // Betöltjük a kapcsolódó modelleket
                .FirstOrDefaultAsync(cb => cb.Id == id);

            if (brand == null)
            {
                return NotFound($"Nincs autómárka ezzel az ID-vel: {id}");
            }

            if (brand.CarModels.Any())
            {
                return BadRequest("Nem törölhető, mert még léteznek hozzá tartozó autómodellek!");
            }

            _context.CarBrands.Remove(brand);
            await _context.SaveChangesAsync();

            return NoContent();
        }

    }
}
