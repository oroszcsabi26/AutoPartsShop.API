using AutoPartsShop.Core.DTOs;
using AutoPartsShop.Core.Helpers;
using AutoPartsShop.Core.Models;
using AutoPartsShop.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[Route("api/admin/users")]
[ApiController]
[Authorize] 
public class AdminUsersController : ControllerBase
{
    private readonly AppDbContext m_context;
    private readonly IEmailService m_emailService;

    public AdminUsersController(AppDbContext p_context, IEmailService p_emailService)
    {
        m_context = p_context;
        m_emailService = p_emailService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] string? search,[FromQuery] string? status,[FromQuery] DateTime? from,[FromQuery] DateTime? to)
    {
        var guard = await EnsureAdminAsync();
        if (guard != null) return guard;

        var q = m_context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(u =>
                (u.FirstName + " " + u.LastName).ToLower().Contains(s) ||
                u.Email.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            switch (status.ToLower())
            {
                case "active":
                    q = q.Where(u => u.DeletedAt == null && u.IsActive);
                    break;
                case "locked":
                    q = q.Where(u => u.DeletedAt == null && !u.IsActive);
                    break;
                case "deleted":
                    q = q.Where(u => u.DeletedAt != null);
                    break;
            }
        }

        if (from.HasValue) 
            q = q.Where(u => u.CreatedAt >= from.Value);

        if (to.HasValue) 
            q = q.Where(u => u.CreatedAt <= to.Value);

        var data = await q
            .Select(u => new AdminUserListItemDto
            {
                Id = u.Id,
                FullName = u.FirstName + " " + u.LastName,
                Email = u.Email,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                DeletedAt = u.DeletedAt,
                OrderCount = u.Orders.Count()
            })
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(); 

        return Ok(data);
    }

    [HttpGet("{userId:int}")]
    public async Task<IActionResult> GetUserDetails(int userId,[FromQuery] bool includeOrders = false)
    {
        var guard = await EnsureAdminAsync();
        if (guard != null) return guard;

        IQueryable<User> q = m_context.Users.AsNoTracking().Where(u => u.Id == userId);

        if (includeOrders)
            q = q.Include(u => u.Orders)
                 .ThenInclude(o => o.OrderItems);

        var user = await q.FirstOrDefaultAsync();

        if (user == null) 
            return NotFound("Felhasználó nem található.");

        var dto = new AdminUserDetailsDto
        {
            Id = user.Id,
            FullName = user.FirstName + " " + user.LastName,
            Email = user.Email,
            IsActive = user.IsActive && user.DeletedAt == null,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            DeletedAt = user.DeletedAt,
            OrderCount = includeOrders ? user.Orders.Count : await m_context.Orders.CountAsync(o => o.UserId == user.Id),
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            ShippingAddress = user.ShippingAddress,

            Orders = includeOrders ? user.Orders
                    .OrderByDescending(o => o.OrderDate)
                    .Select(o => new AdminUserOrderDto
                    {
                        Id = o.Id,
                        OrderDate = o.OrderDate,
                        Status = o.Status.ToString(),
                        ItemCount = o.OrderItems.Count,
                        Total = o.OrderItems.Sum(oi => oi.Price * oi.Quantity)
                    })
                    .ToList()
                : null
        };

        return Ok(dto);
    }

    [HttpPut("{userId:int}/deactivate")]
    public async Task<IActionResult> DeactivateUser(int userId)
    {
        var guard = await EnsureAdminAsync();
        if (guard != null) return guard;

        var user = await m_context.Users.FindAsync(userId);
        if (user == null) 
            return NotFound("Felhasználó nem található.");

        user.IsActive = false;
        await m_context.SaveChangesAsync();

        return Ok(new { message = $"Felhasználó ({user.Email}) deaktiválva." });
    }

    [HttpPut("{userId:int}/activate")]
    public async Task<IActionResult> ActivateUser(int userId)
    {
        var guard = await EnsureAdminAsync();
        if (guard != null) return guard;

        var user = await m_context.Users.FindAsync(userId);
        if (user == null) 
            return NotFound("Felhasználó nem található.");

        user.IsActive = true;
        await m_context.SaveChangesAsync();

        return Ok(new { message = $"Felhasználó ({user.Email}) újraaktiválva." });
    }

    private async Task<IActionResult?> EnsureAdminAsync()
    {
        //JWT-ből kiolvassa a bejelentkezett user azonosítóját (NameIdentifier claim).
        var callerId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (callerId == null)
            return Unauthorized("Felhasználó azonosítása sikertelen!");

        //Betölti DB-ből a hívót, és ellenőrzi, admin-e. Ha nem, 403 Forbiddal tér vissza.
        var caller = await m_context.Users.FindAsync(int.Parse(callerId));
        if (caller == null || !caller.IsAdmin)
            return Forbid("Nincs jogosultságod.");

        return null;
    }

    [HttpDelete("{userId:int}")]
    public async Task<IActionResult> SoftDeleteUser(int userId)
    {
        var guard = await EnsureAdminAsync();
        if (guard != null) 
            return guard;

        // ne engedjük, hogy valaki saját magát törölje
        var callerIdStr = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(callerIdStr, out var callerId) && callerId == userId)
            return BadRequest("Saját fiókot nem törölhetsz admin felületről.");

        var user = await m_context.Users
            .Include(u => u.Cart)
            .ThenInclude(c => c.Items)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return NotFound("Felhasználó nem található.");

        if (user.DeletedAt != null)
            return BadRequest("A felhasználó már törölt állapotban van.");

        if(user.IsAdmin)
            return BadRequest("Admin felhasználót nem törölhetsz.");

        // soft delete jelölés
        user.IsActive = false;
        user.DeletedAt = DateTime.UtcNow;

        // opcionális takarítás: kosár és tételei
        if (user.Cart != null)
        {
            m_context.CartItems.RemoveRange(user.Cart.Items);
            m_context.Carts.Remove(user.Cart);
        }

        var orders = await m_context.Orders
        .Where(o => o.UserId == user.Id)
        .ToListAsync();

        if (orders.Count > 0)
        {
            m_context.Orders.RemoveRange(orders);
        }

        await m_context.SaveChangesAsync();

        string subject = "Fiók és rendelések törlése";
        string body = $@"Kedves {user.FirstName} {user.LastName}!

            Tájékoztatjuk, hogy fiókodat töröltük (inaktiváltuk) az AutoPartsShop rendszerében, 
            és a hozzád tartozó kosarat és rendeléseket is eltávolítottuk.

            Ha kérdésed van, kérjük, vedd fel a kapcsolatot ügyfélszolgálatunkkal.

            Üdvözlettel:
            AutoPartsShop";

        try
        {
            await m_emailService.SendEmailAsync(user.Email, subject, body);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Email küldési hiba: {ex.Message}");
        }

        return Ok(new { message = $"Felhasználó ({user.Email}) törölve (soft delete), rendelések és kosár törölve." });
    }
}
