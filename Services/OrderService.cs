namespace ECommerce.Services
{
    // Services/IOrderService.cs
    using System.ComponentModel.DataAnnotations;
    using AutoMapper;
    using Ecommerce.Data;
    using ECommerce.DTOs;
    using ECommerce.Models;
    using Google;
    using Microsoft.EntityFrameworkCore;

    public interface IOrderService
    {
        Task<OrderCalculationResult> CalculateOrderAsync(OrderCalculationRequest request);
        Task<Order> CreateOrderAsync(CreateOrderRequest request);
        Task<Order?> GetOrderByIdAsync(int orderId, string userId);
        Task<List<Order>> GetUserOrdersAsync(string userId);
        Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus status);
        Task<bool> ValidatePromoCodeAsync(string code, decimal orderAmount);
        Task<decimal> CalculatePromoDiscountAsync(string promoCode, decimal orderAmount);
    }

    // Services/OrderService.cs
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _context;
        private readonly IShippingCalculationService _shippingService;
        private readonly IPointsService _pointService;
        private readonly IPointsService _pointsService;
        private readonly ILogger<OrderService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;

        public OrderService(
            AppDbContext context,
            IShippingCalculationService shippingService,
            IPointsService pointsService,
            ILogger<OrderService> logger,
            IConfiguration configuration ,
            IPointsService pointService,
            IMapper mapper)
        {
            _context = context;
            _shippingService = shippingService;
            _pointsService = pointsService;
            _logger = logger;
            _configuration = configuration;
            _pointService = pointService;
            _mapper= mapper;
        }

        public async Task<OrderCalculationResult> CalculateOrderAsync(OrderCalculationRequest request)
        {
            var result = new OrderCalculationResult();

            try
            {
                // Validate cart items exist and get current prices
                var productIds = request.CartItems.Select(ci => ci.Id).ToList();
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, p => p);

                if (products.Count != request.CartItems.Count)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Some products in cart are no longer available";
                    return result;
                }

                // Calculate subtotal using current product prices
                result.SubTotal = 0;
                foreach (var item in request.CartItems)
                {
                    if (products.TryGetValue(item.Id, out var product))
                    {
                        result.SubTotal += product.Price * item.Quantity;
                    }
                }

                // Calculate shipping cost based on location
                result.ShippingCost = await _shippingService.CalculateShippingCostAsync(
                    request.DeliveryLatitude,
                    request.DeliveryLongitude);

                // Apply promo code discount if provided
                if (!string.IsNullOrEmpty(request.PromoCode))
                {
                    var isValidPromo = await ValidatePromoCodeAsync(request.PromoCode, result.SubTotal);
                    if (isValidPromo)
                    {
                        result.PromoCodeDiscount = await CalculatePromoDiscountAsync(
                            request.PromoCode,
                            result.SubTotal);
                    }
                    else
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = "Invalid or expired promo code";
                        return result;
                    }
                }

                // Apply points discount if points are being used
                if (request.PointsToUse > 0)
                {
                    var userPoints = await _pointService.GetUserPointsAsync(request.UserId);
                    if (userPoints.AvailablePoints >= request.PointsToUse)
                    {
                        // 1 point = 1 unit of currency (configurable)
                        var pointValue = _configuration.GetValue<decimal>("Points:EarnRatePerPound", 1.0m);
                        result.PointsDiscount = request.PointsToUse * pointValue;
                    }
                    else
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = $"Insufficient points available. You have {userPoints} points but tried to use {request.PointsToUse}";
                        return result;
                    }
                }

                // Add optional tip
                result.TipAmount = request.TipAmount ?? 0;

                // Calculate total
                var beforeDiscounts = result.SubTotal + result.ShippingCost + result.TipAmount;
                result.TotalAmount = Math.Max(0, beforeDiscounts - result.PromoCodeDiscount - result.PointsDiscount);

                // Calculate points that will be earned (based on final amount spent)
                var pointsEarnRate = _configuration.GetValue<decimal>("Points:EarnRatePerDollar", 1.0m);
                result.PointsToEarn = (int)Math.Floor(result.TotalAmount * pointsEarnRate);

                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating order for user {UserId}", request.UserId);
                result.IsSuccess = false;
                result.ErrorMessage = "An error occurred while calculating your order. Please try again.";
            }

            return result;
        }

        public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // First, calculate the order to ensure all values are correct
                var calculation = await CalculateOrderAsync(new OrderCalculationRequest
                {
                    UserId = request.UserId,
                    CartItems = request.CartItems,
                    DeliveryLatitude = request.DeliveryLatitude,
                    DeliveryLongitude = request.DeliveryLongitude,
                    PromoCode = request.PromoCode,
                    PointsToUse = request.PointsToUse,
                    TipAmount = request.TipAmount
                });

                if (!calculation.IsSuccess)
                    throw new InvalidOperationException(calculation.ErrorMessage);

                // Validate stock availability
                var productIds = request.CartItems.Select(ci => ci.Id).ToList();
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, p => p);

                foreach (var cartItem in request.CartItems)
                {
                    if (products.TryGetValue(cartItem.Id, out var product))
                    {
                        if (product.Stock < cartItem.Quantity)
                        {
                            throw new InvalidOperationException($"Insufficient stock for product {product.Name}. Available: {product.Stock}, Requested: {cartItem.Quantity}");
                        }
                    }
                }

                // Create the order
                var order = new Order
                {
                    UserId = request.UserId,
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.Pending,
                    DeliveryAddress = request.DeliveryAddress,
                    DeliveryLatitude = request.DeliveryLatitude,
                    DeliveryLongitude = request.DeliveryLongitude,
                    SubTotal = calculation.SubTotal,
                    ShippingCost = calculation.ShippingCost,
                    TipAmount = calculation.TipAmount,
                    DiscountFromPromoCode = calculation.PromoCodeDiscount,
                    DiscountFromPoints = calculation.PointsDiscount,
                    TotalAmount = calculation.TotalAmount,
                    PromoCodeUsed = request.PromoCode,
                    PointsUsed = request.PointsToUse,
                    PointsEarned = calculation.PointsToEarn,
                    CustomerName = request.CustomerName,
                    CustomerPhone = request.CustomerPhone

                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Add order items and update stock
                foreach (var cartItem in request.CartItems)
                {
                    if (products.TryGetValue(cartItem.Id, out var product))
                    {
                        var orderItem = new OrderItem
                        {
                            OrderId = order.Id,
                            ProductId = cartItem.Id,
                            Quantity = cartItem.Quantity,
                            Price=cartItem.UnitPrice,
                        };
                        _context.Order_items.Add(orderItem);

                        // Update stock
                        product.Stock -= cartItem.Quantity;
                    }
                }

                // Update promo code usage count
                if (!string.IsNullOrEmpty(request.PromoCode))
                {
                    var promoCode = await _context.PromoCodes
                        .FirstOrDefaultAsync(p => p.Code == request.PromoCode);
                    if (promoCode != null)
                    {
                        promoCode.UsedCount++;
                    }
                }

                // Handle points transactions
                if (request.PointsToUse > 0)
                {
                    var pointsUsed = await _pointsService.UsePointsAsync(request.UserId, request.PointsToUse, order.Id);
                    if (!pointsUsed)
                    {
                        throw new InvalidOperationException("Failed to use points for order");
                    }
                }

                // Add earned points
                if (calculation.PointsToEarn > 0)
                {
                    await _pointsService.AddPointsAsync(request.UserId, calculation.PointsToEarn,
                        $"Points earned from order #{order.Id}", order.Id);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Order {OrderId} created successfully for user {UserId}", order.Id, request.UserId);


                return order;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating order for user {UserId}", request.UserId);
                throw;
            }
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId, string userId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
        //        .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId );
        }

        public async Task<List<Order>> GetUserOrdersAsync(string userId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus status)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null) return false;

                var oldStatus = order.Status;
                order.Status = status;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Order {OrderId} status updated from {OldStatus} to {NewStatus}",
                    orderId, oldStatus, status);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for order {OrderId}", orderId);
                return false;
            }
        }

        public async Task<bool> ValidatePromoCodeAsync(string code, decimal orderAmount)
        {
            try
            {
                var promoCode = await _context.PromoCodes
                    .FirstOrDefaultAsync(p => p.Code.ToUpper() == code.ToUpper() && p.IsActive);

                if (promoCode == null)
                {
                    _logger.LogDebug("Promo code {Code} not found or inactive", code);
                    return false;
                }

                var now = DateTime.UtcNow;
                if (now < promoCode.StartDate || now > promoCode.EndDate)
                {
                    _logger.LogDebug("Promo code {Code} is outside valid date range", code);
                    return false;
                }

                if (promoCode.MinimumOrderAmount.HasValue &&
                    orderAmount < promoCode.MinimumOrderAmount.Value)
                {
                    _logger.LogDebug("Order amount {Amount} is below minimum {Minimum} for promo code {Code}",
                        orderAmount, promoCode.MinimumOrderAmount.Value, code);
                    return false;
                }

                if (promoCode.UsageLimit.HasValue &&
                    promoCode.UsedCount >= promoCode.UsageLimit.Value)
                {
                    _logger.LogDebug("Promo code {Code} usage limit exceeded", code);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating promo code {Code}", code);
                return false;
            }
        }

        public async Task<decimal> CalculatePromoDiscountAsync(string promoCode, decimal orderAmount)
        {
            try
            {
                var promo = await _context.PromoCodes
                    .FirstOrDefaultAsync(p => p.Code.ToUpper() == promoCode.ToUpper() && p.IsActive);

                if (promo == null || !await ValidatePromoCodeAsync(promoCode, orderAmount))
                    return 0;

                if (promo.Type == PromoCodeType.Percentage)
                {
                    return orderAmount * (promo.Value / 100);
                }
                else
                {
                    return Math.Min(promo.Value, orderAmount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating promo discount for code {Code}", promoCode);
                return 0;
            }
        }
    }


}
