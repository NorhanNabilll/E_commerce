using AutoMapper;
using ECommerce.DTOs;
using ECommerce.Helpers;
using ECommerce.Models;
using ECommerce.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CategoryController> _logger;
        private readonly APIResponse _response;

        public CategoryController(
            ICategoryRepository categoryRepository,
            IMapper mapper,
            ILogger<CategoryController> logger,
            APIResponse response)
        {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
            _logger = logger;
            _response = response;
        }

        // Get all categories
        [HttpGet]
        [SwaggerOperation(Summary = "Get All Categories")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(APIResponse))]
        public async Task<ActionResult<APIResponse>> GetAllCategories()
        {
            try
            {
                _logger.LogInformation("Fetching all categories");
                var categories = await _categoryRepository.GetAllAsync(c => !c.IsDeleted);
                _response.Result = _mapper.Map<List<CategoryDTO>>(categories);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching categories");
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(500, _response);
            }
        }

        // Get category with products
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get Category By Id with Products")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(APIResponse))]
        public async Task<ActionResult<APIResponse>> GetCategory(int id)
        {
            try
            {
                var category = await _categoryRepository.GetCategoryWithProductsAsync(c => c.Id == id && !c.IsDeleted);
                if (category == null)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.NotFound;
                    return NotFound(_response);
                }

                var result = new ResponseCategoryDTO
                {
                    Id = category.Id,
                    Name = category.Name,
                    Products = category.Products.Select(p => new ProductsResponseDTO
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Price = p.Price,
                        ImageUrl = p.ImageUrl,
                        IsNew = p.CreatedAt >= DateTime.UtcNow.AddMonths(-1),
                    }).ToList()
                };

                _response.Result = result;

                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching category");
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(500, _response);
            }
        }

        // Create category
        [HttpPost]
       // [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Create Category")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(APIResponse))]
        public async Task<ActionResult<APIResponse>> CreateCategory([FromBody] CreateCategoryDTO createCategoryDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }

                var category = _mapper.Map<Category>(createCategoryDTO);
                await _categoryRepository.CreateAsync(category);

                _response.Result = _mapper.Map<CategoryDTO>(category);
                _response.StatusCode = HttpStatusCode.Created;
                return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, _response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating category");
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(500, _response);
            }
        }

        // Update category
        [HttpPut]
      //  [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Update Category")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(APIResponse))]
        public async Task<ActionResult<APIResponse>> UpdateCategory([FromBody] UpdateCategoryDTO updateCategoryDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }

                var existingCategory = await _categoryRepository.GetAsync(c => c.Id == updateCategoryDTO.Id && !c.IsDeleted);
                if (existingCategory == null)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.NotFound;
                    return NotFound(_response);
                }

                _mapper.Map(updateCategoryDTO, existingCategory);
                await _categoryRepository.UpdateAsync(existingCategory);

                _response.Result = _mapper.Map<CategoryDTO>(existingCategory);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating category");
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(500, _response);
            }
        }

        // Soft delete category
        [HttpDelete("{id}")]
       // [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Soft Delete Category")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(APIResponse))]
        public async Task<ActionResult<APIResponse>> DeleteCategory(int id)
        {
            try
            {
                var category = await _categoryRepository.GetAsync(c => c.Id == id && !c.IsDeleted);
                if (category == null)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.NotFound;
                    return NotFound(_response);
                }

                await _categoryRepository.SoftDeleteAsync(category);

                _response.StatusCode = HttpStatusCode.NoContent;
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting category");
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(500, _response);
            }
        }
    }
}