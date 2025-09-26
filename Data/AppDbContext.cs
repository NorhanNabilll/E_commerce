
using ECommerce.Configration;
using ECommerce.Configuration;
using ECommerce.Configurations;
using ECommerce.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace Ecommerce.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> Cart_items { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> Order_items { get; set; }
        public DbSet<ShippingZone> ShippingZones { get; set; }
        public DbSet<PromoCode> PromoCodes { get; set; }
        public DbSet<UserPoints> UserPoints { get; set; }
        public DbSet<PointTransaction> PointTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); 


            // Apply configurations
            modelBuilder.ApplyConfiguration(new AppUserConfiguration());
            modelBuilder.ApplyConfiguration(new CategoryConfiguration());
            modelBuilder.ApplyConfiguration(new ProductConfiguration());
            modelBuilder.ApplyConfiguration(new CartConfiguration());
            modelBuilder.ApplyConfiguration(new CartItemConfiguration());
            modelBuilder.ApplyConfiguration(new OrderConfiguration());
            modelBuilder.ApplyConfiguration(new OrderItemConfiguration());
            modelBuilder.ApplyConfiguration(new ShippingZoneConfiguration());
            modelBuilder.ApplyConfiguration(new ProductConfiguration());
            modelBuilder.ApplyConfiguration(new UserPointsConfiguration());
            modelBuilder.ApplyConfiguration(new PointTransactionConfiguration());

        }
    }
}
