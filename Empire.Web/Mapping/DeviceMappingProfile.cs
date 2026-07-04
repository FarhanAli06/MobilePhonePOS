using AutoMapper;
using Empire.Web.DTOs.Device;

namespace Empire.Web.Mapping
{
    public class DeviceMappingProfile : Profile
    {
        public DeviceMappingProfile()
        {
            // DeviceDto to UpdateDeviceRequest
            // DeviceDto.DeviceType is string ("Phone", "Laptop", etc.)
            // UpdateDeviceRequest.DeviceType is int (1=Phone, 2=Laptop, 3=Part, 4=Accessories)
            CreateMap<DeviceDto, UpdateDeviceRequest>()
                .ForMember(dest => dest.DeviceType,
                    opt => opt.MapFrom(src => MapDeviceTypeStringToInt(src.DeviceType)))
                .ForMember(dest => dest.Brand, opt => opt.MapFrom(src => src.Brand ?? ""))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category ?? ""))
                .ForMember(dest => dest.Model, opt => opt.MapFrom(src => src.Model ?? ""))
                .ForMember(dest => dest.ModelNumber, opt => opt.MapFrom(src => src.ModelNumber ?? ""))
                .ForMember(dest => dest.IMEISerialNumber, opt => opt.MapFrom(src => src.IMEISerialNumber ?? ""))
                .ForMember(dest => dest.NetworkStatus, opt => opt.MapFrom(src => src.NetworkStatus ?? "Unlocked"))
                .ForMember(dest => dest.ScratchesCondition, opt => opt.MapFrom(src => src.ScratchesCondition ?? "Excellent"))
                .ForMember(dest => dest.Source, opt => opt.MapFrom(src => src.Source ?? ""))
                .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.Notes ?? ""));

            // DeviceSelectionDto to UpdateDeviceRequest (used by ToggleAvailabilityAsync / MarkAsSoldAsync)
            // DeviceSelectionDto.DeviceType is also a string
            CreateMap<DeviceSelectionDto, UpdateDeviceRequest>()
                .ForMember(dest => dest.DeviceType,
                    opt => opt.MapFrom(src => MapDeviceTypeStringToInt(src.DeviceType)))
                .ForMember(dest => dest.Brand, opt => opt.MapFrom(src => src.Brand ?? ""))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category ?? ""))
                .ForMember(dest => dest.Model, opt => opt.MapFrom(src => src.Model ?? ""))
                .ForMember(dest => dest.ModelNumber, opt => opt.MapFrom(src => src.ModelNumber ?? ""))
                .ForMember(dest => dest.IMEISerialNumber, opt => opt.MapFrom(src => src.IMEISerialNumber ?? ""))
                .ForMember(dest => dest.NetworkStatus, opt => opt.MapFrom(src => src.NetworkStatus ?? "Unlocked"))
                .ForMember(dest => dest.ScratchesCondition, opt => opt.MapFrom(src => src.ScratchesCondition ?? "Excellent"))
                .ForMember(dest => dest.Source, opt => opt.MapFrom(src => src.Source ?? ""))
                .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.Notes ?? ""))
                .ForMember(dest => dest.PurchasePrice, opt => opt.MapFrom(src => src.BuyingPrice))
                .ForMember(dest => dest.Status, opt => opt.Ignore());
        }

        /// <summary>
        /// Maps DeviceType string name to int matching the DeviceType enum:
        /// Phone=1, Laptop=2, Part=3, Accessories=4. Defaults to 1 (Phone).
        /// </summary>
        private static int MapDeviceTypeStringToInt(string? deviceType) =>
            (deviceType ?? string.Empty).Trim().ToLower() switch
            {
                "phone"       => 1,
                "laptop"      => 2,
                "part"        => 3,
                "parts"       => 3,
                "accessories" => 4,
                "accessory"   => 4,
                _             => 1
            };
    }
}
