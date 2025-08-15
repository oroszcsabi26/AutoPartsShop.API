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

        public CarModelController(AppDbContext context)
        {
            m_context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CarModel>>> GetCarModels()
        {
            return await m_context.CarModels.ToListAsync();
        }

        [HttpGet("brand/{brandId}")]
        public async Task<ActionResult<IEnumerable<CarModel>>> GetModelsByBrand(int p_brandId)
        {
            var models = await m_context.CarModels
                                       .Where(cm => cm.CarBrandId == p_brandId)
                                       .ToListAsync();

            if (models == null || models.Count == 0)
            {
                return NotFound($"Nincs autómodell ezzel a márka ID-vel: {p_brandId}");
            }

            return models;
        }

        [HttpPost("{brandId}")]
        public async Task<ActionResult<CarModel>> AddCarModel(int p_brandId, [FromBody] CarModel p_model)
        {
            var brandExists = await m_context.CarBrands.AnyAsync(cb => cb.Id == p_brandId);
            if (!brandExists)
            {
                return NotFound($"Nincs autómárka ezzel az ID-val: {p_brandId}");
            }

            p_model.CarBrandId = p_brandId; 

            m_context.CarModels.Add(p_model);
            await m_context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCarModels), new { id = p_model.Id }, p_model);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCarModel(int p_id, [FromBody] CarModel p_updatedModel)
        {
            if (p_updatedModel == null || string.IsNullOrWhiteSpace(p_updatedModel.Name))
            {
                return BadRequest("A modell neve nem lehet üres!");
            }

            var existingModel = await m_context.CarModels.FindAsync(p_id);
            if (existingModel == null)
            {
                return NotFound($"Nincs autómodell ezzel az ID-vel: {p_id}");
            }

            existingModel.Name = p_updatedModel.Name;
            existingModel.Year = p_updatedModel.Year;
            await m_context.SaveChangesAsync();

            return Ok(existingModel);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCarModel(int p_id)
        {
            var model = await m_context.CarModels
                .Include(cm => cm.Parts) 
                .FirstOrDefaultAsync(cm => cm.Id == p_id);

            if (model == null)
            {
                return NotFound($"Nincs autómodell ezzel az ID-vel: {p_id}");
            }

            if (model.Parts.Any())
            {
                return BadRequest("Nem törölhető, mert még léteznek hozzá tartozó alkatrészek!");
            }

            var hasVariants = await m_context.EngineVariants.AnyAsync(ev => ev.CarModelId == p_id);
            if (hasVariants)
            {
                return BadRequest("Nem törölhető, mert tartoznak hozzá motorváltozatok (EngineVariants).");
            }

            m_context.CarModels.Remove(model);
            await m_context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("brandId/{brandId}/modelName/{modelName}/year/{year}/engine-options")]
        public async Task<ActionResult<IEnumerable<string>>> GetEngineOptions(int p_brandId, string p_modelName, int p_year)
        {
            var engines = await m_context.EngineVariants
                .Where(ev =>
                    ev.CarModel.CarBrandId == p_brandId &&
                    ev.CarModel.Name.ToLower() == p_modelName.ToLower() &&
                    ev.YearFrom <= p_year && p_year <= ev.YearTo)
                .Select(ev => ev.FuelType + "/" + ev.EngineSize)
                .Distinct()
                .ToListAsync();

            if (!engines.Any())
                return NotFound("Nem találhatók motorváltozatok a megadott paraméterekre.");

            return Ok(engines);
        }

        [HttpGet("compatible-years/model/{modelId}")]
        public async Task<ActionResult<IEnumerable<int>>> GetCompatibleYearsByModel(int p_modelId)
        {
            var spans = await m_context.EngineVariants
                .Where(ev => ev.CarModelId == p_modelId)
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

