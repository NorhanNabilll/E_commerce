using ECommerce.Helpers;

namespace ECommerce.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }

        // Customer info
        public string DeliveryAddress { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }

        // Delivery Address
        public double DeliveryLatitude { get; set; }
        public double DeliveryLongitude { get; set; }


        // Pricing
        public decimal SubTotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal? TipAmount { get; set; } // Optional tip
        public decimal DiscountFromPromoCode { get; set; }
        public decimal DiscountFromPoints { get; set; }
        public decimal TotalAmount { get; set; }


        // Points and Promo
        public string? PromoCodeUsed { get; set; }
        public int? PointsUsed { get; set; }
        public int PointsEarned { get; set; }

        public  AppUser User { get; set; }
        public  ICollection<OrderItem> OrderItems { get; set; }
        public  PromoCode? PromoCode { get; set; }
        public virtual ICollection<PointTransaction> PointTransactions { get; set; }
    }
    // Enums
    public enum OrderStatus
    {
        Pending,
        Processing,
        Shipped,
        OutForDelivery,
        Delivered,
        Cancelled,
        Refunded
    }
}
