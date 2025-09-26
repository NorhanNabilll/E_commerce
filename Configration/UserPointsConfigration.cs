using ECommerce.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Configuration
{
    public class UserPointsConfiguration : IEntityTypeConfiguration<UserPoints>
    {
        public void Configure(EntityTypeBuilder<UserPoints> builder)
        {
            // Table name
            builder.ToTable("UserPoints");

            // Primary Key
            builder.HasKey(up => up.Id);

            // Properties
            builder.Property(up => up.UserId)
                   .IsRequired();

            builder.Property(up => up.TotalPoints)
                   .IsRequired()
                   .HasDefaultValue(0);

            builder.Property(up => up.AvailablePoints)
                   .IsRequired()
                   .HasDefaultValue(0);

            builder.Property(up => up.LastUpdated)
                   .HasDefaultValueSql("GETUTCDATE()");

            // Relationships
            builder.HasOne(up => up.User)
                   .WithMany(u => u.UserPoints)   // لازم تضيف ICollection<UserPoints> في AppUser
                   .HasForeignKey(up => up.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(up => up.PointTransactions)
                   .WithOne(pt => pt.UserPoints)  // لازم تضيف prop UserPoints في PointTransaction
                   .HasForeignKey(pt => pt.UserId)
                   .HasPrincipalKey(up => up.UserId) // لأنه string مش int
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
