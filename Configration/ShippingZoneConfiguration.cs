using ECommerce.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Configration
{
    public class ShippingZoneConfiguration : IEntityTypeConfiguration<ShippingZone>
    {
        public void Configure(EntityTypeBuilder<ShippingZone> builder)
        {
            // Table name
            builder.ToTable("ShippingZones");

            // Primary Key
            builder.HasKey(sz => sz.Id);

            // Name (Required + Max length)
            builder.Property(sz => sz.Name)
                   .IsRequired()
                   .HasMaxLength(100);

            // Coordinates (Required)
            builder.Property(sz => sz.CenterLatitude)
                   .IsRequired();

            builder.Property(sz => sz.CenterLongitude)
                   .IsRequired();

            // Radius (Default = 10 km)
            builder.Property(sz => sz.RadiusKm)
                   .IsRequired()
                   .HasDefaultValue(10);

            // ShippingCost (Required + Decimal Precision)
            builder.Property(sz => sz.ShippingCost)
                   .IsRequired()
                   .HasColumnType("decimal(10,2)");

            // IsActive (Default = true)
            builder.Property(sz => sz.IsActive)
                   .IsRequired()
                   .HasDefaultValue(true);
        }
    }
}
