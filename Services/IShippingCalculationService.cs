using ECommerce.DTOs;
using ECommerce.Models;

namespace ECommerce.Services
{
    public interface IShippingCalculationService
    {
        Task<decimal> CalculateShippingCostAsync(double destinationLatitude, double destinationLongitude);
        double CalculateDistance(double lat1, double lon1, double lat2, double lon2);
        Task<List<ShippingZone>> GetActiveShippingZonesAsync();
        Task<ShippingEstimateDto> GetShippingEstimateAsync(double destinationLatitude, double destinationLongitude);
        Task<bool> IsDeliveryAvailableAsync(double latitude, double longitude);
    }
}
