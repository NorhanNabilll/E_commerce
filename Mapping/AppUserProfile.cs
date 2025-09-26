
using AutoMapper;
using ECommerce.DTOs.Authentication;
using ECommerce.Models;


namespace ECommerce.Profiles
{
    public class AppUserProfile : Profile
    {
        public AppUserProfile()
        {
            /////----- Register ------////
            // RegisterRequestDto ->  AppUser
            CreateMap<RegisterRequestDto, AppUser>()
                  .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email)) // Map Email to UserName
                 .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateOnly.FromDateTime(DateTime.UtcNow))); // Set CreatedAt to current UTC time



            /////----- Login ------////
            //  AppUser -> UserDTO
            CreateMap<AppUser, UserDTO>();
                

            //  AppUser -> AdminDTO
            CreateMap<AppUser, AdminDTO>();

        }



 

    }

}