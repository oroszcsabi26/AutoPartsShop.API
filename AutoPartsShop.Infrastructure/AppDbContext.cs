using AutoPartsShop.Core.Helpers;
using AutoPartsShop.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace AutoPartsShop.Infrastructure
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> p_options) : base(p_options)
        {
        }
        public DbSet<CarBrand> CarBrands { get; set; }
        public DbSet<CarModel> CarModels { get; set; }
        public DbSet<PartsCategory> PartsCategories { get; set; } 
        public DbSet<Part> Parts { get; set; } 
        public DbSet<EquipmentCategory> EquipmentCategories { get; set; }
        public DbSet<Equipment> Equipments { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<User> Users { get; set; } 
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<EngineVariant> EngineVariants { get; set; }
        public DbSet<PartEngineVariant> PartEngineVariants { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

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
                .HasColumnType("decimal(18,2)"); 

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
                .HasMany(c => c.Items) //"Egy kosárban sok Item lehet (CartItem-ek listája a Cart.Items property-ben)."
                .WithOne(ci => ci.Cart) // Egy kosár-tételnek (CartItem) pontosan egy kosara van (Cart property)."
                .HasForeignKey(ci => ci.CartId) //A kosár-tételben (CartItem) a CartId mező mondja meg, hogy melyik kosárhoz tartozik
                .OnDelete(DeleteBehavior.Cascade); //Ha kitörlöd a kosarat (Cart), akkor az összes hozzá tartozó tételt (CartItem) is töröld automatikusan."

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
                .OnDelete(DeleteBehavior.Cascade); 

            p_modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade); 

            p_modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Part)
                .WithMany()
                .HasForeignKey(oi => oi.PartId)
                .OnDelete(DeleteBehavior.Restrict); 

            p_modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Equipment)
                .WithMany()
                .HasForeignKey(oi => oi.EquipmentId)
                .OnDelete(DeleteBehavior.Restrict); 

            p_modelBuilder.Entity<Order>()
                .Property(o => o.Status)
                .HasConversion<string>(); 

            p_modelBuilder.Entity<Order>()
                .Property(o => o.ShippingMethod)
                .HasConversion<string>();

            p_modelBuilder.Entity<Order>()
                .Property(o => o.PaymentMethod)
                .HasConversion<string>();

            p_modelBuilder.Entity<OrderItem>()
                .Property(o => o.Price)
                .HasColumnType("decimal(18,2)");

            p_modelBuilder.Entity<CartItem>()
                .Property(c => c.Price)
                .HasColumnType("decimal(18,2)");

            p_modelBuilder.Entity<PartEngineVariant>()
                .HasKey(pev => new { pev.PartId, pev.EngineVariantId });

            p_modelBuilder.Entity<PartEngineVariant>()
                .HasOne(pev => pev.Part)
                .WithMany(p => p.PartEngineVariants)
                .HasForeignKey(pev => pev.PartId)
                .OnDelete(DeleteBehavior.Cascade);   

            p_modelBuilder.Entity<PartEngineVariant>()
                .HasOne(pev => pev.EngineVariant)
                .WithMany(ev => ev.PartEngineVariants)
                .HasForeignKey(pev => pev.EngineVariantId)
                .OnDelete(DeleteBehavior.Restrict);

            p_modelBuilder.Entity<PasswordResetToken>()
                .HasIndex(t => t.Token)
                .IsUnique();

            p_modelBuilder.Entity<PasswordResetToken>()
                .HasOne(t => t.User)
                .WithMany(u => u.PasswordResetTokens)        
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
