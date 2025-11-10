using AutoPartsShop.Core.Models;
using AutoPartsShop.Infrastructure;
using AutoPartsShop.Tests.Integration.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace AutoPartsShop.Tests.Integration.Controllers
{
    public class CarModelControllerTests : IClassFixture<DockerWebApplicationFactory>
    {
        DockerWebApplicationFactory m_factory;

        public CarModelControllerTests(DockerWebApplicationFactory factory)
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
        public async Task GetCarmodels_ShouldReturnSeededModels()
        {
            var client = m_factory.CreateClient();
            var db = await GetDbAsync();
            var brand = new CarBrand { Name = "DockerBrand" };
            db.CarBrands.Add(brand);
            await db.SaveChangesAsync();
            db.CarModels.Add(new CarModel
            {
                Name = "DockerModel",
                CarBrandId = brand.Id
            });
            await db.SaveChangesAsync();
            var response = await client.GetAsync("/api/cars/models");
            response.EnsureSuccessStatusCode();
            var models = await response.Content.ReadFromJsonAsync<List<CarModel>>();
            Assert.NotNull(models);
            Assert.Contains(models, m => m.Name == "DockerModel" && m.CarBrandId == brand.Id);
        }

        [Fact]
        public async Task GetModelsByBrand_SchouldReturnModelsOfGivenBrand()
        {
            var client = m_factory.CreateClient();
            var db = await GetDbAsync();
            var brand = new CarBrand { Name = "BrandForModels" };
            db.CarBrands.Add(brand);
            await db.SaveChangesAsync();
            db.CarModels.AddRange(new[]
            {
                new CarModel { Name = "Model1", CarBrandId = brand.Id },
                new CarModel { Name = "Model2", CarBrandId = brand.Id }
            });
            await db.SaveChangesAsync();
            var response = await client.GetAsync($"/api/cars/models/brand/{brand.Id}");
            response.EnsureSuccessStatusCode();
            var models = await response.Content.ReadFromJsonAsync<List<CarModel>>();
            Assert.NotNull(models);
            Assert.Equal(2, models.Count);
            Assert.All(models, m => Assert.Equal(brand.Id, m.CarBrandId));
        }

        [Fact]
        public async Task AddCarModel_ShouldAddModelToDatabase()
        {
            var client = m_factory.CreateClient();
            var db = await GetDbAsync();
            var brand = new CarBrand { Name = "BrandForNewModel" };
            db.CarBrands.Add(brand);
            await db.SaveChangesAsync();
            var newModel = new CarModel { Name = "NewDockerModel" };
            var response = await client.PostAsJsonAsync($"/api/cars/models/{brand.Id}", newModel);
            response.EnsureSuccessStatusCode();
            var createdModel = await response.Content.ReadFromJsonAsync<CarModel>();
            Assert.NotNull(createdModel);
            Assert.Equal("NewDockerModel", createdModel.Name);
            Assert.Equal(brand.Id, createdModel.CarBrandId);
            var modelInDb = await db.CarModels.FindAsync(createdModel.Id);
            Assert.NotNull(modelInDb);
        }

        [Fact]
        public async Task AddCarModel_ShouldReturnErrorWhenCarmodelExistingForCarBrand()
            {
            var client = m_factory.CreateClient();
            var db = await GetDbAsync();
            var brand = new CarBrand { Name = "BrandForExistingModel" };
            db.CarBrands.Add(brand);
            await db.SaveChangesAsync();
            var existingModel = new CarModel { Name = "ExistingModel", CarBrandId = brand.Id };
            db.CarModels.Add(existingModel);
            await db.SaveChangesAsync();
            var newModel = new CarModel { Name = "ExistingModel", CarBrandId = brand.Id };
            var response = await client.PostAsJsonAsync($"/api/cars/models/{brand.Id}", newModel);
            Assert.False(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task UpdateCarmodel_ShouldUpdateExistingModel()
        {
            var client = m_factory.CreateClient();
            var db = await GetDbAsync();
            var brand = new CarBrand { Name = "BrandForUpdateModel" };
            db.CarBrands.Add(brand);
            await db.SaveChangesAsync();
            var model = new CarModel { Name = "ModelToUpdate", CarBrandId = brand.Id };
            db.CarModels.Add(model);
            await db.SaveChangesAsync();
            var updatedModel = new CarModel { Name = "UpdatedModelName", CarBrandId = brand.Id };
            var response = await client.PutAsJsonAsync($"/api/cars/models/{model.Id}", updatedModel);
            response.EnsureSuccessStatusCode();
            var modelInDb = await db.CarModels.AsNoTracking().FirstOrDefaultAsync(m => m.Id == model.Id);
            Assert.NotNull(modelInDb);
            Assert.Equal("UpdatedModelName", modelInDb.Name);
        }

        [Fact]
        public async Task DeleteCarmodel_ShouldRemoveModelFromDatabase()
        {
            var client = m_factory.CreateClient();
            var db = await GetDbAsync();
            var brand = new CarBrand { Name = "BrandForDeleteModel" };
            db.CarBrands.Add(brand);
            await db.SaveChangesAsync();
            var model = new CarModel { Name = "ModelToDelete", CarBrandId = brand.Id };
            db.CarModels.Add(model);
            await db.SaveChangesAsync();
            var response = await client.DeleteAsync($"/api/cars/models/{model.Id}");
            response.EnsureSuccessStatusCode();
            var modelInDb = await db.CarModels.AsNoTracking().FirstOrDefaultAsync(m => m.Id == model.Id);
            Assert.Null(modelInDb);
        }
    }
}
