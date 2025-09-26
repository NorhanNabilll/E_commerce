using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace ECommerce.Models
{
	public class AppUser : IdentityUser
	{
		[Required, MaxLength(50)]
		public string FirstName { get; set; }
        [Required, MaxLength(50)]
        public string LastName { get; set; }
        public DateOnly CreatedAt { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);


        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<PointTransaction> PointTransactions { get; set; }
        public ICollection<UserPoints> UserPoints { get; set; }

        public virtual Cart Cart { get; set; }
    }
}
