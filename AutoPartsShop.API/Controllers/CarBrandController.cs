using AutoPartsShop.Core.Models;
using AutoPartsShop.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsShop.API.Controllers
{
    [Route("api/cars")]   
    [ApiController]
    public class CarBrandController : ControllerBase
    {
        private readonly AppDbContext m_context;     

        public CarBrandController(AppDbContext context)    
        {
            m_context = context;
        }

        // Összes autómárka lekérése
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CarBrand>>> GetCarBrands()
        {
            return await m_context.CarBrands
                .Include(cb => cb.CarModels)
                .ToListAsync();
        }

        // Új autómárka rögzítése
        [HttpPost]
        public async Task<ActionResult<CarBrand>> AddCarBrand(CarBrand p_newBrand)
        {
            if (p_newBrand == null || string.IsNullOrWhiteSpace(p_newBrand.Name))
            {
                return BadRequest("Az autómárka neve nem lehet üres!");
            }

            m_context.CarBrands.Add(p_newBrand);
            await m_context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCarBrands), new { id = p_newBrand.Id }, p_newBrand);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCarBrand(int p_id, [FromBody] CarBrand p_updatedBrand)
        {
            if (p_updatedBrand == null || string.IsNullOrWhiteSpace(p_updatedBrand.Name))
            {
                return BadRequest("Az autómárka neve nem lehet üres!");
            }

            var existingBrand = await m_context.CarBrands.FindAsync(p_id);
            if (existingBrand == null)
            {
                return NotFound($"Nincs autómárka ezzel az ID-vel: {p_id}");
            }

            existingBrand.Name = p_updatedBrand.Name;
            await m_context.SaveChangesAsync();

            return Ok(existingBrand);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCarBrand(int p_id)
        {
            var brand = await m_context.CarBrands
                .Include(cb => cb.CarModels)
                .FirstOrDefaultAsync(cb => cb.Id == p_id);

            if (brand == null)
            {
                return NotFound($"Nincs autómárka ezzel az ID-vel: {p_id}");
            }

            if (brand.CarModels.Any())
            {
                return BadRequest("Nem törölhető, mert még léteznek hozzá tartozó autómodellek!");
            }

            m_context.CarBrands.Remove(brand);
            await m_context.SaveChangesAsync();

            return NoContent();
        }
    }
}
