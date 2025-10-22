using AutoPartsShop.Core.Models;
using AutoPartsShop.Infrastructure;
using AutoPartsShop.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AutoPartsShop.Tests
{
    public class EngineVariantsControllerTests : IClassFixture<CustomWebApplicationFactory> // xUnit osztály szintű fixture használata miatti inicializáláshoz (itt: CustomWebApplicationFactory) 
    {
        private readonly CustomWebApplicationFactory m_factory;

        public EngineVariantsControllerTests(CustomWebApplicationFactory p_factory)
        {
            m_factory = p_factory;
        }

        private async Task<AppDbContext> GetDbAsync()
        {
            var scope = m_factory.Services.CreateScope(); // scope létrehozása a scoped szolgáltatások eléréséhez
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); // AppDbContext lekérése a DI konténerből

            // Minden teszt előtt takarítás, hogy ne folyjanak össze az adatok
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();

            return db;
        }


        [Fact]
        public async Task GetByCarModel_ShouldReturnVariants_ForExistingModel()
        {
            var client = m_factory.CreateClient(); // HttpClient létrehozása a tesztelt alkalmazáshoz   
            var db = await GetDbAsync(); // friss, üres adatbázis

            // Seed: létrehozunk egy CarBrand + CarModel + 2 EngineVariant
            var brand = new CarBrand { Name = "BrandX" };
            db.CarBrands.Add(brand);
            await db.SaveChangesAsync();

            var model = new CarModel { Name = "ModelA", CarBrandId = brand.Id };
            db.CarModels.Add(model);
            await db.SaveChangesAsync();

            db.EngineVariants.AddRange(
                new EngineVariant { CarModelId = model.Id, FuelType = "Benzin", EngineSize = 1600, YearFrom = 2015, YearTo = 2018 },
                new EngineVariant { CarModelId = model.Id, FuelType = "Dízel", EngineSize = 2000, YearFrom = 2016, YearTo = 2019 }
            );
            await db.SaveChangesAsync();

            var resp = await client.GetAsync($"/api/enginevariants/carModel/{model.Id}");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var list = await resp.Content.ReadFromJsonAsync<List<EngineVariant>>();
            Assert.NotNull(list);
            Assert.Equal(2, list!.Count);
        }

        [Fact]
        public async Task Create_ShouldReturnCreated_AndPersist()
        {
            var client = m_factory.CreateClient();
            var db = await GetDbAsync();

            // Kell egy létező CarModel
            var brand = new CarBrand { Name = "B1" };
            db.CarBrands.Add(brand);
            await db.SaveChangesAsync();

            var model = new CarModel { Name = "M1", CarBrandId = brand.Id };
            db.CarModels.Add(model);
            await db.SaveChangesAsync();

            var payload = new EngineVariant
            {
                CarModelId = model.Id,
                FuelType = "Benzin",
                EngineSize = 1400,
                YearFrom = 2012,
                YearTo = 2016
            };

            var resp = await client.PostAsJsonAsync("/api/enginevariants", payload);
            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

            // Ellenőrizzük a visszaadott EV-t
            var created = await resp.Content.ReadFromJsonAsync<EngineVariant>();
            Assert.NotNull(created);
            Assert.True(created!.Id > 0);
            Assert.Equal(model.Id, created.CarModelId);

            // És hogy tényleg bekerült a DB-be
            var fromDb = await db.EngineVariants.FindAsync(created.Id);
            Assert.NotNull(fromDb);
            Assert.Equal("Benzin", fromDb!.FuelType);
        }

        [Fact]
        public async Task Create_ShouldReturnBadRequest_WhenCarModelNotExists()
        {
            var client = m_factory.CreateClient();

            var payload = new EngineVariant
            {
                CarModelId = 9999, // nem létezik
                FuelType = "Benzin",
                EngineSize = 1200,
                YearFrom = 2010,
                YearTo = 2014
            };

            var resp = await client.PostAsJsonAsync("/api/enginevariants", payload);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);

            var message = await resp.Content.ReadAsStringAsync();
            Assert.Contains("nem létezik", message.ToLower());
        }

        [Fact]
        public async Task GetById_ShouldReturnNotFound_WhenMissing()
        {
            var client = m_factory.CreateClient();
            var resp = await client.GetAsync("/api/enginevariants/123456");
            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        [Fact]
        public async Task Update_ShouldReturnBadRequest_WhenIdMismatch()
        {
            var client = m_factory.CreateClient();
            var db = await GetDbAsync();

            var brand = new CarBrand { Name = "B2" };
            db.CarBrands.Add(brand);
            await db.SaveChangesAsync();

            var model = new CarModel { Name = "M2", CarBrandId = brand.Id };
            db.CarModels.Add(model);
            await db.SaveChangesAsync();

            var ev = new EngineVariant
            {
                CarModelId = model.Id,
                FuelType = "Benzin",
                EngineSize = 1800,
                YearFrom = 2017,
                YearTo = 2020
            };
            db.EngineVariants.Add(ev);
            await db.SaveChangesAsync();

            // Téves ID az URL-ben
            var payload = new EngineVariant
            {
                Id = ev.Id,
                CarModelId = model.Id,
                FuelType = "Benzin",
                EngineSize = 2000,
                YearFrom = 2017,
                YearTo = 2020
            };

            var resp = await client.PutAsJsonAsync($"/api/enginevariants/{ev.Id + 1}", payload);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task Update_ShouldReturnBadRequest_WhenCarModelInvalid()
        {
            var client = m_factory.CreateClient();
            var db = await GetDbAsync();

            var brand = new CarBrand { Name = "B3" };
            db.CarBrands.Add(brand);
            await db.SaveChangesAsync();

            var model = new CarModel { Name = "M3", CarBrandId = brand.Id };
            db.CarModels.Add(model);
            await db.SaveChangesAsync();

            var ev = new EngineVariant
            {
                CarModelId = model.Id,
                FuelType = "Dízel",
                EngineSize = 1900,
                YearFrom = 2015,
                YearTo = 2018
            };
            db.EngineVariants.Add(ev);
            await db.SaveChangesAsync();

            // Nem létező CarModelId-re állítjuk
            var payload = new EngineVariant
            {
                Id = ev.Id,
                CarModelId = 99999,
                FuelType = "Dízel",
                EngineSize = 1900,
                YearFrom = 2015,
                YearTo = 2018
            };

            var resp = await client.PutAsJsonAsync($"/api/enginevariants/{ev.Id}", payload);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task Update_ShouldReturnNoContent_AndPersistChanges()
        {
            var client = m_factory.CreateClient();
            var db = await GetDbAsync();

            var brand = new CarBrand { Name = "B4" };
            db.CarBrands.Add(brand);
            await db.SaveChangesAsync();

            var model = new CarModel { Name = "M4", CarBrandId = brand.Id };
            db.CarModels.Add(model);
            await db.SaveChangesAsync();

            var ev = new EngineVariant
            {
                CarModelId = model.Id,
                FuelType = "Benzin",
                EngineSize = 1600,
                YearFrom = 2010,
                YearTo = 2012
            };
            db.EngineVariants.Add(ev);
            await db.SaveChangesAsync();

            var payload = new EngineVariant
            {
                Id = ev.Id,
                CarModelId = model.Id,
                FuelType = "Benzin",
                EngineSize = 1800,  // módosítjuk
                YearFrom = 2011,    // módosítjuk
                YearTo = 2013       // módosítjuk
            };

            var resp = await client.PutAsJsonAsync($"/api/enginevariants/{ev.Id}", payload);
            Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

            db.Entry(ev).State = EntityState.Detached; //ha ezt nem tesszük meg a kiolvasás előtt, az EF a memóriában lévő példányt adja vissza, nem a DB-ből újat
            var refreshed = await db.EngineVariants.FindAsync(ev.Id);
            Assert.NotNull(refreshed);
            Assert.Equal(2011, refreshed.YearFrom);
            Assert.Equal(2013, refreshed.YearTo);
            Assert.Equal(1800, refreshed!.EngineSize);
        }

        [Fact]
        public async Task Delete_ShouldRemoveVariant_AndItsPartLinks()
        {
            var client = m_factory.CreateClient();
            var db = await GetDbAsync();

            var brand = new CarBrand { Name = "B5" };
            db.CarBrands.Add(brand);
            await db.SaveChangesAsync();

            var model = new CarModel { Name = "M5", CarBrandId = brand.Id };
            db.CarModels.Add(model);
            await db.SaveChangesAsync();

            var ev = new EngineVariant
            {
                CarModelId = model.Id,
                FuelType = "Benzin",
                EngineSize = 2000,
                YearFrom = 2018,
                YearTo = 2022
            };
            db.EngineVariants.Add(ev);
            await db.SaveChangesAsync();

            // Létrehozunk hozzákötött PartEngineVariant rekordot is.
            // (InMemory provider nem kényszeríti a relációkat, így a Part léte sem kötelező a teszt sikeréhez.)
            db.PartEngineVariants.Add(new PartEngineVariant
            {
                PartId = 123,             // tetszőleges szám
                EngineVariantId = ev.Id
            });
            await db.SaveChangesAsync();

            var resp = await client.DeleteAsync($"/api/enginevariants/{ev.Id}");
            Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

            db.Entry(ev).State = EntityState.Detached;
            var missing = await db.EngineVariants.FindAsync(ev.Id);
            Assert.Null(missing);
            var linkCount = await db.PartEngineVariants.CountAsync(pev => pev.EngineVariantId == ev.Id);
            Assert.Equal(0, linkCount);

        }
    }
}
