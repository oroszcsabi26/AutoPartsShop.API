using AutoPartsShop.Core.Enums;
using AutoPartsShop.Core.Helpers;
using AutoPartsShop.Core.Models;
using AutoPartsShop.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AutoPartsShop.API.Controllers
{
    public class UpdateStatusRequest
    {
        public string NewStatus { get; set; } = string.Empty;
    }

    [Route("api/orders")]
    [ApiController]
    [Authorize] // Csak bejelentkezett felhasználók használhatják az Order API-t
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public OrderController(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // Rendelés létrehozása a bejelentkezett felhasználó számára
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] Order orderRequest)
        {
            Console.WriteLine("Rendelés létrehozása...");

            if (!ModelState.IsValid)
                return BadRequest("Érvénytelen rendelési adatok!");

            var userId = GetUserId();
            if (userId == null)
                return Unauthorized("Felhasználó azonosítása sikertelen!");

            // Ellenőrizzük, hogy a felhasználónak van-e kosara
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.Items.Any())
                return BadRequest("A kosár üres! Nem lehet rendelést leadni.");

            int extraFee = 0;
            if (orderRequest.PaymentMethod == PaymentMethod.Készpénz || orderRequest.PaymentMethod == PaymentMethod.Bankkártyaátvételkor)
            {
                extraFee = 1000;
            }

            // új rendelés létrehozása
            var newOrder = new Order
            {
                UserId = userId.Value,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Feldolgozás,
                ShippingAddress = orderRequest.ShippingAddress,
                BillingAddress = orderRequest.BillingAddress,
                Comment = orderRequest.Comment,
                PaymentMethod = orderRequest.PaymentMethod,
                ExtraFee = orderRequest.ExtraFee,
                ShippingMethod = orderRequest.ShippingMethod,
                OrderItems = cart.Items.Select(ci => new OrderItem
                {
                    ItemType = ci.ItemType,
                    PartId = ci.PartId,
                    EquipmentId = ci.EquipmentId,
                    Quantity = ci.Quantity,
                    Price = ci.Price + extraFee,
                    Name = ci.Name
                }).ToList()
            };

            _context.Orders.Add(newOrder);
            _context.CartItems.RemoveRange(cart.Items); // Kosár kiürítése
            _context.Carts.Remove(cart); // Kosár törlése

            await _context.SaveChangesAsync();

            try
            {
                var user = await _context.Users.FindAsync(userId);

                // Tétellista összeállítása
                string itemList = string.Join("\n", newOrder.OrderItems.Select(item =>
                    $"- {item.Name} ({item.Quantity} db) - {item.Price} Ft"
                ));

                string subject = "Rendelés megerősítve";
                string body = $"Kedves {user.FirstName}!\n\n" +
                              $"Köszönjük, hogy rendelést adtál le webáruházunkban!\n" +
                              $"A rendelés azonosítója: #{newOrder.Id}\n" +
                              $"Státusz: {newOrder.Status}\n\n" +
                              $"Rendelt tételek:\n{itemList}\n\n" +
                              $"Szállítási cím: {newOrder.ShippingAddress}\n" +
                              $"Számlázási cím: {newOrder.BillingAddress}\n\n" +
                              $"Szállítási mód: {newOrder.ShippingMethod}\n" +
                              $"Fizetési mód: {newOrder.PaymentMethod}\n\n" +
                              $"Üdvözlettel:\nAutoPartsShop";

                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email küldési hiba (rendelés leadásakor): {ex.Message}");
            }

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

        [HttpGet("my-orders")]
        public async Task<IActionResult> GetUserOrders()
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized("Felhasználó azonosítása sikertelen!");

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Ok(orders);
        }

        // Összes rendelés lekérése admin számára
        [HttpGet("all")]
        [Authorize] // Csak bejelentkezett adminok érhetik el
        public async Task<IActionResult> GetAllOrders()
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized("Felhasználó azonosítása sikertelen!");

            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsAdmin)
                return Forbid("Nincs jogosultságod az összes rendelés lekérdezésére!");

            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Ok(orders);
        }

        // Rendelés törlése (csak admin)
        [HttpDelete("delete/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized("Felhasználó azonosítása sikertelen!");

            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsAdmin)
                return Forbid("Nincs jogosultságod a rendelés törlésére!");

            var order = await _context.Orders.Include(o => o.User).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
                return NotFound($"Nem található rendelés ezzel az ID-vel: {id}");

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            try
            {
                var orderUser = await _context.Users.FindAsync(order.UserId);
                await _emailService.SendEmailAsync(
                    orderUser.Email,
                    "Rendelés törölve",
                    $"Kedves {orderUser.FirstName}!\n\nA(z) #{order.Id} számú rendelésed törlésre került.\n\nHa kérdésed van, keress minket bizalommal.\nÜdvözlettel:\nAutoPartsShop"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email küldési hiba (törlés után): {ex.Message}");
            }

            return Ok(new { message = "Rendelés törölve!" });
        }

        [HttpPut("update-status/{orderId}")]
        [Authorize]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] UpdateStatusRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized("Felhasználó azonosítása sikertelen!");

            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsAdmin)
                return Forbid("Nincs jogosultságod a rendelés módosításához!");

            var order = await _context.Orders.Include(o => o.User).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
                return NotFound($"A rendelés nem található azonosítóval: {orderId}");

            if (!Enum.TryParse<OrderStatus>(request.NewStatus, out var parsedStatus))
                return BadRequest($"Érvénytelen rendelés státusz: {request.NewStatus}");

            order.Status = parsedStatus;
            await _context.SaveChangesAsync();

            string subject = "Rendelés státusz frissítve";
            string body = $"Kedves {order.User.FirstName}!\n\n" +
                          $"A(z) #{order.Id} számú rendelésed új státusza: {parsedStatus}.\n\n" +
                          $"Köszönjük, hogy nálunk vásároltál!\nAutoPartsShop";

            try
            {
                await _emailService.SendEmailAsync(order.User.Email, subject, body);
            }
            catch (Exception ex)
            {
                // Logoljuk az email küldési hibát, de ne akadályozzuk meg a státusz frissítését
                Console.WriteLine($"Email küldési hiba: {ex.Message}");
            }

            return Ok(new { message = "Rendelés státusza frissítve!", newStatus = order.Status });
        }
    }
}
