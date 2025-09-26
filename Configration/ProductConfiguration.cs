using ECommerce.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Configration
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.HasKey(p => p.Id);
            
            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);
                
            builder.Property(p => p.Description)
                .HasColumnType("nvarchar(max)");
                
            builder.Property(p => p.Price)
                .IsRequired()
                .HasColumnType("decimal(10,2)");
                
            builder.Property(p => p.Stock)
                .HasDefaultValue(0);
                
            builder.Property(p => p.ImageUrl)
                .HasMaxLength(500);
 
                
            // Relationships
            builder.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId);

        }
    }
}