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
        private readonly AppDbContext m_context;

        public CarModelController(AppDbContext p_context)
        {
            m_context = p_context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CarModel>>> GetCarModels()
        {
            return await m_context.CarModels.ToListAsync();
        }

        [HttpGet("brand/{brandId}")]
        public async Task<ActionResult<IEnumerable<CarModel>>> GetModelsByBrand(int brandId)
        {
            var models = await m_context.CarModels
                            .Where(cm => cm.CarBrandId == brandId)
                            .ToListAsync();

            if (models == null || models.Count == 0)
            {
                return NotFound($"Nincs autómodell ezzel a márka ID-vel: {brandId}");
            }

            return models;
        }

        [HttpPost("{brandId}")]
        public async Task<ActionResult<CarModel>> AddCarModel(int brandId, [FromBody] CarModel p_model)
        {
            var brandExists = await m_context.CarBrands.AnyAsync(cb => cb.Id == brandId);
            if (!brandExists)
            {
                return NotFound($"Nincs autómárka ezzel az ID-val: {brandId}");
            }

            p_model.CarBrandId = brandId; 

            m_context.CarModels.Add(p_model);
            await m_context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCarModels), new { id = p_model.Id }, p_model);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCarModel(int id, [FromBody] CarModel p_updatedModel)
        {
            if (p_updatedModel == null || string.IsNullOrWhiteSpace(p_updatedModel.Name))
            {
                return BadRequest("A modell neve nem lehet üres!");
            }

            var existingModel = await m_context.CarModels.FindAsync(id);
            if (existingModel == null)
            {
                return NotFound($"Nincs autómodell ezzel az ID-vel: {id}");
            }

            existingModel.Name = p_updatedModel.Name;
            existingModel.Year = p_updatedModel.Year;
            await m_context.SaveChangesAsync();

            return Ok(existingModel);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCarModel(int id)
        {
            var model = await m_context.CarModels
                .Include(cm => cm.Parts) 
                .FirstOrDefaultAsync(cm => cm.Id == id);

            if (model == null)
            {
                return NotFound($"Nincs autómodell ezzel az ID-vel: {id}");
            }

            if (model.Parts.Any())
            {
                return BadRequest("Nem törölhető, mert még léteznek hozzá tartozó alkatrészek!");
            }

            var hasVariants = await m_context.EngineVariants.AnyAsync(ev => ev.CarModelId == id);
            if (hasVariants)
            {
                return BadRequest("Nem törölhető, mert tartoznak hozzá motorváltozatok (EngineVariants).");
            }

            m_context.CarModels.Remove(model);
            await m_context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("brandId/{brandId}/modelName/{modelName}/year/{year}/engine-options")]
        public async Task<ActionResult<IEnumerable<string>>> GetEngineOptions(int brandId, string modelName, int year)
        {
            var engines = await m_context.EngineVariants
                .Where(ev =>
                    ev.CarModel.CarBrandId == brandId &&
                    ev.CarModel.Name.ToLower() == modelName.ToLower() &&
                    ev.YearFrom <= year && year <= ev.YearTo)
                .Select(ev => ev.FuelType + "/" + ev.EngineSize)
                .Distinct()
                .ToListAsync();

            if (!engines.Any())
                return NotFound("Nem találhatók motorváltozatok a megadott paraméterekre.");

            return Ok(engines);
        }

        [HttpGet("compatible-years/model/{modelId}")]
        public async Task<ActionResult<IEnumerable<int>>> GetCompatibleYearsByModel(int modelId)
        {
            var spans = await m_context.EngineVariants
                .Where(ev => ev.CarModelId == modelId)
                .Select(ev => new { ev.YearFrom, ev.YearTo })
                .ToListAsync();

            if (!spans.Any())
                return NotFound("Nem találhatók évjáratok a megadott modellhez.");

            var years = spans
                .SelectMany(s => Enumerable.Range(s.YearFrom, s.YearTo - s.YearFrom + 1))
                .Distinct()
                .OrderBy(y => y)
                .ToList();

            return Ok(years);
        }
    }
}

