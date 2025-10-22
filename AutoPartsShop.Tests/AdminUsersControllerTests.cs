using AutoPartsShop.API.Controllers;
using AutoPartsShop.Core.Helpers;
using AutoPartsShop.Core.Models;
using AutoPartsShop.Infrastructure;
using AutoPartsShop.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AutoPartsShop.Tests
{
    public class AdminUsersControllerTests
    {
        private readonly AppDbContext _context;
        private readonly FakeEmailService _fakeEmail;
        private readonly AdminUsersController _controller;

        public AdminUsersControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"AdminUserTestsDb_{Guid.NewGuid()}")
                .Options;

            _context = new AppDbContext(options);
            _fakeEmail = new FakeEmailService();

            _controller = new AdminUsersController(_context, _fakeEmail)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public async Task GetUsers_ShouldReturnUnauthorized_WhenNoUser()
        {
            var result = await _controller.GetUsers(null, null, null, null);

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Contains("azonosítása sikertelen", unauthorized.Value?.ToString());
        }

        [Fact]
        public async Task GetUsers_ShouldReturnForbidden_WhenCallerNotAdmin()
        {
            var u = new User
            {
                Id = 1,
                FirstName = "Normál",
                LastName = "User",
                Email = "user@example.com",
                PasswordHash = PasswordHelper.HashPassword("Secret123"),
                IsAdmin = false
            };
            _context.Users.Add(u);
            await _context.SaveChangesAsync();

            TestHttpContextHelper.AttachUser(_controller, u.Id);

            var result = await _controller.GetUsers(null, null, null, null);

            // Forbid() -> ForbidResult
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task GetUsers_ShouldReturnList_WhenCallerIsAdmin()
        {
            var admin = new User
            {
                Id = 10,
                FirstName = "Admin",
                LastName = "Root",
                Email = "admin@example.com",
                PasswordHash = PasswordHelper.HashPassword("Admin1234"),
                IsAdmin = true
            };
            var u1 = new User
            {
                Id = 11,
                FirstName = "Anna",
                LastName = "Teszt",
                Email = "anna@example.com",
                PasswordHash = PasswordHelper.HashPassword("x"),
                IsActive = true
            };
            var u2 = new User
            {
                Id = 12,
                FirstName = "Béla",
                LastName = "Teszt",
                Email = "bela@example.com",
                PasswordHash = PasswordHelper.HashPassword("y"),
                IsActive = false
            };
            _context.Users.AddRange(admin, u1, u2);
            await _context.SaveChangesAsync();

            TestHttpContextHelper.AttachUser(_controller, admin.Id);

            var result = await _controller.GetUsers(search: "teszt", status: null, from: null, to: null);
            var ok = Assert.IsType<OkObjectResult>(result);

            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            var arr = doc.RootElement;

            Assert.Equal(JsonValueKind.Array, arr.ValueKind);
            Assert.True(arr.GetArrayLength() >= 2);
        }

        [Fact]
        public async Task GetUserDetails_ShouldReturnNotFound_WhenMissing()
        {
            var admin = new User
            {
                Id = 20,
                FirstName = "Admin",
                LastName = "Root",
                Email = "admin2@example.com",
                PasswordHash = PasswordHelper.HashPassword("Admin1234"),
                IsAdmin = true
            };
            _context.Users.Add(admin);
            await _context.SaveChangesAsync();

            TestHttpContextHelper.AttachUser(_controller, admin.Id);

            var result = await _controller.GetUserDetails(999, includeOrders: false);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("nem található", notFound.Value?.ToString()?.ToLower());
        }

        [Fact]
        public async Task GetUserDetails_ShouldReturnWithOrders_WhenIncludeOrdersTrue()
        {
            var admin = new User
            {
                Id = 30,
                FirstName = "Admin",
                LastName = "Root",
                Email = "admin3@example.com",
                PasswordHash = PasswordHelper.HashPassword("Admin1234"),
                IsAdmin = true
            };
            var target = new User
            {
                Id = 31,
                FirstName = "Cél",
                LastName = "User",
                Email = "target@example.com",
                PasswordHash = PasswordHelper.HashPassword("P"),
                IsActive = true
            };
            _context.Users.AddRange(admin, target);

            _context.Orders.Add(new Order
            {
                UserId = target.Id,
                ShippingAddress = "Cím",
                BillingAddress = "Számla",
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { ItemType = "Part", Quantity = 2, Price = 1500, Name = "Alkatrész" }
                }
            });
            await _context.SaveChangesAsync();

            TestHttpContextHelper.AttachUser(_controller, admin.Id);

            var result = await _controller.GetUserDetails(target.Id, includeOrders: true);
            var ok = Assert.IsType<OkObjectResult>(result);

            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.Equal("target@example.com", root.GetProperty("Email").GetString());
            var orders = root.GetProperty("Orders");
            Assert.Equal(JsonValueKind.Array, orders.ValueKind);
            Assert.True(orders.GetArrayLength() >= 1);
        }

        [Fact]
        public async Task DeactivateUser_ShouldSetIsActiveFalse()
        {
            var admin = new User { Id = 40, Email = "admin4@example.com", PasswordHash = "x", IsAdmin = true };
            var target = new User { Id = 41, Email = "user41@example.com", PasswordHash = "y", IsActive = true };
            _context.Users.AddRange(admin, target);
            await _context.SaveChangesAsync();

            TestHttpContextHelper.AttachUser(_controller, admin.Id);

            var result = await _controller.DeactivateUser(target.Id);
            var ok = Assert.IsType<OkObjectResult>(result);

            var updated = await _context.Users.FindAsync(target.Id);
            Assert.False(updated!.IsActive);
        }

        [Fact]
        public async Task ActivateUser_ShouldSetIsActiveTrue()
        {
            var admin = new User { Id = 50, Email = "admin5@example.com", PasswordHash = "x", IsAdmin = true };
            var target = new User { Id = 51, Email = "user51@example.com", PasswordHash = "y", IsActive = false };
            _context.Users.AddRange(admin, target);
            await _context.SaveChangesAsync();

            TestHttpContextHelper.AttachUser(_controller, admin.Id);

            var result = await _controller.ActivateUser(target.Id);
            var ok = Assert.IsType<OkObjectResult>(result);

            var updated = await _context.Users.FindAsync(target.Id);
            Assert.True(updated!.IsActive);
        }

        [Fact]
        public async Task SoftDeleteUser_ShouldBlockSelfDelete()
        {
            var admin = new User { Id = 60, Email = "admin6@example.com", PasswordHash = "x", IsAdmin = true, IsActive = true };
            _context.Users.Add(admin);
            await _context.SaveChangesAsync();

            TestHttpContextHelper.AttachUser(_controller, admin.Id);

            var result = await _controller.SoftDeleteUser(admin.Id);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("saját fiókot", badRequest.Value?.ToString()?.ToLower());
        }

        [Fact]
        public async Task SoftDeleteUser_ShouldBlockDeletingAdmin()
        {
            var admin = new User { Id = 70, Email = "admin7@example.com", PasswordHash = "x", IsAdmin = true };
            var admin2 = new User { Id = 71, Email = "admin71@example.com", PasswordHash = "x", IsAdmin = true };
            _context.Users.AddRange(admin, admin2);
            await _context.SaveChangesAsync();

            TestHttpContextHelper.AttachUser(_controller, admin.Id);

            var result = await _controller.SoftDeleteUser(admin2.Id);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("admin felhasználót nem törölhetsz", badRequest.Value?.ToString()?.ToLower());
        }

        [Fact]
        public async Task SoftDeleteUser_ShouldSoftDeleteAndCleanupAndSendMail()
        {
            var admin = new User { Id = 80, Email = "admin8@example.com", PasswordHash = "xxxxxxxxx", IsAdmin = true };
            var user = new User
            {
                Id = 81,
                FirstName = "Törlendő",
                LastName = "User",
                Email = "deluser@example.com",
                PasswordHash = "yyyyyyyyyy",
                IsActive = true,
                Cart = new Cart
                {
                    Items = new List<CartItem>
                    {
                        new CartItem { ItemType = "Part", Quantity = 1, Price = 100, Name = "C1" }
                    }
                }
            };
            _context.Users.AddRange(admin, user);

            _context.Orders.AddRange(
                new Order
                {
                    UserId = user.Id,
                    ShippingAddress = "Cím",
                    BillingAddress = "Számla",
                    OrderItems = new List<OrderItem>
                    {
                        new OrderItem { ItemType = "Part", Quantity = 1, Price = 500, Name = "O1" }
                    }
                },
                new Order
                {
                    UserId = user.Id,
                    ShippingAddress = "Cím2",
                    BillingAddress = "Számla2",
                    OrderItems = new List<OrderItem>
                    {
                        new OrderItem { ItemType = "Part", Quantity = 2, Price = 300, Name = "O2" }
                    }
                }
            );
            await _context.SaveChangesAsync();

            TestHttpContextHelper.AttachUser(_controller, admin.Id);

            var result = await _controller.SoftDeleteUser(user.Id);
            var ok = Assert.IsType<OkObjectResult>(result);

            var refreshed = await _context.Users.FindAsync(user.Id);
            Assert.False(refreshed!.IsActive);
            Assert.NotNull(refreshed.DeletedAt);

            var ordersLeft = await _context.Orders.CountAsync(o => o.UserId == user.Id);
            Assert.Equal(0, ordersLeft);

            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == user.Id);
            Assert.Null(cart);

            Assert.Single(_fakeEmail.Sent);
            Assert.Equal("deluser@example.com", _fakeEmail.Sent[0].To);
        }
    }
}
