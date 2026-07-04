using AutoMapper;
using Empire.Web.DTOs.User;

namespace Empire.Web.Mapping
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            // UserDto to UpdateUserRequestDto
            CreateMap<UserDto, UpdateUserRequestDto>()
                .ForMember(dest => dest.ShopId, opt => opt.MapFrom(src => src.ShopRoles.FirstOrDefault() != null ? src.ShopRoles.FirstOrDefault()!.ShopId : 0))
                .ForMember(dest => dest.RoleId, opt => opt.MapFrom(src => src.ShopRoles.FirstOrDefault() != null ? src.ShopRoles.FirstOrDefault()!.RoleId : 3)); // Default to Technician
        }
    }
}
