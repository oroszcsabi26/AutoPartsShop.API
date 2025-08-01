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
        private readonly AppDbContext _context;
        private readonly AzureBlobStorageService _blobStorageService;

        public PartsController(AppDbContext context, AzureBlobStorageService blobStorageService)
        {
            _context = context;
            _blobStorageService = blobStorageService;
        }

        // Összes alkatrész lekérése
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Part>>> GetParts()
        {
            return await _context.Parts
                .Include(p => p.PartsCategory)
                .Include(p => p.CarModel)
                .ToListAsync();
        }

        // Egy adott autómodell alkatrészeinek lekérése
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

        [HttpPost]
        public async Task<ActionResult<Part>> AddPart([FromForm] Part newPart, IFormFile? imageFile)
        {
            // Ellenőrzés: Autómodell és alkatrészkategória létezik-e
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

            if (string.IsNullOrWhiteSpace(newPart.Manufacturer))
            {
                return BadRequest("A gyártó megadása kötelező!");
            }

            // Alapértelmezett értékek beállítása (null vagy üres mezők esetén)
            newPart.Quantity = newPart.Quantity == 0 ? 1 : newPart.Quantity;
            newPart.Description ??= "";
            newPart.Type ??= "";
            newPart.Shape ??= "";
            newPart.Size ??= "";
            newPart.Side ??= "";
            newPart.Material ??= "";

            // Képfájl mentése, ha érkezett
            if (imageFile != null && imageFile.Length > 0)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                string imageUrl = await _blobStorageService.UploadFileAsync(imageFile.OpenReadStream(), fileName);
                newPart.ImageUrl = imageUrl;
            }

            // 4Alkatrész mentése adatbázisba
            _context.Parts.Add(newPart);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetParts), new { id = newPart.Id }, newPart);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePart(int id, [FromForm] Part updatedPart, IFormFile? imageFile)
        {
            var part = await _context.Parts.FindAsync(id);
            if (part == null)
            {
                return NotFound($"Nincs alkatrész ezzel az ID-vel: {id}");
            }

            if (string.IsNullOrWhiteSpace(updatedPart.Manufacturer))
            {
                return BadRequest("A gyártó megadása kötelező!");
            }

            // Frissítés
            part.Name = updatedPart.Name;
            part.Price = updatedPart.Price;
            part.PartsCategoryId = updatedPart.PartsCategoryId;
            part.CarModelId = updatedPart.CarModelId;
            part.Manufacturer = updatedPart.Manufacturer;
            part.Quantity = updatedPart.Quantity;
            part.Side = updatedPart.Side;
            part.Shape = updatedPart.Shape;
            part.Size = updatedPart.Size;
            part.Type = updatedPart.Type;
            part.Material = updatedPart.Material;
            part.Description = updatedPart.Description;

            if (imageFile != null && imageFile.Length > 0)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                string imageUrl = await _blobStorageService.UploadFileAsync(imageFile.OpenReadStream(), fileName);
                part.ImageUrl = imageUrl;
            }

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
        public async Task<ActionResult<IEnumerable<PartDisplay>>> SearchParts([FromQuery] string? name, [FromQuery] int? carModelId, [FromQuery] int? partsCategoryId)
        {
            IQueryable<Part> query = _context.Parts
                .Include(p => p.CarModel)
                    .ThenInclude(cm => cm.CarBrand)
                .Include(p => p.PartsCategory);

            if (!carModelId.HasValue && !partsCategoryId.HasValue)
            {
                return BadRequest("Legalább egy szűrési feltétel (autómodell vagy alkatrész kategória) szükséges!");
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(p => p.Name.Contains(name));
            }

            if (carModelId.HasValue)
            {
                query = query.Where(p => p.CarModelId == carModelId.Value);
            }

            if (partsCategoryId.HasValue)
            {
                query = query.Where(p => p.PartsCategoryId == partsCategoryId.Value);
            }

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
