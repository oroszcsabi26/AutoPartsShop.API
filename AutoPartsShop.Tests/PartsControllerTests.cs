using AutoPartsShop.Core.Models;
using AutoPartsShop.Infrastructure;
using AutoPartsShop.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AutoPartsShop.Tests
{
    public class PartsControllerTests : IClassFixture<DockerWebApplicationFactory>
    {
        private readonly DockerWebApplicationFactory m_factory;

        public PartsControllerTests(DockerWebApplicationFactory factory)
        {
            m_factory = factory;
        }

        private async Task<AppDbContext> GetDbAsync()
        {
            var scope = m_factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();
            return db;
        }

        [Fact]
        public async Task GetParts_ShouldReturnSeededPart()
        {
            var client = m_factory.CreateClient();
            var db = await GetDbAsync();

            var brand = new CarBrand { Name = "DockerBrand" };
            db.CarBrands.Add(brand);
            await db.SaveChangesAsync();

            var model = new CarModel { Name = "DockerModel", CarBrandId = brand.Id };
            db.CarModels.Add(model);
            await db.SaveChangesAsync();

            var cat = new PartsCategory { Name = "DockerCat" };
            db.PartsCategories.Add(cat);
            await db.SaveChangesAsync();

            db.Parts.Add(new Part
            {
                Name = "DockerPart",
                Price = 9999,
                Manufacturer = "DockerTest",
                CarModelId = model.Id,
                PartsCategoryId = cat.Id,
                Quantity = 1
            });
            await db.SaveChangesAsync();

            var response = await client.GetAsync("/api/parts");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var list = await response.Content.ReadFromJsonAsync<List<object>>();
            Assert.NotNull(list);
            Assert.True(list!.Count >= 1);
        }

        [Fact]
        public async Task AddPart_ShouldCreatePart_AndUploadImage()
        {
            var client = m_factory.CreateClient();
            var db = await GetDbAsync();

            // Seed: Brand + Model + Category
            var brand = new CarBrand { Name = "VW" };
            db.CarBrands.Add(brand);
            await db.SaveChangesAsync();

            var model = new CarModel { Name = "Golf 8", CarBrandId = brand.Id };
            db.CarModels.Add(model);
            await db.SaveChangesAsync();

            var category = new PartsCategory { Name = "Fék" };
            db.PartsCategories.Add(category);
            await db.SaveChangesAsync();

            // Fake image fájl
            var imageBytes = new byte[] { 1, 2, 3, 4 };
            var content = new MultipartFormDataContent
            {
                { new StringContent("Bosch"), "Manufacturer" },
                { new StringContent("Féktárcsa"), "Name" },
                { new StringContent("15000"), "Price" },
                { new StringContent(model.Id.ToString()), "CarModelId" },
                { new StringContent(category.Id.ToString()), "PartsCategoryId" },
                { new StreamContent(new MemoryStream(imageBytes)), "p_imageFile", "brake.jpg" }
            };

            var response = await client.PostAsync("/api/parts", content);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<Part>();
            Assert.NotNull(created);
            Assert.Contains("fake.blob.core.windows.net", created!.ImageUrl);

            var partInDb = await db.Parts.FindAsync(created.Id);
            Assert.NotNull(partInDb);
            Assert.Equal("Bosch", partInDb!.Manufacturer);
        }

        [Fact]
        public async Task GetParts_ShouldReturnAllParts()
        {
            var client = m_factory.CreateClient();
            var db = await GetDbAsync();
            
            var brand = new CarBrand { Name = "BMW" };
            db.CarBrands.Add(brand);
            await db.SaveChangesAsync();

            var model = new CarModel { Name = "E90", CarBrandId = brand.Id };
            db.CarModels.Add(model);
            await db.SaveChangesAsync();

            var cat = new PartsCategory { Name = "Motor" };
            db.PartsCategories.Add(cat);
            await db.SaveChangesAsync();

            db.Parts.Add(new Part
            {
                Name = "Olajszűrő",
                Price = 4500,
                Manufacturer = "Mann",
                CarModelId = model.Id,
                PartsCategoryId = cat.Id
            });
            await db.SaveChangesAsync();

            var resp = await client.GetAsync("/api/parts");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var list = await resp.Content.ReadFromJsonAsync<List<dynamic>>();
            Assert.NotNull(list);
            Assert.True(list!.Count >= 1);
        }

        [Fact]
        public async Task DeletePart_ShouldRemovePart_AndKeepEngineVariants()
        {
            var client = m_factory.CreateClient();
            var db = await GetDbAsync();

            var brand = new CarBrand { Name = "Opel" };
            db.CarBrands.Add(brand);
            await db.SaveChangesAsync();

            var model = new CarModel { Name = "Astra", CarBrandId = brand.Id };
            db.CarModels.Add(model);
            await db.SaveChangesAsync();

            var cat = new PartsCategory { Name = "Futómű" };
            db.PartsCategories.Add(cat);
            await db.SaveChangesAsync();

            var ev = new EngineVariant
            {
                CarModelId = model.Id,
                FuelType = "Benzin",
                EngineSize = 1400,
                YearFrom = 2015,
                YearTo = 2019
            };
            db.EngineVariants.Add(ev);
            await db.SaveChangesAsync();

            var part = new Part
            {
                Name = "Lengéscsillapító",
                Price = 18000,
                Manufacturer = "Monroe",
                CarModelId = model.Id,
                PartsCategoryId = cat.Id
            };
            db.Parts.Add(part);
            await db.SaveChangesAsync();

            db.PartEngineVariants.Add(new PartEngineVariant
            {
                PartId = part.Id,
                EngineVariantId = ev.Id
            });
            await db.SaveChangesAsync();

            var resp = await client.DeleteAsync($"/api/parts/{part.Id}");
            Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

            var partExists = await db.Parts
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == part.Id);
            Assert.Null(partExists);

            var evExists = await db.EngineVariants
                .AsNoTracking()
                .FirstOrDefaultAsync(ev => ev.Id == ev.Id);
            Assert.NotNull(evExists);
        }
    }
}
