using AutoPartsShop.API.Controllers;
using AutoPartsShop.Core.Models;
using AutoPartsShop.Infrastructure;
using AutoPartsShop.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AutoPartsShop.Tests.Unit.Controllers
{
    public class CartControllerTests
    {
        private readonly AppDbContext _context;
        private readonly CartController _controller;

        public CartControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"CartTestDb_{Guid.NewGuid()}")
                .Options;

            _context = new AppDbContext(options);
            _controller = new CartController(_context);
        }

        [Fact]
        public async Task GetUserCart_ShouldReturnUnauthorized_WhenNoUser()
        {
            var result = await _controller.GetUserCart();
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Contains("azonosítása sikertelen", unauthorized.Value!.ToString());
        }

        [Fact]
        public async Task GetUserCart_ShouldReturnEmptyList_WhenCartDoesNotExist()
        {
            TestHttpContextHelper.AttachUser(_controller, 1);

            var result = await _controller.GetUserCart();
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsType<List<CartItem>>(ok.Value);
            Assert.Empty(list);
        }

        [Fact]
        public async Task AddToCart_ShouldReturnUnauthorized_WhenNoUser()
        {
            var result = await _controller.AddToCart(new CartItem { ItemType = "Part" });
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Contains("azonosítása sikertelen", unauthorized.Value!.ToString());
        }

        [Fact]
        public async Task AddToCart_ShouldReturnBadRequest_WhenInvalidItemType()
        {
            TestHttpContextHelper.AttachUser(_controller, 10);

            var item = new CartItem { ItemType = "InvalidType" };
            var result = await _controller.AddToCart(item);
            var badReq = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("hibás terméktípus", badReq.Value!.ToString().ToLower());
        }

        [Fact]
        public async Task AddToCart_ShouldReturnNotFound_WhenPartDoesNotExist()
        {
            TestHttpContextHelper.AttachUser(_controller, 2);

            var item = new CartItem { ItemType = "Part", PartId = 999, Quantity = 1 };
            var result = await _controller.AddToCart(item);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("part id", notFound.Value!.ToString().ToLower());
        }

        [Fact]
        public async Task AddToCart_ShouldAddNewItem_WhenValidPart()
        {
            TestHttpContextHelper.AttachUser(_controller, 3);

            var part = new Part { Id = 1, Name = "Féktárcsa", Price = 12000, Manufacturer = "Bosch" };
            _context.Parts.Add(part);
            await _context.SaveChangesAsync();

            var item = new CartItem { ItemType = "Part", PartId = part.Id, Quantity = 2 };

            var result = await _controller.AddToCart(item);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("sikeresen hozzáadva", ok.Value!.ToString());

            var cart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == 3);
            Assert.NotNull(cart);
            Assert.Single(cart!.Items);
            Assert.Equal("Féktárcsa", cart.Items[0].Name);
        }

        [Fact]
        public async Task UpdateCartItemQuantity_ShouldReturnNotFound_WhenNoCart()
        {
            TestHttpContextHelper.AttachUser(_controller, 4);

            var result = await _controller.UpdateCartItemQuantity(1, 5);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("nincs kosara", notFound.Value!.ToString().ToLower());
        }

        [Fact]
        public async Task UpdateCartItemQuantity_ShouldUpdateQuantity_WhenExists()
        {
            TestHttpContextHelper.AttachUser(_controller, 5);
            var cart = new Cart { UserId = 5, Items = new List<CartItem> { new CartItem { Id = 1, Name = "Cikk", Quantity = 1, ItemType = "Part" } } };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            var result = await _controller.UpdateCartItemQuantity(1, 5);
            var ok = Assert.IsType<OkObjectResult>(result);

            var updated = await _context.CartItems.FindAsync(1);
            Assert.Equal(5, updated!.Quantity);
        }

        [Fact]
        public async Task RemoveFromCart_ShouldReturnNotFound_WhenCartMissing()
        {
            TestHttpContextHelper.AttachUser(_controller, 6);
            var result = await _controller.RemoveFromCart(1);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("nincs kosara", notFound.Value!.ToString().ToLower());
        }

        [Fact]
        public async Task RemoveFromCart_ShouldRemoveItem_WhenExists()
        {
            TestHttpContextHelper.AttachUser(_controller, 7);
            var cart = new Cart
            {
                UserId = 7,
                Items = new List<CartItem>
                {
                    new CartItem { Id = 1, Name = "Elem", ItemType = "Part", Quantity = 1 }
                }
            };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            var result = await _controller.RemoveFromCart(1);
            var ok = Assert.IsType<OkObjectResult>(result);

            var refreshed = await _context.Carts.Include(c => c.Items).FirstAsync();
            Assert.Empty(refreshed.Items);
        }

        [Fact]
        public async Task DeleteCart_ShouldReturnOk_WhenCartDoesNotExist()
        {
            TestHttpContextHelper.AttachUser(_controller, 8);
            var result = await _controller.DeleteCart();
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("nincs mit törölni", ok.Value!.ToString().ToLower());
        }

        [Fact]
        public async Task DeleteCart_ShouldRemoveCart_WhenExists()
        {
            TestHttpContextHelper.AttachUser(_controller, 9);
            var cart = new Cart
            {
                UserId = 9,
                Items = new List<CartItem> { new CartItem { Id = 1, Name = "Elem", ItemType = "Equipment" } }
            };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteCart();
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("sikeresen törölve", ok.Value!.ToString().ToLower());

            Assert.Empty(_context.Carts);
        }

        [Fact]
        public async Task CreateCart_ShouldReturnBadRequest_WhenCartAlreadyExists()
        {
            TestHttpContextHelper.AttachUser(_controller, 10);
            _context.Carts.Add(new Cart { UserId = 10 });
            await _context.SaveChangesAsync();

            var result = await _controller.CreateCart();
            var badReq = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("már van kosara", badReq.Value!.ToString().ToLower());
        }

        [Fact]
        public async Task CreateCart_ShouldReturnOk_WhenCreated()
        {
            TestHttpContextHelper.AttachUser(_controller, 11);
            var result = await _controller.CreateCart();
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Contains("létrehozva", ok.Value!.ToString().ToLower());

            Assert.Single(_context.Carts);
        }
    }
}
