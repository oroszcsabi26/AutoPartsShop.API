using AutoPartsShop.Core.Models;
using Microsoft.EntityFrameworkCore;
using AutoPartsShop.Core.Helpers;

namespace AutoPartsShop.Infrastructure
{
    // Az adatbázis-kapcsolati osztály az Entity Framework számára
    public class AppDbContext : DbContext
    {
        // Beállítja az adatbázis kapcsolat konfigurációját
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSet-ek (ezekből lesznek az adatbázis táblák)
        public DbSet<CarBrand> CarBrands { get; set; }
        public DbSet<CarModel> CarModels { get; set; }
        public DbSet<PartsCategory> PartsCategories { get; set; } // Alkatrész kategóriák táblája
        public DbSet<Part> Parts { get; set; } // Alkatrészek táblája
        public DbSet<EquipmentCategory> EquipmentCategories { get; set; }
        public DbSet<Equipment> Equipments { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<User> Users { get; set; } // Felhasználók táblája
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder p_modelBuilder)
        {
            base.OnModelCreating(p_modelBuilder);

            // Egy autómárkához több modell tartozhat (1:N kapcsolat)
            p_modelBuilder.Entity<CarModel>()
                .HasOne(cm => cm.CarBrand)
                .WithMany(cb => cb.CarModels)
                .HasForeignKey(cm => cm.CarBrandId)
                .OnDelete(DeleteBehavior.Cascade);

            // Egy PartsCategory-hoz több Part tartozhat (1:N kapcsolat)
            p_modelBuilder.Entity<Part>()
                .HasOne(p => p.PartsCategory)
                .WithMany(pc => pc.Parts)
                .HasForeignKey(p => p.PartsCategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Alkatrész árának pontos SQL típusa, hogy ne legyen adatvesztés
            p_modelBuilder.Entity<Part>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)"); // 18 számjegy, 2 tizedesjegy

            // Egy EquipmentCategory-hoz több Equipment tartozhat (1:N kapcsolat)
            p_modelBuilder.Entity<Equipment>()
                .HasOne(e => e.EquipmentCategory)
                .WithMany(ec => ec.Equipments)
                .HasForeignKey(e => e.EquipmentCategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            p_modelBuilder.Entity<Equipment>()
                .Property(e => e.Price)
                .HasColumnType("decimal(18,2)");

            p_modelBuilder.Entity<Cart>()
                .HasMany(c => c.Items)
                .WithOne(ci => ci.Cart)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            p_modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Part)
                .WithMany()
                .HasForeignKey(ci => ci.PartId)
                .OnDelete(DeleteBehavior.Restrict);

            p_modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Equipment)
                .WithMany()
                .HasForeignKey(ci => ci.EquipmentId)
                .OnDelete(DeleteBehavior.Restrict);

            p_modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Ha a felhasználó törlődik, a rendelései is törlődnek

            // Egy rendelés több OrderItem-et is tartalmazhat (1:N kapcsolat)
            p_modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade); // Ha a rendelés törlődik, a rendelési tételek is törlődnek

            // Egy OrderItem vagy egy alkatrészre, vagy egy felszerelésre hivatkozik (opcionális)
            p_modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Part)
                .WithMany()
                .HasForeignKey(oi => oi.PartId)
                .OnDelete(DeleteBehavior.Restrict); // Alkatrészek ne törlődjenek a rendelésekkel

            p_modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Equipment)
                .WithMany()
                .HasForeignKey(oi => oi.EquipmentId)
                .OnDelete(DeleteBehavior.Restrict); // Felszerelések ne törlődjenek a rendelésekkel

            p_modelBuilder.Entity<User>().HasData(new User
            {
                Id = -1,
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@autopartsshop.com",
                PasswordHash = PasswordHelper.HashPassword("Admin123!"), // Hash-elt jelszó
                Address = "Admin Street 1",
                ShippingAddress = "Admin Street 1",
                PhoneNumber = "+36123456789",
                IsAdmin = true
            });

            p_modelBuilder.Entity<Order>()
                .Property(o => o.Status)
                .HasConversion<string>(); 

            p_modelBuilder.Entity<Order>()
                .Property(o => o.ShippingMethod)
                .HasConversion<string>();

            p_modelBuilder.Entity<Order>()
                .Property(o => o.PaymentMethod)
                .HasConversion<string>();

            // Itt lehet a decimal típusokra is beállítani például:
            p_modelBuilder.Entity<OrderItem>()
                .Property(o => o.Price)
                .HasColumnType("decimal(18,2)");

            p_modelBuilder.Entity<CartItem>()
                .Property(c => c.Price)
                .HasColumnType("decimal(18,2)");
        }
    }
}
