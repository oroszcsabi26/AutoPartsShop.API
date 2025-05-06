using AutoPartsShop.API.Controllers;
using AutoPartsShop.Core.Models;
using AutoPartsShop.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Xunit;

namespace AutoPartsShop.Tests
{
    public class OrderControllerTests
    {
        private readonly OrderController m_controller;  // a tesztelendő controller
        private readonly AppDbContext m_context; // az adatbázis kontextus

        // a konstruktorban inicializáljuk az adatbázis kontextust és a controller-t
        public OrderControllerTests()
        {
            // InMemory adatbázis beállítása (nemigényel valódi SQL-kapcsolatot)
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;

            // saját dbcontext példány létrehozása
            m_context = new AppDbContext(options);

            // Ordercontroller példányosítása úgy hogy tudja használni az InMemory adatbázist
            m_controller = new OrderController(m_context);

            // Egy mok-olt felhasználót hozunk létre akinek az azonosítója "1". Ez szükséges mert a UserController a User objektumot használja az azonosításhoz. Itt egy bejelentkezett felhasználót szimulálunk.
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]   // Ez az egész tesztelt "felhasználó" — a User, amit a controller használ. A ClaimsPrincipal az az objektum, amit a.NET automatikusan a HttpContext.User - be rak, amikor valódi felhasználó van bejelentkezve.
            {
                new Claim(ClaimTypes.NameIdentifier, "1") // ez azt jelenti hogy ez az aktuális user (UserId) azonosítója.
            }, "mock"));                                    // a mock egy tetszőleges hitelesítési típus, lehetne bármi más is

            // A controllerbe beleinjektáljuk a mock felhasználót, mintha ténylegesen be lenne jelentkezve.
            m_controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task CreateOrder_ShouldCreateOrder_WhenCartIsNotEmpty()
        {
            // A mosk-olt user azonosítója
            var userId = 1;

            // Létrehozunk egy teszt kosarat, amiben van egy alkatrész. Ez szimulálja a kosárba helyezett terméket.
            var cart = new Cart
            {
                UserId = userId,
                Items = new List<CartItem>
                {
                    new CartItem
                    {
                        ItemType = "Part",
                        PartId = 1,
                        Quantity = 2,
                        Price = 5000,
                        Name = "Teszt alkatrész"
                    }
                }
            };

            // A kosarat elmentjük az InMemory adatbázisba
            await m_context.Carts.AddAsync(cart);
            await m_context.SaveChangesAsync();

            // A rendelés kéréshez csak a címeket adjuk meg
            var orderRequest = new Order
            {
                ShippingAddress = "Teszt utca 1",
                BillingAddress = "Számla utca 2",
                Comment = "Kérem gyorsan szállítani."
            };

            // Meghívjuk az OrderController CreateOrder metódusát.
            var result = await m_controller.CreateOrder(orderRequest);

            // Ellenőrizzük hogy a válasz "OK"-e
            var okResult = Assert.IsType<OkObjectResult>(result);

            // A CreateOrder metódus visszatérési értékében (okResult.Value) egy anonim objektum van. Mivel ez nem rendelkezik típussal, nem tudod csak úgy kiolvasni belőle az orderId értéket. Ezért előbb JSON stringgé alakítjuk az objektumot.
            string json = JsonSerializer.Serialize(okResult.Value);
            // A JSON stringet feldolgozza a JsonDocument.Parse() segítségével, és így létrejön egy JSON objektum (root), amin keresztül már lekérdezhetőek a tulajdonságai.
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            // Kiolvassa a JSON-ban található orderId mezőt, és egész számmá (int) konvertálja.
            int orderId = root.GetProperty("orderId").GetInt32();
            Assert.True(orderId > 0);

            // Lekérdezi az adtbázisból a létrehozott rendelést a hozzá tartozó tételekkel együtt.
            var order = await m_context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            // Validálás
            Assert.NotNull(order);  // Tényleg létrejött-e a rendelés
            Assert.Single(order.OrderItems); // Pontosan 1 tétel van-e benne
            Assert.Equal("Teszt alkatrész", order.OrderItems.First().Name); // A tétel neve helyes-e
        }

        [Fact]
        public async Task CreateOrder_ShouldReturnBadRequest_WhenCartIsEmpty()
        {
            // Arrange – bejelentkezett felhasználó (userId = 1) létezik, de az ő kosara üres
            var userId = 1;

            // Üres kosár létrehozása és mentése
            var emptyCart = new Cart
            {
                UserId = userId,
                Items = new List<CartItem>() // nincs benne tétel
            };

            await m_context.Carts.AddAsync(emptyCart);
            await m_context.SaveChangesAsync();

            // Létrehozunk egy rendelési kérelmet (de ez nem számít, mert a kosár üres)
            var orderRequest = new Order
            {
                ShippingAddress = "Teszt utca 1",
                BillingAddress = "Számla utca 2",
                Comment = "Üres kosárból nem lehet rendelni."
            };

            // Act – meghívjuk a CreateOrder metódust
            var result = await m_controller.CreateOrder(orderRequest);

            // Assert – ellenőrizzük, hogy a válasz BadRequest legyen
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("A kosár üres! Nem lehet rendelést leadni.", badRequest.Value);
        }

        [Fact]
        public async Task CreateOrder_ShouldReturnUnauthorized_WhenUserIsNotLoggedIn()
        {
            // Létrehozunk egy controllert, amiben nincs User (nem jelentkezett be senki)
            var controllerWithoutUser = new OrderController(m_context)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // Próbál rendelést létrehozni
            var result = await controllerWithoutUser.CreateOrder(new Order
            {
                ShippingAddress = "Cím",
                BillingAddress = "Számlázási cím"
            });

            // Válasznak UnauthorizedObjectResult-nak kell lennie
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Felhasználó azonosítása sikertelen!", unauthorized.Value);
        }

        [Fact]
        public async Task CreateOrder_ShouldAddNewOrder_WhenPreviousOrdersExist()
        {
            var userId = 1;

            // 1. meglévő rendelés
            var previousOrder = new Order
            {
                UserId = userId,
                ShippingAddress = "Régi cím",
                BillingAddress = "Régi számla",
                OrderDate = DateTime.UtcNow.AddDays(-2),
                OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                    ItemType = "Part",
                    PartId = 99,
                    Quantity = 1,
                    Price = 1000,
                    Name = "Régi tétel"
                    }
                }
            };
            await m_context.Orders.AddAsync(previousOrder);

            // 2. új kosár a következő rendeléshez
            var cart = new Cart
            {
                UserId = userId,
                Items = new List<CartItem>
                {
                    new CartItem
                    {
                    ItemType = "Part",
                    PartId = 1,
                    Quantity = 2,
                    Price = 5000,
                    Name = "Új tétel"
                    }
                }
            };
            await m_context.Carts.AddAsync(cart);
            await m_context.SaveChangesAsync();

            var orderRequest = new Order
            {
                ShippingAddress = "Új cím",
                BillingAddress = "Új számla"
            };

            // 3. új rendelés leadása
            await m_controller.CreateOrder(orderRequest);

            var userOrders = await m_context.Orders
                .Where(o => o.UserId == userId)
                .ToListAsync();

            // Két rendelésnek kell lennie: régi + új
            Assert.Equal(2, userOrders.Count);
            Assert.Contains(userOrders, o => o.ShippingAddress == "Új cím");
            Assert.Contains(userOrders, o => o.ShippingAddress == "Régi cím");
        }

        [Fact]
        public async Task GetUserOrders_ShouldReturnUnauthorized_WhenUserIsNotLoggedIn()
        {
            // Létrehozunk egy új controllert, aminek nincs bejelentkezett felhasználója
            var controllerWithoutUser = new OrderController(m_context)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext() // => User = null
                }
            };

            // Meghívjuk a GetUserOrders metódust
            var result = await controllerWithoutUser.GetUserOrders();

            // Ellenőrizzük, hogy a válasz UnauthorizedObjectResult típusú, és megfelelő szöveget tartalmaz
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Felhasználó azonosítása sikertelen!", unauthorizedResult.Value);
        }

        [Fact]
        public async Task GetUserOrders_ShouldReturnEmptyList_WhenUserHasNoOrders()
        {
            // Arrange – a mockolt userId = 1, de nincs rendelése ebben az adatbázisban
            var userId = 1;

            // Biztosítjuk, hogy nincs rendelés az adott userhez
            var existingOrders = await m_context.Orders
                .Where(o => o.UserId == userId)
                .ToListAsync();

            if (existingOrders.Any())
            {
                m_context.Orders.RemoveRange(existingOrders);
                await m_context.SaveChangesAsync();
            }

            // Act – meghívjuk a metódust
            var result = await m_controller.GetUserOrders();

            // Assert – a visszatérési típus legyen OK, de az érték egy üres lista
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedOrders = Assert.IsAssignableFrom<IEnumerable<Order>>(okResult.Value);

            Assert.Empty(returnedOrders); // az eredmény egy üres lista kell legyen
        }
    }
}
