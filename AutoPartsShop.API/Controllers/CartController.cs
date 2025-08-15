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
    [Authorize] 
    public class CartController : ControllerBase
    {
        private readonly AppDbContext m_context;

        public CartController(AppDbContext p_context)
        {
            m_context = p_context;
        }

        [HttpGet("my-cart")]
        public async Task<ActionResult<IEnumerable<CartItem>>> GetUserCart()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("Felhasználó azonosítása sikertelen!");

            var cart = await m_context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null) return Ok(new List<CartItem>()); 

            return Ok(cart.Items);
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] CartItem p_newItem)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("Felhasználó azonosítása sikertelen!");

            var cart = await GetOrCreateCart(userId.Value);

            if (p_newItem.ItemType != "Part" && p_newItem.ItemType != "Equipment")
                return BadRequest("Hibás terméktípus! Csak 'Part' vagy 'Equipment' lehet.");

            if (p_newItem.ItemType == "Part")
            {
                var part = await m_context.Parts.FirstOrDefaultAsync(p => p.Id == p_newItem.PartId);
                if (part == null) return NotFound("A megadott Part ID nem létezik.");
                p_newItem.Name = part.Name;
                p_newItem.Price = part.Price;
            }
            else if (p_newItem.ItemType == "Equipment")
            {
                var equipment = await m_context.Equipments.FirstOrDefaultAsync(e => e.Id == p_newItem.EquipmentId);
                if (equipment == null) return NotFound("A megadott Equipment ID nem létezik.");
                p_newItem.Name = equipment.Name;
                p_newItem.Price = equipment.Price;
            }

            var existingItem = cart.Items.FirstOrDefault(c =>
                (c.PartId == p_newItem.PartId && p_newItem.ItemType == "Part") ||
                (c.EquipmentId == p_newItem.EquipmentId && p_newItem.ItemType == "Equipment"));

            if (existingItem != null)
            {
                existingItem.Quantity += p_newItem.Quantity;
            }
            else
            {
                p_newItem.CartId = cart.Id;
                cart.Items.Add(p_newItem);
            }

            await m_context.SaveChangesAsync();

            return Ok(new { message = "A termék sikeresen hozzáadva a kosárhoz!", cartItem = p_newItem });
        }

        [HttpPut("update/{cartItemId}/{quantity}")]
        public async Task<IActionResult> UpdateCartItemQuantity(int p_cartItemId, int p_quantity)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("Felhasználó azonosítása sikertelen!");

            var cart = await m_context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null) return NotFound("A felhasználónak nincs kosara!");

            var item = cart.Items.FirstOrDefault(ci => ci.Id == p_cartItemId);
            if (item == null) return NotFound("Nincs ilyen termék a kosárban!");

            item.Quantity = Math.Max(1, p_quantity);
            await m_context.SaveChangesAsync();

            return Ok(new { message = "Termék mennyisége frissítve!", cartItem = item });
        }

        [HttpDelete("remove/{cartItemId}")]
        public async Task<IActionResult> RemoveFromCart(int p_cartItemId)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("Felhasználó azonosítása sikertelen!");

            var cart = await m_context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null) return NotFound("A felhasználónak nincs kosara!");

            var item = cart.Items.FirstOrDefault(ci => ci.Id == p_cartItemId);
            if (item == null) return NotFound("Nincs ilyen termék a kosárban!");

            cart.Items.Remove(item);
            await m_context.SaveChangesAsync();

            return Ok(new { message = "Termék eltávolítva a kosárból!" });
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteCart()
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized("Felhasználó azonosítása sikertelen!");

                var cart = await m_context.Carts.Include(c => c.Items)
                                               .FirstOrDefaultAsync(c => c.UserId == userId);
                if (cart == null) return Ok(new { message = "A kosár nem létezik, nincs mit törölni." });

                m_context.CartItems.RemoveRange(cart.Items);

                m_context.Carts.Remove(cart);

                await m_context.SaveChangesAsync();

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

        private int? GetUserId()
        {
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : (int?)null;
        }

        private async Task<Cart> GetOrCreateCart(int p_userId)
        {
            var cart = await m_context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == p_userId);
            if (cart == null)
            {
                cart = new Cart { UserId = p_userId };
                m_context.Carts.Add(cart);
                await m_context.SaveChangesAsync();
            }
            return cart;
        }
        
        [HttpPost("create")]
        public async Task<ActionResult<Cart>> CreateCart()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized("Felhasználó azonosítása sikertelen!");

            var existingCart = await m_context.Carts.FirstOrDefaultAsync(c => c.UserId == int.Parse(userId));
            if (existingCart != null)
            {
                return BadRequest("A felhasználónak már van kosara!");
            }

            var newCart = new Cart { UserId = int.Parse(userId) };
            m_context.Carts.Add(newCart);
            await m_context.SaveChangesAsync();

            return Ok(new { message = "Kosár sikeresen létrehozva!", cartId = newCart.Id });
        }
    }
}

