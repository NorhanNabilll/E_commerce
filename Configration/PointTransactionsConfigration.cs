using ECommerce.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Configuration
{
    public class PointTransactionConfiguration : IEntityTypeConfiguration<PointTransaction>
    {
        public void Configure(EntityTypeBuilder<PointTransaction> builder)
        {
            // Table name
            builder.ToTable("PointTransactions");

            // Primary Key
            builder.HasKey(pt => pt.Id);

            // Properties
            builder.Property(pt => pt.UserId)
                   .IsRequired();

            builder.Property(pt => pt.Points)
                   .IsRequired();

            builder.Property(pt => pt.Type)
                   .IsRequired();

            builder.Property(pt => pt.Description)
                   .HasMaxLength(500);

            builder.Property(pt => pt.CreatedDate)
                   .HasDefaultValueSql("GETUTCDATE()");

            // Relationships
            builder.HasOne(pt => pt.User)
                   .WithMany(u => u.PointTransactions) // لازم تضيف ICollection<PointTransaction> في AppUser
                   .HasForeignKey(pt => pt.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(pt => pt.Order)
                   .WithMany(o => o.PointTransactions) // لازم تضيف ICollection<PointTransaction> في Order
                   .HasForeignKey(pt => pt.OrderId)
                   .OnDelete(DeleteBehavior.SetNull); // لو الأوردر اتمسح، المعاملة تفضل بس OrderId يبقى null
        }
    }
}
