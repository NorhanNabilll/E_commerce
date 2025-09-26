namespace ECommerce.Models
{
    public class JWTmodel
    {
        public string SecretKey { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int ExpirationInDays { get; set; }
    }
}