using AutoPartsShop.API.Controllers;
using AutoPartsShop.Core.Enums;
using AutoPartsShop.Core.Models;
using AutoPartsShop.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using AutoPartsShop.Tests.Unit.Helpers;

namespace AutoPartsShop.Tests.Unit.Controllers
{
    public class OrderControllerTests
    {
        private readonly OrderController m_controller;  // a tesztelendő controller
        private readonly AppDbContext m_context; // az adatbázis kontextus
        private readonly FakeEmailService m_fakeEmailService;

        private readonly int m_testUserId = 1; // a teszt felhasználó azonosítója
        private readonly int m_adminUserId = 2;      // admin user
        private readonly int m_orderOwnerUserId = 3; // akinek a rendelését töröljük/módosítjuk

        // a konstruktorban inicializáljuk az adatbázis kontextust és a controller-t
        public OrderControllerTests()
        {
            // InMemory adatbázis beállítása (nemigényel valódi SQL-kapcsolatot)
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"OrderTestsDb_{Guid.NewGuid()}")
                .Options;

            // saját dbcontext példány létrehozása
            m_context = new AppDbContext(options);
            m_fakeEmailService = new FakeEmailService();
            m_controller = new OrderController(m_context, m_fakeEmailService); // Ordercontroller példányosítása úgy hogy tudja használni az InMemory adatbázist

            // Egy mok-olt felhasználót hozunk létre akinek az azonosítója "1". Ez szükséges mert a UserController a User objektumot használja az azonosításhoz. Itt egy bejelentkezett felhasználót szimulálunk.
            m_context.Users.Add(new User
            {
                Id = m_testUserId,
                FirstName = "Teszt",
                LastName = "Felhasznalo",
                Email = "teszt@example.com",
                IsActive = true,
                IsAdmin = false
            });

            m_context.Users.Add(new User
            {
                Id = m_adminUserId,
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@example.com",
                IsActive = true,
                IsAdmin = true
            });

            // seed: rendelés tulaj user (Id=3) – hogy a törlés/státuszváltás e-mailt neki küldhessük
            m_context.Users.Add(new User
            {
                Id = m_orderOwnerUserId,
                FirstName = "Rendelo",
                LastName = "Ugyfel",
                Email = "order.owner@example.com",
                IsActive = true,
                IsAdmin = false
            });

            m_context.SaveChanges();

            TestHttpContextHelper.AttachUser(m_controller, m_testUserId);
        }

        [Fact]
        public async Task CreateOrder_ShouldCreateOrder_WhenCartIsNotEmpty()
        {
            // Létrehozunk egy teszt kosarat, amiben van egy alkatrész. Ez szimulálja a kosárba helyezett terméket.
            var cart = new Cart
            {
                UserId = m_testUserId,
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
                Comment = "Kérem gyorsan szállítani.",
                ShippingMethod = ShippingMethod.SzemélyesÁtvétel
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
            Assert.Single(m_fakeEmailService.Sent); // Egy email lett elküldve
            Assert.Equal("teszt@example.com", m_fakeEmailService.Sent[0].To); // A címzett helyes-e
        }

        [Fact]
        public async Task CreateOrder_ShouldReturnBadRequest_WhenCartIsEmpty()
        {
            // Üres kosár létrehozása és mentése
            var emptyCart = new Cart
            {
                UserId = m_testUserId,
                Items = new List<CartItem>() // nincs benne tétel
            };

            await m_context.Carts.AddAsync(emptyCart);
            await m_context.SaveChangesAsync();

            // Létrehozunk egy rendelési kérelmet (de ez nem számít, mert a kosár üres)
            var orderRequest = new Order
            {
                ShippingAddress = "Teszt utca 1",
                BillingAddress = "Számla utca 2",
                Comment = "Üres kosárból nem lehet rendelni.",
                ShippingMethod = ShippingMethod.SzemélyesÁtvétel
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
            var controllerWithoutUser = new OrderController(m_context, m_fakeEmailService)
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
                BillingAddress = "Számlázási cím",
                ShippingMethod = ShippingMethod.SzemélyesÁtvétel
            });

            // Válasznak UnauthorizedObjectResult-nak kell lennie
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Felhasználó azonosítása sikertelen!", unauthorized.Value);
        }

