using AutoPartsShop.Core.Models;
using AutoPartsShop.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AutoPartsShop.API.Controllers
{
    [Route("api/cart")]
    [ApiController]
    [Authorize] // Bejelentkezés szükséges minden végponthoz
    public class CartController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        // A bejelentkezett felhasználó saját kosarának lekérése
        [HttpGet("my-cart")]
        public async Task<ActionResult<IEnumerable<CartItem>>> GetUserCart()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("Felhasználó azonosítása sikertelen!");

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null) return Ok(new List<CartItem>()); // Üres listát ad vissza, nem 404-et

            return Ok(cart.Items);
        }

        // Termék hozzáadása a saját kosárhoz
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] CartItem newItem)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("Felhasználó azonosítása sikertelen!");

            var cart = await GetOrCreateCart(userId.Value);

            if (newItem.ItemType != "Part" && newItem.ItemType != "Equipment")
                return BadRequest("Hibás terméktípus! Csak 'Part' vagy 'Equipment' lehet.");

            if (newItem.ItemType == "Part")
            {
                var part = await _context.Parts.FirstOrDefaultAsync(p => p.Id == newItem.PartId);
                if (part == null) return NotFound("A megadott Part ID nem létezik.");
                newItem.Name = part.Name;
                newItem.Price = part.Price;
            }
            else if (newItem.ItemType == "Equipment")
            {
                var equipment = await _context.Equipments.FirstOrDefaultAsync(e => e.Id == newItem.EquipmentId);
                if (equipment == null) return NotFound("A megadott Equipment ID nem létezik.");
                newItem.Name = equipment.Name;
                newItem.Price = equipment.Price;
            }

            var existingItem = cart.Items.FirstOrDefault(c =>
                (c.PartId == newItem.PartId && newItem.ItemType == "Part") ||
                (c.EquipmentId == newItem.EquipmentId && newItem.ItemType == "Equipment"));

            if (existingItem != null)
            {
                existingItem.Quantity += newItem.Quantity;
            }
            else
            {
                newItem.CartId = cart.Id;
                cart.Items.Add(newItem);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "A termék sikeresen hozzáadva a kosárhoz!", cartItem = newItem });
        }

        // Kosárban lévő termék mennyiségének módosítása
        [HttpPut("update/{cartItemId}/{quantity}")]
        public async Task<IActionResult> UpdateCartItemQuantity(int cartItemId, int quantity)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("Felhasználó azonosítása sikertelen!");

            var cart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null) return NotFound("A felhasználónak nincs kosara!");

            var item = cart.Items.FirstOrDefault(ci => ci.Id == cartItemId);
            if (item == null) return NotFound("Nincs ilyen termék a kosárban!");

            item.Quantity = Math.Max(1, quantity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Termék mennyisége frissítve!", cartItem = item });
        }

        // Termék eltávolítása a saját kosárból
        [HttpDelete("remove/{cartItemId}")]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("Felhasználó azonosítása sikertelen!");

            var cart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null) return NotFound("A felhasználónak nincs kosara!");

            var item = cart.Items.FirstOrDefault(ci => ci.Id == cartItemId);
            if (item == null) return NotFound("Nincs ilyen termék a kosárban!");

            cart.Items.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Termék eltávolítva a kosárból!" });
        }

        // Saját kosár teljes törlése
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteCart()
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized("Felhasználó azonosítása sikertelen!");

                var cart = await _context.Carts.Include(c => c.Items)
                                               .FirstOrDefaultAsync(c => c.UserId == userId);
                if (cart == null) return Ok(new { message = "A kosár nem létezik, nincs mit törölni." });

                // Előbb töröljük az összes tételt a kosárból
                _context.CartItems.RemoveRange(cart.Items);

                // Majd töröljük magát a kosarat
                _context.Carts.Remove(cart);

                // 🔹 Adatbázis mentése
                await _context.SaveChangesAsync();

                return Ok(new { message = "A kosár sikeresen törölve!" });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Console.WriteLine($"⚠️ DbUpdateConcurrencyException hiba történt: {ex.Message}");
                return StatusCode(500, "Adatbázis ütközés történt a törlés során.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Hiba történt a kosár törlésekor: {ex.Message}");
                return StatusCode(500, "Belső szerverhiba történt.");
            }
        }



        // Felhasználói azonosító lekérése a JWT tokenből
        private int? GetUserId()
        {
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : (int?)null;
        }

        // Ha nincs kosár, akkor létrehozzuk és visszaadjuk
        private async Task<Cart> GetOrCreateCart(int userId)
        {
            var cart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }
            return cart;
        }
        
        //Ideiglenes végpont kosár létrehozásához
        [HttpPost("create")]
        public async Task<ActionResult<Cart>> CreateCart()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized("Felhasználó azonosítása sikertelen!");

            // Ellenőrizzük, hogy a felhasználónak már van-e kosara
            var existingCart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == int.Parse(userId));
            if (existingCart != null)
            {
                return BadRequest("A felhasználónak már van kosara!");
            }

            // Új kosár létrehozása
            var newCart = new Cart { UserId = int.Parse(userId) };
            _context.Carts.Add(newCart);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Kosár sikeresen létrehozva!", cartId = newCart.Id });
        }
    }
}

