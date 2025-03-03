using AutoPartsShop.Core.Models;
using AutoPartsShop.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AutoPartsShop.API.Controllers
{
    [Route("api/orders")]
    [ApiController]
    [Authorize] // Csak bejelentkezett felhasználók használhatják az Order API-t
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        // Rendelés létrehozása a bejelentkezett felhasználó számára
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] Order orderRequest)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized("Felhasználó azonosítása sikertelen!");

            // Ellenőrizzük, hogy a felhasználónak van-e kosara
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.Items.Any())
                return BadRequest("A kosár üres! Nem lehet rendelést leadni.");

            // új rendelés létrehozása
            var newOrder = new Order
            {
                UserId = userId.Value,
                OrderDate = DateTime.UtcNow,
                Status = "Feldolgozás alatt",
                ShippingAddress = orderRequest.ShippingAddress, // Felhasználó által megadott
                BillingAddress = orderRequest.BillingAddress,   // Felhasználó által megadott
                Comment = orderRequest.Comment,                 // ÚJ: Megjegyzés
                OrderItems = cart.Items.Select(ci => new OrderItem
                {
                    ItemType = ci.ItemType,
                    PartId = ci.PartId,
                    EquipmentId = ci.EquipmentId,
                    Quantity = ci.Quantity,
                    Price = ci.Price,
                    Name = ci.Name
                }).ToList()
            };

            _context.Orders.Add(newOrder);
            _context.CartItems.RemoveRange(cart.Items); // Kosár kiürítése
            _context.Carts.Remove(cart); // Kosár törlése

            await _context.SaveChangesAsync();

            return Ok(new { message = "Rendelés sikeresen leadva!", orderId = newOrder.Id });
        }

        // A bejelentkezett felhasználó rendelési adatainak betöltése
        [HttpGet("user-data")]
        public async Task<IActionResult> GetUserOrderData()
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized("Felhasználó azonosítása sikertelen!");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("Felhasználó nem található!");

            return Ok(new
            {
                name = $"{user.FirstName} {user.LastName}",
                phoneNumber = user.PhoneNumber,
                shippingAddress = user.ShippingAddress,
                billingAddress = user.Address // Alapértelmezett számlázási cím
            });
        }

        // Segédfüggvény a bejelentkezett felhasználó azonosítójának lekérésére
        private int? GetUserId()
        {
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : (int?)null;
        }
    }
}
