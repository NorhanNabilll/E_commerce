using System.ComponentModel.DataAnnotations;
using ECommerce.Models;

namespace ECommerce.DTOs
{
    // request calculation for service(with userid and cart items)
    public class OrderCalculationRequest
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MinLength(1, ErrorMessage = "Cart cannot be empty")]
        public List<CartItemDto> CartItems { get; set; } = new();

        [Required]
        [Range(-90, 90, ErrorMessage = "Invalid latitude")]
        public double DeliveryLatitude { get; set; }

        [Required]
        [Range(-180, 180, ErrorMessage = "Invalid longitude")]
        public double DeliveryLongitude { get; set; }

        public string? PromoCode { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Points to use cannot be negative")]
        public int PointsToUse { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Tip amount cannot be negative")]
        public decimal? TipAmount { get; set; }
    }
    // request calculation for api (without userid and cart items)
    public class OrderCalculationRequestDto
    {
        [Required]
        [Range(-90, 90, ErrorMessage = "Invalid latitude")]
        public double DeliveryLatitude { get; set; }

        [Required]
        [Range(-180, 180, ErrorMessage = "Invalid longitude")]
        public double DeliveryLongitude { get; set; }

        public string? PromoCode { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Points to use cannot be negative")]
        public int PointsToUse { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Tip amount cannot be negative")]
        public decimal? TipAmount { get; set; }
    }

    // response calculation
    public class OrderCalculationResult
    {
        public decimal SubTotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal TipAmount { get; set; }
        public decimal PromoCodeDiscount { get; set; }
        public decimal PointsDiscount { get; set; }
        public decimal TotalAmount { get; set; }
        public int PointsToEarn { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }



    // request create order for api
    public class CreateOrderRequest
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MinLength(1, ErrorMessage = "Cart cannot be empty")]
        public List<CartItemDto> CartItems { get; set; } = new();

        [Required]
        [StringLength(500, ErrorMessage = "Delivery address is too long")]
        public string DeliveryAddress { get; set; } = string.Empty;

        [Required]
        [Range(-90, 90, ErrorMessage = "Invalid latitude")]
        public double DeliveryLatitude { get; set; }

        [Required]
        [Range(-180, 180, ErrorMessage = "Invalid longitude")]
        public double DeliveryLongitude { get; set; }

        public string? PromoCode { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Points to use cannot be negative")]
        public int PointsToUse { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Tip amount cannot be negative")]
        public decimal? TipAmount { get; set; }
        [Required]
        public string CustomerName { get; set; }
        [Required]
        public string CustomerPhone { get; set; }
    }
    // request create order for service
    public class CreateOrderRequestDto
    {
        [Required]
        [StringLength(500, ErrorMessage = "Delivery address is too long")]
        public string DeliveryAddress { get; set; } = string.Empty;

        [Required]
        [Range(-90, 90, ErrorMessage = "Invalid latitude")]
        public double DeliveryLatitude { get; set; }

        [Required]
        [Range(-180, 180, ErrorMessage = "Invalid longitude")]
        public double DeliveryLongitude { get; set; }

        public string? PromoCode { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Points to use cannot be negative")]
        public int PointsToUse { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Tip amount cannot be negative")]
        public decimal? TipAmount { get; set; }
        [Required]
        public string CustomerName { get; set; }
        [Required]
        public string CustomerPhone { get; set; }
    }


    //update order statuse
    public class UpdateOrderStatusRequest
    {
        [Required]
        public OrderStatus Status { get; set; }
    }


    // for retriving orders data
    public class OrderItemDto
    {
        public string ProductName { get; set; } // اسم المنتج للعرض
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public class OrderDto
    {
        public int Id { get; set; } 
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }

        // Customer info
        public string DeliveryAddress { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }

        // Pricing
        public decimal SubTotal { get; set; }
        public decimal? TipAmount { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal DiscountFromPromoCode { get; set; }
        public decimal DiscountFromPoints { get; set; }
        public decimal TotalAmount { get; set; }


        // Items
        public List<OrderItemDto> OrderItems { get; set; }
    }
}
