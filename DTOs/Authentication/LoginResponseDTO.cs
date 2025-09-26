
namespace ECommerce.DTOs.Authentication
{
    public class LoginResponseDto<T>
    {
        public T User { get; set; }
        public string Token { get; set; }
        public string Role { get; set; }

    }
}
