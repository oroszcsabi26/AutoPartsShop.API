using AutoPartsShop.API.Controllers;
using AutoPartsShop.Core.DTOs;
using AutoPartsShop.Core.Helpers;
using AutoPartsShop.Core.Models;
using AutoPartsShop.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using AutoPartsShop.Tests.Unit.Helpers;

namespace AutoPartsShop.Tests.Unit.Controllers
{
    public class UserControllerTests
    {
        private readonly AppDbContext m_context;
        private readonly UserController m_controller;
        private readonly FakeEmailService m_fakeEmail;
        private readonly IConfiguration m_config;

        public UserControllerTests()
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"UserTestsDb_{Guid.NewGuid()}")
                .Options;

            m_context = new AppDbContext(options);

            var dict = new Dictionary<string, string>
            {
                { "JwtSettings:SecretKey", "MySuperSecretKeyForTesting1234567890" },
                { "JwtSettings:Issuer", "TestIssuer" },
                { "JwtSettings:Audience", "TestAudience" },
                { "JwtSettings:ExpiryMinutes", "60" },

                { "PasswordReset:TokenMinutes", "30" },
                { "PasswordReset:FrontendBaseUrl", "http://localhost:4200/reset-password" }
            };

            // Mock konfiguráció a JWT-hez
            m_config = new ConfigurationBuilder()
                .AddInMemoryCollection(dict!)
                .Build();

            m_fakeEmail = new FakeEmailService();

            m_controller = new UserController(m_context, m_config, m_fakeEmail)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public async Task Register_ShouldCreateUser_WhenDataIsValid()
        {
            var newUser = new User
            {
                FirstName = "Teszt",
                LastName = "Felhasználó",
                Email = "teszt@example.com",
                PasswordHash = "Jelszo123", // A controller hash-elni!
                Address = "Fő utca 1",
                ShippingAddress = "Mellék utca 2",
                PhoneNumber = "123456789"
            };

            // Act – regisztráció meghívása
            var result = await m_controller.Register(newUser);

            // Assert – sikeres válasz és ellenőrzés
            var okResult = Assert.IsType<OkObjectResult>(result);

            var json = JsonSerializer.Serialize(okResult.Value);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            Assert.Equal("Sikeres regisztráció!", root.GetProperty("message").GetString());

            var userElement = root.GetProperty("user");
            Assert.Equal("Teszt", userElement.GetProperty("firstName").GetString());
            Assert.Equal("Felhasználó", userElement.GetProperty("lastName").GetString());
            Assert.Equal("teszt@example.com", userElement.GetProperty("email").GetString());

            var inserted = await m_context.Users.FirstOrDefaultAsync(u => u.Email == "teszt@example.com");
            Assert.NotNull(inserted);
            Assert.NotEqual("Jelszo123", inserted!.PasswordHash); // hash-elve lett
        }

        [Fact]
        public async Task Register_ShouldReturnConflict_WhenEmailAlreadyExists()
        {
            m_context.Users.Add(new User
            {
                FirstName = "X",
                LastName = "Y",
                Email = "dup@example.com",
                PasswordHash = PasswordHelper.HashPassword("Secret123"),
                Address = "Cím",
                ShippingAddress = "Száll cím",
                PhoneNumber = "111"
            });
            await m_context.SaveChangesAsync();

            var newUser = new User
            {
                FirstName = "Z",
                LastName = "W",
                Email = "dup@example.com",
                PasswordHash = PasswordHelper.HashPassword("Secret321"),
                Address = "Cím",
                ShippingAddress = "Száll cím",
                PhoneNumber = "222"
            };

            var result = await m_controller.Register(newUser);

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Contains("már regisztrálva", conflict.Value?.ToString());
        }

