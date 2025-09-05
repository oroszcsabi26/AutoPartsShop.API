using AutoPartsShop.Core.DTOs;
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
using AutoPartsShop.Core.Helpers;
using Microsoft.AspNetCore.WebUtilities;

namespace AutoPartsShop.API.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext m_context;
        private readonly IConfiguration m_configuration; // Az appsettings.json elérése
        private readonly IEmailService m_emailService;

        public UserController(AppDbContext p_context, IConfiguration p_configuration, IEmailService p_emailService)
        {
            m_context = p_context;
            m_configuration = p_configuration;
            m_emailService = p_emailService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User p_newUser)
        {
            if (string.IsNullOrWhiteSpace(p_newUser.FirstName) ||
                string.IsNullOrWhiteSpace(p_newUser.LastName) ||
                string.IsNullOrWhiteSpace(p_newUser.Email) ||
                string.IsNullOrWhiteSpace(p_newUser.PasswordHash) ||
                string.IsNullOrWhiteSpace(p_newUser.Address) ||
                string.IsNullOrWhiteSpace(p_newUser.ShippingAddress) ||
                string.IsNullOrWhiteSpace(p_newUser.PhoneNumber))
            {
                return BadRequest(new { message = "Minden mezőt ki kell tölteni!" });
            }

            p_newUser.Email = p_newUser.Email.Trim().ToLower();

            bool emailExists = await m_context.Users.AnyAsync(u => u.Email == p_newUser.Email);
            if (emailExists)
            {
                return Conflict(new { message = "Ez az e-mail cím már regisztrálva van!" });
            }

            p_newUser.PasswordHash = PasswordHelper.HashPassword(p_newUser.PasswordHash);

            m_context.Users.Add(p_newUser);
            p_newUser.CreatedAt = DateTime.UtcNow;
            p_newUser.IsActive = true;
            await m_context.SaveChangesAsync();

            return Ok(new
            {
                message = "Sikeres regisztráció!",
                user = new
                {
                    id = p_newUser.Id,
                    firstName = p_newUser.FirstName,
                    lastName = p_newUser.LastName,
                    email = p_newUser.Email
                }
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest p_request)
        {
            if (string.IsNullOrWhiteSpace(p_request.Email) || string.IsNullOrWhiteSpace(p_request.Password))
            {
                return BadRequest("Az e-mail és jelszó megadása kötelező!");
            }

            var email = p_request.Email.Trim().ToLower();
            var user = await m_context.Users.FirstOrDefaultAsync(u => u.Email == email);       
            if (user == null)
            {
                return Unauthorized("Hibás e-mail vagy jelszó!");
            }

            if (user.DeletedAt != null)
            {
                return Forbid("A fiók törölve lett, a bejelentkezés nem engedélyezett.");
            }
            if (!user.IsActive)
            {
                return Forbid("A fiók deaktiválva van. Kérjük, vedd fel a kapcsolatot az ügyfélszolgálattal.");
            }

            if (!VerifyPassword(p_request.Password, user.PasswordHash))
            {
                return Unauthorized("Hibás e-mail vagy jelszó!");
            }

            user.LastLoginAt = DateTime.UtcNow;
            await m_context.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            return Ok(new
            {
                message = "Sikeres bejelentkezés!",
                token, 
                user = new
                {
                    id = user.Id,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    email = user.Email,
                    isAdmin = user.IsAdmin
                }
            });
        }

        private string GenerateJwtToken(User p_user)
        {
            var jwtSettings = m_configuration.GetSection("JwtSettings");
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]));

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, p_user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, p_user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) 
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

        private bool VerifyPassword(string p_password, string p_storedHash)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(p_password);
                byte[] hash = sha256.ComputeHash(bytes);
                string hashString = Convert.ToBase64String(hash);
                return hashString == p_storedHash;
            }
        }

        [HttpGet("profile")]
        [Authorize] 
        public async Task<IActionResult> GetUserProfile()
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { message = "Felhasználó azonosítása sikertelen!" });

            var user = await m_context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "Felhasználó nem található!" });

            return Ok(new
            {
                user.FirstName,
                user.LastName,
                user.Email,
                user.PhoneNumber,
                user.Address, 
                user.ShippingAddress 
            });
        }

        [HttpPut("update-profile")]
        [Authorize]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UserProfileUpdate p_updatedUserData)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { message = "Felhasználó azonosítása sikertelen!" });

            var user = await m_context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "Felhasználó nem található!" });

            user.FirstName = p_updatedUserData.FirstName;
            user.LastName = p_updatedUserData.LastName;
            user.Email = p_updatedUserData.Email; 
            user.PhoneNumber = p_updatedUserData.PhoneNumber;
            user.Address = p_updatedUserData.Address;
            user.ShippingAddress = p_updatedUserData.ShippingAddress;

            m_context.Users.Update(user);
            await m_context.SaveChangesAsync();

            return Ok(new { message = "Felhasználói adatok sikeresen frissítve!" });
        }

        [HttpGet("my-orders")]
        [Authorize]
        public async Task<IActionResult> GetUserOrders()
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { message = "Felhasználó azonosítása sikertelen!" });

            var orders = await m_context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Ok(orders);
        }

        private int? GetUserId()
        {
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : (int?)null;
        }

        [HttpPost("create-admin")]
        [Authorize] 
        public async Task<IActionResult> CreateAdmin([FromBody] AdminCreateRequest p_request)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized("Felhasználó azonosítása sikertelen!");

            var requestingUser = await m_context.Users.FindAsync(userId);
            if (requestingUser == null || !requestingUser.IsAdmin)
                return Forbid("Nincs jogosultságod admin létrehozására!");

            bool emailExists = await m_context.Users.AnyAsync(u => u.Email == p_request.Email);
            if (emailExists)
                return Conflict("Ez az e-mail cím már regisztrálva van!");

            var newAdmin = new User
            {
                FirstName = p_request.FirstName,
                LastName = p_request.LastName,
                Email = p_request.Email,
                PasswordHash = PasswordHelper.HashPassword(p_request.Password), 
                Address = p_request.Address,
                ShippingAddress = p_request.ShippingAddress,
                PhoneNumber = p_request.PhoneNumber,
                IsAdmin = true 
            };

            m_context.Users.Add(newAdmin);
            await m_context.SaveChangesAsync();

            return Ok(new { message = "Új admin sikeresen létrehozva!", adminId = newAdmin.Id });
        }

        [HttpGet("admins")]
        [Authorize] 
        public async Task<IActionResult> GetAdmins()
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized("Felhasználó azonosítása sikertelen!");

            var requestingUser = await m_context.Users.FindAsync(userId);
            if (requestingUser == null || !requestingUser.IsAdmin)
                return Forbid("Nincs jogosultságod az adminok listázására!");

            var admins = await m_context.Users
                .Where(u => u.IsAdmin)
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.PhoneNumber,
                    u.Address
                })
                .ToListAsync();

            return Ok(admins);
        }

        [HttpPut("update-admin-profile")]
        [Authorize] 
        public async Task<IActionResult> UpdateAdminProfile([FromBody] UserProfileUpdate p_updatedAdminData)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized("Felhasználó azonosítása sikertelen!");

            var user = await m_context.Users.FindAsync(userId);
            if (user == null || !user.IsAdmin)
                return Forbid("Nincs jogosultságod admin profil módosítására!");

            user.FirstName = p_updatedAdminData.FirstName;
            user.LastName = p_updatedAdminData.LastName;
            user.Email = p_updatedAdminData.Email;
            user.PhoneNumber = p_updatedAdminData.PhoneNumber;
            user.Address = p_updatedAdminData.Address;
            user.ShippingAddress = p_updatedAdminData.ShippingAddress;

            m_context.Users.Update(user);
            await m_context.SaveChangesAsync();

            return Ok(new { message = "Admin adatai sikeresen frissítve!" });
        }

        [HttpPost("reset-request")]
        [AllowAnonymous] // a login oldalon nem bejelentkezett user is kérhet resetet
        public async Task<IActionResult> RequestPasswordReset([FromBody] ResetPasswordRequestDto p_dto)
        {
            if (!ModelState.IsValid)
                return BadRequest("Érvénytelen kérés.");

            var email = p_dto.Email.Trim().ToLower();

            var user = await m_context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.DeletedAt == null);

            // Mindig ugyanezt a választ adjuk vissza, hogy ne lehessen "email létezést" szondázni.
            var genericOk = Ok(new { message = "Ha az e-mail cím létezik a rendszerben, küldtünk rá egy visszaállítási linket." });

            if (user == null)
                return genericOk;

            var oldTokens = m_context.PasswordResetTokens
                .Where(t => t.UserId == user.Id && (t.UsedAt != null || t.ExpiresAt < DateTime.UtcNow));
                m_context.PasswordResetTokens.RemoveRange(oldTokens);

            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            var token = WebEncoders.Base64UrlEncode(bytes);

            var minutes = m_configuration.GetValue<int?>("PasswordReset:TokenMinutes") ?? 60;
            var prt = new PasswordResetToken
            {
                UserId = user.Id,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(minutes),
                CreatedAt = DateTime.UtcNow,
                UsedAt = null
            };

            m_context.PasswordResetTokens.Add(prt);
            await m_context.SaveChangesAsync();

            var baseUrl = m_configuration["PasswordReset:FrontendBaseUrl"]
                          ?? "http://localhost:4200/reset-password";

            var resetLink = $"{baseUrl}?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";

            try
            {
                var subject = "Jelszó visszaállítása";
                var body =
                    $"Kedves {user.FirstName} {user.LastName}!\n\n" +
                    $"A jelszavad visszaállításához kattints az alábbi linkre (a link {minutes} percig érvényes):\n\n" +
                    $"{resetLink}\n\n" +
                    $"Ha nem te kérted, hagyd figyelmen kívül ezt az üzenetet.\n\n" +
                    $"Üdvözlettel:\nAutoPartsShop";

                await m_emailService.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ResetRequest] E-mail küldési hiba: {ex.Message}");
            }

            return genericOk;
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmPasswordReset([FromBody] ResetPasswordConfirmDto p_dto)
        {
            if (!ModelState.IsValid)
                return BadRequest("Érvénytelen adatok.");

            var email = p_dto.Email.Trim().ToLower();

            var user = await m_context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.DeletedAt == null);

            var genericError = BadRequest("A link érvénytelen vagy lejárt.");

            if (user == null)
                return genericError;

            var now = DateTime.UtcNow;
            var tokenRow = await m_context.PasswordResetTokens
                .FirstOrDefaultAsync(t =>
                    t.UserId == user.Id &&
                    t.Token == p_dto.Token &&
                    t.UsedAt == null &&
                    t.ExpiresAt > now);

            if (tokenRow == null)
                return genericError;

            user.PasswordHash = PasswordHelper.HashPassword(p_dto.NewPassword);

            tokenRow.UsedAt = now;

            var otherActiveTokens = m_context.PasswordResetTokens
                .Where(t => t.UserId == user.Id && t.UsedAt == null && t.Id != tokenRow.Id);

            await otherActiveTokens.ForEachAsync(t => t.UsedAt = now);
            await m_context.SaveChangesAsync();

            return Ok(new { message = "A jelszó sikeresen frissült. Most már bejelentkezhetsz az új jelszóval." });
        }
    }
}
