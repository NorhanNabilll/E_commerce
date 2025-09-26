using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

using ECommerce.Models;
using ECommerce.DTOs;
using ECommerce.Helpers;
using Ecommerce.Data;

namespace ECommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        [Authorize]
        public async Task<IActionResult> GetCartSummary()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                return NotFound("Cart not found.");

            var items = cart.CartItems.Select(ci => new CartItemDto
            {
                Id = ci.Id,
                ProductName = ci.Product.Name,
                ProductImage = ci.Product.ImageUrl,
                UnitPrice = ci.Product.Price,
                Quantity = ci.Quantity
            }).ToList();

            var subtotal = items.Sum(i => i.Total);
     /*       var summary = new CartSummaryDto
            {
                Subtotal = subtotal,
                Shipping = shipping,

            };
     */

            return Ok(new CartFullDto
            {
                Items = items,
                Subtotal = subtotal,
            });
        }



        // POST: api/cart/add
        [HttpPost("add")]
        [Authorize]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var userId = GetUserId();

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return NotFound("Product not found");

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var item = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
            if (item != null)
            {
                item.Quantity += quantity;
            }
            else
            {
                cart.CartItems.Add(new CartItem
                {
                    ProductId = productId,
                    Quantity = quantity
                });
            }

            await _context.SaveChangesAsync();

            return Ok("Item added to cart");
        }

        // PUT: api/cart/update
        [HttpPut("update")]
        [Authorize]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            if (quantity < 1)
                return BadRequest("Quantity must be at least 1");

            var userId = GetUserId();

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                return NotFound("Cart not found");

            var item = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
            if (item == null)
                return NotFound("Item not found in cart");

            item.Quantity = quantity;
            await _context.SaveChangesAsync();

            return Ok("Quantity updated");
        }

        // DELETE: api/cart/removeItem
        [Authorize]
        [HttpDelete("removeItem")]
        public async Task<IActionResult> RemoveItem(int productId)
        {
            var userId = GetUserId();

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                return NotFound("Cart not found");

            var item = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
            if (item == null)
                return NotFound("Item not in cart");

            cart.CartItems.Remove(item);
            await _context.SaveChangesAsync();

            return Ok("Item removed");
        }

        // DELETE: api/cart/clear
        [HttpDelete("clear")]
        [Authorize]
        public async Task<IActionResult> ClearCart()
        {
            var userId = GetUserId();

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                return NotFound("Cart not found");

            // Remove all items first
            _context.Cart_items.RemoveRange(cart.CartItems);

            // Then remove the cart itself
            _context.Carts.Remove(cart);

            await _context.SaveChangesAsync();

            return Ok("Cart and its items cleared and deleted.");
        }




        private string GetUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("User ID claim not found");
            return userId;
        }
    }

  

}
