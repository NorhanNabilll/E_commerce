namespace ECommerce.DTOs
{
    
    public class ShippingEstimateDto
    {
        public bool IsAvailable { get; set; }
        public decimal EstimatedCost { get; set; }
        public double DistanceKm { get; set; }
        public int EstimatedDeliveryDays { get; set; }
        public DateTime EstimatedDeliveryDate { get; set; }
        public string? ShippingZone { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ShippingCostResult
    {
        public decimal ShippingCost { get; set; }
        public double DistanceKm { get; set; }
        public int EstimatedDeliveryDays { get; set; }
        public DateTime EstimatedDeliveryDate { get; set; }
        public string? ShippingZone { get; set; }

    }

}
