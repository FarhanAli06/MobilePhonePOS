using AutoMapper;
using Empire.Web.DTOs.Shop;

namespace Empire.Web.Mapping
{
    public class ShopMappingProfile : Profile
    {
        public ShopMappingProfile()
        {
            // ShopDto to UpdateShopRequestDto
            CreateMap<ShopDto, UpdateShopRequestDto>();
        }
    }
}
