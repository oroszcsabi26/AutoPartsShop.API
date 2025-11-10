using AutoPartsShop.API.Controllers;
using AutoPartsShop.Infrastructure;
using Microsoft.EntityFrameworkCore;
using AutoPartsShop.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartsShop.Tests.Unit.Controllers
{
    public class PartsCategoryControllerTests
    {
        private readonly PartsCategoryController m_controller;
        private readonly AppDbContext m_context;

        public PartsCategoryControllerTests()
        {

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            m_context = new AppDbContext(options);
            m_controller = new PartsCategoryController(m_context);
        }

        [Fact]
        public async Task GetPartsCategories_ReturnsAllCategories()
        {
            m_context.PartsCategories.AddRange(
                new PartsCategory { Name = "Engine", Parts = new List<Part>() },
                new PartsCategory { Name = "Brakes", Parts = new List<Part>() }
            );

            Part testPart = new Part
            {
                Name = "Test Part",
                Price = 100.00m,
                CarModelId = 1,
                PartsCategoryId = 1,
                Manufacturer = "Test Manufacturer",
                Quantity = 10
            };

            Part testPart1 = new Part
            {
                Name = "Test Part 2",
                Price = 150.00m,
                CarModelId = 1,
                PartsCategoryId = 2,
                Manufacturer = "Test Manufacturer 2",
                Quantity = 5
            };

            var engineCategory = m_context.PartsCategories.FirstOrDefault<PartsCategory>(c => c.Name == "Engine");
            var brakesCategory = m_context.PartsCategories.FirstOrDefault<PartsCategory>(c => c.Name == "Brakes");

            if (engineCategory != null)
                engineCategory.Parts.Add(testPart);

            if (brakesCategory != null)
                brakesCategory.Parts.Add(testPart1);

            await m_context.SaveChangesAsync();
            
            var result = await m_controller.GetPartsCategories();
            
            var categories = Assert.IsType<List<PartsCategory>>(result.Value);
            Assert.Equal(2, categories.Count);
        }

        [Fact]
        public async Task AddPartsCategory_ValidCategory_AddsCategory()
        {
            var newCategory = new PartsCategory { Name = "Suspension" };
            var result = await m_controller.AddPartsCategory(newCategory);

            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnValue = Assert.IsType<PartsCategory>(createdAtActionResult.Value);
            Assert.Equal("Suspension", returnValue.Name);
            Assert.Equal(1, m_context.PartsCategories.Count());
        }

        [Fact]
        public async Task AddPartsCategory_DuplicateName_ReturnsConflict()
        {
            var existingCategory = new PartsCategory { Name = "Wheels" };
            m_context.PartsCategories.Add(existingCategory);
            await m_context.SaveChangesAsync();
            var newCategory = new PartsCategory { Name = "Wheels" };
            var result = await m_controller.AddPartsCategory(newCategory);
            var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Equal(409, conflictResult.StatusCode);
        }

        [Fact]
        public async Task UpdatePartsCategory_ValidUpdate_UpdatesCategory()
        {
            var existingCategory = new PartsCategory { Name = "Exhaust" };
            m_context.PartsCategories.Add(existingCategory);
            await m_context.SaveChangesAsync();
            var updatedCategory = new PartsCategory { Name = "Exhaust Systems" };
            var result = await m_controller.UpdatePartsCategory(existingCategory.Id, updatedCategory);
            Assert.IsType<NoContentResult>(result);
            var categoryInDb = await m_context.PartsCategories.FindAsync(existingCategory.Id);
            Assert.Equal("Exhaust Systems", categoryInDb.Name);
        }

        [Fact]
        public async Task DeletePartsCategory_ShouldDeleteCategory()
        {
            var categoryToDelete = new PartsCategory { Name = "Transmission" };
            m_context.PartsCategories.Add(categoryToDelete);
            await m_context.SaveChangesAsync();
            var result = await m_controller.DeletePartsCategory(categoryToDelete.Id);
            Assert.IsType<NoContentResult>(result);
            var categoryInDb = await m_context.PartsCategories.FindAsync(categoryToDelete.Id);
            Assert.Null(categoryInDb);
        }
    }
}
