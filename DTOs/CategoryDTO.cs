using System.ComponentModel.DataAnnotations;

namespace ECommerce.DTOs
{
    public class CategoryDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class CreateCategoryDTO
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
    }

    public class UpdateCategoryDTO
    {
        [Required]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
    }

    public class ResponseCategoryDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<ProductsResponseDTO> Products { get; set; }
    }

    public class ProductsResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public bool IsNew { get; set; }

    }


}