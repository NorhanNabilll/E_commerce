namespace ECommerce.Models
{
    // Models/PromoCode.cs
    public class PromoCode
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public PromoCodeType Type { get; set; } // Percentage or Fixed
        public decimal Value { get; set; }
        public decimal? MinimumOrderAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public bool IsActive { get; set; }

        public virtual ICollection<Order> Orders { get; set; }
    }
    public enum PromoCodeType
    {
        Percentage,
        FixedAmount
    }
}