        [Fact]
        public async Task Login_ShouldReturnToken_WhenCredentialsAreValid()
        {
            var plain = "TesztJelszo123";
            m_context.Users.Add(new User
            {
                FirstName = "Béla",
                LastName = "Teszt",
                Email = "bela@example.com",
                PasswordHash = PasswordHelper.HashPassword(plain),
                Address = "Cím 1",
                ShippingAddress = "Szállítási cím",
                PhoneNumber = "987654321",
                IsActive = true
            });

            await m_context.SaveChangesAsync();

            var req = new UserLoginRequest { Email = "bela@example.com", Password = plain };
            var result = await m_controller.Login(req);

            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.Equal("Sikeres bejelentkezés!", root.GetProperty("message").GetString());
            Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("token").GetString()));
            Assert.Equal("bela@example.com", root.GetProperty("user").GetProperty("email").GetString());
        }

        [Fact]
        public async Task Login_ShouldReturnUnauthorized_WhenPasswordWrong()
        {
            var correct = "HelyesJelszo123";
            m_context.Users.Add(new User
            {
                FirstName = "Teszt",
                LastName = "Felhasználó",
                Email = "rosszjelszo@example.com",
                PasswordHash = PasswordHelper.HashPassword(correct),
                Address = "Teszt utca 10",
                ShippingAddress = "Másik utca 2",
                PhoneNumber = "987654321",
                IsActive = true
            });
            await m_context.SaveChangesAsync();

            var req = new UserLoginRequest { Email = "rosszjelszo@example.com", Password = "HibásJelszo999" };
            var result = await m_controller.Login(req);

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Contains("Hibás e-mail vagy jelszó", unauthorized.Value?.ToString());
        }

        [Fact]
        public async Task GetUserProfile_ShouldReturnUnauthorized_WhenNoUser()
        {
            var result = await m_controller.GetUserProfile();
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Contains("azonosítása sikertelen", unauthorized.Value?.ToString());
        }

        [Fact]
        public async Task GetUserProfile_ShouldReturnData_WhenUserAttached()
        {
            var u = new User
            {
                Id = 101,
                FirstName = "Anna",
                LastName = "Teszt",
                Email = "anna@example.com",
                PasswordHash = PasswordHelper.HashPassword("Secret123"),
                Address = "Cím",
                ShippingAddress = "Száll",
                PhoneNumber = "111",
                IsActive = true
            };
            m_context.Users.Add(u);
            await m_context.SaveChangesAsync();

            TestHttpContextHelper.AttachUser(m_controller, u.Id);

            var result = await m_controller.GetUserProfile();

            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.Equal("anna@example.com", root.GetProperty("Email").GetString());
            Assert.Equal("Anna", root.GetProperty("FirstName").GetString());
        }

        [Fact]
        public async Task UpdateUserProfile_ShouldPersistChanges()
        {
            var u = new User
            {
                Id = 202,
                FirstName = "Régi",
                LastName = "Név",
                Email = "regi@example.com",
                PasswordHash = PasswordHelper.HashPassword("Secret123"),
                Address = "Régi cím",
                ShippingAddress = "Régi száll",
                PhoneNumber = "111",
                IsActive = true
            };
            m_context.Users.Add(u);
            await m_context.SaveChangesAsync();

            TestHttpContextHelper.AttachUser(m_controller, u.Id);

            var dto = new UserProfileUpdate
            {
                FirstName = "Új",
                LastName = "Név",
                Email = "uj@example.com",
                PhoneNumber = "222",
                Address = "Új cím",
                ShippingAddress = "Új száll"
            };

            var result = await m_controller.UpdateUserProfile(dto);
            var ok = Assert.IsType<OkObjectResult>(result);

            var updated = await m_context.Users.FindAsync(u.Id);
            Assert.Equal("Új", updated!.FirstName);
            Assert.Equal("uj@example.com", updated.Email);
        }

        [Fact]
        public async Task GetUserOrders_ShouldReturnOrdersOfCaller()
        {
            var userId = 303;
            var u = new User
            {
                Id = userId,
                FirstName = "Order",
                LastName = "Owner",
                Email = "owner@example.com",
                PasswordHash = PasswordHelper.HashPassword("Secret123"),
                PhoneNumber = "123",
                IsActive = true
            };
            m_context.Users.Add(u);

            m_context.Orders.Add(new Order
            {
                UserId = userId,
                ShippingAddress = "Cím",
                BillingAddress = "Számla",
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { ItemType = "Part", Quantity = 1, Price = 1000, Name = "T1" }
                }
            });

            await m_context.SaveChangesAsync();

            TestHttpContextHelper.AttachUser(m_controller, userId);

            var result = await m_controller.GetUserOrders();

            var ok = Assert.IsType<OkObjectResult>(result);
            // közvetlenül listaként vizsgáljuk:
            var orders = Assert.IsAssignableFrom<IEnumerable<Order>>(ok.Value);

            Assert.NotNull(orders);
            Assert.True(orders.Any());
            Assert.True(orders.First().OrderItems.Count >= 1);
        }

        [Fact]
        public async Task RequestPasswordReset_ShouldBeGenericAndSendEmail()
        {
            var u = new User
            {
                Id = 404,
                FirstName = "Jani",
                LastName = "Reset",
                Email = "reset@example.com",
                PasswordHash = PasswordHelper.HashPassword("XyZ123456"),
                IsActive = true
            };
            m_context.Users.Add(u);
            await m_context.SaveChangesAsync();

            var dto = new ResetPasswordRequestDto { Email = "reset@example.com" };
            var result = await m_controller.RequestPasswordReset(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("ha az e-mail cím létezik", ok.Value?.ToString()?.ToLower());

            var tokenRows = await m_context.PasswordResetTokens.Where(t => t.UserId == u.Id).ToListAsync();
            Assert.Single(tokenRows);

            Assert.Single(m_fakeEmail.Sent);
            Assert.Equal("reset@example.com", m_fakeEmail.Sent[0].To);
        }

        [Fact]
        public async Task ConfirmPasswordReset_ShouldUpdatePassword_AndInvalidateOtherTokens()
        {
            var u = new User
            {
                Id = 505,
                FirstName = "Eva",
                LastName = "UjJelszo",
                Email = "eva@example.com",
                PasswordHash = PasswordHelper.HashPassword("OldPass123"),
                IsActive = true
            };
            m_context.Users.Add(u);

            var now = DateTime.UtcNow;
            var valid = new PasswordResetToken
            {
                UserId = u.Id,
                Token = "VALID_TOKEN",
                CreatedAt = now.AddMinutes(-1),
                ExpiresAt = now.AddMinutes(15),
                UsedAt = null
            };
            var otherActive = new PasswordResetToken
            {
                UserId = u.Id,
                Token = "OTHER_TOKEN",
                CreatedAt = now.AddMinutes(-2),
                ExpiresAt = now.AddMinutes(15),
                UsedAt = null
            };
            m_context.PasswordResetTokens.AddRange(valid, otherActive);
            await m_context.SaveChangesAsync();

            var dto = new ResetPasswordConfirmDto
            {
                Email = "eva@example.com",
                Token = "VALID_TOKEN",
                NewPassword = "NewPass999"
            };

            var result = await m_controller.ConfirmPasswordReset(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("sikeresen frissült", ok.Value?.ToString()?.ToLower());

            var refreshedUser = await m_context.Users.FindAsync(u.Id);
            Assert.NotEqual(PasswordHelper.HashPassword("OldPass123"), refreshedUser!.PasswordHash);
            Assert.Equal(PasswordHelper.HashPassword("NewPass999"), refreshedUser.PasswordHash);

            var tokens = await m_context.PasswordResetTokens.Where(t => t.UserId == u.Id).ToListAsync();
            Assert.All(tokens, t => Assert.NotNull(t.UsedAt));
        }

        [Fact]
        public async Task Register_ShouldReturnBadRequest_WhenRequiredFieldsAreMissing()
        {
            var incompleteUser = new User
            {
                FirstName = "Teszt",
                LastName = "Felhasználó",
                Email = "", // kötelező mező hiányzik
                PasswordHash = "Jelszo123",
                Address = "Fő utca 1",
                ShippingAddress = "Mellék utca 2",
                PhoneNumber = "123456789"
            };

            var result = await m_controller.Register(incompleteUser);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var json = JsonSerializer.Serialize(badRequest.Value);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.Equal("Minden mezőt ki kell tölteni!", root.GetProperty("message").GetString());
        }
    }
}
