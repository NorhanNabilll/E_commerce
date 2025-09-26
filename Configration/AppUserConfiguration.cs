
using ECommerce.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace ECommerce.Configration
{
	public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
	{
		public void Configure(EntityTypeBuilder<AppUser> builder)
		{
			
			builder.Property(u => u.FirstName)
				.IsRequired()
				.HasMaxLength(50);

            builder.Property(u => u.FirstName)
				.IsRequired()
				.HasMaxLength(50);

            builder.Property(u => u.CreatedAt)
				.HasColumnType("DATE");

		}
	}
}
