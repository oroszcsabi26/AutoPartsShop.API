﻿using AutoPartsShop.Core.DTOs;
using AutoPartsShop.Core.Models;
using AutoPartsShop.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AutoPartsShop.API.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration; // Az appsettings.json elérése

        public UserController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Felhasználó regisztrációja
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User newUser)
        {
            // 1. Ellenőrizzük, hogy minden mező ki van-e töltve
            if (string.IsNullOrWhiteSpace(newUser.FirstName) ||
                string.IsNullOrWhiteSpace(newUser.LastName) ||
                string.IsNullOrWhiteSpace(newUser.Email) ||
                string.IsNullOrWhiteSpace(newUser.PasswordHash) ||
                string.IsNullOrWhiteSpace(newUser.Address) ||
                string.IsNullOrWhiteSpace(newUser.ShippingAddress) ||
                string.IsNullOrWhiteSpace(newUser.PhoneNumber))
            {
                return BadRequest(new { message = "Minden mezőt ki kell tölteni!" });
            }

            // 2. Normalizáljuk az e-mail címet (kisbetűssé alakítjuk)
            newUser.Email = newUser.Email.Trim().ToLower();

            // 3. Ellenőrizzük, hogy az e-mail cím már foglalt-e
            bool emailExists = await _context.Users.AnyAsync(u => u.Email == newUser.Email);
            if (emailExists)
            {
                return Conflict(new { message = "Ez az e-mail cím már regisztrálva van!" });
            }

            // 4. Jelszó hash-elése
            newUser.PasswordHash = HashPassword(newUser.PasswordHash);

            // 5. Új felhasználó mentése az adatbázisba
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // 6. Válasz küldése a frontendnek
            return Ok(new
            {
                message = "Sikeres regisztráció!",
                user = new
                {
                    id = newUser.Id,
                    firstName = newUser.FirstName,
                    lastName = newUser.LastName,
                    email = newUser.Email
                }
            });
        }


        // Felhasználó bejelentkezése + JWT token generálás
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Az e-mail és jelszó megadása kötelező!");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return Unauthorized("Hibás e-mail vagy jelszó!");
            }

            // Ellenőrizzük a hash-elt jelszót
            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                return Unauthorized("Hibás e-mail vagy jelszó!");
            }

            // JWT token generálás a bejelentkezett felhasználónak
            var token = GenerateJwtToken(user);

            return Ok(new
            {
                message = "Sikeres bejelentkezés!",
                token, // A generált JWT token
                user = new
                {
                    id = user.Id,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    email = user.Email
                }
            });
        }

        // JWT Token generáló metódus
        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]));

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Egyedi token azonosító
            };

            var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpiryMinutes"])),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Jelszó hash-elő függvény (SHA256 algoritmus)
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        // Jelszóellenőrző metódus
        private bool VerifyPassword(string password, string storedHash)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);
                string hashString = Convert.ToBase64String(hash);
                return hashString == storedHash;
            }
        }

        // Felhasználói profil lekérése (név, email, cím, telefonszám)
        [HttpGet("profile")]
        [Authorize] // Csak bejelentkezett felhasználók érhetik el
        public async Task<IActionResult> GetUserProfile()
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { message = "Felhasználó azonosítása sikertelen!" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "Felhasználó nem található!" });

            return Ok(new
            {
                user.FirstName,
                user.LastName,
                user.Email,
                user.PhoneNumber,
                user.Address, // Számlázási cím
                user.ShippingAddress // Szállítási cím
            });
        }

        //  Felhasználói profil módosítása (név, telefonszám, címek) – DTO használata
        [HttpPut("update-profile")]
        [Authorize]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UserProfileUpdate updatedUserData)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { message = "Felhasználó azonosítása sikertelen!" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "Felhasználó nem található!" });

            // Frissítjük az adatokat (de a PasswordHash érintetlen marad!)
            user.FirstName = updatedUserData.FirstName;
            user.LastName = updatedUserData.LastName;
            user.Email = updatedUserData.Email; // Az email változtatása nem ajánlott, de engedjük most
            user.PhoneNumber = updatedUserData.PhoneNumber;
            user.Address = updatedUserData.Address;
            user.ShippingAddress = updatedUserData.ShippingAddress;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Felhasználói adatok sikeresen frissítve!" });
        }

        // Felhasználó rendeléseinek lekérése
        [HttpGet("my-orders")]
        [Authorize]
        public async Task<IActionResult> GetUserOrders()
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { message = "Felhasználó azonosítása sikertelen!" });

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Ok(orders);
        }

        // Segédfüggvény a bejelentkezett felhasználó azonosítójának lekérésére
        private int? GetUserId()
        {
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : (int?)null;
        }
    }
}
