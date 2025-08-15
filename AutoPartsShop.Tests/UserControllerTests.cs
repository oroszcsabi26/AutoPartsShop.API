using AutoPartsShop.API.Controllers;
using AutoPartsShop.Core.DTOs;
using AutoPartsShop.Core.Helpers;
using AutoPartsShop.Core.Models;
using AutoPartsShop.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

//JELENLEG NEM MŰKÖDŐ KÓDRÉSZ

namespace AutoPartsShop.Tests
{
    public class UserControllerTests
    {
        private readonly AppDbContext _context;
        private readonly UserController _controller;

        public UserControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("UserTestsDb")
                .Options;

            _context = new AppDbContext(options);

            // Mock konfiguráció a JWT-hez
            var mockConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "JwtSettings:SecretKey", "MySuperSecretKeyForTesting1234567890" },
                    { "JwtSettings:Issuer", "TestIssuer" },
                    { "JwtSettings:Audience", "TestAudience" },
                    { "JwtSettings:ExpiryMinutes", "60" }
                })
                .Build();

            _controller = new UserController(_context, mockConfig);
        }

        [Fact]
        public async Task Register_ShouldCreateUser_WhenDataIsValid()
        {
            // Arrange – új felhasználó adatai
            var newUser = new User
            {
                FirstName = "Teszt",
                LastName = "Felhasználó",
                Email = "teszt@example.com",
                PasswordHash = "Jelszo123", // A controller fogja hash-elni!
                Address = "Fő utca 1",
                ShippingAddress = "Mellék utca 2",
                PhoneNumber = "123456789"
            };

            // Act – regisztráció meghívása
            var result = await _controller.Register(newUser);

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
        }

        [Fact]
        public async Task Register_ShouldReturnBadRequest_WhenRequiredFieldsAreMissing()
        {
            // Arrange – hiányzó mező: Email
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

            // Act
            var result = await _controller.Register(incompleteUser);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var json = JsonSerializer.Serialize(badRequest.Value);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.Equal("Minden mezőt ki kell tölteni!", root.GetProperty("message").GetString());
        }

        [Fact]
        public async Task Login_ShouldReturnToken_WhenCredentialsAreValid()
        {
            // Arrange – létrehozunk egy felhasználót
            var plainPassword = "TesztJelszo123";
            var user = new User
            {
                FirstName = "Béla",
                LastName = "Teszt",
                Email = "bela@example.com",
                PasswordHash = PasswordHelper.HashPassword(plainPassword),
                Address = "Cím 1",
                ShippingAddress = "Szállítási cím",
                PhoneNumber = "987654321"
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var loginRequest = new UserLoginRequest
            {
                Email = "bela@example.com",
                Password = plainPassword
            };

            // Act
            var result = await _controller.Login(loginRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(okResult.Value);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.Equal("Sikeres bejelentkezés!", root.GetProperty("message").GetString());
            Assert.False(string.IsNullOrEmpty(root.GetProperty("token").GetString()));
            Assert.Equal("bela@example.com", root.GetProperty("user").GetProperty("email").GetString());
        }

        [Fact]
        public async Task Login_ShouldReturnUnauthorized_WhenPasswordIsWrong()
        {
            // Arrange – létrehozunk egy felhasználót helyes hash-elt jelszóval
            var correctPassword = "HelyesJelszo123";
            var user = new User
            {
                FirstName = "Teszt",
                LastName = "Felhasználó",
                Email = "rosszjelszo@example.com",
                PasswordHash = PasswordHelper.HashPassword(correctPassword),
                Address = "Teszt utca 10",
                ShippingAddress = "Másik utca 2",
                PhoneNumber = "987654321"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Hibás jelszót próbálunk meg
            var loginRequest = new UserLoginRequest
            {
                Email = "rosszjelszo@example.com",
                Password = "HibásJelszo999"
            };

            // Act – meghívjuk a Login metódust
            var result = await _controller.Login(loginRequest);

            // Assert – Unauthorized választ várunk
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Hibás e-mail vagy jelszó!", unauthorized.Value);
        }
    }
}
