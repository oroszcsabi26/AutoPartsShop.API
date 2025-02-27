using AutoPartsShop.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsShop.Infrastructure
{
    // Az adatbázis-kapcsolati osztály az Entity Framework számára
    public class AppDbContext : DbContext
    {
        // Beállítja az adatbázis kapcsolat konfigurációját
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // 🔹 DbSet-ek (ezekből lesznek az adatbázis táblák)
        public DbSet<CarBrand> CarBrands { get; set; }
        public DbSet<CarModel> CarModels { get; set; }
        public DbSet<PartsCategory> PartsCategories { get; set; } // Alkatrész kategóriák táblája
        public DbSet<Part> Parts { get; set; } // Alkatrészek táblája
        public DbSet<EquipmentCategory> EquipmentCategories { get; set; }
        public DbSet<Equipment> Equipments { get; set; }

        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<User> Users { get; set; } // Felhasználók táblája

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 🔹 Egy autómárkához több modell tartozhat (1:N kapcsolat)
            modelBuilder.Entity<CarModel>()
                .HasOne(cm => cm.CarBrand)
                .WithMany(cb => cb.CarModels)
                .HasForeignKey(cm => cm.CarBrandId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🔹 Egy PartsCategory-hoz több Part tartozhat (1:N kapcsolat)
            modelBuilder.Entity<Part>()
                .HasOne(p => p.PartsCategory)
                .WithMany(pc => pc.Parts)
                .HasForeignKey(p => p.PartsCategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🔹 Alkatrész árának pontos SQL típusa, hogy ne legyen adatvesztés
            modelBuilder.Entity<Part>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)"); // 18 számjegy, 2 tizedesjegy

            // 🔹 Egy EquipmentCategory-hoz több Equipment tartozhat (1:N kapcsolat)
            modelBuilder.Entity<Equipment>()
                .HasOne(e => e.EquipmentCategory)
                .WithMany(ec => ec.Equipments)
                .HasForeignKey(e => e.EquipmentCategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Equipment>()
                .Property(e => e.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Cart>()
                .HasMany(c => c.Items)
                .WithOne(ci => ci.Cart)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Part)
                .WithMany()
                .HasForeignKey(ci => ci.PartId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Equipment)
                .WithMany()
                .HasForeignKey(ci => ci.EquipmentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