        [Fact]
        public async Task CreateOrder_ShouldAddNewOrder_WhenPreviousOrdersExist()
        {
            // 1. meglévő rendelés
            var previousOrder = new Order
            {
                UserId = m_testUserId,
                ShippingAddress = "Régi cím",
                BillingAddress = "Régi számla",
                OrderDate = DateTime.UtcNow.AddDays(-2),
                ShippingMethod = ShippingMethod.SzemélyesÁtvétel,
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
                UserId = m_testUserId,
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
                UserId = m_testUserId,
                ShippingAddress = "Új cím",
                BillingAddress = "Új számla",
                ShippingMethod = ShippingMethod.SzemélyesÁtvétel,
            };

            // 3. új rendelés leadása
            await m_controller.CreateOrder(orderRequest);

            var userOrders = await m_context.Orders
                .Where(o => o.UserId == m_testUserId)
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
            var controllerWithoutUser = new OrderController(m_context, m_fakeEmailService)
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
            // Biztosítjuk, hogy nincs rendelés az adott userhez
            var existingOrders = await m_context.Orders
                .Where(o => o.UserId == m_testUserId)
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

        [Fact]
        public async Task GetAllOrders_ShouldReturnForbid_WhenUserIsNotAdmin()
        {
            // beállítjuk a nem-admin usert
            TestHttpContextHelper.AttachUser(m_controller, m_testUserId);

            var result = await m_controller.GetAllOrders();

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task GetAllOrders_ShouldReturnOrders_WhenUserIsAdmin()
        {
            // seed: 2 rendelés különböző userektől
            m_context.Orders.AddRange(
                new Order
                {
                    UserId = m_testUserId,
                    ShippingAddress = "Cím 1",
                    BillingAddress = "Számla 1",
                    ShippingMethod = ShippingMethod.SzemélyesÁtvétel,
                    OrderDate = DateTime.UtcNow.AddDays(-1),
                    OrderItems = new List<OrderItem>
                    {
                        new OrderItem { ItemType="Part", Name="T1", Quantity=1, Price=1000m }
                    }
                },
                new Order
                {
                    UserId = m_orderOwnerUserId,
                    ShippingAddress = "Cím 2",
                    BillingAddress = "Számla 2",
                    ShippingMethod = ShippingMethod.Házhozszállítás,
                    OrderDate = DateTime.UtcNow,
                    OrderItems = new List<OrderItem>
                    {
                        new OrderItem { ItemType="Part", Name="T2", Quantity=2, Price=2000m }
                    }
                }
            );
            await m_context.SaveChangesAsync();

            // beállítjuk az admin usert
            TestHttpContextHelper.AttachUser(m_controller, m_adminUserId);

            var result = await m_controller.GetAllOrders();

            var ok = Assert.IsType<OkObjectResult>(result);
            var orders = Assert.IsAssignableFrom<IEnumerable<Order>>(ok.Value);
            Assert.Equal(2, orders.Count());
        }

        [Fact]
        public async Task DeleteOrder_ShouldReturnForbid_WhenUserIsNotAdmin()
        {
            // nem-admin user
            TestHttpContextHelper.AttachUser(m_controller, m_testUserId);

            var result = await m_controller.DeleteOrder(id: 123);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeleteOrder_ShouldReturnNotFound_WhenOrderDoesNotExist_ForAdmin()
        {
            int id = 999;
            // admin user
            TestHttpContextHelper.AttachUser(m_controller, m_adminUserId);

            var result = await m_controller.DeleteOrder(id);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains($"Nem található rendelés ezzel az ID-vel: {id}", notFound.Value?.ToString());
        }

        [Fact]
        public async Task DeleteOrder_ShouldDeleteAndSendEmail_WhenAdminDeletesExistingOrder()
        {
            // seed: egy rendelés, amely a m_orderOwnerUserId tulajdona
            var order = new Order
            {
                UserId = m_orderOwnerUserId,
                ShippingAddress = "Del cím",
                BillingAddress = "Del számla",
                ShippingMethod = ShippingMethod.SzemélyesÁtvétel,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { ItemType="Part", Name="Del tétel", Quantity=1, Price=1234m }
                }
            };
            m_context.Orders.Add(order);
            await m_context.SaveChangesAsync();

            // admin user
            TestHttpContextHelper.AttachUser(m_controller, m_adminUserId);

            var result = await m_controller.DeleteOrder(order.Id);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("Rendelés törölve", ok.Value?.ToString());

            // a rendelés törölve?
            var stillThere = await m_context.Orders.FindAsync(order.Id);
            Assert.Null(stillThere);

            // e-mail „elküldve” a rendelés tulajának
            Assert.Single(m_fakeEmailService.Sent);
            Assert.Equal("order.owner@example.com", m_fakeEmailService.Sent[0].To);
        }

        [Fact]
        public async Task UpdateOrderStatus_ShouldReturnForbid_WhenUserIsNotAdmin()
        {
            // nem-admin user
            TestHttpContextHelper.AttachUser(m_controller, m_testUserId);

            var result = await m_controller.UpdateOrderStatus(1, new UpdateStatusRequest { NewStatus = "Kiszállítva" });

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task UpdateOrderStatus_ShouldReturnNotFound_WhenOrderMissing_ForAdmin()
        {
            // admin user
            TestHttpContextHelper.AttachUser(m_controller, m_adminUserId);

            var result = await m_controller.UpdateOrderStatus(9999, new UpdateStatusRequest { NewStatus = "Kiszállítva" });

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("A rendelés nem található", notFound.Value?.ToString());
        }

        [Fact]
        public async Task UpdateOrderStatus_ShouldReturnBadRequest_WhenStatusInvalid_ForAdmin()
        {
            // seed: létező rendelés
            var order = new Order
            {
                UserId = m_orderOwnerUserId,
                ShippingAddress = "Upd cím",
                BillingAddress = "Upd számla",
                ShippingMethod = ShippingMethod.SzemélyesÁtvétel
            };
            m_context.Orders.Add(order);
            await m_context.SaveChangesAsync();

            // admin user
            TestHttpContextHelper.AttachUser(m_controller, m_adminUserId);

            var result = await m_controller.UpdateOrderStatus(order.Id, new UpdateStatusRequest { NewStatus = "NINCSILYEN" });

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Érvénytelen rendelés státusz", bad.Value?.ToString());
        }

        [Fact]
        public async Task UpdateOrderStatus_ShouldUpdateAndSendEmail_WhenAdminSetsValidStatus()
        {
            // seed: létező rendelés + tulaj user (m_orderOwnerUserId már seedelve)
            var order = new Order
            {
                UserId = m_orderOwnerUserId,
                ShippingAddress = "Upd2 cím",
                BillingAddress = "Upd2 számla",
                ShippingMethod = ShippingMethod.SzemélyesÁtvétel,
                Status = OrderStatus.Feldolgozás
            };
            m_context.Orders.Add(order);
            await m_context.SaveChangesAsync();

            // admin user
            TestHttpContextHelper.AttachUser(m_controller, m_adminUserId);

            var req = new UpdateStatusRequest { NewStatus = nameof(OrderStatus.Kiszállítva) }; // "Kiszállítva"
            var result = await m_controller.UpdateOrderStatus(order.Id, req);

            var ok = Assert.IsType<OkObjectResult>(result);

            // frissült az állapot?
            var updated = await m_context.Orders.FindAsync(order.Id);
            Assert.NotNull(updated);
            Assert.Equal(OrderStatus.Kiszállítva, updated!.Status);

            // e-mail „elküldve” a rendelés tulajának
            Assert.Single(m_fakeEmailService.Sent);
            Assert.Equal("order.owner@example.com", m_fakeEmailService.Sent[0].To);
        }
    }
}

