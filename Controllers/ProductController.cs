using AutoMapper;
using Ecommerce.Data;
using ECommerce.DTOs;
using ECommerce.Helpers;
using ECommerce.Models;
using ECommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;

namespace ECommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductController> _logger;
        private readonly APIResponse _response;
        private readonly IFileService _fileService;

        public ProductController(
            AppDbContext db,
            IMapper mapper,
            ILogger<ProductController> logger,
            APIResponse response,
            IFileService fileService)
        {
            _db = db;
            _mapper = mapper;
            _logger = logger;
            _response = response;
            _fileService = fileService;
        }


        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<APIResponse>> GetAllProducts()
        {
            try
            {
                var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);
                var products = await _db.Products
                    .Select(p => new ProductsResponseDTO
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Price = p.Price,
                        ImageUrl = p.ImageUrl,
                        IsNew = p.CreatedAt >= oneMonthAgo,
                    })
                    .ToListAsync();

                _response.Result = products;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(500, _response);
            }
        }

        // Get product details
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<APIResponse>> GetProduct(int id)
        {
            try
            {
                var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);
                var product = await _db.Products
                  //  .Where(p => p.Id == id && p.ApprovalStatus == "approved")
                    .Select(p => new ProductDetailsDTO
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Details=p.Detailes,
                        Price = p.Price,
                        Stock = p.Stock,
                        ImageUrl = p.ImageUrl,
                        IsNew = p.CreatedAt >= oneMonthAgo,

                    })
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.NotFound;
                    return NotFound(_response);
                }

                _response.Result = product;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(500, _response);
            }
        }


        //[Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<APIResponse>> CreateProduct([FromForm] CreateProductDTO createProductDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                
                var product = _mapper.Map<Product>(createProductDTO);

                product.ImageUrl = await _fileService.UploadFileAsync(createProductDTO.Image);

                await _db.Products.AddAsync(product);
                await _db.SaveChangesAsync();

                _response.Result = _mapper.Map<ProductDetailsDTO>(product);
                _response.StatusCode = HttpStatusCode.Created;
                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, _response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(500, _response);
            }
        }


        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult<APIResponse>> UpdateProduct(int id, [FromForm] UpdateProductDTO updateProductDTO)
        {
            try
            {
                if (id != updateProductDTO.Id)
                    return BadRequest();

                var product = await _db.Products.FindAsync(id);
                if (product == null)
                    return NotFound();

                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole!="Admin")
                    return Forbid();

                _mapper.Map(updateProductDTO, product);
                if (updateProductDTO.Image != null)
                {
                    await _fileService.DeleteFileAsync(product.ImageUrl);
                    product.ImageUrl = await _fileService.UploadFileAsync(updateProductDTO.Image);
                }

                await _db.SaveChangesAsync();

                _response.Result = _mapper.Map<ProductDetailsDTO>(product);
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(500, _response);
            }
        }






    //    [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<APIResponse>> DeleteProduct(int id)
        {
            try
            {
                var product = await _db.Products.FindAsync(id);
                if (product == null)
                    return NotFound();

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isAdmin = User.IsInRole("Admin");

                if (!isAdmin )
                    return Forbid();

                await _fileService.DeleteFileAsync(product.ImageUrl);
                _db.Products.Remove(product);
                await _db.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(500, _response);
            }
        }
    }
}