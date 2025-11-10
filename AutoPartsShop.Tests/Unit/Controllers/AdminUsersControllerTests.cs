using AutoPartsShop.API.Controllers;
using AutoPartsShop.Core.Helpers;
using AutoPartsShop.Core.Models;
using AutoPartsShop.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using AutoPartsShop.Tests.Unit.Helpers;

namespace AutoPartsShop.Tests.Unit.Controllers
{
    public class AdminUsersControllerTests
    {
        private readonly AppDbContext m_context;
        private readonly FakeEmailService m_fakeEmail;
        private readonly AdminUsersController m_controller;

        public AdminUsersControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"AdminUserTestsDb_{Guid.NewGuid()}")
                .Options;

            m_context = new AppDbContext(options);
            m_fakeEmail = new FakeEmailService();

            m_controller = new AdminUsersController(m_context, m_fakeEmail)
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
            var result = await m_controller.GetUsers(null, null, null, null);

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
            m_context.Users.Add(u);
            await m_context.SaveChangesAsync();

            TestHttpContextHelper.AttachUser(m_controller, u.Id);

            var result = await m_controller.GetUsers(null, null, null, null);

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
            m_context.Users.AddRange(admin, u1, u2);
            await m_context.SaveChangesAsync();

            TestHttpContextHelper.AttachUser(m_controller, admin.Id);

            var result = await m_controller.GetUsers(search: "teszt", status: null, from: null, to: null);
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
            m_context.Users.Add(admin);
            await m_context.SaveChangesAsync();

            TestHttpContextHelper.AttachUser(m_controller, admin.Id);

            var result = await m_controller.GetUserDetails(999, includeOrders: false);
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
            m_context.Users.AddRange(admin, target);

            m_context.Orders.Add(new Order
            {
                UserId = target.Id,
                ShippingAddress = "Cím",
                BillingAddress = "Számla",
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { ItemType = "Part", Quantity = 2, Price = 1500, Name = "Alkatrész" }
                }
            });
            await m_context.SaveChangesAsync();

            TestHttpContextHelper.AttachUser(m_controller, admin.Id);

            var result = await m_controller.GetUserDetails(target.Id, includeOrders: true);
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
            m_context.Users.AddRange(admin, target);
            await m_context.SaveChangesAsync();

            TestHttpContextHelper.AttachUser(m_controller, admin.Id);

            var result = await m_controller.DeactivateUser(target.Id);
            var ok = Assert.IsType<OkObjectResult>(result);

            var updated = await m_context.Users.FindAsync(target.Id);
            Assert.False(updated!.IsActive);
        }

        [Fact]
        public async Task ActivateUser_ShouldSetIsActiveTrue()
        {
            var admin = new User { Id = 50, Email = "admin5@example.com", PasswordHash = "x", IsAdmin = true };
            var target = new User { Id = 51, Email = "user51@example.com", PasswordHash = "y", IsActive = false };
            m_context.Users.AddRange(admin, target);
            await m_context.SaveChangesAsync();

            TestHttpContextHelper.AttachUser(m_controller, admin.Id);

            var result = await m_controller.ActivateUser(target.Id);
            var ok = Assert.IsType<OkObjectResult>(result);

            var updated = await m_context.Users.FindAsync(target.Id);
            Assert.True(updated!.IsActive);
        }

        [Fact]
        public async Task SoftDeleteUser_ShouldBlockSelfDelete()
        {
            var admin = new User { Id = 60, Email = "admin6@example.com", PasswordHash = "x", IsAdmin = true, IsActive = true };
            m_context.Users.Add(admin);
            await m_context.SaveChangesAsync();

            TestHttpContextHelper.AttachUser(m_controller, admin.Id);

            var result = await m_controller.SoftDeleteUser(admin.Id);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("saját fiókot", badRequest.Value?.ToString()?.ToLower());
        }

        [Fact]
        public async Task SoftDeleteUser_ShouldBlockDeletingAdmin()
        {
            var admin = new User { Id = 70, Email = "admin7@example.com", PasswordHash = "x", IsAdmin = true };
            var admin2 = new User { Id = 71, Email = "admin71@example.com", PasswordHash = "x", IsAdmin = true };
            m_context.Users.AddRange(admin, admin2);
            await m_context.SaveChangesAsync();

            TestHttpContextHelper.AttachUser(m_controller, admin.Id);

            var result = await m_controller.SoftDeleteUser(admin2.Id);
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
            m_context.Users.AddRange(admin, user);

            m_context.Orders.AddRange(
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
            await m_context.SaveChangesAsync();

            TestHttpContextHelper.AttachUser(m_controller, admin.Id);

            var result = await m_controller.SoftDeleteUser(user.Id);
            var ok = Assert.IsType<OkObjectResult>(result);

            var refreshed = await m_context.Users.FindAsync(user.Id);
            Assert.False(refreshed!.IsActive);
            Assert.NotNull(refreshed.DeletedAt);

            var ordersLeft = await m_context.Orders.CountAsync(o => o.UserId == user.Id);
            Assert.Equal(0, ordersLeft);

            var cart = await m_context.Carts.FirstOrDefaultAsync(c => c.UserId == user.Id);
            Assert.Null(cart);

            Assert.Single(m_fakeEmail.Sent);
            Assert.Equal("deluser@example.com", m_fakeEmail.Sent[0].To);
        }
    }
}
