using AutoPartsShop.Core.Models;
using AutoPartsShop.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsShop.API.Controllers
{
    [Route("api/cars/models")]
    [ApiController]
    public class CarModelController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CarModelController(AppDbContext context)
        {
            _context = context;
        }

        // Összes autómodell lekérése
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CarModel>>> GetCarModels()
        {
            return await _context.CarModels.ToListAsync();
        }

        // Autómárkához tartozó modellek lekérése ID alapján
        [HttpGet("brand/{brandId}")]
        public async Task<ActionResult<IEnumerable<CarModel>>> GetModelsByBrand(int brandId)
        {
            var models = await _context.CarModels
                                       .Where(cm => cm.CarBrandId == brandId)
                                       .ToListAsync();

            if (models == null || models.Count == 0)
            {
                return NotFound($"Nincs autómodell ezzel a márka ID-vel: {brandId}");
            }

            return models;
        }

        [HttpPost("{brandId}")]
        public async Task<ActionResult<CarModel>> AddCarModel(int brandId, [FromBody] CarModel model)
        {
            var brandExists = await _context.CarBrands.AnyAsync(cb => cb.Id == brandId);
            if (!brandExists)
            {
                return NotFound($"Nincs autómárka ezzel az ID-val: {brandId}");
            }

            model.CarBrandId = brandId; // Csak az ID-t állítjuk be, a CarBrand objektumot NEM
            //model.CarBrand = null;

            _context.CarModels.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCarModels), new { id = model.Id }, model);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCarModel(int id, [FromBody] CarModel updatedModel)
        {
            if (updatedModel == null || string.IsNullOrWhiteSpace(updatedModel.Name))
            {
                return BadRequest("A modell neve nem lehet üres!");
            }

            var existingModel = await _context.CarModels.FindAsync(id);
            if (existingModel == null)
            {
                return NotFound($"Nincs autómodell ezzel az ID-vel: {id}");
            }

            existingModel.Name = updatedModel.Name;
            existingModel.Year = updatedModel.Year;
            await _context.SaveChangesAsync();

            return Ok(existingModel);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCarModel(int id)
        {
            var model = await _context.CarModels
                .Include(cm => cm.Parts) // Betöltjük a kapcsolódó alkatrészeket
                .FirstOrDefaultAsync(cm => cm.Id == id);

            if (model == null)
            {
                return NotFound($"Nincs autómodell ezzel az ID-vel: {id}");
            }

            if (model.Parts.Any())
            {
                return BadRequest("Nem törölhető, mert még léteznek hozzá tartozó alkatrészek!");
            }

            _context.CarModels.Remove(model);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Motorváltozatok lekérdezése adott autómárka ID, modellnév és évjárat alapján
        [HttpGet("brandId/{brandId}/modelName/{modelName}/year/{year}/engine-options")]
        public async Task<ActionResult<IEnumerable<string>>> GetEngineOptions(int brandId, string modelName, int year)
        {
            var engines = await _context.CarModels
                .Where(cm =>
                    cm.CarBrandId == brandId &&
                    cm.Name.ToLower() == modelName.ToLower() &&
                    cm.Year == year)
                .Select(cm => cm.FuelType + "/" + cm.EngineSize)
                .Distinct()
                .ToListAsync();

            if (!engines.Any())
            {
                return NotFound("Nem találhatók motorváltozatok a megadott paraméterekre.");
            }

            return Ok(engines);
        }

        [HttpGet("compatible-years/model/{modelId}")]
        public async Task<ActionResult<IEnumerable<int>>> GetCompatibleYearsByModel(int modelId)
        {
            var years = await _context.CarModels
                .Where(cm => cm.Id == modelId)
                .Select(cm => cm.Year)
                .Distinct()
                .ToListAsync();

            if (!years.Any())
            {
                return NotFound("Nem találhatók évjáratok a megadott modellhez.");
            }

            return Ok(years);
        }
    }
}

