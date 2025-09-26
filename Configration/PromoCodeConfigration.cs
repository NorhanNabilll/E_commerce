using ECommerce.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Configuration
{
    public class PromoCodeConfiguration : IEntityTypeConfiguration<PromoCode>
    {
        public void Configure(EntityTypeBuilder<PromoCode> builder)
        {
            // Table name
            builder.ToTable("PromoCodes");

            // Primary Key
            builder.HasKey(p => p.Id);

            // Properties
            builder.Property(p => p.Code)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.Description)
                .HasMaxLength(250);

            builder.Property(p => p.Type)
                .IsRequired();

            builder.Property(p => p.Value)
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            builder.Property(p => p.MinimumOrderAmount)
                .HasColumnType("decimal(10,2)");

            builder.Property(p => p.StartDate)
                .IsRequired();

            builder.Property(p => p.EndDate)
                .IsRequired();

            builder.Property(p => p.UsageLimit)
                .IsRequired(false);

            builder.Property(p => p.UsedCount)
                .HasDefaultValue(0);

            builder.Property(p => p.IsActive)
                .HasDefaultValue(true);

            // Relationships
            builder.HasMany(p => p.Orders)
                .WithOne() // لو عندك navigation في Order اربطيها هنا
                .HasForeignKey("PromoCodeId") // EF هيعمل FK في Order
                .OnDelete(DeleteBehavior.SetNull); // لو اتمسح الكود يفضل الأوردر موجود
        }
    }
}
