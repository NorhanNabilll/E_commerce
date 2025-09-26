using System.ComponentModel.DataAnnotations;

namespace ECommerce.DTOs.Authentication
{
    public class loginRequestDto
    {
        [Required]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
