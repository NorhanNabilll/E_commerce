using AutoMapper;
using ECommerce.DTOs;
using ECommerce.Models;

namespace ECommerce.Mapping
{
    public class CategoryProfile : Profile
    {
        public CategoryProfile()
        {
            CreateMap<Category, CategoryDTO>().ReverseMap();
            CreateMap<Category, CreateCategoryDTO>().ReverseMap();
            CreateMap<Category, UpdateCategoryDTO>().ReverseMap();

            CreateMap<Product, ProductsResponseDTO>()
                .ForMember(dest => dest.IsNew, opt => opt.MapFrom(src =>
                    src.CreatedAt >= DateTime.UtcNow.AddMonths(-1)));

            CreateMap<Category, ResponseCategoryDTO>()
                .ForMember(dest => dest.Products, opt => opt.MapFrom(src => src.Products));

            CreateMap<CreateProductDTO, Product>();
            CreateMap<Product, ProductDetailsDTO>()
                .ForMember(dest => dest.IsNew, opt => opt.MapFrom(src =>
                    src.CreatedAt >= DateTime.UtcNow.AddMonths(-1))); ;
        }
    }
}
