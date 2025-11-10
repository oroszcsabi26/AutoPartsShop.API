using AutoPartsShop.Infrastructure;
using AutoPartsShop.Tests.Integration.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace AutoPartsShop.Tests.Integration.Controllers
{
    public class CarBrandControllerTests : IClassFixture<DockerWebApplicationFactory>
    {
        DockerWebApplicationFactory m_factory;

        public CarBrandControllerTests(DockerWebApplicationFactory p_factory)
        {
            m_factory = p_factory;
        }

        // Ez a metódus lekér egy AppDbContext példányt a DI konténerből //a DockerWebApplicationFactory által előre felépített adatbázishoz kapcsolódik. minden teszthez külön DbContext példányt ad (új objektum a memóriában), de ugyanazt az SQL adatbázist használja a háttérben.
        public async Task<AppDbContext> GetDbAsync()
        {
            var scope = m_factory.Services.CreateScope(); // a scope létrehozása ami a scoped szolgáltatások eléréséhez szükséges
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); // egy AppDbContext példány lekérése a DI konténerből
            await db.Database.EnsureCreatedAsync(); // Biztosítja, hogy az adatbázis létre legyen hozva
            return db; // visszatér az AppDbContext példánnyal
        }

        [Fact]
        public async Task GetCarBrands_ShouldReturnSeededBrand()
        {
            var client = m_factory.CreateClient();
            var db = await GetDbAsync();
            var brand = new Core.Models.CarBrand
            {
                Name = "DockerBrand"
            };
            db.CarBrands.Add(brand);

            var brand1 = new Core.Models.CarBrand
            {
                Name = "DockerBrand2"
            };
            db.CarBrands.Add(brand1);
            await db.SaveChangesAsync();

            var response = await client.GetAsync("/api/cars");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var list = await response.Content.ReadFromJsonAsync<List<object>>();
            Assert.NotNull(list);
            Assert.True(list!.Count >= 1);
        }

        [Fact]
        public async Task AddCarBrand_ShouldAddNewBrand()
        {
            var client = m_factory.CreateClient();
            var newBrand = new Core.Models.CarBrand
            {
                Name = "NewDockerBrand"
            };
            var response = await client.PostAsJsonAsync("/api/cars", newBrand);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var createdBrand = await response.Content.ReadFromJsonAsync<Core.Models.CarBrand>();
            Assert.NotNull(createdBrand);
            Assert.Equal("NewDockerBrand", createdBrand!.Name);
        }

        [Fact]
        public async Task UpdateCarBrand_ShouldUpdateExistingBrand()
        {
            var client = m_factory.CreateClient();
            var db = await GetDbAsync();
            var brand = new Core.Models.CarBrand
            {
                Name = "BrandToUpdate"
            };
            db.CarBrands.Add(brand);
            await db.SaveChangesAsync();
            var updatedBrand = new Core.Models.CarBrand
            {
                Name = "UpdatedBrandName"
            };
            var response = await client.PutAsJsonAsync($"/api/cars/{brand.Id}", updatedBrand);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var returnedBrand = await response.Content.ReadFromJsonAsync<Core.Models.CarBrand>();
            Assert.NotNull(returnedBrand);
            Assert.Equal("UpdatedBrandName", returnedBrand!.Name);
        }

        [Fact]
        public async Task UpdateCarBrand_ShouldReturnNotFound_ForNonExistingBrand()
        {
            var client = m_factory.CreateClient();
            var updatedBrand = new Core.Models.CarBrand
            {
                Name = "NonExistentBrand"
            };
            var response = await client.PutAsJsonAsync($"/api/cars/9999", updatedBrand); 
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteCarBrand_ShouldDeleteExistingBrand()
        {
            var client = m_factory.CreateClient();
            var db = await GetDbAsync();
            var brand = new Core.Models.CarBrand
            {
                Name = "BrandToDelete"
            };
            db.CarBrands.Add(brand);
            await db.SaveChangesAsync();
            var response = await client.DeleteAsync($"/api/cars/{brand.Id}");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var deletedBrand = await db.CarBrands.AsNoTracking().FirstOrDefaultAsync(b => b.Id == brand.Id);
            Assert.Null(deletedBrand);
        }
    }
}
