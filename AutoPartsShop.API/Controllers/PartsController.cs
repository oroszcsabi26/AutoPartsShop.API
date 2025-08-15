using AutoPartShop.Core.Helpers;
using AutoPartsShop.Core.DTOs;
using AutoPartsShop.Core.Models;
using AutoPartsShop.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsShop.API.Controllers
{
    [Route("api/parts")]
    [ApiController]
    public class PartsController : ControllerBase
    {
        private readonly AppDbContext m_context;
        private readonly AzureBlobStorageService m_blobStorageService;

        public PartsController(AppDbContext context, AzureBlobStorageService blobStorageService)
        {
            m_context = context;
            m_blobStorageService = blobStorageService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetParts()
        {
            var parts = await m_context.Parts
                .Include(p => p.PartsCategory)
                .Include(p => p.CarModel)
                .Include(p => p.PartEngineVariants)
                .ToListAsync();

            var result = parts.Select(p => new
            {
                p.Id,
                p.Name,
                p.Price,
                p.Manufacturer,
                p.Side,
                p.Shape,
                p.Size,
                p.Type,
                p.Material,
                p.Description,
                p.Quantity,
                p.ImageUrl,
                p.CarModelId,
                p.PartsCategoryId,
                EngineVariantIds = p.PartEngineVariants.Select(pev => pev.EngineVariantId).ToList()
            });

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetPartById(int p_id)
        {
            var part = await m_context.Parts
                .Include(p => p.PartEngineVariants)
                .FirstOrDefaultAsync(p => p.Id == p_id);

            if (part == null)
                return NotFound();

            var engineVariantIds = part.PartEngineVariants
                .Select(pev => pev.EngineVariantId)
                .ToList();

            return Ok(new
            {
                part.Id,
                part.Name,
                part.Price,
                part.Manufacturer,
                part.Side,
                part.Shape,
                part.Size,
                part.Type,
                part.Material,
                part.Description,
                part.Quantity,
                part.ImageUrl,
                part.CarModelId,
                part.PartsCategoryId,
                EngineVariantIds = engineVariantIds
            });
        }

        [HttpGet("carModel/{carModelId}/year/{year}")]
        public async Task<ActionResult<IEnumerable<Part>>> GetPartsByCarModelAndYear(int p_carModelId, int p_year, [FromQuery] string? p_fuelType, [FromQuery] int? p_engineSize)
        {
            var matchingVariants = await m_context.EngineVariants
                .Where(ev => ev.CarModelId == p_carModelId &&
                             ev.YearFrom <= p_year && p_year <= ev.YearTo &&
                             (p_fuelType == null || ev.FuelType.ToLower() == p_fuelType.ToLower()) &&
                             (!p_engineSize.HasValue || ev.EngineSize == p_engineSize.Value))
                .Select(ev => ev.Id)
                .ToListAsync();

            if (!matchingVariants.Any())
                return NotFound("Nem található motorváltozat a megadott feltételekkel.");

            var parts = await m_context.PartEngineVariants
                .Where(pev => matchingVariants.Contains(pev.EngineVariantId))
                .Include(pev => pev.Part)
                    .ThenInclude(p => p.CarModel)
                        .ThenInclude(cm => cm.CarBrand)
                .Include(pev => pev.Part.PartsCategory)
                .Select(pev => pev.Part)
                .Distinct()
                .ToListAsync();

            return Ok(parts);
        }

        [HttpPost]
        public async Task<ActionResult<Part>> AddPart([FromForm] Part p_newPart, [FromForm] List<int>? p_engineVariantIds, IFormFile? p_imageFile)
        {
            var carModelExists = await m_context.CarModels.AnyAsync(cm => cm.Id == p_newPart.CarModelId);
            var categoryExists = await m_context.PartsCategories.AnyAsync(pc => pc.Id == p_newPart.PartsCategoryId);

            if (!carModelExists)
            {
                return BadRequest($"Nincs ilyen autómodell ID: {p_newPart.CarModelId}");
            }

            if (!categoryExists)
            {
                return BadRequest($"Nincs ilyen alkatrészkategória ID: {p_newPart.PartsCategoryId}");
            }

            if (string.IsNullOrWhiteSpace(p_newPart.Manufacturer))
            {
                return BadRequest("A gyártó megadása kötelező!");
            }

            if (p_engineVariantIds != null && p_engineVariantIds.Any())
            {
                p_engineVariantIds = p_engineVariantIds.Distinct().ToList();

                var validEvIds = await m_context.EngineVariants
                    .Where(ev => p_engineVariantIds.Contains(ev.Id))
                    .Select(ev => new { ev.Id, ev.CarModelId })
                    .ToListAsync();

                if (validEvIds.Count != p_engineVariantIds.Count)
                    return BadRequest("Egy vagy több megadott EngineVariant nem létezik.");

                if (validEvIds.Any(x => x.CarModelId != p_newPart.CarModelId))
                    return BadRequest("Minden EngineVariantnak ugyanahhoz a CarModelhez kell tartoznia, mint a Part.CarModelId.");
            }

            p_newPart.Quantity = p_newPart.Quantity == 0 ? 1 : p_newPart.Quantity;
            p_newPart.Description ??= "";
            p_newPart.Type ??= "";
            p_newPart.Shape ??= "";
            p_newPart.Size ??= "";
            p_newPart.Side ??= "";
            p_newPart.Material ??= "";

            if (p_imageFile != null && p_imageFile.Length > 0)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(p_imageFile.FileName);
                string imageUrl = await m_blobStorageService.UploadFileAsync(p_imageFile.OpenReadStream(), fileName);
                p_newPart.ImageUrl = imageUrl;
            }

            m_context.Parts.Add(p_newPart);
            await m_context.SaveChangesAsync();

            if (p_engineVariantIds != null)
            {
                foreach (var evId in p_engineVariantIds)
                {
                    m_context.PartEngineVariants.Add(new PartEngineVariant
                    {
                        PartId = p_newPart.Id,
                        EngineVariantId = evId
                    });
                }
                await m_context.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(GetPartById), new { id = p_newPart.Id }, p_newPart);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePart(int p_id, [FromForm] Part p_updatedPart, [FromForm] List<int>? p_engineVariantIds, IFormFile? p_imageFile)
        {
            var part = await m_context.Parts
                .Include(p => p.PartEngineVariants)
                .FirstOrDefaultAsync(p => p.Id == p_id);

            if (part == null)
            {
                return NotFound($"Nincs alkatrész ezzel az ID-vel: {p_id}");
            }

            if (string.IsNullOrWhiteSpace(p_updatedPart.Manufacturer))
            {
                return BadRequest("A gyártó megadása kötelező!");
            }

            part.Name = p_updatedPart.Name;
            part.Price = p_updatedPart.Price;
            part.PartsCategoryId = p_updatedPart.PartsCategoryId;
            part.CarModelId = p_updatedPart.CarModelId;
            part.Manufacturer = p_updatedPart.Manufacturer;
            part.Quantity = p_updatedPart.Quantity;
            part.Side = p_updatedPart.Side;
            part.Shape = p_updatedPart.Shape;
            part.Size = p_updatedPart.Size;
            part.Type = p_updatedPart.Type;
            part.Material = p_updatedPart.Material;
            part.Description = p_updatedPart.Description;

            if (p_imageFile != null && p_imageFile.Length > 0)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(p_imageFile.FileName);
                string imageUrl = await m_blobStorageService.UploadFileAsync(p_imageFile.OpenReadStream(), fileName);
                part.ImageUrl = imageUrl;
            }

            if (p_engineVariantIds != null && p_engineVariantIds.Any())
            {
                p_engineVariantIds = p_engineVariantIds.Distinct().ToList();

                var validEvIds = await m_context.EngineVariants
                    .Where(ev => p_engineVariantIds.Contains(ev.Id))
                    .Select(ev => new { ev.Id, ev.CarModelId })
                    .ToListAsync();

                if (validEvIds.Count != p_engineVariantIds.Count)
                    return BadRequest("Egy vagy több megadott EngineVariant nem létezik.");

                if (validEvIds.Any(x => x.CarModelId != p_updatedPart.CarModelId))
                    return BadRequest("Minden EngineVariantnak ugyanahhoz a CarModelhez kell tartoznia, mint a Part.CarModelId.");

                m_context.PartEngineVariants.RemoveRange(part.PartEngineVariants);

                foreach (var evId in p_engineVariantIds)
                {
                    m_context.PartEngineVariants.Add(new PartEngineVariant
                    {
                        PartId = part.Id,
                        EngineVariantId = evId
                    });
                }
            }

            await m_context.SaveChangesAsync();
            return NoContent(); 
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePart(int p_id)
        {
            var part = await m_context.Parts
                .Include(p => p.PartEngineVariants)
                .FirstOrDefaultAsync(p => p.Id == p_id);
            if (part == null)
            {
                return NotFound($"Nincs ilyen alkatrész az adatbázisban: {p_id}");
            }

            m_context.PartEngineVariants.RemoveRange(part.PartEngineVariants);
            m_context.Parts.Remove(part);
            await m_context.SaveChangesAsync();
            return NoContent(); 
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<PartDisplay>>> SearchParts([FromQuery] string? p_name, [FromQuery] int? p_carModelId, [FromQuery] int? p_partsCategoryId, [FromQuery] int? p_engineVariantId)
        {
            IQueryable<Part> query = m_context.Parts
                .Include(p => p.CarModel)
                .ThenInclude(cm => cm.CarBrand)
                .Include(p => p.PartsCategory)
                .Include(p => p.PartEngineVariants);

            if (!string.IsNullOrWhiteSpace(p_name))
                query = query.Where(p => p.Name.Contains(p_name));

            if (p_carModelId.HasValue)
                query = query.Where(p => p.CarModelId == p_carModelId.Value);

            if (p_partsCategoryId.HasValue)
                query = query.Where(p => p.PartsCategoryId == p_partsCategoryId.Value);

            if (p_engineVariantId.HasValue)
                query = query.Where(p => p.PartEngineVariants.Any(pev => pev.EngineVariantId == p_engineVariantId.Value));

            var parts = await query.ToListAsync();

            var result = parts.Select(p => new PartDisplay
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Manufacturer = p.Manufacturer ?? "",
                Side = p.Side,
                Shape = p.Shape,
                Size = p.Size,
                Type = p.Type,
                Material = p.Material,
                Description = p.Description,
                Quantity = p.Quantity,
                CategoryName = p.PartsCategory?.Name ?? "",
                CarModelName = p.CarModel?.Name ?? "",
                CarBrandName = p.CarModel?.CarBrand?.Name ?? "",
                CarModelId = p.CarModelId,
                PartsCategoryId = p.PartsCategoryId,
                ImageUrl = p.ImageUrl
            }).ToList();

            return Ok(result);
        }
    }
}
