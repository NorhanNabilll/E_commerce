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
    [Authorize]
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



                //  Prepare email
                string subject = $"Order Confirmation - #{order.Id}";
                string message = $@"
                                    Dear Customer, 

                                    Thank you for your order!
                                    Your order number is: {order.Id}.
                                    Total Amount: {order.TotalAmount:C}.

                                    Regards,
                                    E-Commerce Team
                                ";
                // Get user Email
                var user = await _userRepository.GetUserByIdAsync(userId);
                //  Send email
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