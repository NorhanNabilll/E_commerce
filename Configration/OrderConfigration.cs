using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ECommerce.Models;

namespace ECommerce.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            // Table name
            builder.ToTable("Orders");

            // Primary Key
            builder.HasKey(o => o.Id);

            // Relationships
            builder.HasOne(o => o.User)
                    .WithMany(u => u.Orders)
                   .HasForeignKey(o => o.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(o => o.OrderItems)
                   .WithOne(oi => oi.Order)
                   .HasForeignKey(oi => oi.OrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Properties
            builder.Property(o => o.UserId)
                   .IsRequired();

            builder.Property(o => o.CustomerName)
                   .IsRequired()
                   .HasMaxLength(150);

            builder.Property(o => o.CustomerPhone)
                   .IsRequired()
                   .HasMaxLength(20);

            builder.Property(o => o.DeliveryAddress)
                   .IsRequired()
                   .HasMaxLength(250);

            builder.Property(o => o.SubTotal)
                   .HasColumnType("decimal(18,2)");

            builder.Property(o => o.ShippingCost)
                   .HasColumnType("decimal(18,2)");

            builder.Property(o => o.TipAmount)
                   .HasColumnType("decimal(18,2)");

            builder.Property(o => o.DiscountFromPromoCode)
                   .HasColumnType("decimal(18,2)");

            builder.Property(o => o.DiscountFromPoints)
                   .HasColumnType("decimal(18,2)");

            builder.Property(o => o.TotalAmount)
                   .HasColumnType("decimal(18,2)");

            builder.Property(o => o.PromoCodeUsed)
                   .HasMaxLength(50);

            builder.Property(o => o.PointsUsed);

            builder.Property(o => o.PointsEarned);

            builder.Property(o => o.Status)
                   .HasConversion<string>() // store enum as string
                   .HasMaxLength(50);
        }
    }
}
