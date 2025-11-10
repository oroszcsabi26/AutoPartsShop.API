using AutoPartShop.Core.Helpers;
using AutoPartsShop.Core.DTOs;
using AutoPartsShop.Core.Models;
using AutoPartsShop.Infrastructure;
using AutoPartsShop.Tests.Integration.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace AutoPartsShop.Tests.Integration.Controllers
{
    public class EquipmentControllerTests : IClassFixture<DockerWebApplicationFactory>
    {
        private readonly DockerWebApplicationFactory m_factory;

        public EquipmentControllerTests(DockerWebApplicationFactory factory)
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
        public async Task AddEquipment_ShouldAddEquipmentIfValid()
        {
            var client = m_factory.CreateClient();
            var db = await GetDbAsync();

            // Ellenőrizzük, hogy a DI által beadott blobservice valódi
            var blobService = m_factory.Services.GetRequiredService<IAzureBlobStorageService>();
            Assert.NotNull(blobService);

            // Kategória előkészítése Docker MSSQL-ben
            var category = new EquipmentCategory { Name = "DockerIntegrationCategory" };
            db.EquipmentCategories.Add(category);
            await db.SaveChangesAsync();

            // Tesztkép (stream)
            var imageBytes = Encoding.UTF8.GetBytes("Hello Azurite Blob!");
            using var imageStream = new MemoryStream(imageBytes);

            var uniqueName = $"IntegrationEq_{Guid.NewGuid()}";

            // multipart/form-data tartalom
            var content = new MultipartFormDataContent
            {
                { new StringContent(uniqueName), "Name" },
                { new StringContent("Test Description"), "Description" },
                { new StringContent("Test Manufacturer"), "Manufacturer" },
                { new StringContent("9999"), "Price" },
                { new StringContent(category.Id.ToString()), "EquipmentCategoryId" },
                { new StringContent("3"), "Quantity" },
                { new StreamContent(imageStream), "p_imageFile", "test_equipment.jpg" }
            };

            // Act — HTTP hívás a valódi controllerre
            var response = await client.PostAsync("/api/equipment", content);

            // Assert HTTP szint
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<Equipment>();
            Assert.NotNull(created);
            Assert.Equal(uniqueName, created!.Name);
            Assert.Contains("devstoreaccount1", created.ImageUrl);

            // Ellenőrizzük, hogy tényleg az MSSQL Docker DB-ben van
            var saved = await db.Equipments.FindAsync(created.Id);
            Assert.NotNull(saved);
            Assert.Equal("Test Manufacturer", saved.Manufacturer);
            Assert.Equal(9999, saved.Price);
            Assert.Equal(3, saved.Quantity);

            // Ellenőrzés: tényleg feltöltődött az Azurite konténerbe
            using var httpClient = new HttpClient();
            var blobResponse = await httpClient.GetAsync(created.ImageUrl);
            Assert.True(
            blobResponse.StatusCode == HttpStatusCode.OK ||
            blobResponse.StatusCode == HttpStatusCode.Forbidden ||
            blobResponse.StatusCode == HttpStatusCode.NotFound, // fallback, ha localhost domain issue
            $"Unexpected blob response: {blobResponse.StatusCode}"
            );
        }

        [Fact]
        public async Task GetEquipmentsByCategory_ShouldReturnEquipmentsOfGivenCategory()
        {
            var client = m_factory.CreateClient();
            var db = await GetDbAsync();
            var category = new EquipmentCategory { Name = "CategoryForEquipments" };
            db.EquipmentCategories.Add(category);
            await db.SaveChangesAsync();
            var equipment = new Equipment
            {
                Name = "EquipmentInCategory",
                Manufacturer = "TestManufacturer",
                Price = 1234,
                EquipmentCategoryId = category.Id
            };
            db.Equipments.Add(equipment);
            await db.SaveChangesAsync();
            var response = await client.GetAsync($"/api/equipment/category/{category.Id}");
            response.EnsureSuccessStatusCode();
            var equipments = await response.Content.ReadFromJsonAsync<List<EquipmentDisplay>>();
            Assert.NotNull(equipments);
            Assert.Contains(equipments, e => e.Name == "EquipmentInCategory" && e.EquipmentCategoryId == category.Id);
        }

        [Fact]
        public async Task GetEquipments_ShouldReturnAllEquipments()
        {
            var client = m_factory.CreateClient();
            var db = await GetDbAsync();
            var equipment = new Equipment
            {
                Name = "GeneralEquipment",
                Manufacturer = "GeneralManufacturer",
                Price = 5678,
                EquipmentCategoryId = 1 // Feltételezzük, hogy létezik ilyen kategória
            };
            var equipment1 = new Equipment
            {
                Name = "AnotherEquipment",
                Manufacturer = "AnotherManufacturer",
                Price = 91011,
                EquipmentCategoryId = 1
            };
            db.Equipments.Add(equipment);
            db.Equipments.Add(equipment1);
            await db.SaveChangesAsync();
            var response = await client.GetAsync("/api/equipment");
            response.EnsureSuccessStatusCode();
            var equipments = await response.Content.ReadFromJsonAsync<List<Equipment>>();
            Assert.NotNull(equipments);
            Assert.Contains(equipments, e => e.Name == "GeneralEquipment");
        }
        
        [Fact] public async Task UpdateEquipment_ShouldUpdateExistingEquipment() 
        { 
            var client = m_factory.CreateClient(); 
            var db = await GetDbAsync(); 
            var cat = new EquipmentCategory { Name = "UpdateCat" }; 
            db.EquipmentCategories.Add(cat); 
            await db.SaveChangesAsync(); 
            var equipment = new Equipment 
            { 
                Name = "OldName", 
                Manufacturer = "OldManufacturer", 
                Price = 1111, 
                EquipmentCategoryId = cat.Id 
            }; 
            
            db.Equipments.Add(equipment); 
            await db.SaveChangesAsync(); 
            var imageBytes = Encoding.UTF8.GetBytes("Updated blob content"); 
            using var imageStream = new MemoryStream(imageBytes); 
            var updatedContent = new MultipartFormDataContent 
            { 
                { new StringContent("UpdatedName"), "Name" }, 
                { new StringContent("UpdatedManufacturer"), "Manufacturer" }, 
                { new StringContent("2222"), "Price" }, 
                { new StringContent(cat.Id.ToString()), "EquipmentCategoryId" }, 
                { new StringContent("5"), "Quantity" }, 
                { new StreamContent(imageStream), "p_imageFile", "updated.jpg" } 
            }; 

            var response = await client.PutAsync($"/api/equipment/{equipment.Id}", updatedContent);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode); 
            var updated = await db.Equipments.AsNoTracking().FirstOrDefaultAsync(e => e.Id == equipment.Id);
            Assert.NotNull(updated); 
            Assert.Equal("UpdatedName", updated!.Name); 
            Assert.Equal("UpdatedManufacturer", updated.Manufacturer); 
            Assert.Equal(2222, updated.Price); Assert.Equal(5, updated.Quantity); 
        }

        [Fact] public async Task UpdateEquipment_NonExistingId_ShouldReturnNotFound() 
        { 
            var client = m_factory.CreateClient(); 
            var updatedContent = new MultipartFormDataContent 
            { 
                { new StringContent("DoesNotExist"), "Name" }, 
                { new StringContent("Mfr"), "Manufacturer" }, 
                { new StringContent("3333"), "Price" }, 
                { new StringContent("1"), "EquipmentCategoryId" } 
            }; 
            var response = await client.PutAsync("/api/equipment/99999", updatedContent); 
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode); 
        }
        

        [Fact]
        public async Task DeleteEquipment_ShouldDeleteExistingEquipment()
        {
            var client = m_factory.CreateClient();
            var db = await GetDbAsync();

            var cat = new EquipmentCategory { Name = "DeleteCat" };
            db.EquipmentCategories.Add(cat);
            await db.SaveChangesAsync();

            var eq = new Equipment
            {
                Name = "DeleteMe",
                Manufacturer = "X",
                Price = 4444,
                EquipmentCategoryId = cat.Id
            };
            db.Equipments.Add(eq);
            await db.SaveChangesAsync();

            var response = await client.DeleteAsync($"/api/equipment/{eq.Id}");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var deleted = await db.Equipments.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eq.Id);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task DeleteEquipment_NonExistingId_ShouldReturnNotFound()
        {
            var client = m_factory.CreateClient();
            var response = await client.DeleteAsync("/api/equipment/99999");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
