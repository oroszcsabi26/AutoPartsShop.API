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
    [Authorize] 
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext m_context;
        private readonly IEmailService m_emailService;

        public OrderController(AppDbContext context, IEmailService emailService)
        {
            m_context = context;
            m_emailService = emailService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] Order p_orderRequest)
        {
            Console.WriteLine("Rendelés létrehozása...");

            if (!ModelState.IsValid)
                return BadRequest("Érvénytelen rendelési adatok!");

            var userId = GetUserId();
            if (userId == null)
                return Unauthorized("Felhasználó azonosítása sikertelen!");

            var cart = await m_context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.Items.Any())
                return BadRequest("A kosár üres! Nem lehet rendelést leadni.");

            int extraFee = 0;
            if (p_orderRequest.PaymentMethod == PaymentMethod.Készpénz || p_orderRequest.PaymentMethod == PaymentMethod.Bankkártyaátvételkor)
            {
                extraFee = 1000;
            }

            var newOrder = new Order
            {
                UserId = userId.Value,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Feldolgozás,
                ShippingAddress = p_orderRequest.ShippingAddress,
                BillingAddress = p_orderRequest.BillingAddress,
                Comment = p_orderRequest.Comment,
                PaymentMethod = p_orderRequest.PaymentMethod,
                ExtraFee = p_orderRequest.ExtraFee,
                ShippingMethod = p_orderRequest.ShippingMethod,
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

            m_context.Orders.Add(newOrder);
            m_context.CartItems.RemoveRange(cart.Items); 
            m_context.Carts.Remove(cart); 

            await m_context.SaveChangesAsync();

            try
            {
                var user = await m_context.Users.FindAsync(userId);

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

                await m_emailService.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email küldési hiba (rendelés leadásakor): {ex.Message}");
            }

            return Ok(new { message = "Rendelés sikeresen leadva!", orderId = newOrder.Id });
        }

        [HttpGet("user-data")]
        public async Task<IActionResult> GetUserOrderData()
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized("Felhasználó azonosítása sikertelen!");

            var user = await m_context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("Felhasználó nem található!");

            return Ok(new
            {
                name = $"{user.FirstName} {user.LastName}",
                phoneNumber = user.PhoneNumber,
                shippingAddress = user.ShippingAddress,
                billingAddress = user.Address 
            });
        }

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

            var orders = await m_context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Ok(orders);
        }

        [HttpGet("all")]
        [Authorize] 
        public async Task<IActionResult> GetAllOrders()
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized("Felhasználó azonosítása sikertelen!");

            var user = await m_context.Users.FindAsync(userId);
            if (user == null || !user.IsAdmin)
                return Forbid("Nincs jogosultságod az összes rendelés lekérdezésére!");

            var orders = await m_context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Ok(orders);
        }

        [HttpDelete("delete/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteOrder(int p_id)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized("Felhasználó azonosítása sikertelen!");

            var user = await m_context.Users.FindAsync(userId);
            if (user == null || !user.IsAdmin)
                return Forbid("Nincs jogosultságod a rendelés törlésére!");

            var order = await m_context.Orders.Include(o => o.User).FirstOrDefaultAsync(o => o.Id == p_id);
            if (order == null)
                return NotFound($"Nem található rendelés ezzel az ID-vel: {p_id}");

            m_context.Orders.Remove(order);
            await m_context.SaveChangesAsync();

            try
            {
                var orderUser = await m_context.Users.FindAsync(order.UserId);
                await m_emailService.SendEmailAsync(
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
        public async Task<IActionResult> UpdateOrderStatus(int p_orderId, [FromBody] UpdateStatusRequest p_request)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized("Felhasználó azonosítása sikertelen!");

            var user = await m_context.Users.FindAsync(userId);
            if (user == null || !user.IsAdmin)
                return Forbid("Nincs jogosultságod a rendelés módosításához!");

            var order = await m_context.Orders.Include(o => o.User).FirstOrDefaultAsync(o => o.Id == p_orderId);
            if (order == null)
                return NotFound($"A rendelés nem található azonosítóval: {p_orderId}");

            if (!Enum.TryParse<OrderStatus>(p_request.NewStatus, out var parsedStatus))
                return BadRequest($"Érvénytelen rendelés státusz: {p_request.NewStatus}");

            order.Status = parsedStatus;
            await m_context.SaveChangesAsync();

            string subject = "Rendelés státusz frissítve";
            string body = $"Kedves {order.User.FirstName}!\n\n" +
                          $"A(z) #{order.Id} számú rendelésed új státusza: {parsedStatus}.\n\n" +
                          $"Köszönjük, hogy nálunk vásároltál!\nAutoPartsShop";

            try
            {
                await m_emailService.SendEmailAsync(order.User.Email, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email küldési hiba: {ex.Message}");
            }

            return Ok(new { message = "Rendelés státusza frissítve!", newStatus = order.Status });
        }
    }
}
