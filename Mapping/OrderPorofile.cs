using AutoMapper;
using ECommerce.Models;
using ECommerce.DTOs;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name));

        CreateMap<Order, OrderDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
    }
}
