using AutoPartsShop.Tests.Integration.Factories;
using Microsoft.Extensions.DependencyInjection;
using AutoPartsShop.Infrastructure;
using System.Net.Http.Json;
using AutoPartsShop.Core.Models;
using System.Net;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsShop.Tests.Integration.Controllers
{
    public class EquipmentCategoryControllerTests : IClassFixture<DockerWebApplicationFactory>
    {
        DockerWebApplicationFactory m_factory;

        public EquipmentCategoryControllerTests(DockerWebApplicationFactory p_factory)
        {
            m_factory = p_factory;
        }

        public async Task<AppDbContext> GetDbAsync()
        {
            var scope = m_factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();
            return db;
        }

        [Fact]
        public async Task GetEquipmentCategories_ShouldReturnSeededCategories()
        {
            HttpClient client = m_factory.CreateClient();
            AppDbContext db = await GetDbAsync();
            EquipmentCategory category = new EquipmentCategory
            {
                Name = "DockerCategory"
            };
            db.EquipmentCategories.Add(category);
            await db.SaveChangesAsync();
            var response = await client.GetAsync("/api/equipmentcategories");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var list = await response.Content.ReadFromJsonAsync<List<object>>();
            Assert.NotNull(list);
            Assert.True(list!.Count >= 1);
        }

        [Fact]
        public async Task AddEquipmentCategory_ShouldAddNewCategory()
        {
            HttpClient client = m_factory.CreateClient();
            EquipmentCategory newCategory = new EquipmentCategory
            {
                Name = "NewDockerCategory"
            };
            var response = await client.PostAsJsonAsync("/api/equipmentcategories", newCategory);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var createdCategory = await response.Content.ReadFromJsonAsync<EquipmentCategory>();
            Assert.NotNull(createdCategory);
            Assert.Equal("NewDockerCategory", createdCategory!.Name);
        }

        [Fact]
        public async Task UpdateEquipmentCategory_ShouldUpdateExistingCategory()
        {
            HttpClient client = m_factory.CreateClient();
            AppDbContext db = await GetDbAsync();
            EquipmentCategory category = new EquipmentCategory
            {
                Name = "UpdateDockerCategory"
            };
            db.EquipmentCategories.Add(category);
            await db.SaveChangesAsync();
            category.Name = "UpdatedDockerCategory";
            var response = await client.PutAsJsonAsync($"/api/equipmentcategories/{category.Id}", category);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var updatedCategory = await db.EquipmentCategories.FindAsync(category.Id);
            Assert.NotNull(updatedCategory);
            Assert.Equal("UpdatedDockerCategory", updatedCategory!.Name);
        }

        [Fact]
        public async Task DeleteEquipmentCategory_ShouldDeleteExistingCategory()
        {
            HttpClient client = m_factory.CreateClient();
            AppDbContext db = await GetDbAsync();
            EquipmentCategory category = new EquipmentCategory
            {
                Name = "DeleteDockerCategory"
            };
            db.EquipmentCategories.Add(category);
            await db.SaveChangesAsync();
            var response = await client.DeleteAsync($"/api/equipmentcategories/{category.Id}");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var deletedCategory = await db.EquipmentCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == category.Id);
            Assert.Null(deletedCategory);
        }

        [Fact]
        public async Task DeleteEquipmentCategory_NonExistingCategory_ShouldReturnNotFound()
        {
            HttpClient client = m_factory.CreateClient();
            var response = await client.DeleteAsync($"/api/equipmentcategories/99999");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}

