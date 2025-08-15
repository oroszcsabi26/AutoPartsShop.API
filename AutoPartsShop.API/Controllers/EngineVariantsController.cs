using AutoPartsShop.Core.Models;
using AutoPartsShop.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsShop.API.Controllers
{
    [Route("api/enginevariants")]
    [ApiController]
    public class EngineVariantsController : ControllerBase
    {
        private readonly AppDbContext m_context;

        public EngineVariantsController(AppDbContext context)
        {
            m_context = context;
        }

        [HttpGet("carModel/{carModelId}")]
        public async Task<ActionResult<IEnumerable<EngineVariant>>> GetByCarModel(int p_carModelId)
        {
            var variants = await m_context.EngineVariants
                .Where(ev => ev.CarModelId == p_carModelId)
                .ToListAsync();

            return Ok(variants);
        }

        [HttpPost]
        public async Task<ActionResult<EngineVariant>> Create([FromBody] EngineVariant p_variant)
        {
            var modelExists = await m_context.CarModels.AnyAsync(cm => cm.Id == p_variant.CarModelId);
            if (!modelExists)
                return BadRequest("A megadott autómodell nem létezik.");

            m_context.EngineVariants.Add(p_variant);
            await m_context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetByCarModel), new { carModelId = p_variant.CarModelId }, p_variant);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<EngineVariant>> GetById(int p_id)
        {
            var ev = await m_context.EngineVariants.FindAsync(p_id);
            return ev is null ? NotFound() : Ok(ev);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int p_id, [FromBody] EngineVariant p_variant)
        {
            if (p_id != p_variant.Id)
                return BadRequest("Az URL-ben lévő ID nem egyezik a törzzsel.");

            var modelExists = await m_context.CarModels.AnyAsync(cm => cm.Id == p_variant.CarModelId);
            if (!modelExists)
                return BadRequest("A megadott autómodell nem létezik.");

            var validationError = ValidateVariant(p_variant);
            if (validationError is not null)
                return BadRequest(validationError);

            var existing = await m_context.EngineVariants.FindAsync(p_id);
            if (existing is null) return NotFound();

            existing.CarModelId = p_variant.CarModelId;
            existing.FuelType = p_variant.FuelType?.Trim() ?? "";
            existing.EngineSize = p_variant.EngineSize;
            existing.YearFrom = p_variant.YearFrom;
            existing.YearTo = p_variant.YearTo;

            await m_context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int p_id)
        {
            var ev = await m_context.EngineVariants
                .Include(x => x.PartEngineVariants)
                .FirstOrDefaultAsync(x => x.Id == p_id);

            if (ev is null) return NotFound();

            m_context.PartEngineVariants.RemoveRange(ev.PartEngineVariants);
            m_context.EngineVariants.Remove(ev);

            await m_context.SaveChangesAsync();
            return NoContent();
        }

        private static string? ValidateVariant(EngineVariant p_v)
        {
            if (string.IsNullOrWhiteSpace(p_v.FuelType))
                return "A FuelType kötelező.";
            if (p_v.EngineSize <= 0)
                return "Az EngineSize legyen pozitív.";
            if (p_v.YearFrom <= 0 || p_v.YearTo <= 0)
                return "Az évszámok legyenek pozitívak.";
            if (p_v.YearFrom > p_v.YearTo)
                return "YearFrom nem lehet nagyobb, mint YearTo.";
            return null;
        }
    }
}
