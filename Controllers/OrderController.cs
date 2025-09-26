using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using ECommerce.Services;
using ECommerce.Models;
using Microsoft.EntityFrameworkCore;
using Google;
using Ecommerce.Data;
using ECommerce.DTOs;
using AutoMapper;
using CloudinaryDotNet.Actions;
using ECommerce.Repositories.Interfaces;

namespace ECommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly AppDbContext _context;
        private readonly ILogger<OrderController> _logger;
        private readonly IShippingCalculationService _shippingService;
        private readonly IMapper _mapper;
        private readonly EmailService _emailService;
        private readonly IUserRepository _userRepository;
        public OrderController(
            IOrderService orderService,
            AppDbContext context,
            ILogger<OrderController> logger,
            IShippingCalculationService shippingService,
            IMapper mapper,
            EmailService emailService,
            IUserRepository userRepository)
        {
            _orderService = orderService;
            _context = context;
            _logger = logger;
            _shippingService = shippingService;
            _mapper = mapper;
            _emailService = emailService;
            _userRepository = userRepository;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? throw new UnauthorizedAccessException("User not found in token");
        }

        [HttpPost("calculate")]
        [Authorize]
        public async Task<ActionResult<OrderCalculationResult>> CalculateOrder([FromBody] OrderCalculationRequestDto request)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Get cart items from database
                var cartItems = await GetUserCartItems(userId);
                if (!cartItems.Any())
                {
                    return BadRequest("Cart is empty");
                }

                var calculationRequest = new OrderCalculationRequest
                {
                    UserId = userId,
                    CartItems = cartItems,
                    DeliveryLatitude = request.DeliveryLatitude,
                    DeliveryLongitude = request.DeliveryLongitude,
                    PromoCode = request.PromoCode,
                    PointsToUse = request.PointsToUse,
                    TipAmount = request.TipAmount
                };

                var result = await _orderService.CalculateOrderAsync(calculationRequest);

                if (!result.IsSuccess)
                {
                    return BadRequest(result.ErrorMessage);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating order for user");
                return StatusCode(500, "An error occurred while calculating order");
            }
        }

        [HttpPost("create")]
        [Authorize]
        public async Task<ActionResult<Order>> CreateOrder([FromBody] CreateOrderRequestDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                // Get cart items from database
                var cartItems = await GetUserCartItems(userId);
                if (!cartItems.Any())
                {
                    return BadRequest("Cart is empty");
                }
                var createOrderRequest = new CreateOrderRequest
                {
                    UserId = userId,
                    CartItems = cartItems,
                    DeliveryAddress = request.DeliveryAddress,
                    DeliveryLatitude = request.DeliveryLatitude,
                    DeliveryLongitude = request.DeliveryLongitude,
                    PromoCode = request.PromoCode,
                    PointsToUse = request.PointsToUse,
                    TipAmount = request.TipAmount,
                    CustomerName = request.CustomerName,
                    CustomerPhone = request.CustomerPhone
                };
                var order = await _orderService.CreateOrderAsync(createOrderRequest);
                // Clear user's cart after successful order
                await ClearUserCart(userId);

                // Get user Email
                var user = await _userRepository.GetUserByIdAsync(userId);

                // Prepare  email
                string subject = $"Order Confirmed! #{order.Id} - Thank You for Your Purchase";

                // Build order items HTML
                var orderItemsHtml = string.Join("", cartItems.Select(item => $@"
            <tr>
                <td style='padding: 12px; border-bottom: 1px solid #f0f0f0;'>
                    <strong>{item.ProductName}</strong>
                </td>
                <td style='padding: 12px; border-bottom: 1px solid #f0f0f0; text-align: center;'>
                    {item.Quantity}
                </td>
                <td style='padding: 12px; border-bottom: 1px solid #f0f0f0; text-align: right;'>
                    ${item.UnitPrice:F2}
                </td>
                <td style='padding: 12px; border-bottom: 1px solid #f0f0f0; text-align: right; font-weight: bold;'>
                    ${(item.UnitPrice * item.Quantity):F2}
                </td>
            </tr>"));

                //update this......
                var shippingEstimation = await _shippingService.GetShippingEstimateAsync(request.DeliveryLatitude, request.DeliveryLongitude);
                var estimatedDeliveryDate = shippingEstimation
                    .EstimatedDeliveryDate
                    .ToString("MMMM dd, yyyy");

                string message = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
        </head>
        <body style='margin: 0; padding: 0; background-color: #f8f9fa; font-family: Arial, sans-serif;'>
            <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);'>
                
                <!-- Header -->
                <div style='background: linear-gradient(135deg, #28a745 0%, #20c997 100%); color: white; padding: 30px 20px; text-align: center;'>
                    <h1 style='margin: 0; font-size: 28px; font-weight: bold;'>Order Confirmed!</h1>
                    <p style='margin: 10px 0 0; font-size: 18px; opacity: 0.9;'>Thank you for your purchase</p>
                </div>

                <!-- Content -->
                <div style='padding: 30px 20px;'>
                    
                    <!-- Greeting -->
                    <div style='margin-bottom: 30px;'>
                        <h2 style='color: #333; margin: 0 0 15px; font-size: 24px;'>Hi {user.FirstName ?? "Valued Customer"}!</h2>
                        <p style='color: #666; font-size: 16px; line-height: 1.6; margin: 0;'>
                            Great news! Your order has been confirmed and is being prepared for delivery. Here are your order details:
                        </p>
                    </div>

                    <!-- Order Summary Box -->
                    <div style='background-color: #f8f9fa; border-radius: 8px; padding: 20px; margin-bottom: 30px; border-left: 4px solid #28a745;'>
                        <div style='display: flex; justify-content: space-between; align-items: center; margin-bottom: 15px;'>
                            <h3 style='color: #333; margin: 0; font-size: 20px;'>Order #{order.Id}</h3>
                            <span style='background-color: #28a745; color: white; padding: 6px 12px; border-radius: 20px; font-size: 14px; font-weight: bold;'>Confirmed</span>
                        </div>
                        <p style='color: #666; margin: 0; font-size: 14px;'>
                            <strong>Order Date:</strong> {order.OrderDate:MMMM dd, yyyy 'at' HH:mm}<br>
                            <strong>Estimated Delivery:</strong> {estimatedDeliveryDate}
                        </p>
                    </div>

                    <!-- Order Items -->
                    <div style='margin-bottom: 30px;'>
                        <h3 style='color: #333; margin: 0 0 20px; font-size: 20px; border-bottom: 2px solid #28a745; padding-bottom: 10px;'>Order Items</h3>
                        <table style='width: 100%; border-collapse: collapse; background-color: white;'>
                            <thead>
                                <tr style='background-color: #f8f9fa;'>
                                    <th style='padding: 15px 12px; text-align: left; color: #333; font-weight: bold; border-bottom: 2px solid #dee2e6;'>Item</th>
                                    <th style='padding: 15px 12px; text-align: center; color: #333; font-weight: bold; border-bottom: 2px solid #dee2e6;'>Qty</th>
                                    <th style='padding: 15px 12px; text-align: right; color: #333; font-weight: bold; border-bottom: 2px solid #dee2e6;'>Price</th>
                                    <th style='padding: 15px 12px; text-align: right; color: #333; font-weight: bold; border-bottom: 2px solid #dee2e6;'>Total</th>
                                </tr>
                            </thead>
                            <tbody>
                                {orderItemsHtml}
                            </tbody>
                        </table>
                    </div>

                    <!-- Order Totals -->
                    <div style='background-color: #f8f9fa; border-radius: 8px; padding: 20px; margin-bottom: 30px;'>
                        <div style='display: flex; justify-content: space-between; margin-bottom: 8px;'>
                            <span style='color: #666;'>Subtotal:</span>
                            <span style='color: #333; font-weight: bold;'>${order.SubTotal:F2}</span>
                        </div>
                        <div style='display: flex; justify-content: space-between; margin-bottom: 8px;'>
                            <span style='color: #666;'>Shipping:</span>
                            <span style='color: #333; font-weight: bold;'>${order.ShippingCost:F2}</span>
                        </div>
                        {(order.TipAmount > 0 ? $@"
                        <div style='display: flex; justify-content: space-between; margin-bottom: 8px;'>
                            <span style='color: #666;'>Tip:</span>
                            <span style='color: #333; font-weight: bold;'>${order.TipAmount:F2}</span>
                        </div>" : "")}
                        {(order.DiscountFromPromoCode > 0 ? $@"
                        <div style='display: flex; justify-content: space-between; margin-bottom: 8px;'>
                            <span style='color: #28a745;'>Promo Discount:</span>
                            <span style='color: #28a745; font-weight: bold;'>-${order.DiscountFromPromoCode:F2}</span>
                        </div>" : "")}
                        {(order.DiscountFromPoints > 0 ? $@"
                        <div style='display: flex; justify-content: space-between; margin-bottom: 8px;'>
                            <span style='color: #28a745;'>Points Used:</span>
                            <span style='color: #28a745; font-weight: bold;'>-${order.DiscountFromPoints:F2}</span>
                        </div>" : "")}
                        <hr style='border: none; border-top: 1px solid #dee2e6; margin: 15px 0;'>
                        <div style='display: flex; justify-content: space-between; font-size: 18px;'>
                            <span style='color: #333; font-weight: bold;'>Total:</span>
                            <span style='color: #28a745; font-weight: bold; font-size: 20px;'>${order.TotalAmount:F2}</span>
                        </div>
                    </div>

                    <!-- Delivery Information -->
                    <div style='background-color: #e3f2fd; border-radius: 8px; padding: 20px; margin-bottom: 30px; border-left: 4px solid #2196f3;'>
                        <h3 style='color: #1976d2; margin: 0 0 15px; font-size: 18px;'>Delivery Information</h3>
                        <p style='color: #333; margin: 0; line-height: 1.6;'>
                            <strong>Deliver to:</strong> {order.CustomerName ?? user.FirstName + " " + user.LastName}<br>
                            <strong>Address:</strong> {order.DeliveryAddress}<br>
                            <strong>Phone:</strong> {order.CustomerPhone ?? user.PhoneNumber}
                        </p>
                    </div>

                    {(order.PointsEarned > 0 ? $@"
                    <!-- Points Earned -->
                    <div style='background-color: #fff3cd; border-radius: 8px; padding: 20px; margin-bottom: 30px; border-left: 4px solid #ffc107;'>
                        <h3 style='color: #856404; margin: 0 0 10px; font-size: 18px;'>🎉 Points Earned!</h3>
                        <p style='color: #856404; margin: 0; font-size: 16px;'>
                            You've earned <strong>{order.PointsEarned} points</strong> from this purchase! Use them on your next order for instant savings.
                        </p>
                    </div>" : "")}

                    <!-- What's Next -->
                    <div style='text-align: center; margin-bottom: 30px;'>
                        <h3 style='color: #333; margin: 0 0 20px; font-size: 20px;'>What happens next?</h3>
                        <div style='display: flex; justify-content: space-around; flex-wrap: wrap; gap: 20px;'>
                            <div style='flex: 1; min-width: 150px; text-align: center;'>
                                <div style='background-color: #28a745; color: white; width: 40px; height: 40px; border-radius: 50%; display: flex; align-items: center; justify-content: center; margin: 0 auto 10px; font-weight: bold;'>1</div>
                                <p style='color: #666; margin: 0; font-size: 14px;'><strong>Processing</strong><br>We're preparing your items</p>
                            </div>
                            <div style='flex: 1; min-width: 150px; text-align: center;'>
                                <div style='background-color: #6c757d; color: white; width: 40px; height: 40px; border-radius: 50%; display: flex; align-items: center; justify-content: center; margin: 0 auto 10px; font-weight: bold;'>2</div>
                                <p style='color: #666; margin: 0; font-size: 14px;'><strong>Shipping</strong><br>On its way to you</p>
                            </div>
                            <div style='flex: 1; min-width: 150px; text-align: center;'>
                                <div style='background-color: #6c757d; color: white; width: 40px; height: 40px; border-radius: 50%; display: flex; align-items: center; justify-content: center; margin: 0 auto 10px; font-weight: bold;'>3</div>
                                <p style='color: #666; margin: 0; font-size: 14px;'><strong>Delivered</strong><br>Enjoy your purchase!</p>
                            </div>
                        </div>
                    </div>


                </div>


            </div>
        </body>
        </html>";

                // Send email
                await _emailService.SendEmailAsync(user.Email, subject, message);

                return CreatedAtRoute(
                         "GetOrder",
                          new { id = order.Id },
                          _mapper.Map<OrderDto>(order)
                 );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order for user");
                return StatusCode(500, "An error occurred while creating order");
            }
        }
        [HttpGet("{id}", Name = "GetOrder")]
        [Authorize]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var order = await _orderService.GetOrderByIdAsync(id, userId);

                if (order == null)
                {
                    return NotFound("Order not found");
                }
                //order to retunr
                var ResponseOrder=_mapper.Map<OrderDto>(order);
                return Ok(ResponseOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order {OrderId}", id);
                return StatusCode(500, "An error occurred while retrieving order");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<List<Order>>> GetUserOrders( )
        {
            try
            {
                var userId = GetCurrentUserId();
                var orders = await _orderService.GetUserOrdersAsync(userId);

                //orders to return 
                var ResponseOrders = _mapper.Map<List<OrderDto>>(orders);

                return Ok(ResponseOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders for user");
                return StatusCode(500, "An error occurred while retrieving orders");
            }
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")] 
        public async Task<ActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                var success = await _orderService.UpdateOrderStatusAsync(id, request.Status);

                if (!success)
                {
                    return NotFound("Order not found");
                }

                return Ok("Order status updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for order {OrderId}", id);
                return StatusCode(500, "An error occurred while updating order status");
            }
        }

        [HttpPost("validate-promo")]
        public async Task<ActionResult<PromoValidationResult>> ValidatePromoCode([FromBody] ValidatePromoRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Calculate subtotal from current cart
                var cartItems = await GetUserCartItems(userId);
                var subtotal = cartItems.Sum(ci => ci.UnitPrice * ci.Quantity);

                var isValid = await _orderService.ValidatePromoCodeAsync(request.PromoCode, subtotal);
                var discount = isValid ? await _orderService.CalculatePromoDiscountAsync(request.PromoCode, subtotal) : 0;

                return Ok(new PromoValidationResult
                {
                    IsValid = isValid,
                    DiscountAmount = discount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating promo code");
                return StatusCode(500, "An error occurred while validating promo code");
            }
        }



        // Calculate shipping cost for a location
        [HttpGet("shipping-cost")]
        [Authorize]
        [ProducesResponseType(typeof(ShippingCostResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ShippingCostResult>> CalculateShippingCost(
            [FromQuery] double latitude,
            [FromQuery] double longitude)
        {
            try
            {
                if (latitude == 0 || longitude == 0)
                    return BadRequest(new { message = "Valid latitude and longitude coordinates are required" });

                if (latitude < -90 || latitude > 90)
                    return BadRequest(new { message = "Latitude must be between -90 and 90 degrees" });

                if (longitude < -180 || longitude > 180)
                    return BadRequest(new { message = "Longitude must be between -180 and 180 degrees" });

                var shippingEstimate = await _shippingService.GetShippingEstimateAsync(latitude, longitude);

                if (!shippingEstimate.IsAvailable)
                    return BadRequest(new { message = shippingEstimate.Message });

                return Ok(new ShippingCostResult
                {
                    ShippingCost = shippingEstimate.EstimatedCost,
                    DistanceKm = shippingEstimate.DistanceKm,
                    EstimatedDeliveryDays = shippingEstimate.EstimatedDeliveryDays,
                    EstimatedDeliveryDate = shippingEstimate.EstimatedDeliveryDate,
                    ShippingZone = shippingEstimate.ShippingZone
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating shipping cost for coordinates {Lat}, {Lng}", latitude, longitude);
                return StatusCode(500, new { message = "Error calculating shipping cost" });
            }
        }



        private async Task<List<CartItemDto>> GetUserCartItems(string userId)
        {
            return await _context.Cart_items
                .Include(ci => ci.Product)
                .Where(ci => ci.Cart.UserId == userId)
                .Select(ci => new CartItemDto
                {
                    Id = ci.ProductId,
                    ProductName = ci.Product.Name,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.Product.Price
                })
                .ToListAsync();
        }

        private async Task ClearUserCart(string userId)
        {
            var cartItems = await _context.Cart_items
                .Where(ci => ci.Cart.UserId == userId)
                .ToListAsync();

            _context.Cart_items.RemoveRange(cartItems);
            await _context.SaveChangesAsync();
        }




    }
}