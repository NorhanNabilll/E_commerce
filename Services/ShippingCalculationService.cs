using Ecommerce.Data;
using ECommerce.DTOs;
using ECommerce.Models;
using Google;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ECommerce.Services
{

    public class ShippingCalculationService : IShippingCalculationService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ShippingCalculationService> _logger;


        // Your store/warehouse coordinates (configure in appsettings.json)
        private readonly double _warehouseLatitude;
        private readonly double _warehouseLongitude;
        private readonly decimal _maxDeliveryDistanceKm;

        public ShippingCalculationService(
            AppDbContext context,
            IConfiguration configuration,
            ILogger<ShippingCalculationService> logger
)
        {
            _context = context;
            _configuration = configuration;
            _logger=logger;

            // Load warehouse coordinates from configuration
            _warehouseLatitude = _configuration.GetValue<double>("Shipping:WarehouseLatitude");
            _warehouseLongitude = _configuration.GetValue<double>("Shipping:WarehouseLongitude");
            _maxDeliveryDistanceKm = _configuration.GetValue<decimal>("Shipping:MaxDeliveryDistanceKm");
        }

        public async Task<decimal> CalculateShippingCostAsync(double destinationLatitude, double destinationLongitude)
        {
            try
            {
                // Check if delivery is available to this location
                if (!await IsDeliveryAvailableAsync(destinationLatitude, destinationLongitude))
                {
                    throw new InvalidOperationException("Delivery is not available to this location");
                }

                // Calculate distance from warehouse to destination
                var distanceKm = CalculateDistance(
                    _warehouseLatitude,
                    _warehouseLongitude,
                    destinationLatitude,
                    destinationLongitude);

                // Check if destination falls within any predefined shipping zone
                var shippingZone = await GetShippingZoneForLocationAsync(destinationLatitude, destinationLongitude);

                if (shippingZone != null)
                {
                    // Use zone-based pricing
                    var zoneCost = shippingZone.ShippingCost;
                                 

                    _logger.LogDebug("Calculated zone-based shipping cost: {Cost} for distance {Distance}km in zone {Zone}",
                        zoneCost, distanceKm, shippingZone.Name);

                    return Math.Round(zoneCost);
                }
            
                // Default distance-based calculation if no zone is found
                var baseShippingCost = _configuration.GetValue<decimal>("Shipping:BaseShippingCost", 5.0m);
                var costPerKm = _configuration.GetValue<decimal>("Shipping:CostPerKm", 0.5m);
                var maxShippingCost = _configuration.GetValue<decimal>("Shipping:MaxShippingCost", 50.0m);

                var calculatedCost = baseShippingCost + (decimal)distanceKm * costPerKm;

                // Apply maximum shipping cost limit
                var finalCost = Math.Min(calculatedCost, maxShippingCost);

                _logger.LogDebug("Calculated default shipping cost: {Cost} for distance {Distance}km",
                    finalCost, distanceKm);

                return Math.Round(finalCost);
            }
            catch (InvalidOperationException)
            {
                // throw delivery availability exceptions
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating shipping cost for coordinates {Lat}, {Lng}",
                    destinationLatitude, destinationLongitude);

                // Return default shipping cost on error
                var defaultCost = _configuration.GetValue<decimal>("Shipping:DefaultShippingCost", 10.0m);
                return defaultCost;
            }
        }

        public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine formula to calculate distance between two points 
            const double R = 6371; // Earth's radius in kilometers

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c; // Distance in kilometers
        }

        public async Task<List<ShippingZone>> GetActiveShippingZonesAsync()
        {
            try
            {
                var zones = await _context.ShippingZones
                    .Where(sz => sz.IsActive)
                    .OrderBy(sz => sz.RadiusKm)
                    .ToListAsync();

                return zones;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active shipping zones");
                return new List<ShippingZone>();
            }
        }


        public async Task<ShippingEstimateDto> GetShippingEstimateAsync(double destinationLatitude, double destinationLongitude)
        {
            try
            {
                var distanceKm = CalculateDistance(
                    _warehouseLatitude,
                    _warehouseLongitude,
                    destinationLatitude,
                    destinationLongitude);

                var isAvailable = await IsDeliveryAvailableAsync(destinationLatitude, destinationLongitude);

                if (!isAvailable)
                {
                    return new ShippingEstimateDto
                    {
                        IsAvailable = false,
                        Message = "Delivery is not available to this location",
                        DistanceKm = distanceKm
                    };
                }

                var cost = await CalculateShippingCostAsync(destinationLatitude, destinationLongitude);
                var estimatedDays = CalculateEstimatedDeliveryDays(distanceKm);
                var zone = await GetShippingZoneForLocationAsync(destinationLatitude, destinationLongitude);

                return new ShippingEstimateDto
                {
                    IsAvailable = true,
                    EstimatedCost = cost,
                    DistanceKm = distanceKm,
                    EstimatedDeliveryDays = estimatedDays,
                    EstimatedDeliveryDate = CalculateDeliveryDate(estimatedDays),
                    ShippingZone = zone?.Name,
                    Message = $"Delivery available in {estimatedDays} business days"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shipping estimate for coordinates {Lat}, {Lng}",
                    destinationLatitude, destinationLongitude);

                return new ShippingEstimateDto
                {
                    IsAvailable = false,
                    Message = "Unable to calculate shipping estimate. Please try again.",
                    DistanceKm = 0
                };
            }
        }

        //Not avillable for too long distances
        public async Task<bool> IsDeliveryAvailableAsync(double latitude, double longitude)
        {
            try
            {
                var distance = CalculateDistance(_warehouseLatitude, _warehouseLongitude, latitude, longitude);

                // Check if within maximum delivery distance
                if (distance > (double)_maxDeliveryDistanceKm)
                {
                    _logger.LogDebug("Location {Lat}, {Lng} is outside delivery area. Distance: {Distance}km, Max: {Max}km",
                        latitude, longitude, distance, _maxDeliveryDistanceKm);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking delivery availability for {Lat}, {Lng}", latitude, longitude);
                return false;
            }
        }



        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        private async Task<ShippingZone?> GetShippingZoneForLocationAsync(double latitude, double longitude)
        {
            try
            {
                var activeZones = await GetActiveShippingZonesAsync();

                // Find the smallest zone that contains the location
                foreach (var zone in activeZones.OrderBy(z => z.RadiusKm))
                {
                    var distanceToZoneCenter = CalculateDistance(
                        zone.CenterLatitude,
                        zone.CenterLongitude,
                        latitude,
                        longitude);

                    if (distanceToZoneCenter <= zone.RadiusKm)
                    {
                        return zone;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding shipping zone for location {Lat}, {Lng}", latitude, longitude);
                return null;
            }
        }


        // Simple business logic for delivery estimation
        private int CalculateEstimatedDeliveryDays(double distanceKm)
        {
            return distanceKm switch
            {
                <= 10 => 1, // Same day or next day for very close locations
                <= 25 => 2, // 2 days for nearby locations
                <= 50 => 3, // 3 days for moderate distances
                _ => 5      // 5 days for far locations
            };
        }

        private DateTime CalculateDeliveryDate(int businessDays)
        {
            var deliveryDate = DateTime.UtcNow;
            var addedDays = 0;

            while (addedDays < businessDays)
            {
                deliveryDate = deliveryDate.AddDays(1);

                // Skip weekends (simple implementation)
                if (deliveryDate.DayOfWeek != DayOfWeek.Friday &&
                    deliveryDate.DayOfWeek != DayOfWeek.Saturday)
                {
                    addedDays++;
                }
            }

            return deliveryDate;
        }
    }

}
