using AutoMapper;
using Empire.Web.DTOs.Common;
using Empire.Web.DTOs.Brand;
using Empire.Web.DTOs.Category;
using Empire.Web.DTOs.Customer;
using Empire.Web.DTOs.Device;
using Empire.Web.DTOs.Lookup;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Empire.Web.Mapping
{
    public class CommonMappingProfile : Profile
    {
        public CommonMappingProfile()
        {
            // Brand to DropdownItemDto
            CreateMap<BrandDto, DropdownItemDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name));

            // Category to DropdownItemDto
            CreateMap<CategoryDto, DropdownItemDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name));

            // DeviceModel to DropdownItemDto
            CreateMap<DeviceModelDto, DropdownItemDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name));

            // Customer to DropdownItemDto (full name)
            CreateMap<CustomerDto, DropdownItemDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));

            // LookupValue to SelectListItem
            CreateMap<LookupValueDto, SelectListItem>()
                .ForMember(dest => dest.Value, opt => opt.MapFrom(src => src.Value))
                .ForMember(dest => dest.Text, opt => opt.MapFrom(src => src.Description));
        }
    }
}
